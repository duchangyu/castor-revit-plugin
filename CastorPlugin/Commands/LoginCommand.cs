using System.Windows;
using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.ViewModels.Pages;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;
using Revit.Async;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class LoginCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (RevitApi.UiApplication == null)
            {
                RevitApi.UiApplication = UiApplication;
            }

            RevitTask.Initialize(RevitApi.UiApplication);

            var viewModel = Host.GetService<LoginViewModel>();
            var loginWindow = new Views.Pages.LoginWindow(viewModel);

            // Show as modal dialog (no owner in Revit context)
            loginWindow.ShowDialog();

            // After dialog closes, check if login was successful and refresh ribbon
            var settingsService = Host.GetService<ISettingsService>();
            if (settingsService.IsLoggedIn)
            {
                RibbonController.ReloadPanels(settingsService);
                Host.GetService<ICastorService>().Show<DashboardView>();
            }
        }
    }
}
