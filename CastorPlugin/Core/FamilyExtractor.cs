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

namespace CastorPlugin.Core
{
    internal class FamilyExtractor
    {
        private readonly RevitFamilyFingerprint _familyFingerprint;
        private readonly string _apiUrl;

        public FamilyExtractor(Document document, IConfiguration configuration)
        {
            _familyFingerprint = new RevitFamilyFingerprint(document);
            _apiUrl = configuration.GetValue<string>("ApiUrl") ?? @"http://macbook-pro:3000/nft-works-candidates";
        }

        public async Task<int> ExtractFamilies()
        {
            int count = 0;
            foreach (var nftCandidate in _familyFingerprint.ExtractFamilies())
            {
                await PostToServerAsCandidate(nftCandidate);
                count++;
            }
            return count;
        }

        private async Task PostToServerAsCandidate(NftWorksCandidates nftCandidate)
        {
            try
            {
                string response = await WebServiceBroker.SendPostRequestAsync(_apiUrl, nftCandidate);

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
}
