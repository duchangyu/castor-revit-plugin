using Autodesk.Revit.UI;
using CastorPlugin.Commands;
using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Nice3point.Revit.Toolkit.External;
using Nice3point.Revit.Toolkit.External.Handlers;
using Serilog.Events;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


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
            CreateLogger();
            CreateRibbon();

            Host.Start();

        }

        public override void OnShutdown()
        {
            Log.CloseAndFlush();
            SaveSettings();

            Host.Stop();
        }

        private void SaveSettings()
        {
            var settingsService = Host.GetService<ISettingsService>();
            settingsService.Save();
        }

        private void CreateRibbon()
        {
            var panel = Application.CreatePanel("Commands", "CastorPlugin");

            var showButtonSimple = panel.AddPushButton<Command>("Execute");
            showButtonSimple.SetImage("/CastorPlugin;component/Resources/Icons/RibbonIcon16.png");
            showButtonSimple.SetLargeImage("/CastorPlugin;component/Resources/Icons/RibbonIcon32.png");

            var showButton = panel.AddPushButton<StartCommand>("ShowWpfWin");
            showButton.SetImage("/CastorPlugin;component/Resources/Icons/RibbonIcon16.png");
            showButton.SetLargeImage("/CastorPlugin;component/Resources/Icons/RibbonIcon32.png");







        }

        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var e = (Exception)args.ExceptionObject;
                Log.Fatal(e, "Domain unhandled exception");
            };
        }
    }
}