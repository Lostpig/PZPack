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
            passwordCtrl.Focus();
        }
        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            string password = passwordCtrl.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                Alert.ShowMessage(Translate.MSG_Password_empty);
                return;
            }

            bool success = Reader.Open(Source, password);
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
        private void Pw_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                OnOpenFile(sender, e);
            }
        }
    }

    internal class ROWindowModel : INotifyPropertyChanged
    {
        private string _source;
        public string Source { get => _source; set { _source = value; NotifyPropertyChanged(nameof(Source)); } }
        public ROWindowModel()
        {
            _source = "";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
