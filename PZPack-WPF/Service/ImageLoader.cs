using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System;
using PZPack.Core.Index;
using System.Threading.Tasks;

namespace PZPack.View.Service
{
    internal class ImageLoader
    {
        public static async Task<BitmapSource?> TryLoadImageSource(PZFile file)
        {
            BitmapSource? source = null;
            if (Reader.Instance == null) return source;

            bool success = false;
            byte[] data = await Reader.Instance.ReadFileAsync(file);
            using var stream = new MemoryStream(data);
            try
            {
                source = DefaultLoad(stream);
                success = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            if (success) return source;

            try
            {
                source = DecoderLoad(stream, file.Extension);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return source;
        }
        private static BitmapImage DefaultLoad(MemoryStream stream)
        {
            stream.Position = 0;
            BitmapImage bitmap = new();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        private static BitmapSource DecoderLoad(MemoryStream stream, string ext)
        {
            BitmapDecoder bitmapDecoder = ext switch
            {
                ".jpeg" or ".jpg" => new JpegBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad),
                ".png" => new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad),
                ".gif" => new GifBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad),
                ".bmp" => new BmpBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad),
                _ => throw new NotSupportedException($"Not support extension \"{ext}\"")
            };
            return bitmapDecoder.Preview;
        }
    }
}
