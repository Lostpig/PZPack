using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PZPack.View.Service;

namespace PZPack.View.Windows
{
    /// <summary>
    /// OpenPwBookWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OpenPwBookWindow : Window
    {
        private readonly bool IsCreate;
        readonly private OPBWindowModel VModel;
        public OpenPwBookWindow(string source, bool isCreate)
        {
            InitializeComponent();
            IsCreate = isCreate;

            VModel = new()
            {
                Source = source
            };
            DataContext = VModel;
        }

        private void OnOpen(object sender, RoutedEventArgs e)
        {
            string password = passwordCtrl.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                Alert.ShowMessage(Translate.MSG_Password_empty);
                return;
            }

            try
            {
                if (IsCreate) PWBook.Create(VModel.Source, password);
                else PWBook.Load(VModel.Source, password);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alert.ShowException(ex);
            }
        }
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void Pw_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnOpen(sender, e);
            }
        }
    }

    internal class OPBWindowModel : INotifyPropertyChanged
    {

        private string _source;
        public string Source { get => _source; set { _source = value; NotifyPropertyChanged(nameof(Source)); } }
        public OPBWindowModel()
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
