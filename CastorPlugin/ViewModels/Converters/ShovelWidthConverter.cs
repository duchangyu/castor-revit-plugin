  using System;
  using System.Globalization;
  using System.Windows.Data;

  namespace CastorPlugin.ViewModels.Converters
  {
      public class ShovelWidthConverter : IValueConverter
      {
          public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          {
              double gridWidth = (double)value;
              return gridWidth * 0.20; // 50% of grid width
          }

          public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
          {
              throw new NotImplementedException();
          }
      }

      public class ShovelHeightConverter : IValueConverter
      {
          public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          {
              double gridHeight = (double)value;
              return gridHeight * 0.20; // 50% of grid height
          }

          public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
          {
              throw new NotImplementedException();
          }
      }
  }