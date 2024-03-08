using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Utils;
using CastorPlugin.ViewModels;
using CastorPlugin.Views;
using Nice3point.Revit.Toolkit.External;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class Command : ExternalCommand
    {
        public override void Execute()
        {
            if (RevitApi.UiApplication == null)
            {
                RevitApi.UiApplication = UiApplication;
            }

            if (WindowController.Focus<CastorPluginView>()) return;

            var viewModel = new CastorPluginViewModel();
            var view = new CastorPluginView(viewModel);
            WindowController.Show(view, UiApplication.MainWindowHandle);
        }
    }
}