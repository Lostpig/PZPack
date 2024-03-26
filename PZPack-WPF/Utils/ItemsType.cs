using PZPack.Core.Index;

namespace PZPack.View.Utils
{
    enum PZItemType
    {
        Folder,
        Picture,
        Video,
        Audio,
        Other
    }

    internal class ItemsType
    {
        public static PZItemType GetItemType(string ext)
        {
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif" or ".webp"
                    => PZItemType.Picture,
                ".mp4" or ".avi" or ".mkv" or ".wmv"
                    => PZItemType.Video,
                ".mp3" or ".ogg" or ".flac" or ".ape"
                    => PZItemType.Audio,
                _ => PZItemType.Other
            };
        }
        public static PZItemType GetItemType(PZFile file)
        {
            return GetItemType(file.Extension);
        }

        public static bool IsPicture(PZFile file)
        {
            return GetItemType(file) == PZItemType.Picture;
        }
        public static bool IsVideo(PZFile file)
        {
            return GetItemType(file) == PZItemType.Video;
        }
        public static bool IsAudio(PZFile file)
        {
            return GetItemType(file) == PZItemType.Audio;
        }
    }
}
