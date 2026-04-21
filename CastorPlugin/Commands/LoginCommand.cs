using System.Windows;
using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.ViewModels.Pages;
using Nice3point.Revit.Toolkit.External;
using Revit.Async;
using Wpf.Ui.Controls;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class LoginCommand : ExternalCommand
    {
        public override void Execute()
        {
            RevitTask.Initialize(RevitApi.UiApplication);

            var viewModel = Host.GetService<LoginViewModel>();
            var loginWindow = new Views.Pages.LoginWindow(viewModel);

            // Set as modal dialog
            loginWindow.Owner = System.Windows.Application.Current.MainWindow;
            loginWindow.ShowDialog();

            // After dialog closes, check if login was successful and refresh ribbon
            var settingsService = Host.GetService<ISettingsService>();
            if (settingsService.IsLoggedIn)
            {
                RibbonController.ReloadPanels(settingsService);
            }
        }
    }
}
