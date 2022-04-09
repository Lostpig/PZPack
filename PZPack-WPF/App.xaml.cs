using System.Windows;

namespace PZPack.View
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Service.Language.Init();
        }
        protected override void OnExit(ExitEventArgs e)
        {
            if (Service.Reader.Instance != null)
            {
                Service.Reader.Close();
            }
            Service.DashServer.Close();

            base.OnExit(e);
        }
    }
}
