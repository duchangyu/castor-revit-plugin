using Autodesk.Revit.DB;
using CastorPlugin.Utils;
using Nice3point.Revit.Extensions;
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
    /// Handles the extraction and processing of Revit families as NFT candidates.
    /// </summary>
    internal class ApiService
    {
        private readonly RevitFamilyExtractor _familyFingerprintExtractor;
        private readonly string _sourceDocumentId;

        /// <summary>
        /// Event triggered when a new candidate is posted to the server.
        /// </summary>
        public event Action? CandidatePosted;

        /// <summary>
        /// Initializes a new instance of the FamilyExtractor class.
        /// </summary>
        /// <param name="document">The Revit document to extract families from.</param>
        /// <param name="documentId">The ID of the source document.</param>
        public ApiService(Document document, string documentId)
        {
            _familyFingerprintExtractor = new RevitFamilyExtractor(document);
            _sourceDocumentId = documentId;
        }

        /// <summary>
        /// Extracts families from the Revit document and processes them as NFT candidates.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>An ExtractionResult containing the total number of families checked and posted.</returns>
        public async Task<ExtractionResult> ExtractFamilies(CancellationToken cancellationToken)
        {
            int totalChecked = 0;
            int posted = 0;

            // Iterate through each NFT candidate extracted from the Revit families
            foreach (var nftCandidate in _familyFingerprintExtractor.ExtractFamilies())
            {
                totalChecked++;

                // Check if the fingerprint already exists on the server
                bool exists = await FingerprintExists(nftCandidate.FingerPrintHash);
                if (!exists)
                {
                    // If the fingerprint doesn't exist, post it to the server as a new candidate
                    await PostToServerAsCandidate(nftCandidate);
                    posted++;
                    CandidatePosted?.Invoke(); // Notify subscribers that a new candidate has been posted
                }
                else
                {
                    // Log if the candidate already exists on the server
                    Log.Information($"NFT Works Candidate with FingerPrintHash {nftCandidate.FingerPrintHash} already exists on the server.");
                }
            }

            // Return the results of the extraction process
            return new ExtractionResult { TotalChecked = totalChecked, Posted = posted };
        }

        /// <summary>
        /// Checks if a fingerprint already exists on the server.
        /// </summary>
        /// <param name="fingerPrintHash">The hash of the fingerprint to check.</param>
        /// <returns>True if the fingerprint exists, false otherwise.</returns>
        private async Task<bool> FingerprintExists(string fingerPrintHash)
        {
            var payload = new { fingerPrintHash };

            try
            {
                string response = await WebServiceBroker.SendPostRequestAsync("/nft-works-candidates/check-fingerprint", payload);

                if (!string.IsNullOrEmpty(response))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var checkResponse = JsonSerializer.Deserialize<CheckFingerprintResponse>(response, options);

                    if (checkResponse != null && !string.IsNullOrEmpty(checkResponse.FingerprintId))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (JsonException ex)
            {
                Log.Error($"Error deserializing fingerprint check response: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking fingerprint existence: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Posts an NFT candidate to the server.
        /// </summary>
        /// <param name="nftCandidate">The NFT candidate to post.</param>
        private async Task PostToServerAsCandidate(NftWorksCandidates nftCandidate)
        {
            try
            {
                string url = $"/nft-works-candidates?sourceDocumentId={_sourceDocumentId}";
                string response = await WebServiceBroker.SendPostRequestAsync(url, nftCandidate);

                if (!string.IsNullOrEmpty(response))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var createdCandidate = JsonSerializer.Deserialize<NftWorksCandidates>(response, options);

                    if (createdCandidate != null && !string.IsNullOrEmpty(createdCandidate.FingerPrintHash))
                    {
                        Log.Information($"NFT Works Candidate created successfully. ID: {createdCandidate.FingerPrintHash}");
                    }
                    else
                    {
                        Log.Warning("Failed to parse the server response.");
                    }
                }
                else
                {
                    Log.Error("Failed to create NFT Works Candidate. No response from server.");
                }
            }
            catch (JsonException ex)
            {
                Log.Error($"Error deserializing JSON response: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error posting NFT Works Candidate to server: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Data Transfer Object for fingerprint check response.
    /// </summary>
    internal class CheckFingerprintResponse
    {
        public string FingerprintId { get; set; }
        public string Name { get; set; }
        public int ReferenceCount { get; set; }
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
