using System.Windows;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PZPack.Core.Index;
using System;
using static System.Net.WebRequestMethods;
using PZPack.View.Utils;

namespace PZPack.View.Service
{
    internal class Dialogs
    {
        static private ViewWindow? imageViewPtr;
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
        public static void OpenBuilderWindow()
        {
            BuilderWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }
        public static void OpenViewWindow(PZFile file)
        {
            var tp = ItemsType.GetItemType(file);
            if (tp == PZItemType.Picture)
            {
                OpenImageViewWindow(file);
            }
            else if (tp == PZItemType.Video)
            {
                OpenVideoViewWindow(file);
            }
        }

        private static void OpenImageViewWindow(PZFile file)
        {
            if (!ItemsType.IsPicture(file) || Reader.Instance is null) return;

            var parentFolder = Reader.Instance.Index.GetFolder(file.Pid);
            Reader.Instance.Index.GetChildren(parentFolder, out var _, out var files);
            int index = Array.IndexOf(files, file);
            index = index < 0 ? 0 : index;

            if (imageViewPtr == null)
            {
                imageViewPtr = new();
                imageViewPtr.Closed += ViewPtr_Closed;
            }

            List<PZFile> pictures = files.Where(f => ItemsType.IsPicture(f)).ToList();
            imageViewPtr.BindFiles(file, pictures);

            if (imageViewPtr.IsVisible)
            {
                imageViewPtr.Activate();
                if (imageViewPtr.WindowState == WindowState.Minimized)
                {
                    imageViewPtr.WindowState = WindowState.Normal;
                }
            }
            else
            {
                imageViewPtr.Show();
                imageViewPtr.WindowState = WindowState.Maximized;
            }
        }
        private static void OpenVideoViewWindow(PZFile file)
        {
            if (!ItemsType.IsVideo(file) || Reader.Instance is null) return;

            VideoWindow win = new();

            win.WindowState = WindowState.Maximized;
            win.SetFile(file);

            win.ShowDialog();
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

        public static void OpenExtractFolderWindow(PZFolder folder)
        {
            string? output = FileSystem.OpenSelectDirectryDialog();
            if (output == null) return;

            ExtractWindow win = new() { Owner = Application.Current.MainWindow };
            win.StartExtractFolder(folder, output);
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
            if (imageViewPtr != null)
            {
                imageViewPtr.Close();
            }
        }
        private static void ViewPtr_Closed(object? sender, System.EventArgs e)
        {
            if (imageViewPtr != null)
            {
                imageViewPtr.Closed -= ViewPtr_Closed;
                imageViewPtr = null;
            }
        }
    }
}
