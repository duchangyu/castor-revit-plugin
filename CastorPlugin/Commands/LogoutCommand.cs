using Autodesk.Revit.Attributes;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Nice3point.Revit.Toolkit.External;
using Revit.Async;

namespace CastorPlugin.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class LogoutCommand : ExternalCommand
    {
        public override void Execute()
        {
            RevitTask.Initialize(RevitApi.UiApplication);

            var authService = Host.GetService<IAuthService>();
            authService.Logout();

            var settingsService = Host.GetService<ISettingsService>();
            RibbonController.ReloadPanels(settingsService);
        }
    }
}
