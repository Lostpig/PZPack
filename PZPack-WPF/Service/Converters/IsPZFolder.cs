using PZPack.Core.Index;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PZPack.View.Service.Converters
{
    internal class IsPZFolder : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is PZFolder || value is PZDesigningFolder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
