﻿using System;
using System.Windows;
using System.ComponentModel;
using PZPack.View.Service;

namespace PZPack.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly MainWindowModel VModel;
        public MainWindow()
        {
            InitializeComponent();
            VModel = new MainWindowModel();
            this.DataContext = VModel;
        }

        private void OnOpenFile(object sender, RoutedEventArgs e)
        {
            Dialogs.TryOpenPZFile();
        }

        private void Reader_PZReaderChanged(object? sender, PZReaderChangeEventArgs e)
        {
            VModel.Update(e);
            if (e.Type == Core.PZTypes.PZPACK || e.Type == Core.PZTypes.PZVIDEO)
            {
                mainContent.Visibility = Visibility.Visible;
                mainContent.Update(true);
            }
            else
            {
                mainContent.Visibility = Visibility.Collapsed;
                mainContent.Update(false);
            }

            Dialogs.CloseViewWindow();
        }
        private void PWBook_PWBookChanged(object? sender, PZPwBookChangeEventArgs e)
        {
            VModel.SetPwBookOpened(e.Opened);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            Service.Reader.PZReaderChanged += Reader_PZReaderChanged;
            Service.PWBook.PWBookChanged += PWBook_PWBookChanged;
            mainContent.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Service.Dialogs.CloseViewWindow();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Service.Reader.PZReaderChanged -= Reader_PZReaderChanged;
            Service.PWBook.PWBookChanged -= PWBook_PWBookChanged;
        }
    }

    enum AppState
    {
        WAIT, BROWSER, BROWSERVIDEO, VIEWING
    }

    internal class MainWindowModel : INotifyPropertyChanged
    {
        private AppState State = AppState.WAIT;
        public Visibility BackdropVisible
        {
            get
            {
                return State == AppState.WAIT ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility ContentVisible
        {
            get
            {
                return State == AppState.BROWSER ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        public Visibility VideoContentVisible
        {
            get
            {
                return State == AppState.BROWSERVIDEO ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string FileSource { get; set; } = "-";
        public string FileSize { get; set; } = "0MB";
        public string FileVersion { get; set; } = "-";

        public bool FileOpened
        {
            get
            {
                return State != AppState.WAIT;
            }
        }

        public void SetPwBookOpened(bool opened)
        {
            _pwbookOpened = opened;
            NotifyPropertyChanged(nameof(PWBookOpened));
        }
        private bool _pwbookOpened = false;
        public bool PWBookOpened
        {
            get
            {
                return _pwbookOpened;
            }
        }

        public void Update(PZReaderChangeEventArgs e)
        {
            if (e.Action == Service.PZReaderChangeAction.OPEN)
            {
                State = e.Type == Core.PZTypes.PZPACK ? AppState.BROWSER : AppState.BROWSERVIDEO;
                LoadFileInfo();
            }
            else
            {
                State = AppState.WAIT;
                ClearFileInfo();
            }

            NotifyPropertyChanged(nameof(BackdropVisible));
            NotifyPropertyChanged(nameof(ContentVisible));
            NotifyPropertyChanged(nameof(VideoContentVisible));
            NotifyPropertyChanged(nameof(FileOpened));
        }
        private void LoadFileInfo()
        {
            if (Reader.Instance != null)
            {
                FileSource = Reader.Instance.Source;
                FileSize = FileSystem.ComputeFileSize(Reader.Instance.Info.FileSize);
                FileVersion = Reader.Instance.Version.ToString();

                NotifyFileInfoUpdate();
            }
        }
        private void ClearFileInfo()
        {
            FileSource = "-";
            FileSize = "0.0MB";
            FileVersion = "-";

            NotifyFileInfoUpdate();
        }
        private void NotifyFileInfoUpdate()
        {
            NotifyPropertyChanged(nameof(FileSource));
            NotifyPropertyChanged(nameof(FileSize));
            NotifyPropertyChanged(nameof(FileVersion));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
