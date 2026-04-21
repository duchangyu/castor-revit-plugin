using Autodesk.Revit.DB;
using CastorPlugin.Utils;
using Nice3point.Revit.Extensions;
using Serilog;
using System.Net.Http;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private const int ThumbnailMinWidthPx = 640;
        private const int ThumbnailMinHeightPx = 360;

        private readonly RevitFamilyExtractor _familyFingerprintExtractor;
        private readonly string _sourceDocumentId;
        private readonly string _userId;

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
        public ApiService(Document document, string documentId, string userId = null)
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
            int failed = 0;
            int skipped = 0;

            Log.Information($"开始挖宝流程，sourceDocumentId: {_sourceDocumentId}, userId: {_userId}");

            // Extract all Revit API data before the first await. After HTTP awaits resume, the
            // continuation may no longer be in a safe Revit API context.
            var families = _familyFingerprintExtractor.ExtractFamilyData().ToList();

            // Iterate through each extracted family and upload pure DTO data to the server.
            foreach (var familyData in families)
            {
                cancellationToken.ThrowIfCancellationRequested();

                totalChecked++;

                // Fire progress event
                ProgressChanged?.Invoke(familyData.Name, totalChecked);

                Log.Information($"检查族 [{totalChecked}]: {familyData.Name}");

                try
                {
                    // Convert to CreateComponentDto and post to server
                    var createDto = await ConvertToCreateComponentDto(familyData, cancellationToken);
                    var result = await PostComponentAsync(createDto, cancellationToken);

                    if (result.Success)
                    {
                        posted++;
                        CandidatePosted?.Invoke();
                        Log.Information($"族 {familyData.Name} 上传成功，当前已上传: {posted}");
                    }
                    else if (result.IsDuplicate)
                    {
                        skipped++;
                        Log.Information($"族 {familyData.Name} 已存在，跳过");
                    }
                    else
                    {
                        failed++;
                        Log.Warning($"族 {familyData.Name} 上传失败: {result.ErrorMessage}");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (HttpRequestException ex) when (IsFatalHttpError(ex))
                {
                    Log.Error($"族 {familyData.Name} 遇到不可继续的 HTTP 错误: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Log.Warning($"族 {familyData.Name} 处理失败: {ex.Message}");
                }
            }

            Log.Information($"挖宝完成，总计检查: {totalChecked}, 新增上传: {posted}, 跳过: {skipped}, 失败: {failed}");
            return new ExtractionResult { TotalChecked = totalChecked, Posted = posted, Skipped = skipped, Failed = failed };
        }

        /// <summary>
        /// Converts family extraction data to CreateComponentDto
        /// </summary>
        private async Task<CreateComponentDto> ConvertToCreateComponentDto(FamilyExtractionData familyData, CancellationToken cancellationToken)
        {
            // Parse fingerprint JSON to extract key parameters
            var keyParameters = new Dictionary<string, object>();
            string sizeGeometry = "";
            string materialOrUse = "";

            if (familyData.ParameterCharacteristics.Count > 0)
            {
                foreach (var typeParameters in familyData.ParameterCharacteristics.Values)
                {
                    foreach (var parameter in typeParameters)
                    {
                        keyParameters[parameter.Key] = parameter.Value;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(familyData.FingerPrintJson))
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

            var category = NormalizeFingerprintField(familyData.Category, "未分类", 2, 80, "类");
            var title = NormalizeRequestField(familyData.Name, $"{category}构件", 3, 120, "构件");

            RemoveForbiddenFingerprintEntries(keyParameters);
            if (keyParameters.Count == 0)
            {
                keyParameters["familyName"] = SanitizeFingerprintText(familyData.Name);
            }

            // Get geometry description
            if (familyData.GeometryBoundingBox != null)
            {
                sizeGeometry = $"{ToMillimeters(familyData.GeometryBoundingBox.Width)}x{ToMillimeters(familyData.GeometryBoundingBox.Depth)}x{ToMillimeters(familyData.GeometryBoundingBox.Height)}mm BoundingBox几何";
            }

            // Get material/usage from key parameters
            if (TryGetMaterialValue(keyParameters, out var materialValue))
            {
                materialOrUse = SanitizeFingerprintText(materialValue);
            }

            var thumbnail = await UploadThumbnailAsync(familyData, cancellationToken);
            var sizeDescription = LimitLength(BuildSizeGeometryDescription(sizeGeometry, title), 1000);
            var materialDescription = LimitLength(BuildMaterialOrUseDescription(materialOrUse, category), 500);
            var parameterDescription = string.Join(", ", keyParameters.Take(10).Select(kv => $"{kv.Key}={kv.Value}"));

            return new CreateComponentDto
            {
                Title = title,
                Category = category,
                Description = LimitLength(BuildDescription(title, parameterDescription), 2000),
                KeyAttributes = keyParameters,
                Fingerprint = new StructuredFingerprintDto
                {
                    ComponentCategory = category,
                    KeyParameters = keyParameters,
                    SizeGeometryDescription = sizeDescription,
                    MaterialOrIntendedUse = materialDescription
                },
                Thumbnail = thumbnail,
                AttributionDeclarationAccepted = true,
                NonLegalNoticeAccepted = true
            };
        }

        private async Task<ThumbnailInputDto> UploadThumbnailAsync(FamilyExtractionData familyData, CancellationToken cancellationToken)
        {
            var thumbnailBytes = TryDecodeDataUri(familyData.ThumbnailBase64);
            if (thumbnailBytes.Length == 0)
            {
                thumbnailBytes = CreateFallbackThumbnail(familyData.Name, familyData.Category ?? "未分类");
            }

            thumbnailBytes = EnsureThumbnailMinimumSize(
                thumbnailBytes,
                familyData.Name,
                familyData.Category ?? "未分类");

            var fileName = $"{familyData.FingerPrintHash ?? Guid.NewGuid().ToString("N")}.png";
            var response = await WebServiceBroker.SendMultipartFileRequestAsync(
                "/thumbnails/upload",
                thumbnailBytes,
                fileName,
                "image/png",
                cancellationToken);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var uploadResponse = JsonSerializer.Deserialize<ThumbnailUploadResponse>(response, options);
            if (string.IsNullOrWhiteSpace(uploadResponse?.StorageKey))
            {
                throw new InvalidOperationException($"族 {familyData.Name} 缩略图上传响应缺少 storageKey");
            }

            return new ThumbnailInputDto
            {
                StorageKey = uploadResponse.StorageKey,
                MimeType = string.IsNullOrWhiteSpace(uploadResponse.MimeType) ? "image/png" : uploadResponse.MimeType,
                FileSizeBytes = uploadResponse.FileSizeBytes > 0 ? uploadResponse.FileSizeBytes : thumbnailBytes.LongLength,
                WidthPx = uploadResponse.WidthPx > 0 ? uploadResponse.WidthPx : ThumbnailMinWidthPx,
                HeightPx = uploadResponse.HeightPx > 0 ? uploadResponse.HeightPx : ThumbnailMinHeightPx
            };
        }

        private static byte[] TryDecodeDataUri(string dataUri)
        {
            if (string.IsNullOrWhiteSpace(dataUri))
            {
                return Array.Empty<byte>();
            }

            try
            {
                var commaIndex = dataUri.IndexOf(',');
                var base64 = commaIndex >= 0 ? dataUri.Substring(commaIndex + 1) : dataUri;
                return Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                Log.Warning($"缩略图 Base64 解码失败: {ex.Message}");
                return Array.Empty<byte>();
            }
        }

        private static byte[] EnsureThumbnailMinimumSize(byte[] imageBytes, string familyName, string category)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return CreateFallbackThumbnail(familyName, category);
            }

            try
            {
                using (var input = new MemoryStream(imageBytes))
                using (var source = System.Drawing.Image.FromStream(input))
                {
                    if (source.Width >= ThumbnailMinWidthPx && source.Height >= ThumbnailMinHeightPx)
                    {
                        return imageBytes;
                    }

                    using (var bitmap = new Bitmap(ThumbnailMinWidthPx, ThumbnailMinHeightPx))
                    using (var graphics = Graphics.FromImage(bitmap))
                    using (var backgroundBrush = new SolidBrush(System.Drawing.Color.FromArgb(244, 247, 250)))
                    using (var stream = new MemoryStream())
                    {
                        graphics.Clear(System.Drawing.Color.White);
                        graphics.FillRectangle(backgroundBrush, 0, 0, ThumbnailMinWidthPx, ThumbnailMinHeightPx);
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                        const int padding = 20;
                        var scale = Math.Min(
                            (ThumbnailMinWidthPx - padding * 2) / (double)source.Width,
                            (ThumbnailMinHeightPx - padding * 2) / (double)source.Height);
                        var drawWidth = (int)Math.Round(source.Width * scale);
                        var drawHeight = (int)Math.Round(source.Height * scale);
                        var drawX = (ThumbnailMinWidthPx - drawWidth) / 2;
                        var drawY = (ThumbnailMinHeightPx - drawHeight) / 2;

                        graphics.DrawImage(source, drawX, drawY, drawWidth, drawHeight);
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"缩略图尺寸规范化失败，改用兜底缩略图: {ex.Message}");
                return CreateFallbackThumbnail(familyName, category);
            }
        }

        private static byte[] CreateFallbackThumbnail(string familyName, string category)
        {
            using (var bitmap = new Bitmap(ThumbnailMinWidthPx, ThumbnailMinHeightPx))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var backgroundBrush = new SolidBrush(System.Drawing.Color.FromArgb(244, 247, 250)))
            using (var accentBrush = new SolidBrush(System.Drawing.Color.FromArgb(43, 102, 214)))
            using (var textBrush = new SolidBrush(System.Drawing.Color.FromArgb(32, 36, 42)))
            using (var titleFont = new Font(SystemFonts.DefaultFont.FontFamily, 22, FontStyle.Bold))
            using (var bodyFont = new Font(SystemFonts.DefaultFont.FontFamily, 16, FontStyle.Regular))
            using (var stream = new MemoryStream())
            {
                graphics.Clear(System.Drawing.Color.White);
                graphics.FillRectangle(backgroundBrush, 0, 0, ThumbnailMinWidthPx, ThumbnailMinHeightPx);
                graphics.FillRectangle(accentBrush, 0, 0, ThumbnailMinWidthPx, 12);
                graphics.DrawString(TrimForThumbnail(category), titleFont, textBrush, new RectangleF(40, 92, 560, 48));
                graphics.DrawString(TrimForThumbnail(familyName), bodyFont, textBrush, new RectangleF(40, 154, 560, 120));
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private static int ToMillimeters(double feet)
        {
            return (int)Math.Round(feet * 304.8);
        }

        private static bool TryGetMaterialValue(Dictionary<string, object> keyParameters, out string materialValue)
        {
            foreach (var parameter in keyParameters)
            {
                if (parameter.Value == null)
                {
                    continue;
                }

                if (parameter.Key.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    parameter.Key.Contains("材质") ||
                    parameter.Key.Contains("材料"))
                {
                    materialValue = parameter.Value.ToString();
                    return !string.IsNullOrWhiteSpace(materialValue);
                }
            }

            materialValue = null;
            return false;
        }

        private static void RemoveForbiddenFingerprintEntries(Dictionary<string, object> keyParameters)
        {
            var forbiddenKeys = keyParameters
                .Where(parameter => ContainsForbiddenFingerprintText(parameter.Key) ||
                                    ContainsForbiddenFingerprintText(parameter.Value?.ToString()))
                .Select(parameter => parameter.Key)
                .ToList();

            foreach (var key in forbiddenKeys)
            {
                keyParameters.Remove(key);
            }
        }

        private static string SanitizeFingerprintText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return ContainsForbiddenFingerprintText(value) ? "参数化构件信息" : value.Trim();
        }

        private static string NormalizeFingerprintField(string value, string fallback, int minLength, int maxLength, string padSuffix)
        {
            return NormalizeRequestField(SanitizeFingerprintText(value), fallback, minLength, maxLength, padSuffix);
        }

        private static string NormalizeRequestField(string value, string fallback, int minLength, int maxLength, string padSuffix)
        {
            var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                text = fallback;
            }

            var suffix = string.IsNullOrEmpty(padSuffix) ? "构件" : padSuffix;
            while (text.Length < minLength)
            {
                text += suffix;
            }

            return LimitLength(text, maxLength);
        }

        private static string LimitLength(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private static bool ContainsForbiddenFingerprintText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var forbiddenTokens = new[] { ".rfa", ".rvt", ".ifc", ".dwg", "族文件", "项目文件", "源文件", "原始族文件" };
            return forbiddenTokens.Any(token => value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string BuildDescription(string familyName, string parameterDescription)
        {
            var description = string.IsNullOrWhiteSpace(parameterDescription)
                ? $"由 Revit 族自动生成：{familyName}。已提取族类别、类型参数和几何包围盒信息用于构件登记。"
                : $"由 Revit 族自动生成：{familyName}。参数：{parameterDescription}。";

            return description.Length >= 20
                ? description
                : $"{description} 已提取构件登记所需的基础信息。";
        }

        private static string BuildSizeGeometryDescription(string sizeGeometry, string familyName)
        {
            if (!string.IsNullOrWhiteSpace(sizeGeometry) && sizeGeometry.Length >= 10)
            {
                return sizeGeometry;
            }

            var safeName = SanitizeFingerprintText(familyName);
            return string.IsNullOrWhiteSpace(safeName)
                ? "参数化构件几何信息，未能读取包围盒尺寸"
                : $"{safeName} 参数化构件几何信息，未能读取包围盒尺寸";
        }

        private static string BuildMaterialOrUseDescription(string materialOrUse, string category)
        {
            if (!string.IsNullOrWhiteSpace(materialOrUse) && materialOrUse.Trim().Length >= 2)
            {
                return materialOrUse.Trim();
            }

            return string.IsNullOrWhiteSpace(category)
                ? "通用构件用途"
                : $"{category} 构件用途";
        }

        private static string TrimForThumbnail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= 24 ? value : value.Substring(0, 24);
        }

        private static bool IsFatalHttpError(HttpRequestException ex)
        {
            return ex.Message.Contains("HTTP 401") ||
                   ex.Message.Contains("HTTP 403") ||
                   ex.Message.Contains("HTTP 429");
        }

        /// <summary>
        /// Posts a component to the server.
        /// </summary>
        private async Task<PostComponentResult> PostComponentAsync(CreateComponentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                Log.Information($"上传构件到服务器: {dto.Title}, Category: {dto.Category}");
                string response = await WebServiceBroker.SendPostRequestAsync("/components", dto, cancellationToken);

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
                if (IsFatalHttpError(ex))
                {
                    throw;
                }

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
    /// Response when a thumbnail is uploaded
    /// </summary>
    internal class ThumbnailUploadResponse
    {
        public string StorageKey { get; set; }
        public string Url { get; set; }
        public string MimeType { get; set; }
        public long FileSizeBytes { get; set; }
        public int WidthPx { get; set; }
        public int HeightPx { get; set; }
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

        /// <summary>
        /// The number of families skipped because the server reported an existing or duplicate component.
        /// </summary>
        public int Skipped { get; set; }

        /// <summary>
        /// The number of families that failed extraction, thumbnail upload, or component registration.
        /// </summary>
        public int Failed { get; set; }
    }
}
