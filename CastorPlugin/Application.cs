using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.External.Handlers;
using System.Diagnostics;
using System.IO;
using Serilog;
using System.Reflection;

namespace CastorPlugin
{
    [UsedImplicitly]
    public class Application : ExternalApplication
    {

        public static ActionEventHandler ActionEventHandler { get; private set; }
     

        public override void OnStartup()
        {
            try
            {
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                string logFilePath = Path.Combine(assemblyDirectory, "CastorPlugin.log");

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.File(logFilePath, 
                        rollingInterval: RollingInterval.Day, 
                        shared: true,
                        flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .WriteTo.Debug() // This will output to the Debug window
                    .CreateLogger();

                Log.Information("Application starting up");
                Log.Information($"Log file path: {logFilePath}");

                // Test if we can write to the log file
                File.AppendAllText(logFilePath, "Test log entry\n");
                Log.Information("Successfully wrote to log file");
            }
            catch (Exception ex)
            {
                // If we can't set up logging, show an error message
                System.Windows.MessageBox.Show($"Failed to initialize logging: {ex.Message}", "Logging Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }

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
            Log.Information("Application shutting down");
            
            SaveSettings();
            UpdateSoftware();
            
            Log.CloseAndFlush();
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