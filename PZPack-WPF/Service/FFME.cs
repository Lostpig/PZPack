using System;
using System.Linq;
using System.IO;

namespace PZPack.View.Service
{
    internal static class FFME
    {
        private static bool ffmpegEnabled = false;
        public static bool CheckSetting() {
            if (ffmpegEnabled) return true;

            string ffmpegDir = Config.Instance.FFMpegDirectory;
            if (string.IsNullOrEmpty(ffmpegDir))
            {
                Alert.ShowWarning(Translate.EX_FfmpegDirectoryNotSet);
                return false;
            }

            DirectoryInfo directory = new DirectoryInfo(ffmpegDir);
            int checkedFileCount = 0;
            string[] ffmpegExes = new string[3] { "ffmpeg.exe", "ffplay.exe", "ffprobe.exe" };
            if (directory.Exists)
            {
                var files = directory.GetFiles();
                foreach (var file in files)
                {
                    if (ffmpegExes.Contains(file.Name)) 
                    {
                        checkedFileCount++;
                        if (checkedFileCount == ffmpegExes.Length)
                        {
                            break;
                        }
                    }
                }
            }

            if (checkedFileCount != ffmpegExes.Length)
            {
                Alert.ShowWarning(
                    String.Format(Translate.EX_FfmpegDirectoryInvalid, ffmpegExes.ToString())
                );
                return false;
            }

            Unosquare.FFME.Library.FFmpegDirectory = ffmpegDir;
            ffmpegEnabled = true;

            return true;
        }
    }
}
