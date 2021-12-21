using System;
using System.Globalization;
using System.Windows.Data;


namespace PZPack.View.Service.Converters
{
    internal class FileCouldPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = "unknown.unknown";
            if (value is string name)
            {
                fileName = name;
            }
            return Reader.IsPicture(fileName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
