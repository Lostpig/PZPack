using PZPack.View.Service;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PZPack.View
{
    /// <summary>
    /// MakeFolderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MakeNameWindow : Window
    {
        public string ResultName = "";

        public MakeNameWindow(string defaultName = "")
        {
            InitializeComponent();
            this.DataContext = new MakeNameWindowModel(defaultName);
        }
        private MakeNameWindowModel VModel => (MakeNameWindowModel)DataContext;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            textBox.Focus();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            string name = VModel.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                Alert.ShowMessage(Translate.MSG_Folder_name_empty);
                return;
            }

            ResultName = name;
            DialogResult = true;
            Close();
        }
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnTextKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnOk(sender, e);
            }
        }
    }

    internal class MakeNameWindowModel : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get => _name; set { _name = value; NotifyPropertyChanged(nameof(Name)); } }
        public MakeNameWindowModel(string defaultName)
        {
            _name = defaultName;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
