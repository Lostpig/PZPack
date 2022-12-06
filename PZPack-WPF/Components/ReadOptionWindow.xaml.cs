using System;
using System.ComponentModel;
using System.Windows;
using PZPack.View.Service;
using System.Windows.Input;

namespace PZPack.View
{
    /// <summary>
    /// ReadOptionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ReadOptionWindow : Window
    {
        readonly private ROWindowModel VModel;
        public ReadOptionWindow(string source)
        {
            InitializeComponent();
            VModel = new()
            {
                Source = source
            };
            this.DataContext = VModel;
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            pwText.Focus();
        }
        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            string password = VModel.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                Alert.ShowMessage(Translate.MSG_Password_empty);
                return;
            }

            bool success = Reader.Open(Source, Password);

            if (success)
            {
                Close();
            }
        }
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string Source { get => VModel.Source; }
        private string Password { get => VModel.Password; }

        private void pwText_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                OnOpenFile(sender, e);
            }
        }
    }

    internal class ROWindowModel : INotifyPropertyChanged
    {
        private string _password;
        public string Password { get => _password; set { _password = value; NotifyPropertyChanged(nameof(Password)); } }
        private string _source;
        public string Source { get => _source; set { _source = value; NotifyPropertyChanged(nameof(Source)); } }
        public ROWindowModel()
        {
            _password = "";
            _source = "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
