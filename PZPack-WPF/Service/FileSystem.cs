using System.Windows.Forms;
using System.Diagnostics;
using PZPack.Core.Index;

namespace PZPack.View.Service
{
    internal class FileSystem
    {
        static public string? OpenSelectFileDialog(string filter, string? initDir = null)
        {
            Microsoft.Win32.OpenFileDialog dlg = new()
            {
                // dlg.DefaultExt = ".pzpk";
                Filter = filter,
                Multiselect = false
            };
            if (!string.IsNullOrEmpty(initDir))
            {
                dlg.InitialDirectory = initDir;
            }

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                return filename;
            }
            return null;
        }
        static public string? OpenSelectDirectryDialog()
        {
            FolderBrowserDialog dlg = new();
            DialogResult result = dlg.ShowDialog();

            if (result == DialogResult.OK)
            {
                Debug.WriteLine(dlg.SelectedPath);
                return dlg.SelectedPath;
            }
            return null;
        }
        static public string? OpenSaveFileDialog(string filter, string? initDir = null)
        {
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                // dlg.DefaultExt = ".pzpk";
                Filter = filter
            };
            if (!string.IsNullOrEmpty(initDir))
            {
                dlg.InitialDirectory = initDir;
            }

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                return filename;
            }
            return null;
        }
        static public string? OpenSaveExtractFileDialog(PZFile file)
        {
            Microsoft.Win32.SaveFileDialog dlg = new()
            {
                DefaultExt = file.Extension,
                FileName = file.Name
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filename = dlg.FileName;
                return filename;
            }
            return null;
        }
        static public string ComputeFileSize(double size)
        {
            int count = 0;
            double n = size / 1024;
            string[] suffix = { " KB", " MB", " GB", " TB", " PB" };

            while (n > 1024 && count < suffix.Length)
            {
                n /= 1024;
                count++;
            }

            return n.ToString("f1") + suffix[count];
        }
        static public string ComputeFileSize(long size)
        {
            return ComputeFileSize((double)size);
        }
    }
}
