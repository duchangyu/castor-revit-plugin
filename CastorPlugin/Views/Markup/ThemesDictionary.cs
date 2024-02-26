using CastorPlugin.Services.Contracts;
using System.Windows;
using System.Windows.Markup;
using Wpf.Ui.Appearance;

namespace CastorPlugin.Views.Markup
{

    /// <summary>
    ///     Provides a dictionary implementation that contains <c>WPF UI</c> theme resources used by components and other elements of a WPF application.
    /// </summary>
    [Localizability(LocalizationCategory.Ignore)]
    [Ambient]
    [UsableDuringInitialization(true)]
    public sealed class ThemesDictionary : ResourceDictionary
    {

        public ThemesDictionary() {
#if DEBUG
            ApplicationTheme theme;
            try
            {
                theme = Host.GetService<ISettingsService>().Theme;
            }
            catch
            {
                //UnHosted build
                theme = ApplicationTheme.Light;
            }
#else
        var theme = Host.GetService<ISettingsService>().Theme;
#endif
            var themeName = theme switch
            {
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                _ => "Light"
            };

            Source = new Uri($"{ApplicationThemeManager.ThemesDictionaryPath}{themeName}.xaml", UriKind.Absolute);

        }
    }
}
