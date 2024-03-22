using CastorPlugin.Config;
using CastorPlugin.Services;
using CastorPlugin.Services.Contracts;
using CastorPlugin.ViewModels.Contracts;
using CastorPlugin.ViewModels.Pages;
using CastorPlugin.Views;
using CastorPlugin.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using Wpf.Ui;

namespace CastorPlugin
{
    public static class Host
    {

        private static IHost _host;

        public static void Start()
        {
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly()!.Location),
                DisableDefaults = true
            });

            //Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddLoggerConfiguration();

            //Configuration
            builder.Configuration.AddFoldersConfiguration();

            //App services
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddSingleton<ISoftwareUpdateService, SoftwareUpdateService>();
            builder.Services.AddSingleton<IDigService, DigService>();

            //UI services
            builder.Services.AddScoped<INavigationService, NavigationService>();
            builder.Services.AddScoped<ISnackbarService, SnackbarService>();
            builder.Services.AddScoped<IContentDialogService, ContentDialogService>();
            builder.Services.AddScoped<NotificationService>();


            //Views
       
            builder.Services.AddScoped<AboutView>();
            builder.Services.AddScoped<AboutViewModel>();
            builder.Services.AddScoped<DashboardView>();
            builder.Services.AddScoped<IDashboardViewModel, DashboardViewModel>();
            builder.Services.AddScoped<SettingsView>();
            builder.Services.AddScoped<SettingsViewModel>();
            builder.Services.AddScoped<DigView>();
            builder.Services.AddScoped<DigViewModel>();


            builder.Services.AddScoped<IWindow, CastorMainWindowView>();

            //Startup view
            builder.Services.AddTransient<ICastorService, CastorService>();

            _host = builder.Build();
            _host.Start();

        }



        public static void Start(IHost host)
        {

            _host = host;
            host.Start();

        }
        public static void Stop()
        {
            _host.StopAsync().Wait();

             _host.Dispose();

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
