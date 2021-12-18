﻿using System;
using System.ComponentModel;
using System.Windows;
using PZPack.View.Service;

namespace PZPack.View
{
    /// <summary>
    /// ReadOptionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ReadOptionWindow : Window
    {
        readonly private ROWindowModel VModel;
        public ReadOptionWindow()
        {
            InitializeComponent();
            VModel = new ROWindowModel();
            this.DataContext = VModel;
        }

        private void OnSelectFile(object sender, RoutedEventArgs e)
        {
            string? path = FileSystem.OpenSelectFileDialog();
            if (path != null)
            {
                VModel.Source = path;
            }
        }
        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            string password = VModel.Password;
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is empty", "Warning", MessageBoxButton.OK);
                return;
            }

            DialogResult = true;
            Close();
        }
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public string Source { get => VModel.Source; }
        public string Password { get => VModel.Password; }
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
