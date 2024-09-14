using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CastorPlugin.Services.DTO; 

namespace CastorPlugin.Core
{
    /// <summary>
    /// Represents a fingerprint of a Revit family, containing its properties and methods for extraction and processing.
    /// </summary>
    public class RevitFamilyFingerprint
    {
        // Public properties
        public string AssetType { get; } = "RevitFamily";
        public string AssetName { get; private set; }
        public Dictionary<string, Dictionary<string, string>> Types { get; } = new Dictionary<string, Dictionary<string, string>>();
        public string Thumbnail { get; private set; }
        public string FingerPrintHash { get; private set; }
        public string FingerPrintInJson { get; private set; }

        // Private fields
        private readonly Document _document;

        /// <summary>
        /// Initializes a new instance of the RevitFamilyFingerprint class.
        /// </summary>
        /// <param name="document">The Revit document containing the families to process.</param>
        public RevitFamilyFingerprint(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Extracts all eligible families from the Revit document and processes them.
        /// </summary>
        /// <returns>An IEnumerable of NftWorksCandidates representing the processed families.</returns>
        public IEnumerable<NftWorksCandidates> ExtractFamilies()
        {
            var collector = new FilteredElementCollector(_document).OfClass(typeof(Family));
            
            foreach (Family family in collector)
            {
                if (ShouldProcessFamily(family))
                {
                    ExtractFamilyData(family);
                    yield return CreateNftCandidate();
                }
            }
        }

        /// <summary>
        /// Extracts all relevant data from a given family.
        /// </summary>
        /// <param name="family">The family to process.</param>
        private void ExtractFamilyData(Family family)
        {
            AssetName = family.Name;
            ExtractFamilyParameters(family);
            ExtractThumbnail(family);
            GenerateFingerPrintJson();
            GenerateFingerPrintHash();
        }

        /// <summary>
        /// Extracts parameters from a family and its types.
        /// </summary>
        /// <param name="family">The family to extract parameters from.</param>
        private void ExtractFamilyParameters(Family family)
        {
            using (Document familyDocument = _document.EditFamily(family))
            {
                if (familyDocument.IsFamilyDocument)
                {
                    var mgr = familyDocument.FamilyManager;
                    var fps = GetFamilyParameters(mgr);

                    foreach (FamilyType t in mgr.Types)
                    {
                        ExtractTypeParameters(t, fps, familyDocument);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a thumbnail image for the family.
        /// </summary>
        /// <param name="family">The family to extract the thumbnail from.</param>
        private void ExtractThumbnail(Family family)
        {
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = _document.GetElement(symbolId) as FamilySymbol;
                if (symbol != null)
                {
                    string thumbnail = ExtractFamilySymbolPreviewThumbnail(symbol);
                    if (!string.IsNullOrEmpty(thumbnail))
                    {
                        Thumbnail = thumbnail;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Creates an NftWorksCandidate object from the extracted family data.
        /// </summary>
        /// <returns>An NftWorksCandidate object representing the processed family.</returns>
        private NftWorksCandidates CreateNftCandidate()
        {
            return new NftWorksCandidates
            {
                Name = AssetName,
                Type = 1, // REVIT Family
                FingerPrintHash = FingerPrintHash,
                FingerPrintInJson = FingerPrintInJson,
                Thumbnail = Thumbnail
            };
        }

        /// <summary>
        /// Generates a JSON representation of the family fingerprint.
        /// </summary>
        private void GenerateFingerPrintJson()
        {
            FingerPrintInJson = ToJson();
        }

        /// <summary>
        /// Generates a SHA256 hash of the JSON fingerprint.
        /// </summary>
        private void GenerateFingerPrintHash()
        {
            FingerPrintHash = ConvertToSha256(FingerPrintInJson);
        }

        /// <summary>
        /// Extracts the preview thumbnail image from a family symbol.
        /// </summary>
        /// <param name="familySymbol">The family symbol to extract the thumbnail from.</param>
        /// <returns>A base64-encoded string representation of the thumbnail image.</returns>
        private string ExtractFamilySymbolPreviewThumbnail(FamilySymbol familySymbol)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 200);
            Bitmap image = familySymbol.GetPreviewImage(size);

            if (image != null)
            {
                return ImageToBase64(image);
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Converts a Bitmap image to a base64-encoded string.
        /// </summary>
        /// <param name="image">The Bitmap image to convert.</param>
        /// <returns>A base64-encoded string representation of the image.</returns>
        private string ImageToBase64(Bitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] imageBytes = ms.ToArray();
                return "data:image/png;base64," + Convert.ToBase64String(imageBytes);
            }
        }

        /// <summary>
        /// Converts a string to its SHA256 hash representation.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>The SHA256 hash of the input string.</returns>
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

        /// <summary>
        /// Retrieves a dictionary of family parameters from a FamilyManager.
        /// </summary>
        /// <param name="mgr">The FamilyManager to retrieve parameters from.</param>
        /// <returns>A dictionary of family parameters.</returns>
        private Dictionary<string, FamilyParameter> GetFamilyParameters(FamilyManager mgr)
        {
            var result = new Dictionary<string, FamilyParameter>(StringComparer.OrdinalIgnoreCase);

            try
            {
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
                // Log error or handle exception as needed
            }

            return result;
        }

        /// <summary>
        /// Retrieves the string representation of a family parameter value.
        /// </summary>
        /// <param name="t">The family type.</param>
        /// <param name="fp">The family parameter.</param>
        /// <param name="doc">The document containing the family.</param>
        /// <returns>The string representation of the family parameter value.</returns>
        private static string FamilyParamValueString(FamilyType t, FamilyParameter fp, Document doc)
        {
            string value = t.AsValueString(fp);
            switch (fp.StorageType)
            {
                case StorageType.Double:
                    value = RealString((double)t.AsDouble(fp)) + " (double)";
                    break;
                case StorageType.ElementId:
                    ElementId id = t.AsElementId(fp);
                    Element e = doc.GetElement(id);
                    value = id.ToString() + " (" + ElementDescription(e) + ")";
                    break;
                case StorageType.Integer:
                    value = t.AsInteger(fp).ToString() + " (int)";
                    break;
                case StorageType.String:
                    value = "'" + t.AsString(fp) + "' (string)";
                    break;
            }
            return value;
        }

        /// <summary>
        /// Retrieves a description of an element.
        /// </summary>
        /// <param name="e">The element to describe.</param>
        /// <returns>A string description of the element.</returns>
        private static string ElementDescription(Element e)
        {
            if (null == e) return "<null>";

            FamilyInstance fi = e as FamilyInstance;
            string fn = (null == fi) ? string.Empty : fi.Symbol.Family.Name + " ";
            string cn = (null == e.Category) ? e.GetType().Name : e.Category.Name;

            return string.Format("{0} {1}<{2} {3}>", cn, fn, e.Id.ToString(), e.Name);
        }

        /// <summary>
        /// Formats a double value as a string with a maximum of two decimal places.
        /// </summary>
        /// <param name="a">The double value to format.</param>
        /// <returns>The formatted string representation of the double value.</returns>
        private static string RealString(double a)
        {
            return a.ToString("0.##");
        }

        /// <summary>
        /// Adds a type parameter to the fingerprint.
        /// </summary>
        /// <param name="typeName">The name of the family type.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public void AddTypeParameter(string typeName, string parameterName, string value)
        {
            if (!Types.ContainsKey(typeName))
            {
                Types[typeName] = new Dictionary<string, string>();
            }
            Types[typeName][parameterName] = value;
        }

        /// <summary>
        /// Converts the fingerprint to a JSON string.
        /// </summary>
        /// <returns>The JSON representation of the fingerprint.</returns>
        public string ToJson()
        {
            var fingerPrintDict = new Dictionary<string, object>
            {
                ["Asset"] = new Dictionary<string, object>
                {
                    ["AssetType"] = AssetType,
                    ["AssetName"] = AssetName,
                    ["v1"] = Types
                }
            };

            string json = JsonSerializer.Serialize(fingerPrintDict);
            JsonNode jsonNode = JsonNode.Parse(json);
            JsonNode sortedNode = SortJsonNode(jsonNode);
            return sortedNode.ToJsonString();
        }

        /// <summary>
        /// Sorts a JSON node recursively.
        /// </summary>
        /// <param name="node">The JSON node to sort.</param>
        /// <returns>The sorted JSON node.</returns>
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

            // Return the original node if it's not an object, array, or value
            return node;
        }

        // You can add geometry-related methods here in the future
        public void AddGeometryCharacteristics(/* parameters for geometry data */)
        {
            // Implement logic to add geometry characteristics
        }

        /// <summary>
        /// Determines whether a family should be processed based on certain criteria.
        /// </summary>
        /// <param name="family">The family to check.</param>
        /// <returns>True if the family should be processed, false otherwise.</returns>
        public bool ShouldProcessFamily(Family family)
        {
            BuiltInCategory familyCategory = (BuiltInCategory)family.FamilyCategory.Id.IntegerValue;
            return family.IsEditable 
                && !IsCommonCategory(familyCategory)
                && family.GetFamilySymbolIds().Count > 0;
        }

        /// <summary>
        /// Checks if a given category is considered a common category that should be excluded from processing.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if the category is common, false otherwise.</returns>
        private bool IsCommonCategory(BuiltInCategory category)
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
            }.Contains(category);
        }

        /// <summary>
        /// Extracts parameters from a specific family type.
        /// </summary>
        /// <param name="t">The family type to extract parameters from.</param>
        /// <param name="fps">A dictionary of family parameters.</param>
        /// <param name="familyDocument">The family document.</param>
        private void ExtractTypeParameters(FamilyType t, Dictionary<string, FamilyParameter> fps, Document familyDocument)
        {
            foreach (var fp in fps.Values)
            {
                if (t.HasValue(fp))
                {
                    string value = FamilyParamValueString(t, fp, familyDocument);
                    AddTypeParameter(t.Name, fp.Definition.Name, value);
                }
            }
        }
    }
}