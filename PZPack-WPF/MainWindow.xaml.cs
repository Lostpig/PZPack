using System;
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
            Dialogs.OpenReadOptionWindow();
        }

        private void Reader_PZReaderChanged(object? sender, PZReaderChangeEventArgs e)
        {
            VModel.Update(e.Action == Service.PZReaderChangeAction.OPEN);
            mainContent.Update(e.Action == Service.PZReaderChangeAction.OPEN);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            Service.Reader.PZReaderChanged += Reader_PZReaderChanged; ;

            /// TEST: DELETE BEFORE RELEASE
            // Service.Reader.Open(@"D:\Media\pictures2.pzpk", "4294967296");
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Service.Reader.PZReaderChanged -= Reader_PZReaderChanged;
        }
    }

    enum AppState
    {
        WAIT, BROWSER, VIEWING
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

        public string FileSource { get; set; } = "-";
        public string FileSize { get; set; } = "0MB";
        public string FileInnerCounts { get; set; } = "0";
        public string FileDescription { get; set; } = "-";
        public string FileVersion { get; set; } = "-";

        public bool FileOpened
        {
            get
            {
                return State != AppState.WAIT;
            }
        }

        public void Update(bool fileOpen)
        {
            if (fileOpen == true)
            {
                State = AppState.BROWSER;
                LoadFileInfo();
            }
            else
            {
                State = AppState.WAIT;
                ClearFileInfo();
            }

            NotifyPropertyChanged(nameof(BackdropVisible));
            NotifyPropertyChanged(nameof(ContentVisible));
            NotifyPropertyChanged(nameof(FileOpened));
        }
        private void LoadFileInfo ()
        {
            if (Reader.Instance != null)
            {
                FileSource = Reader.Instance.Source;
                FileSize = FileSystem.ComputeFileSize(Reader.Instance.PackSize);
                FileInnerCounts = Reader.Instance.FileCount.ToString();
                FileDescription = Reader.Instance.Description;
                FileVersion = Reader.Instance.FileVersion.ToString();

                NotifyFileInfoUpdate();
            }
        }
        private void ClearFileInfo()
        {
            FileSource = "-";
            FileSize = "0.0MB";
            FileInnerCounts = "0";
            FileDescription = "-";
            FileVersion = "-";

            NotifyFileInfoUpdate();
        }
        private void NotifyFileInfoUpdate()
        {
            NotifyPropertyChanged(nameof(FileSource));
            NotifyPropertyChanged(nameof(FileSize));
            NotifyPropertyChanged(nameof(FileInnerCounts));
            NotifyPropertyChanged(nameof(FileDescription));
            NotifyPropertyChanged(nameof(FileVersion));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
