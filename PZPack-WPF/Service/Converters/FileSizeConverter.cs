using PZPack.Core.Index;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PZPack.View.Service.Converters
{
    class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IPZFile file)
            {
                return FileSystem.ComputeFileSize(file.Size);
            }
            else if (value is int intval)
            {
                return FileSystem.ComputeFileSize(intval);
            }
            else if (value is long longval)
            {
                return FileSystem.ComputeFileSize(longval);
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
