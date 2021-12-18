using System;
using System.Globalization;
using System.Windows.Data;

namespace PZPack.View.Service.Converters
{
    class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                return FileSystem.ComputeFileSize(size);
            }

            return "0.0MB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
