using Autodesk.Revit.UI;
using CastorPlugin.Commands;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Utils;
using Serilog;

namespace CastorPlugin
{
    internal class RibbonController
    {
        private const string PanelTitle = "Castor Plugin-蓖麻链";

        public static void CreatePanel(UIControlledApplication application, ISettingsService settingsService)
        {
            _ = settingsService;

            var addinPanel = application.CreatePanel(PanelTitle);
            var startButton = addinPanel.AddPushButton<StartCommand>("开始挖宝");
            startButton.SetImage("/CastorPlugin;component/Resources/Images/RibbonIcon16.png");
            startButton.SetLargeImage("/CastorPlugin;component/Resources/Images/RibbonIcon32.png");

            startButton.ToolTip = "打开挖宝大厅。登录、挖宝和资产广场都可以从这里进入。";
        }

        public static void ReloadPanels(ISettingsService settingsService)
        {
            _ = settingsService;

            if (Application.ActionEventHandler is null)
            {
                TryReloadPanelsCore(settingsService);
                return;
            }

            Application.ActionEventHandler.Raise(_ =>
            {
                TryReloadPanelsCore(settingsService);
            });
        }

        private static void TryReloadPanelsCore(ISettingsService settingsService)
        {
            try
            {
                RibbonUtils.RemovePanelByTitle(PanelTitle);

                var controlledApplication = RevitApi.CreateUiControlledApplication();
                CreatePanel(controlledApplication, settingsService);

                RibbonUtils.ReloadShortcuts();
            }
            catch (Exception ex)
            {
                Log.Warning($"Ribbon 热刷新失败，已忽略: {ex.Message}");
            }
        }
    }
}
