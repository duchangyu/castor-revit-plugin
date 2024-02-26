using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CastorPlugin.ViewModels.Converters;


[ValueConversion(typeof(WindowBackdropType), typeof(string))]
public sealed class BackgroundTypeConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var backgroundType = (WindowBackdropType)value!;
        return backgroundType switch
        {
            WindowBackdropType.None => "Disabled",
            WindowBackdropType.Acrylic => "Acrylic",
            WindowBackdropType.Tabbed => "Blur",
            WindowBackdropType.Mica => "Mica",
            WindowBackdropType.Auto => "Windows default",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}

[ValueConversion(typeof(ApplicationTheme), typeof(string))]
public sealed class ApplicationThemeConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var applicationTheme = (ApplicationTheme)value!;
        return applicationTheme switch
        {
            ApplicationTheme.Light => "Light",
            ApplicationTheme.Dark => "Dark",
            ApplicationTheme.HighContrast => "High contrast",
            ApplicationTheme.Unknown => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
