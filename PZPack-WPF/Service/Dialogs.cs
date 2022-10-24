using System.Windows;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PZPack.Core.Index;

namespace PZPack.View.Service
{
    internal class Dialogs
    {
        static private ViewWindow? viewPtr;
        public static void TryOpenPZFile()
        {
            string? path = FileSystem.OpenSelectFileDialog("PZPack Files|*.pzpk;*.pzmv", Config.Instance.LastOpenDirectory);
            if (path != null)
            {
                Config.Instance.LastOpenDirectory = Path.GetDirectoryName(path) ?? "";

                bool openSuccess = Reader.TryOpen(path);
                if (openSuccess) return;

                OpenReadOptionWindow(path);
            }
        }
        public static void OpenReadOptionWindow(string path)
        {
            ReadOptionWindow win = new(path) { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }
        public static void OpenPackWindow()
        {
            PackWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }
        public static void OpenViewWindow(PZFile[] list, int index)
        {
            PZFile file = list[index];
            if (!Reader.IsPicture(file)) return;

            if (viewPtr == null)
            {
                viewPtr = new();
                viewPtr.Closed += ViewPtr_Closed;
            }

            List<PZFile> pictures = list.Where(f => Reader.IsPicture(f)).ToList();
            viewPtr.BindFiles(file, pictures);

            if (viewPtr.IsVisible)
            {
                viewPtr.Activate();
                if (viewPtr.WindowState == WindowState.Minimized)
                {
                    viewPtr.WindowState = WindowState.Normal;
                }
            }
            else
            {
                viewPtr.Show();
                viewPtr.WindowState = WindowState.Maximized;
            }
        }
        public static void OpenSettingWindow()
        {
            SettingWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }

        public static void OpenPwBookOpenWindow(bool isCreate)
        {
            string? path;
            if (isCreate)
            {
                path = FileSystem.OpenSaveFileDialog("PZPasswordBook File|*.pzpwb", Config.Instance.LastPWBookDirectory);
            }
            else
            {
                path = FileSystem.OpenSelectFileDialog("PZPasswordBook File|*.pzpwb", Config.Instance.LastPWBookDirectory);
            }
            if (path == null) return;

            Config.Instance.LastPWBookDirectory = Path.GetDirectoryName(path) ?? "";
            OpenPwBookWindow win = new(path, isCreate) { Owner = Application.Current.MainWindow };
            bool? isOpened = win.ShowDialog();

            if (isOpened == true && isCreate)
            {
                OpenPwBookManageWindow();
            }
        }
        public static void OpenPwBookManageWindow()
        {
            if (PWBook.Current == null) return;
            PWBookWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }

        public static void OpenExtractAllWindow()
        {
            string? output = FileSystem.OpenSelectDirectryDialog();
            if (output == null) return;

            ExtractWindow win = new() { Owner = Application.Current.MainWindow };
            win.StartExtractAll(output);
            win.ShowDialog();
        }
        public static void OpenExtractWindow(PZFile file)
        {
            string? output = FileSystem.OpenSaveExtractFileDialog(file);
            if (output == null) return;

            ExtractWindow win = new() { Owner = Application.Current.MainWindow };
            win.StartExtractFile(file, output);
            win.ShowDialog();
        }

        public static void CloseViewWindow()
        {
            if (viewPtr != null)
            {
                viewPtr.Close();
            }
        }
        private static void ViewPtr_Closed(object? sender, System.EventArgs e)
        {
            if (viewPtr != null)
            {
                viewPtr.Closed -= ViewPtr_Closed;
                viewPtr = null;
            }
        }
    }
}
