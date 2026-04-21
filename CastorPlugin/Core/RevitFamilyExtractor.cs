using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CastorPlugin.Services.DTO;
using CastorPlugin.Utils;

namespace CastorPlugin.Core
{
    /// <summary>
    /// Represents extracted family data for component registration
    /// </summary>
    public class FamilyExtractionData
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string FingerPrintHash { get; set; }
        public string FingerPrintJson { get; set; }
        public string ThumbnailBase64 { get; set; }
        public GeometryBoundingBoxData GeometryBoundingBox { get; set; }
        public Dictionary<string, Dictionary<string, string>> ParameterCharacteristics { get; set; } = new Dictionary<string, Dictionary<string, string>>();
    }

    /// <summary>
    /// Geometry bounding box data
    /// </summary>
    public class GeometryBoundingBoxData
    {
        public double Width { get; set; }
        public double Depth { get; set; }
        public double Height { get; set; }
    }

    /// <summary>
    /// Represents a fingerprint of a Revit family, containing its properties and methods for extraction and processing.
    /// </summary>
    public class RevitFamilyExtractor
    {
        #region private members
        /// <summary>
        /// Gets the type of asset, which is always "RevitFamily" for this class.
        /// </summary>
        private string _assetType { get; } = "RevitFamily";

        /// <summary>
        /// Gets or sets the name of the asset (family).
        /// </summary>
        private string _assetName { get; set; }

        /// <summary>
        /// Gets or sets the category of the asset (family).
        /// </summary>
        private string _assetCategory { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail image of the family as a base64-encoded string.
        /// </summary>
        private string _thumbnail { get; set; }

        /// <summary>
        /// Gets or sets the SHA256 hash of the fingerprint JSON.
        /// </summary>
        private string _fingerPrintHash { get; set; }

        /// <summary>
        /// Gets or sets the JSON representation of the fingerprint.
        /// </summary>
        private string _fingerPrintInJson { get; set; }

        /// <summary>
        /// Gets a dictionary of parameter characteristics for each family type.
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> _parameterCharacteristics { get; } = new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Gets a dictionary of geometry characteristics for each family symbol.
        /// </summary>
        private Dictionary<string, Dictionary<string, object>> _geometryCharacteristics { get; } = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// Geometry bounding box data
        /// </summary>
        private GeometryBoundingBoxData _boundingBox { get; set; }

        // Private fields
        /// <summary>
        /// The Revit document containing the families to process.
        /// </summary>
        private readonly Document _document;

        #endregion

        /// <summary>
        /// Initializes a new instance of the RevitFamilyFingerprint class.
        /// </summary>
        /// <param name="document">The Revit document containing the families to process.</param>
        public RevitFamilyExtractor(Document document)
        {
            _document = document;
        }

        /// <summary>
        /// Extracts all eligible families from the Revit document and processes them.
        /// </summary>
        /// <returns>An IEnumerable of FamilyExtractionData representing the processed families.</returns>
        public IEnumerable<FamilyExtractionData> ExtractFamilyData()
        {
            var collector = new FilteredElementCollector(_document).OfClass(typeof(Family));

            foreach (Family family in collector)
            {
                FamilyExtractionData data = null;
                try
                {
                    if (!ShouldProcessFamily(family))
                    {
                        continue;
                    }

                    Reset();
                    ExtractFamilyData(family);
                    data = CreateFamilyExtractionData();
                }
                catch (Exception ex)
                {
                    Log.Warning($"族 {family?.Name ?? "<unknown>"} 提取失败，已跳过: {ex.Message}");
                }

                if (data != null)
                {
                    yield return data;
                }
            }
        }

        /// <summary>
        /// Reset extraction data for next family
        /// </summary>
        private void Reset()
        {
            _assetName = null;
            _assetCategory = null;
            _thumbnail = null;
            _fingerPrintHash = null;
            _fingerPrintInJson = null;
            _parameterCharacteristics.Clear();
            _geometryCharacteristics.Clear();
            _boundingBox = null;
        }

        #region private functions

        /// <summary>
        /// Extracts all relevant data from a given family, including geometry characteristics.
        /// </summary>
        /// <param name="family">The family to process.</param>
        private void ExtractFamilyData(Family family)
        {
            _assetName = family.Name;

            // Category participates in the fingerprint, so it must be set before JSON generation.
            if (family.FamilyCategory != null)
            {
                _assetCategory = family.FamilyCategory.Name ?? "未分类";
            }
            else
            {
                _assetCategory = GetCategoryFromFirstSymbol(family) ?? "未分类";
            }

            ExtractFamilyParameters(family);
            ExtractThumbnail(family);
            ExtractGeometryCharacteristics(family);
            GenerateFingerPrintJson();
            GenerateFingerPrintHash();
        }

        /// <summary>
        /// Extracts parameters from a family and its types.
        /// </summary>
        /// <param name="family">The family to extract parameters from.</param>
        private void ExtractFamilyParameters(Family family)
        {
            try
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
            catch (Exception ex)
            {
                Log.Warning($"族 {family.Name} 参数提取失败: {ex.Message}");
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
                        _thumbnail = thumbnail;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a FamilyExtractionData object from the extracted family data.
        /// </summary>
        /// <returns>A FamilyExtractionData object representing the processed family.</returns>
        private FamilyExtractionData CreateFamilyExtractionData()
        {
            return new FamilyExtractionData
            {
                Name = _assetName,
                Category = _assetCategory,
                FingerPrintHash = _fingerPrintHash,
                FingerPrintJson = _fingerPrintInJson,
                ThumbnailBase64 = _thumbnail,
                GeometryBoundingBox = _boundingBox,
                ParameterCharacteristics = new Dictionary<string, Dictionary<string, string>>(_parameterCharacteristics)
            };
        }

        /// <summary>
        /// Generates a JSON representation of the family fingerprint.
        /// </summary>
        private void GenerateFingerPrintJson()
        {
            _fingerPrintInJson = ToJson();
        }

        /// <summary>
        /// Generates a SHA256 hash of the JSON fingerprint.
        /// </summary>
        private void GenerateFingerPrintHash()
        {
            _fingerPrintHash = Util.ConvertToSha256(_fingerPrintInJson);
        }

        /// <summary>
        /// Extracts the preview thumbnail image from a family symbol.
        /// </summary>
        /// <param name="familySymbol">The family symbol to extract the thumbnail from.</param>
        /// <returns>A base64-encoded string representation of the thumbnail image.</returns>
        private string ExtractFamilySymbolPreviewThumbnail(FamilySymbol familySymbol)
        {
            System.Drawing.Size size = new System.Drawing.Size(200, 200);
            using (Bitmap image = familySymbol.GetPreviewImage(size))
            {
                if (image != null)
                {
                    return ImageToBase64(image);
                }
            }

            return string.Empty;
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
                Log.Warning($"获取族参数失败: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Retrieves the string representation of a family parameter value.
        /// </summary>
        private static string FamilyParamValueString(FamilyType t, FamilyParameter fp, Document doc)
        {
            string value = t.AsValueString(fp);
            switch (fp.StorageType)
            {
                case StorageType.Double:
                    value = string.IsNullOrWhiteSpace(value)
                        ? RealString((double)t.AsDouble(fp))
                        : value;
                    break;
                case StorageType.ElementId:
                    ElementId id = t.AsElementId(fp);
                    Element e = doc.GetElement(id);
                    value = e?.Name ?? id.ToString();
                    break;
                case StorageType.Integer:
                    value = t.AsInteger(fp).ToString(CultureInfo.InvariantCulture);
                    break;
                case StorageType.String:
                    value = t.AsString(fp);
                    break;
            }
            return value ?? string.Empty;
        }

        /// <summary>
        /// Retrieves a description of an element.
        /// </summary>
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
        private static string RealString(double a)
        {
            return a.ToString("0.##", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds a type parameter to the fingerprint.
        /// </summary>
        private void AddTypeParameter(string typeName, string parameterName, string value)
        {
            if (!_parameterCharacteristics.ContainsKey(typeName))
            {
                _parameterCharacteristics[typeName] = new Dictionary<string, string>();
            }
            _parameterCharacteristics[typeName][parameterName] = value;
        }

        /// <summary>
        /// Converts the fingerprint to a JSON string.
        /// </summary>
        private string ToJson()
        {
            var fingerPrintDict = new Dictionary<string, object>
            {
                ["Asset"] = new Dictionary<string, object>
                {
                    ["AssetType"] = _assetType,
                    ["AssetName"] = _assetName,
                    ["AssetCategory"] = _assetCategory,
                    ["ParameterCharacteristics"] = _parameterCharacteristics,
                    ["GeometryCharacteristics"] = _geometryCharacteristics
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
            return node;
        }

        /// <summary>
        /// Determines whether a family should be processed based on certain criteria.
        /// </summary>
        public bool ShouldProcessFamily(Family family)
        {
            return family.IsEditable && family.GetFamilySymbolIds().Count > 0;
        }

        private string GetCategoryFromFirstSymbol(Family family)
        {
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                var symbol = _document.GetElement(symbolId) as FamilySymbol;
                var category = symbol?.Category?.Name;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    return category;
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts parameters from a specific family type.
        /// </summary>
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

        /// <summary>
        /// Extracts geometry characteristics from a family.
        /// </summary>
        private void ExtractGeometryCharacteristics(Family family)
        {
            foreach (ElementId symbolId in family.GetFamilySymbolIds())
            {
                FamilySymbol symbol = _document.GetElement(symbolId) as FamilySymbol;
                if (symbol != null)
                {
                    try
                    {
                        GeometryElement geomElem = null;
                        try
                        {
                            geomElem = symbol.get_Geometry(new Options
                            {
                                ComputeReferences = true,
                                DetailLevel = ViewDetailLevel.Fine,
                                IncludeNonVisibleObjects = false
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Unable to get geometry for symbol '{symbol.Name}': {ex.Message}");
                        }

                        if (geomElem != null)
                        {
                            var geometryData = new Dictionary<string, object>();
                            ExtractGeometryData(symbol.Name, geomElem, geometryData);

                            if (geometryData.Count > 0)
                            {
                                _geometryCharacteristics[symbol.Name] = geometryData;
                            }

                            var bbox = geomElem.GetBoundingBox();
                            if (bbox != null)
                            {
                                _boundingBox = new GeometryBoundingBoxData
                                {
                                    Width = Math.Abs(bbox.Max.X - bbox.Min.X),
                                    Depth = Math.Abs(bbox.Max.Y - bbox.Min.Y),
                                    Height = Math.Abs(bbox.Max.Z - bbox.Min.Z)
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error processing symbol '{symbol.Name}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Extracts geometry data from a GeometryElement.
        /// </summary>
        private void ExtractGeometryData(string symbolName, GeometryElement geomElem, Dictionary<string, object> geometryData)
        {
            try
            {
                var bbox = geomElem.GetBoundingBox();
                if (bbox != null)
                {
                    geometryData["BoundingBox"] = ExtractBoundingBoxData(bbox);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error extracting BoundingBox for symbol '{symbolName}': {ex.Message}");
            }

            try { geometryData["SurfaceArea"] = CalculateSurfaceArea(geomElem); }
            catch (Exception ex) { Log.Error($"Error calculating SurfaceArea: {ex.Message}"); }

            try { geometryData["Volume"] = CalculateVolume(geomElem); }
            catch (Exception ex) { Log.Error($"Error calculating Volume: {ex.Message}"); }

            try { geometryData["FaceCount"] = CountFaces(geomElem); }
            catch (Exception ex) { Log.Error($"Error counting Faces: {ex.Message}"); }

            try { geometryData["VertexCount"] = CountVertices(geomElem); }
            catch (Exception ex) { Log.Error($"Error counting Vertices: {ex.Message}"); }
        }

        private Dictionary<string, double> ExtractBoundingBoxData(BoundingBoxXYZ boundingBox)
        {
            return new Dictionary<string, double>
            {
                ["Width"] = Math.Abs(boundingBox.Max.X - boundingBox.Min.X),
                ["Depth"] = Math.Abs(boundingBox.Max.Y - boundingBox.Min.Y),
                ["Height"] = Math.Abs(boundingBox.Max.Z - boundingBox.Min.Z)
            };
        }

        private double CalculateSurfaceArea(GeometryElement geomElem)
        {
            double surfaceArea = 0;
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        surfaceArea += face.Area;
                    }
                }
            }
            return surfaceArea;
        }

        private double CalculateVolume(GeometryElement geomElem)
        {
            double volume = 0;
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    volume += solid.Volume;
                }
            }
            return volume;
        }

        private int CountFaces(GeometryElement geomElem)
        {
            int faceCount = 0;
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    faceCount += solid.Faces.Size;
                }
            }
            return faceCount;
        }

        private int CountVertices(GeometryElement geomElem)
        {
            HashSet<XYZ> uniqueVertices = new HashSet<XYZ>();
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Edge edge in solid.Edges)
                    {
                        uniqueVertices.Add(edge.Tessellate()[0]);
                        uniqueVertices.Add(edge.Tessellate()[1]);
                    }
                }
            }
            return uniqueVertices.Count;
        }
        #endregion
    }
}
