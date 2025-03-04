using System.Threading;
using System;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PZPack.View.Service;

namespace PZPack.View
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string uniqueId = "pzpk_wpf_application";

        protected override void OnStartup(StartupEventArgs e)
        {
            string? startupPath = e.Args.Length > 0 ? e.Args[0] : null;
            SingleInstance.EnsureSingleAppInstance(uniqueId, startupPath);

            base.OnStartup(e);
            Service.Language.Init();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            StartupWith.ApplyStartArg(startupPath);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Service.Reader.Instance != null)
            {
                Service.Reader.Close();
            }

            SingleInstance.ReleaseSingleInstanceMutex();
            base.OnExit(e);
        }
    }
}
