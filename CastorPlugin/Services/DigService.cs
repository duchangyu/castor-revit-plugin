using Autodesk.Revit.UI;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Revit.Async;
using System.Windows;

namespace CastorPlugin.Services
{
    public sealed class DigService : IDigService
    {
        private readonly IConfiguration _configuration;

        public DigService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Dig()
        {
            try
            {
                        bool ro = RevitApi.Document.IsReadOnly;

                        // Create an instance of FamilyExtractor
                        FamilyExtractor familyExtractor = new FamilyExtractor(RevitApi.Document, _configuration);

                        // Extract families to the temporary folder
                       await familyExtractor.ExtractFamilies();

                //string basicInfo =  Tool.GetBasicInformation(tempFolder);
                //string imgbase64 = Tool.GetFamilyPreviewThumbnail(tempFolder);
            }

            catch (Exception ex)
            {
                throw ex;
            }




        }

  
    }
}
