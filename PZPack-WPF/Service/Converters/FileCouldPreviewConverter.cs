using PZPack.Core.Index;
using PZPack.View.Utils;
using System;
using System.Globalization;
using System.Windows.Data;


namespace PZPack.View.Service.Converters
{
    internal class FileCouldPreviewConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PZFile file)
            {
                return ItemsType.IsPicture(file);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
