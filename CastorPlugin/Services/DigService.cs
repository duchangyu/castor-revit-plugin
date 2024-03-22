using Autodesk.Revit.UI;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Revit.Async;
using System.Windows;

namespace CastorPlugin.Services
{
    public sealed class DigService(NotificationService notificationService, IWindow window) : IDigService
    {

        public event EventHandler digEventHandler;



        public  void Dig()
        {
            try
            {
                        bool ro = RevitApi.Document.IsReadOnly;

                        // Create an instance of FamilyExtractor
                        FamilyExtractor familyExtractor = new FamilyExtractor(RevitApi.Document);

                        // Extract families to the temporary folder
                        familyExtractor.ExtractFamilies();

                //string basicInfo =  Tool.GetBasicInformation(tempFolder);
                //string imgbase64 = Tool.GetFamilyPreviewThumbnail(tempFolder);
            }

            catch (Exception ex)
            {
                throw ex;
            }




        }

        private void UpdateWindowVisibility(Visibility visibility)
        {
            if (!window.IsLoaded) return;

            window.Visibility = visibility;
        }
    }
}
