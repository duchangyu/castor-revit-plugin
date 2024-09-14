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
        private readonly Document _document;
        private readonly IConfiguration _configuration;
        private readonly HashSet<BuiltInCategory> _commonCategories;

        public FamilyExtractor(Document document, IConfiguration configuration)
        {
            _document = document;
            _configuration = configuration;
            _commonCategories = InitializeCommonCategories();
        }

        public async Task<int> ExtractFamilies()
        {
            var collector = new FilteredElementCollector(_document).OfClass(typeof(Family));
            int count = 0;

            foreach (Family family in collector)
            {
                if (ShouldProcessFamily(family))
                {
                    await ProcessFamily(family);
                    count++;
                }
            }

            return count;
        }

        private bool ShouldProcessFamily(Family family)
        {
            BuiltInCategory familyCategory = (BuiltInCategory)family.FamilyCategory.Id.IntegerValue;
            return family.IsEditable 
                && !_commonCategories.Contains(familyCategory)
                && family.GetFamilySymbolIds().Count > 0;
        }


        private async Task ProcessFamily(Family family)
        {
            string fingerprint = GetFamilyFingerprintInJson(family);
            Log.Information(fingerprint);

            var nftCandidate = new NftWorksCandidates
            {
                Name = family.Name,
                Type = 1, // REVIT Family
                FingerPrintHash = ConvertToSha256(fingerprint),
                FingerPrintInJson = fingerprint,
                Thumbnail = GetFamilyThumbnail(family)
            };

            await PostToServerAsCandidate(nftCandidate);
        }

        private string GetFamilyThumbnail(Family family)
        {
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = _document.GetElement(symbolId) as FamilySymbol;
                if (symbol != null)
                {
                    string thumbnail = ExtractFamilySymbolPreviewThumbnail(symbol);
                    if (!string.IsNullOrEmpty(thumbnail))
                    {
                        return thumbnail; // Return the first valid thumbnail found, in base64 format
                    }
                }
            }

            Log.Warning($"No valid thumbnail found for family: {family.Name}");
            return string.Empty;
        }

        private static async Task PostToServerAsCandidate(NftWorksCandidates nftCandidate)
        {
            // Post the nftCandidate object to server
            try
            {
                string apiUrl = @"http://macbook-pro:3000" + @"/nft-works-candidates"; //_configuration.GetValue<string>("ApiUrl"); // Get API URL from configuration
                string response = await WebServiceBroker.SendPostRequestAsync(apiUrl, nftCandidate);

                if (!string.IsNullOrEmpty(response))
                {
                    // Deserialize the response string to NftWorksCandidates object using System.Text.Json
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase // Add this if your JSON properties use camelCase
                       

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

        private string ExtractFamilySymbolPreviewThumbnail(FamilySymbol familySymbol)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 200);
            Bitmap image = familySymbol.GetPreviewImage(size);

            // Convert the bitmap to base64 string
            if (image != null)
            {
                return ImageToBase64(image);
            }
            else
            {
                return string.Empty;
            }
        }

        private string ImageToBase64(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
            }
        }

        private string GetFamilyFingerprintInJson(Family family)
        {
            using (Document familyDocument = _document.EditFamily(family))
            {
                if (familyDocument.IsFamilyDocument)
                {
                    var familyFingerPrintDict = ExtractFamilyParameters(familyDocument);
                    
                    // Serialize to JSON
                    string json = JsonSerializer.Serialize(familyFingerPrintDict);

                    // Parse the JSON string to a JsonNode
                    JsonNode jsonNode = JsonNode.Parse(json);

                    // Sort the JSON so that the hash is stable
                    JsonNode sortedNode = SortJsonNode(jsonNode);

                    // Serialize the sorted JSON back to a string
                    return sortedNode.ToJsonString();
                }
            }
            return string.Empty;
        }

        private JsonNode SortJsonNode(JsonNode node)
        {
            if (node is JsonObject obj)
            {
                var sortedObj = new JsonObject();
                foreach (var kvp in obj.OrderBy(kvp => kvp.Key))
                {
                    sortedObj.Add(kvp.Key, SortJsonNode(kvp.Value));
                }
                return sortedObj;
            }
            else if (node is JsonArray arr)
            {
                var sortedArr = new JsonArray();
                foreach (var item in arr)
                {
                    sortedArr.Add(SortJsonNode(item));
                }
                return sortedArr;
            }
            else if (node is JsonValue val)
            {
                return JsonValue.Create(val.GetValue<object>());
            }
            return null;
        }

        private Dictionary<string, object> ExtractFamilyParameters(Document familyDocument)
        {
            var familyFingerPrintDict = InitializeFamilyFingerPrintDict(familyDocument.Title);
            var mgr = familyDocument.FamilyManager;
            var fps = GetFamilyParameters(mgr);

            foreach (FamilyType t in mgr.Types)
            {
                ExtractTypeParameters(t, fps, familyFingerPrintDict, familyDocument);
            }

            return familyFingerPrintDict;
        }

        private Dictionary<string, FamilyParameter> GetFamilyParameters(FamilyManager mgr)
        {
            var result = new Dictionary<string, FamilyParameter>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                // Try to use IEnumerable interface if available (newer versions)
                if (mgr.Parameters is IEnumerable<FamilyParameter> parameters)
                {
                    foreach (var fp in parameters)
                    {
                        if (fp != null && fp.Definition != null)
                        {
                            result[fp.Definition.Name] = fp;
                        }
                    }
                }
                else
                {
                    // Fallback for older versions
                    var enumerator = mgr.Parameters.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var fp = enumerator.Current as FamilyParameter;
                        if (fp != null && fp.Definition != null)
                        {
                            result[fp.Definition.Name] = fp;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error in GetFamilyParameters: {ex.Message}");
                // Consider how you want to handle this error. You might want to:
                // - Throw a custom exception
                // - Return an empty dictionary
                // - Continue with a partial result
            }

            return result;
        }

        private Dictionary<string, object> InitializeFamilyFingerPrintDict(string title)
        {
            return DynamicObjectUtil.CreateNestedDictionary(
                "Asset",
                new (string Key, object Value)[]
                {
                    ("AssetType", "RevitFamily"),
                    ("AssetName", title)
                }
            );
        }

        private void ExtractTypeParameters(FamilyType t, Dictionary<string, FamilyParameter> fps, 
            Dictionary<string, object> familyFingerPrintDict, Document familyDocument)
        {
            foreach (var fp in fps.Values)
            {
                if (t.HasValue(fp))
                {
                    string value = FamilyParamValueString(t, fp, familyDocument);
                    DynamicObjectUtil.AddToNestedDictionary(
                        familyFingerPrintDict, 
                        $"Asset.v1.{t.Name}.{fp.Definition.Name}", 
                        value
                    );
                }
            }
        }

        private static string FamilyParamValueString(
              FamilyType t,
              FamilyParameter fp,
              Document doc)
        {
            string value = t.AsValueString(fp);
            switch (fp.StorageType)
            {
                case StorageType.Double:
                    value = RealString(
                      (double)t.AsDouble(fp))
                      + " (double)";
                    break;

                case StorageType.ElementId:
                    ElementId id = t.AsElementId(fp);
                    Element e = doc.GetElement( id);
                    value = id.ToString() + " ("
                      + ElementDescription(e) + ")";
                    break;

                case StorageType.Integer:
                    value = t.AsInteger(fp).ToString()
                      + " (int)";
                    break;

                case StorageType.String:
                    value = "'" + t.AsString(fp)
                      + "' (string)";
                    break;
            }
            return value;
        }

        private static string ElementDescription(Element e)
        {
            if (null == e)
            {
                return "<null>";
            }
            // for a wall, the element name equals the
            // wall type name, which is equivalent to the
            // family name ...
            FamilyInstance fi = e as FamilyInstance;
            string fn = (null == fi)
              ? string.Empty
              : fi.Symbol.Family.Name + " ";

            string cn = (null == e.Category)
              ? e.GetType().Name
              : e.Category.Name;

            return string.Format("{0} {1}<{2} {3}>",
              cn, fn, e.Id.ToString(), e.Name);
        }

        private static string RealString(double a)
        {
            return a.ToString("0.##");
        }

        private string ConvertToSha256(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static HashSet<BuiltInCategory> InitializeCommonCategories()
        {
            return new HashSet<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_CurtainWallPanels,
                BuiltInCategory.OST_CurtainWallMullions,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Railings,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_Ramps,
                BuiltInCategory.OST_ShaftOpening,
                BuiltInCategory.OST_Ceilings
            };
        }
    }
}
