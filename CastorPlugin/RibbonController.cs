using Autodesk.Revit.UI;
using Autodesk.Windows;
using CastorPlugin.Commands;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Utils;
using Serilog;

namespace CastorPlugin
{
    internal class RibbonController
    {
        private const string PanelName = "CastorPlugin";

        public static void CreatePanel(UIControlledApplication application, ISettingsService settingsService)
        {
            var addinPanel = application.CreatePanel("Castor Plugin-蓖麻链");
            var pullButton = addinPanel.AddPullDownButton("CastorButton", "Castor");
            pullButton.SetImage("/CastorPlugin;component/Resources/Images/RibbonIcon16.png");
            pullButton.SetLargeImage("/CastorPlugin;component/Resources/Images/RibbonIcon32.png");

           
            pullButton.AddPushButton<StartCommand>("开始挖宝");
            pullButton.AddPushButton<DashboardCommand>("挖宝大厅");

            // Dynamic login/logout button based on auth state
            if (settingsService.IsLoggedIn && settingsService.CurrentUser != null)
            {
                pullButton.AddPushButton<LogoutCommand>($"注销 ({settingsService.CurrentUser.Phone})");
            }
            else
            {
                pullButton.AddPushButton<LoginCommand>("登录");
            }

            pullButton.AddPushButton<AboutCommand>("关于我们");

            //var showButton = panel.AddPushButton<StartCommand>("ShowWpfWin");
            //showButton.SetImage("/CastorPlugin;component/Resources/Icons/RibbonIcon16.png");
            //showButton.SetLargeImage("/CastorPlugin;component/Resources/Icons/RibbonIcon32.png");

            //pullButton.AddPushButton<DashboardCommand>("挖宝大厅");
            //ResolveSelectionButton(settingsService, pullButton);

            //pullButton.AddPushButton<EventMonitorCommand>("Event monitor");
        }

        private static void ResolveSelectionButton(ISettingsService settingsService, PulldownButton parentButton)
        {
           

            //var button = modifyPanel.AddPushButton<SnoopSelectionCommand>("Snoop\nSelection");
            //button.SetImage("/CastorPlugin;component/Resources/Images/RibbonIcon16.png");
            //button.SetLargeImage("/CastorPlugin;component/Resources/Images/RibbonIcon32.png");
        }

        public static void ReloadPanels(ISettingsService settingsService)
        {
            if (Application.ActionEventHandler is null)
            {
                Log.Warning("ActionEventHandler 未初始化，尝试在当前上下文直接刷新 Ribbon");
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
                ReloadPanelsCore(settingsService);
            }
            catch (Exception ex)
            {
                Log.Warning($"Ribbon 热刷新失败，已忽略: {ex.Message}");
            }
        }

        private static void ReloadPanelsCore(ISettingsService settingsService)
        {
            RibbonUtils.RemovePanel("CustomCtrl_%CustomCtrl_%Add-Ins%CastorPlugin%CastorButton", PanelName);
            //RibbonUtils.RemovePanel("CustomCtrl_%CastorPlugin%RevitLookup.Commands.SnoopSelectionCommand", PanelName);

            var controlledApplication = RevitApi.CreateUiControlledApplication();
            CreatePanel(controlledApplication, settingsService);

            RibbonUtils.ReloadShortcuts();
        }
    }
}
