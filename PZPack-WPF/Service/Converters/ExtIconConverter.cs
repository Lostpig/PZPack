using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PZPack.View.Service.Converters
{
    internal class ExtIconConverter : IValueConverter
    {
        static private byte[] GetExtensionIcon(string ext)
        {
            byte[] buffer = ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp"
                    => View.StaticResources.IconImage,
                ".mp4" or ".avi" or ".mkv" or ".wmv"
                    => View.StaticResources.IconVideo,
                ".mp3" or ".ogg" or ".flac" or ".ape"
                    => View.StaticResources.IconVideo,
                ".zip" or ".rar" or ".7z"
                    => View.StaticResources.IconZip,
                _ => View.StaticResources.IconFile
            };
            return buffer;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileExt = ".unknown";
            if (value is string ext)
            {
                fileExt = ext;
            }

            byte[] buffer = GetExtensionIcon(fileExt);

            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(buffer);
            bitmap.EndInit();
            return bitmap;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
