using Autodesk.Revit.UI;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Revit.Async;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace CastorPlugin.Services
{
    public sealed class DigService : IDigService
    {
     
        // Thread-safe dictionary to store the last extraction time for each document
        private static readonly ConcurrentDictionary<string, DateTime> _extractedDocuments = new ConcurrentDictionary<string, DateTime>();

        Action IDigService.CandidatePosted { get; set; }

        public event Action CandidatePosted;

        public DigService( )
        {
            
        }

        public async Task Dig(CancellationToken cancellationToken)
        {
            try
            {
                var document = RevitApi.Document;
                var documentId = document.ProjectInformation.UniqueId;
                
                // Get the document's last modified time
                DateTime lastModified = GetDocumentLastModifiedTime(document);

                // Check if the document needs to be extracted
                if (!ShouldExtractDocument(documentId, lastModified))
                {
                    Log.Information("Document has not changed since last extraction. Skipping.");
                    return;
                }

                bool ro = document.IsReadOnly;

                FamilyExtractor familyExtractor = new FamilyExtractor(document);
                
                familyExtractor.CandidatePosted += () => CandidatePosted?.Invoke();

                // Perform the extraction
                var result = await familyExtractor.ExtractFamilies(cancellationToken);

                Log.Information($"Total Checked: {result.TotalChecked}, Posted: {result.Posted}");

                // Update the extracted documents dictionary with the latest extraction time
                _extractedDocuments[documentId] = lastModified;
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
                return DateTime.Now;
            }
        }
    }
}
