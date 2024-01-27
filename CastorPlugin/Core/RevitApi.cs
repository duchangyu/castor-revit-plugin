using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CastorPlugin.Core.OpenRevitOleStorage;
using System.IO;
using Document = Autodesk.Revit.DB.Document;

namespace CastorPlugin.Core
{
    /// <summary>
    ///     The class contains wrapping methods for working with the Revit API.
    /// </summary>
    public static class RevitApi
    {
        public static UIApplication UiApplication { get; set; }
        public static Autodesk.Revit.ApplicationServices.Application Application => UiApplication.Application;
        public static UIDocument UiDocument => UiApplication.ActiveUIDocument;
        public static Document Document => UiDocument.Document;
        public static View ActiveView
        {
            get => UiDocument.ActiveView;
            set => UiDocument.ActiveView = value;
        }



        public static void ScanFamilies()
        {
           

            // Create an instance of FamilyExtractor
            FamilyExtractor familyExtractor = new FamilyExtractor(Document);

            // Extract families to the temporary folder
            familyExtractor.ExtractFamilies();

            //string basicInfo =  Tool.GetBasicInformation(tempFolder);
            //string imgbase64 = Tool.GetFamilyPreviewThumbnail(tempFolder);

            TaskDialog.Show("Success", "Families in project are scanned and ready to accupy~~");
     
        }
    }
    
}