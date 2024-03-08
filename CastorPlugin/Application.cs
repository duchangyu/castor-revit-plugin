using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.External.Handlers;
using System.Diagnostics;
using System.IO;


namespace CastorPlugin
{
    [UsedImplicitly]
    public class Application : ExternalApplication
    {

        public static ActionEventHandler ActionEventHandler { get; private set; }
     

        public override void OnStartup()
        {
            // binding revit application
            RevitApi.UiApplication = UiApplication;

            Host.Start();

            var settingsService = Host.GetService<ISettingsService>();
            var updateService = Host.GetService<ISoftwareUpdateService>();

            EnableHardwareRendering(settingsService);
            RibbonController.CreatePanel(Application, settingsService);

            updateService.CheckUpdates();
        }


        public override void OnShutdown()
        {
       
            SaveSettings();

            UpdateSoftware();

            Host.Stop();
        }


        private static void UpdateSoftware()
        {
            var updateService = Host.GetService<ISoftwareUpdateService>();
            if (File.Exists(updateService.LocalFilePath)) Process.Start(updateService.LocalFilePath);
        }

        private void SaveSettings()
        {
            var settingsService = Host.GetService<ISettingsService>();
            settingsService.Save();
        }




        public static void EnableHardwareRendering(ISettingsService settingsService)
        {
            //if (!settingsService.UseHardwareRendering) return;

            ////Revit overrides render mode during initialization
            ////EventHandler is called after initialisation
            //ActionEventHandler.Raise(_ => RenderOptions.ProcessRenderMode = RenderMode.Default);
        }

        public static void DisableHardwareRendering(ISettingsService settingsService)
        {
            //if (settingsService.UseHardwareRendering) return;
            //RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        }
    }
}