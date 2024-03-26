using PZPack.Core.Index;
using System.Globalization;
using System;
using System.Windows.Data;
using System.Windows;

namespace PZPack.View.Service.Converters
{
    internal class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string s && s.ToLower() == "reverse")
            {
                return value is true ? Visibility.Collapsed : Visibility.Visible;
            } 
            else
            {
                return value is true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
