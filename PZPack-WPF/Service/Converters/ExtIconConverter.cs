using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Collections.Generic;

namespace PZPack.View.Service.Converters
{
    internal class ExtIconConverter : IValueConverter
    {
        enum IconType
        {
            Picture,
            Video,
            Audio,
            Other
        }

        private static Dictionary<IconType, BitmapImage> imageCache = new();
        private static IconType GetExtensionType(string ext)
        {
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp"
                    => IconType.Picture,
                ".mp4" or ".avi" or ".mkv" or ".wmv"
                    => IconType.Video,
                ".mp3" or ".ogg" or ".flac" or ".ape"
                    => IconType.Audio,
                _ => IconType.Other
            };
        }
        static public BitmapImage GetExtensionIcon(string ext)
        {
            IconType iconType = GetExtensionType(ext);
            if (!imageCache.ContainsKey(iconType))
            {
                Bitmap bitmap = ext switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp"
                        => View.StaticResources.file_picture,
                    ".mp4" or ".avi" or ".mkv" or ".wmv"
                        => View.StaticResources.file_video,
                    ".mp3" or ".ogg" or ".flac" or ".ape"
                        => View.StaticResources.file_audio,
                    _ => View.StaticResources.file_other
                };
                using MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();

                imageCache.Add(iconType, image);
            }


            return imageCache[iconType];
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileExt = ".unknown";
            if (value is string ext)
            {
                fileExt = ext;
            }

            return GetExtensionIcon(fileExt);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
