using System.Windows;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PZPack.Core.Index;
using System;
using PZPack.View.Utils;
using PZPack.View.Windows;

namespace PZPack.View.Service
{
    internal class Dialogs
    {
        static private ImagePreviewWindow? imagePreviewPtr;
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
            if (!FFME.CheckSetting()) return;

            var tp = ItemsType.GetItemType(file);
            if (tp == PZItemType.Picture)
            {
                OpenImageViewWindow(file);
            }
            else if (tp == PZItemType.Video || tp == PZItemType.Audio)
            {
                // Alert.ShowMessage("test");
                OpenMediaPlayerWindow(file);
            }
        }

        private static void OpenImageViewWindow(PZFile file)
        {
            if (!ItemsType.IsPicture(file) || Reader.Instance is null) return;

            var parentFolder = Reader.Instance.Index.GetFolder(file.Pid);
            Reader.Instance.Index.GetChildren(parentFolder, out var _, out var files);
            int index = Array.IndexOf(files, file);
            index = index < 0 ? 0 : index;

            if (imagePreviewPtr == null)
            {
                imagePreviewPtr = new();
                imagePreviewPtr.Closed += ViewPtr_Closed;
            }

            List<PZFile> pictures = files.Where(f => ItemsType.IsPicture(f)).ToList();
            imagePreviewPtr.BindFiles(file, pictures);

            if (imagePreviewPtr.IsVisible)
            {
                imagePreviewPtr.Activate();
                if (imagePreviewPtr.WindowState == WindowState.Minimized)
                {
                    imagePreviewPtr.WindowState = WindowState.Normal;
                }
            }
            else
            {
                imagePreviewPtr.Show();
                imagePreviewPtr.WindowState = WindowState.Maximized;
            }
        }

        private static void OpenMediaPlayerWindow(PZFile file)
        {
            if (Reader.Instance is null) return;

            if (ItemsType.IsVideo(file) || ItemsType.IsAudio(file))
            {
                var mediaWin = new MediaPlayerWindow(file);
                mediaWin.ShowDialog();
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
            if (imagePreviewPtr != null)
            {
                imagePreviewPtr.Close();
            }
        }
        private static void ViewPtr_Closed(object? sender, System.EventArgs e)
        {
            if (imagePreviewPtr != null)
            {
                imagePreviewPtr.Closed -= ViewPtr_Closed;
                imagePreviewPtr = null;
            }
        }
    }
}
