using System.Collections.ObjectModel;

namespace PZPack.View.Utils
{
    class BlockSizes : ObservableCollection<int>
    {
        public BlockSizes()
        {
            Add(64 * 1024);
            Add(256 * 1024);
            Add(1024 * 1024);
            Add(2 * 1024 * 1024);
            Add(4 * 1024 * 1024);
            Add(8 * 1024 * 1024);
            Add(16 * 1024 * 1024);
            Add(32 * 1024 * 1024);
            Add(64 * 1024 * 1024);
        }
    }


    enum ResizeImageFormat
    {
        Never = 0,
        JPEG = 1,
        PNG = 2,
        WEBP = 3,
    }
    class ResizeImageOption
    {
        public string Name { get; set; } = "";
        public ResizeImageFormat Value { get; set; } = ResizeImageFormat.Never;
    }
    class ResizeImageOptions : ObservableCollection<ResizeImageOption>
    {
        public ResizeImageOptions()
        {
            Add(new ResizeImageOption() { Name = "-", Value = ResizeImageFormat.Never });
            Add(new ResizeImageOption() { Name = "jpg", Value = ResizeImageFormat.JPEG });
            Add(new ResizeImageOption() { Name = "png", Value = ResizeImageFormat.PNG });
            Add(new ResizeImageOption() { Name = "webp", Value = ResizeImageFormat.WEBP });
        }
    }
}
