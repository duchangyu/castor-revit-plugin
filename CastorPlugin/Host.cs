using CastorPlugin.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using CastorPlugin.Services;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Views;
using UIFramework;

namespace CastorPlugin
{
    public static class Host
    {

        private static IHost _host;

        public static void Start()
        {
            _host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureAppConfiguration(SetConfiguration)
                .ConfigureServices(AddServices)
                .Build();

            _host.Start();

        }

        private static void AddServices(HostBuilderContext context, IServiceCollection services)
        {
            //services
            services.AddSingleton<ISettingsService, SettingsService>();

            //views
            services.AddScoped<IWindow, WindowMain>(); // the main window view
           
        }

        private static void SetConfiguration( IConfigurationBuilder builder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyLocation = assembly.Location;
            var versionInfo = FileVersionInfo.GetVersionInfo(assemblyLocation);
            var addinVersion = new Version(versionInfo.FileVersion).ToString(3);
#if RELEASE
        var version = addinVersion.Split('.')[0];
        if (version == "1") version = "Develop";
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var userDataLocation = Path.Combine(programDataPath, @"Autodesk\Revit\Addins\", version, "Castor");
#else
            var userDataLocation = Path.GetDirectoryName(assemblyLocation)!;
#endif
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var writeAccess = AccessUtils.CheckWriteAccess(assemblyLocation) && !assemblyLocation.StartsWith(appDataPath);

            var targetFrameworkAttributes = assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), true);
            var targetFrameworkAttribute = (TargetFrameworkAttribute)targetFrameworkAttributes.First();
            var targetFramework = targetFrameworkAttribute.FrameworkDisplayName;

            builder.AddInMemoryCollection(new KeyValuePair<string, string>[]
            {
            new("Assembly", assemblyLocation),
            new("Framework", targetFramework),
            new("AddinVersion", addinVersion),
            new("ConfigFolder", Path.Combine(userDataLocation, "Config")),
            new("DownloadFolder", Path.Combine(userDataLocation, "Downloads")),
            new("FolderAccess", writeAccess ? "Write" : "Read")
            });
        }

        public static void Start(IHost host)
        {

            _host = host;
            host.Start();
        }
        public static void Stop()
        {
            _host.StopAsync().Wait();

        }

        public static T GetService<T>() where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }

        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            return serviceProvider.GetService(typeof(T)) as T;
        }


    }
}
