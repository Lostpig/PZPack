using System.Windows;
using System.ComponentModel;
using PZPack.View.Service;
using System.IO;
using PZPack.Core;
using System.Threading;
using System;
using System.Diagnostics;
using PZPack.Core.Index;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace PZPack.View
{
    /// <summary>
    /// PackWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PackWindow : Window
    {
        private CancellationTokenSource? cancelSource;
        private readonly PackWindowModel model;
        private readonly IndexDesigner designer;

        public PackWindow(IndexDesigner designer)
        {
            InitializeComponent();

            model = new();
            DataContext = model;
            this.designer = designer;
        }

        private void OnChooseTarget(object sender, RoutedEventArgs e)
        {
            string? target = FileSystem.OpenSaveFileDialog("PZPack File (.pzpk)|*.pzpk", Config.Instance.LastSaveDirectory);
            if (target != null)
            {
                model.UpdateTarget(target);
            }
        }
        private void OnStart(object sender, RoutedEventArgs e)
        {
            model.Start();
            StartPacking();
        }
        private bool CheckSetting()
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                Alert.ShowWarning(Translate.MSG_Password_empty);
                return false;
            }
            if (string.IsNullOrWhiteSpace(model.Target))
            {
                Alert.ShowWarning(Translate.MSG_Output_file_empty);
                return false;
            }
            if (File.Exists(model.Target))
            {
                Alert.ShowWarning(string.Format(Translate.EX_OutputFileAlreadyExists, model.Target));
                return false;
            }

            return true;
        }
        private async void StartPacking()
        {
            if (!CheckSetting()) return;
            Config.Instance.LastSaveDirectory = Path.GetDirectoryName(model.Target) ?? "";

            model.UpdateState(PackState.Packing);
            cancelSource = new CancellationTokenSource();
            CancellationToken token = cancelSource.Token;

            ProgressReporter<PackProgressArg> reporter = new((args) =>
            {
                model.UpdateProgress(args);
            });

            try
            {
                var startTime = DateTime.Now;
                long size = await PZPacker.Pack(model.Target, designer, model.Password, model.BlockSize, reporter, token);
                var usedTime = DateTime.Now - startTime;

                Alert.ShowMessage(Translate.Packing_complete);
                model.UpdateComplete(size, usedTime);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Packing task canceled");
                Alert.ShowMessage(Translate.Packing_canceled);
                model.UpdateState(PackState.Setting);
            }
            catch(Exception ex)
            {
                Alert.ShowException(ex);
                model.UpdateState(PackState.Setting);
            }
            finally
            {
                cancelSource.Dispose();
                cancelSource = null;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            if (model.State != PackState.Packing)
            {
                Close();
            }
        }
        private void OnStop(object sender, RoutedEventArgs e)
        {
            cancelSource?.Cancel();
        }
    }

    enum PackState
    {
        Setting,
        Packing,
        Complete
    }
    internal class PackWindowModel : INotifyPropertyChanged
    {
        public PackState State { get; private set; } = PackState.Setting;
        public Visibility OptionPanelVisible { get => State == PackState.Setting ? Visibility.Visible : Visibility.Collapsed; }
        public Visibility ProgressPanelVisible { get => State != PackState.Setting ? Visibility.Visible : Visibility.Collapsed; }
        public Visibility PackingVisible { get => State == PackState.Packing ? Visibility.Visible : Visibility.Collapsed; }
        public Visibility CompleteVisible { get => State == PackState.Complete ? Visibility.Visible : Visibility.Collapsed; }
        public void UpdateState(PackState state)
        {
            State = state;
            NotifyPropertyChanged(nameof(OptionPanelVisible));
            NotifyPropertyChanged(nameof(ProgressPanelVisible));
            NotifyPropertyChanged(nameof(PackingVisible));
            NotifyPropertyChanged(nameof(CompleteVisible));
        }

        public DateTime startTime = DateTime.MinValue;
        public int updateInterval = 200;
        private long lastUpdateTime = 0;

        public string UsedTime { get; private set; } = "";
        public string FileCountText { get; private set; } = "";
        public string TotalProgressText { get; private set; } = "";
        public string FileProgressText { get; private set; } = "";
        public double TotalProgressValue { get; private set; } = 0;
        public double FileProgressValue { get; private set; } = 0;
        
        public void Start()
        {
            startTime = DateTime.Now;
        }
        public void UpdateProgress(PackProgressArg progress)
        {
            DateTime now = DateTime.Now;
            if (now.Ticks - lastUpdateTime < updateInterval) return;

            TimeSpan usedTime = now - startTime;
            UsedTime = Math.Floor(usedTime.TotalHours).ToString("f0") + usedTime.ToString(@"\:mm\:ss");
            FileCountText = $"{progress.ProcessedFileCount} / {progress.TotalFileCount}";

            string totalBytes = FileSystem.ComputeFileSize(progress.TotalBytes);
            string totalProcessed = FileSystem.ComputeFileSize(progress.TotalProcessedBytes);
            TotalProgressValue = ((double)progress.TotalProcessedBytes / progress.TotalBytes) * 100;
            TotalProgressText = $"{totalBytes} / {totalProcessed} ({TotalProgressValue:f1}%)";

            string fileBytes = FileSystem.ComputeFileSize(progress.CurrentProcessedBytes);
            string fileProcessed = FileSystem.ComputeFileSize(progress.CurrentBytes);
            FileProgressValue = ((double)progress.CurrentProcessedBytes / progress.CurrentBytes) * 100;
            FileProgressText = $"{fileBytes} / {fileProcessed} ({FileProgressValue:f1}%)";

            NotifyPropertyChanged(nameof(UsedTime));
            NotifyPropertyChanged(nameof(FileCountText));
            NotifyPropertyChanged(nameof(TotalProgressText));
            NotifyPropertyChanged(nameof(FileProgressText));
            NotifyPropertyChanged(nameof(TotalProgressValue));
            NotifyPropertyChanged(nameof(FileProgressValue));

            lastUpdateTime = now.Ticks;
        }

        private string _password = "";
        public string Password { get => _password; set { _password = value; NotifyPropertyChanged(nameof(Password)); } }
        private int _blockSize = 4 * 1024 * 1024;
        public int BlockSize { get => _blockSize; set { _blockSize = value; NotifyPropertyChanged(nameof(BlockSize)); } }

        public string CompleteSizeInfo { get; private set; } = "0.0 MB";
        public string CompleteTimeInfo { get; private set; } = "00:00:00";
        public string CompleteSpeedInfo { get; private set; } = "0.0 MB/s";
        public void UpdateComplete(long size, TimeSpan time)
        {
            CompleteSizeInfo = FileSystem.ComputeFileSize(size);
            CompleteTimeInfo = Math.Floor(time.TotalHours).ToString("f0") + time.ToString(@"\:mm\:ss");
            CompleteSpeedInfo = FileSystem.ComputeFileSize(size / time.TotalSeconds) + "/s";

            NotifyPropertyChanged(nameof(CompleteSizeInfo));
            NotifyPropertyChanged(nameof(CompleteTimeInfo));
            NotifyPropertyChanged(nameof(CompleteSpeedInfo));
            UpdateState(PackState.Complete);
        }

        public string Target { get; private set; } = "";
        public void UpdateTarget(string target)
        {
            Target = target;
            NotifyPropertyChanged(nameof(Target));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
