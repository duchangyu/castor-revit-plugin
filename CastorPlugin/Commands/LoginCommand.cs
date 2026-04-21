using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.ViewModels.Pages;
using CastorPlugin.Views.Pages;
using Nice3point.Revit.Toolkit.External;
using Revit.Async;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class LoginCommand : ExternalCommand
    {
        public override async void Execute()
        {
            RevitTask.Initialize(RevitApi.UiApplication);

            var viewModel = Host.GetService<LoginViewModel>();
            var loginView = new LoginView(viewModel);

            var dialogOptions = new SimpleContentDialogCreateOptions
            {
                Title = "登录 Castor",
                Content = loginView,
                CloseButtonText = "取消",
                DialogMaxWidth = 400
            };

            var dialogService = Host.GetService<IContentDialogService>();
            await dialogService.ShowSimpleDialogAsync(dialogOptions);

            // After dialog closes, check if login was successful and refresh ribbon
            var settingsService = Host.GetService<ISettingsService>();
            if (settingsService.IsLoggedIn)
            {
                RibbonController.ReloadPanels(settingsService);
            }
        }
    }
}
