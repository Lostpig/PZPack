using PZPack.Core.Index;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace PZPack.Core.Core;

public class ImageResizer
{
    public static ImageResizer CreateJpegResizer(int maxSize, int quality)
    {
        JpegEncoder encoder = new () { Quality = quality };
        return new ImageResizer(maxSize, encoder, ".jpg"); 
    }
    public static ImageResizer CreatePngResizer(int maxSize)
    {
        PngEncoder encoder = new ();
        return new ImageResizer(maxSize, encoder, ".png");
    }
    public static ImageResizer CreateWebpResizer(int maxSize, bool lossless, int quality)
    {
        WebpFileFormatType type = lossless ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy;
        WebpEncoder encoder = new () { FileFormat = type, Quality = quality };
        return new ImageResizer(maxSize, encoder, ".webp");
    }

    private readonly ImageEncoder Encoder;
    private readonly int MaxSize;
    public readonly string Extension;

    private ImageResizer(int maxSize, ImageEncoder encoder, string extension)
    {
        this.MaxSize = maxSize;
        this.Encoder = encoder;
        this.Extension = extension;
    }

    private double ComputeResizeScale(int w, int h)
    {
        int large = w > h ? w : h;
        if (large <= MaxSize) return 1;

        return MaxSize / ((double)large);
    }

    public MemoryStream ResizeImage(FileStream source)
    {
        using var img = Image.Load(source);
        var scale = ComputeResizeScale(img.Width, img.Height);

        if (scale < 1)
        {
            int newWidth = (int)(img.Width * scale);
            int newHeight = (int)(img.Height * scale);
            img.Mutate(x => x.Resize(newWidth, newHeight));
        }

        MemoryStream result = new();
        img.Save(result, Encoder);

        return result;
    }

    public static bool IsImageFile(PZDesigningFile file)
    {
        return file.Extension == ".jpg" || file.Extension == ".jpeg" || file.Extension == ".png";
    }
}
