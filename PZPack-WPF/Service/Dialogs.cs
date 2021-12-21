using PZPack.Core;
using System.Windows;
using System.Linq;

namespace PZPack.View.Service
{
    internal class Dialogs
    {
        static private ViewWindow? viewPtr;
        public static void OpenReadOptionWindow()
        {
            ReadOptionWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }
        public static void OpenPackWindow()
        {
            PackWindow win = new() { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }
        public static void OpenViewWindow(PZFile[] files, int index)
        {
            PZFile file = files[index];
            if (!Reader.IsPicture(file)) return;

            if (viewPtr == null)
            {
                viewPtr = new();
                viewPtr.Closed += ViewPtr_Closed;
            }

            PZFile[] pictures = files.Where(f => Reader.IsPicture(f)).ToArray();
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

        public static void OpenExtractAllWindow ()
        {
            string? output = FileSystem.OpenSelectDirectryDialog();
            if (output == null) return;

            ExtractWindow win = new();
            win.Show();

            win.StartExtractAll(output);
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
