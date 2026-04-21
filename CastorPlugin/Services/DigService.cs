using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace CastorPlugin.Services
{
    public sealed class DigService : IDigService
    {

        // Thread-safe dictionary to store the last extraction time for each document
        private static readonly ConcurrentDictionary<string, DateTime> _extractedDocuments = new ConcurrentDictionary<string, DateTime>();
        private readonly ISettingsService _settingsService;

        Action IDigService.CandidatePosted { get; set; }

        public event Action CandidatePosted;

        public DigService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public bool IsAuthenticated => _settingsService.IsLoggedIn;

        public async Task<string> Dig(CancellationToken cancellationToken)
        {
            // SECURITY: Ensure user is logged in before allowing dig
            if (!_settingsService.IsLoggedIn)
            {
                Log.Warning("Dig operation blocked: user not authenticated");
                throw new InvalidOperationException("请先登录后再使用此功能");
            }

            try
            {
                var document = RevitApi.Document;
                var documentId = GetUniqueDocumentId(document);

                Log.Information($"Processing document: {documentId}");

                // Get the document's last modified time
                DateTime lastModified = GetDocumentLastModifiedTime(document);

                // Check if the document needs to be extracted
                if (!ShouldExtractDocument(documentId, lastModified))
                {
                    Log.Information("Document has not changed since last extraction. Skipping.");
                    return documentId;
                }

                var userId = _settingsService.CurrentUser?.Id;
                ApiService familyExtractorApiService = new ApiService(document, documentId, userId);
                
                familyExtractorApiService.CandidatePosted += () => CandidatePosted?.Invoke();

                // Perform the extraction
                var result = await familyExtractorApiService.ExtractFamilies(cancellationToken);

                Log.Information($"Total Checked: {result.TotalChecked}, Posted: {result.Posted}");

                // Update the extracted documents dictionary with the latest extraction time
                _extractedDocuments[documentId] = lastModified;

                return documentId;
            }
            catch (OperationCanceledException)
            {
                Log.Information("Dig operation was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Log.Error($"Error in Dig: {ex.Message}");
                throw;
            }
        }

        private string GetUniqueDocumentId(Autodesk.Revit.DB.Document document)
        {
            string title = document.Title;
            string pathName = document.PathName;
            string documentId = String.Empty;

            // For unsaved documents, PathName will be empty
            if (string.IsNullOrEmpty(pathName))
            {
                documentId =  $"Unsaved_{title}";
            }

            // For saved documents, combine title and full path
            documentId =  $"{title}_{pathName}";

            //convert to sha256
            documentId = Utils.Util.ConvertToSha256(documentId);


            return documentId;

        }

        /// <summary>
        /// Determines whether a document should be extracted based on its last modification time.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document.</param>
        /// <param name="lastModified">The last modification time of the document.</param>
        /// <returns>True if the document should be extracted, false otherwise.</returns>
        private bool ShouldExtractDocument(string documentId, DateTime lastModified)
        {
            // If the document hasn't been extracted before, it should be extracted
            if (!_extractedDocuments.TryGetValue(documentId, out DateTime lastExtracted))
            {
                return true;
            }

            // Extract only if the document has been modified since the last extraction
            return lastModified > lastExtracted;
        }

        /// <summary>
        /// Gets the document's last modified time.
        /// </summary>
        /// <param name="document">Revit document.</param>
        /// <returns>The document's last modified time.</returns>
        private DateTime GetDocumentLastModifiedTime(Autodesk.Revit.DB.Document document)
        {
            if (!string.IsNullOrEmpty(document.PathName))
            {
                // If the document has been saved, use the file's last write time
                return File.GetLastWriteTime(document.PathName);
            }
            else
            {
                // If the document has never been saved, use the current time
                // You might want to store this time for unsaved documents
                return DateTime.Now;
            }
        }

        public async Task<int> FetchCandidateCountAsync()
        {
            try
            {
                var response = await WebServiceBroker.SendGetRequestAsync("/nft-works-candidates/counts");
                if (!string.IsNullOrEmpty(response))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var counts = JsonSerializer.Deserialize<CandidateCounts>(response, options);
                    return counts.TotalCount;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to fetch candidate count: {ex.Message}");
            }
            return 0; // Return 0 or some default value in case of failure
        }

        private class CandidateCounts
        {
            public int TotalCount { get; set; }
            public int AcquiredCount { get; set; }
        }
    }
}
