using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using PZPack.Core.Index;
using PZPack.View.Utils;

namespace PZPack.View.Service.Converters
{
    internal class ExtIconConverter : IValueConverter
    {
        private static readonly Dictionary<PZItemType, BitmapImage> imageCache = new();
        static public BitmapImage GetIconImage(PZItemType iconType)
        {
            if (!imageCache.ContainsKey(iconType))
            {
                Bitmap bitmap = iconType switch
                {
                    PZItemType.Picture => View.StaticResources.file_picture,
                    PZItemType.Video => View.StaticResources.file_video,
                    PZItemType.Audio => View.StaticResources.file_audio,
                    PZItemType.Folder => View.StaticResources.folder,
                    _ => View.StaticResources.file_other
                };
                using MemoryStream ms = new();
                bitmap.Save(ms, ImageFormat.Png);

                BitmapImage image = new();
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
            PZItemType icon = PZItemType.Other;
            if (value is IPZFile file)
            {
                icon = ItemsType.GetItemType(file.Extension);
            }
            else if (value is IPZFolder)
            {
                icon = PZItemType.Folder;
            }

            return GetIconImage(icon);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
