using Autodesk.Revit.UI;
using Autodesk.Windows;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Utils;

namespace CastorPlugin
{
    internal class RibbonController
    {
        private const string PanelName = "CastorPlugin";

        public static void CreatePanel(UIControlledApplication application, ISettingsService settingsService)
        {
            var addinPanel = application.CreatePanel("Revit Lookup");
            var pullButton = addinPanel.AddPullDownButton("RevitLookupButton", "RevitLookup");
            pullButton.SetImage("/CastorPlugin;component/Resources/Images/RibbonIcon16.png");
            pullButton.SetLargeImage("/CastorPlugin;component/Resources/Images/RibbonIcon32.png");

            //pullButton.AddPushButton<DashboardCommand>("Dashboard");
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
            Application.ActionEventHandler.Raise(_ =>
            {
                RibbonUtils.RemovePanel("CustomCtrl_%CustomCtrl_%Add-Ins%Revit Lookup%RevitLookupButton", PanelName);
                RibbonUtils.RemovePanel("CustomCtrl_%Revit Lookup%RevitLookup.Commands.SnoopSelectionCommand", PanelName);

                var controlledApplication = RevitApi.CreateUiControlledApplication();
                CreatePanel(controlledApplication, settingsService);

                RibbonUtils.ReloadShortcuts();
            });
        }
    }
}
