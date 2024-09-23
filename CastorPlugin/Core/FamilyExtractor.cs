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
    internal class FamilyExtractor
    {
        private readonly RevitFamilyFingerprint _familyFingerprint;

        // Event to notify when a new candidate is posted
        public event Action? CandidatePosted;

        public FamilyExtractor(Document document)
        {
            _familyFingerprint = new RevitFamilyFingerprint(document);
        }

        public async Task<ExtractionResult> ExtractFamilies(CancellationToken cancellationToken)
        {
            int totalChecked = 0;
            int posted = 0;

            foreach (var nftCandidate in _familyFingerprint.ExtractFamilies())
            {
                totalChecked++;

                bool exists = await FingerprintExists(nftCandidate.FingerPrintHash);
                if (!exists)
                {
                    await PostToServerAsCandidate(nftCandidate);
                    posted++;
                    CandidatePosted?.Invoke(); // Trigger the event
                }
                else
                {
                    Log.Information($"NFT Works Candidate with FingerPrintHash {nftCandidate.FingerPrintHash} already exists on the server.");
                }
            }

            return new ExtractionResult { TotalChecked = totalChecked, Posted = posted };
        }

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

        private async Task PostToServerAsCandidate(NftWorksCandidates nftCandidate)
        {
            try
            {
                string response = await WebServiceBroker.SendPostRequestAsync("/nft-works-candidates", nftCandidate);

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

    // DTO for fingerprint check response
    internal class CheckFingerprintResponse
    {
        public string FingerprintId { get; set; }
        public string Name { get; set; }
        public int ReferenceCount { get; set; }
    }

    public class ExtractionResult
    {
        public int TotalChecked { get; set; }
        public int Posted { get; set; }
    }
}
