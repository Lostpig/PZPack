using PZPack.View.Service;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace PZPack.View
{
    /// <summary>
    /// SettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingWindow : Window
    {
        readonly private SettingWindowModel VModel;
        public SettingWindow()
        {
            InitializeComponent();
            VModel = new SettingWindowModel();
            DataContext = VModel;
        }

        private void OnSelectFile(object sender, RoutedEventArgs e)
        {
            string? path = FileSystem.OpenSelectFileDialog("Exe file|*.exe");
            if (path != null)
            {
                VModel.ExPlayer = path;
            }
        }
        private void OnSave(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(VModel.ExPlayer))
            {
                if (!File.Exists(VModel.ExPlayer))
                {
                    Alert.ShowWarning(Translate.EX_ExternalPlayerPathNotExists);
                    return;
                }
                string ext = Path.GetExtension(VModel.ExPlayer);
                if (ext.ToUpper() != ".EXE")
                {
                    Alert.ShowWarning(Translate.EX_ExternalPlayerPathNotExists);
                    return;
                }

                Config.Instance.ExternalPlayer = VModel.ExPlayer;
            }

            Close();
        }
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    internal class SettingWindowModel : INotifyPropertyChanged
    {
        private string _explayer;
        public string ExPlayer { get => _explayer; set { _explayer = value; NotifyPropertyChanged(nameof(ExPlayer)); } }
        public SettingWindowModel()
        {
            _explayer = Service.Config.Instance.ExternalPlayer;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
