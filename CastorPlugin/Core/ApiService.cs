using Autodesk.Revit.DB;
using CastorPlugin.Utils;
using Nice3point.Revit.Extensions;
using Serilog;
using System.Net.Http;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using CastorPlugin.Services.DTO;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CastorPlugin.Core
{
    /// <summary>
    /// Handles the extraction and processing of Revit families as component candidates.
    /// </summary>
    internal class ApiService
    {
        private readonly RevitFamilyExtractor _familyFingerprintExtractor;
        private readonly string _sourceDocumentId;
        private readonly int? _userId;

        /// <summary>
        /// Event triggered when a new candidate is posted to the server.
        /// </summary>
        public event Action? CandidatePosted;

        /// <summary>
        /// Event triggered when a family is being processed. Parameters: (familyName, checkedCount)
        /// </summary>
        public event Action<string, int>? ProgressChanged;

        /// <summary>
        /// Initializes a new instance of the FamilyExtractor class.
        /// </summary>
        /// <param name="document">The Revit document to extract families from.</param>
        /// <param name="documentId">The ID of the source document.</param>
        /// <param name="userId">The ID of the logged-in user (optional).</param>
        public ApiService(Document document, string documentId, int? userId = null)
        {
            _familyFingerprintExtractor = new RevitFamilyExtractor(document);
            _sourceDocumentId = documentId;
            _userId = userId;
        }

        /// <summary>
        /// Extracts families from the Revit document and processes them as components.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>An ExtractionResult containing the total number of families checked and posted.</returns>
        public async Task<ExtractionResult> ExtractFamilies(CancellationToken cancellationToken)
        {
            int totalChecked = 0;
            int posted = 0;

            Log.Information($"开始挖宝流程，sourceDocumentId: {_sourceDocumentId}, userId: {_userId}");

            // Iterate through each family extracted from the Revit document
            foreach (var familyData in _familyFingerprintExtractor.ExtractFamilyData())
            {
                cancellationToken.ThrowIfCancellationRequested();

                totalChecked++;

                // Fire progress event
                ProgressChanged?.Invoke(familyData.Name, totalChecked);

                Log.Information($"检查族 [{totalChecked}]: {familyData.Name}");

                // Convert to CreateComponentDto and post to server
                var createDto = ConvertToCreateComponentDto(familyData);
                var result = await PostComponentAsync(createDto);

                if (result.Success)
                {
                    posted++;
                    CandidatePosted?.Invoke();
                    Log.Information($"族 {familyData.Name} 上传成功，当前已上传: {posted}");
                }
                else if (result.IsDuplicate)
                {
                    Log.Information($"族 {familyData.Name} 已存在，跳过");
                }
                else
                {
                    Log.Warning($"族 {familyData.Name} 上传失败: {result.ErrorMessage}");
                }
            }

            Log.Information($"挖宝完成，总计检查: {totalChecked}, 新增上传: {posted}");
            return new ExtractionResult { TotalChecked = totalChecked, Posted = posted };
        }

        /// <summary>
        /// Converts family extraction data to CreateComponentDto
        /// </summary>
        private CreateComponentDto ConvertToCreateComponentDto(FamilyExtractionData familyData)
        {
            // Parse fingerprint JSON to extract key parameters
            var keyParameters = new Dictionary<string, object>();
            string sizeGeometry = "";
            string materialOrUse = "";

            if (!string.IsNullOrEmpty(familyData.FingerPrintJson))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(familyData.FingerPrintJson);
                    if (jsonDoc.RootElement.TryGetProperty("Asset", out var asset))
                    {
                        if (asset.TryGetProperty("ParameterCharacteristics", out var paramsChars))
                        {
                            // Flatten parameter characteristics
                            foreach (var typeProp in paramsChars.EnumerateObject())
                            {
                                foreach (var param in typeProp.Value.EnumerateObject())
                                {
                                    keyParameters[param.Name] = param.Value.ToString();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"解析指纹JSON失败: {ex.Message}");
                }
            }

            // Get category from family
            string category = familyData.Category ?? "未分类";

            // Get geometry description
            if (familyData.GeometryBoundingBox != null)
            {
                sizeGeometry = $"{familyData.GeometryBoundingBox.Width}x{familyData.GeometryBoundingBox.Depth}x{familyData.GeometryBoundingBox.Height} BoundingBox几何";
            }

            // Get material/usage from key parameters
            if (keyParameters.ContainsKey("material"))
            {
                materialOrUse = keyParameters["material"].ToString();
            }

            return new CreateComponentDto
            {
                Title = familyData.Name,
                Category = category,
                Description = $"由 Revit 族自动生成：{familyData.Name}。参数：{string.Join(", ", keyParameters.Take(10).Select(kv => $"{kv.Key}={kv.Value}"))}。",
                KeyAttributes = keyParameters,
                Fingerprint = new StructuredFingerprintDto
                {
                    ComponentCategory = category,
                    KeyParameters = keyParameters,
                    SizeGeometryDescription = sizeGeometry.Length > 10 ? sizeGeometry : $"{familyData.Name} 参数化几何",
                    MaterialOrIntendedUse = materialOrUse.Length > 2 ? materialOrUse : category
                },
                Thumbnail = new ThumbnailInputDto
                {
                    StorageKey = "", // Will be uploaded separately if needed
                    MimeType = "image/png",
                    FileSizeBytes = familyData.ThumbnailBase64?.Length ?? 0,
                    WidthPx = 200,
                    HeightPx = 200
                },
                AttributionDeclarationAccepted = true,
                NonLegalNoticeAccepted = true
            };
        }

        /// <summary>
        /// Posts a component to the server.
        /// </summary>
        private async Task<PostComponentResult> PostComponentAsync(CreateComponentDto dto)
        {
            try
            {
                Log.Information($"上传构件到服务器: {dto.Title}, Category: {dto.Category}");
                string response = await WebServiceBroker.SendPostRequestAsync("/components", dto);

                if (!string.IsNullOrEmpty(response))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var componentResponse = JsonSerializer.Deserialize<ComponentCreatedResponse>(response, options);

                    if (componentResponse != null && !string.IsNullOrEmpty(componentResponse.Id))
                    {
                        Log.Information($"构件 {dto.Title} 上传成功，服务器返回 ID: {componentResponse.Id}");
                        return new PostComponentResult { Success = true, ComponentId = componentResponse.Id };
                    }
                }

                Log.Warning($"构件 {dto.Title} 上传后解析响应失败: {response}");
                return new PostComponentResult { Success = false, ErrorMessage = "解析响应失败" };
            }
            catch (HttpRequestException ex)
            {
                // Check if it's a conflict (duplicate)
                if (ex.Message.Contains("409") || ex.Message.Contains("Conflict"))
                {
                    Log.Information($"构件 {dto.Title} 已存在 (409 Conflict)");
                    return new PostComponentResult { Success = false, IsDuplicate = true };
                }
                Log.Error($"构件 {dto.Title} 上传HTTP错误: {ex.Message}");
                return new PostComponentResult { Success = false, ErrorMessage = ex.Message };
            }
            catch (JsonException ex)
            {
                Log.Error($"构件 {dto.Title} 解析JSON响应失败: {ex.Message}");
                return new PostComponentResult { Success = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                Log.Error($"构件 {dto.Title} 上传异常: {ex.Message}");
                return new PostComponentResult { Success = false, ErrorMessage = ex.Message };
            }
        }
    }

    /// <summary>
    /// Result of posting a component
    /// </summary>
    internal class PostComponentResult
    {
        public bool Success { get; set; }
        public bool IsDuplicate { get; set; }
        public string ComponentId { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response when a component is created
    /// </summary>
    internal class ComponentCreatedResponse
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
    }

    /// <summary>
    /// Represents the result of the family extraction process.
    /// </summary>
    public class ExtractionResult
    {
        /// <summary>
        /// The total number of families checked during extraction.
        /// </summary>
        public int TotalChecked { get; set; }

        /// <summary>
        /// The number of families successfully posted as new candidates.
        /// </summary>
        public int Posted { get; set; }
    }
}
