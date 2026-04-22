using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]   
    public class AboutCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (RevitApi.UiApplication == null)
            {
                RevitApi.UiApplication = UiApplication;
            }

            Host.GetService<ICastorService>()
                .Show<AboutView>();
               
            
        }
    }
}
