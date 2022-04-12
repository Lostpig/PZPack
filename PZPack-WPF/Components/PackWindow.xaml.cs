using System.Windows;
using System.ComponentModel;
using PZPack.View.Service;
using System.IO;
using System.Threading.Tasks;
using PZPack.Core;
using System.Threading;
using System;
using System.Diagnostics;

namespace PZPack.View
{
    /// <summary>
    /// PackWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PackWindow : Window
    {
        private CancellationTokenSource? cancelSource;
        private readonly PackWindowModel model;
        public PackWindow()
        {
            InitializeComponent();

            model = new();
            DataContext = model;
        }

        private void OnChooseSource(object sender, RoutedEventArgs e)
        {
            string? source = FileSystem.OpenSelectDirectryDialog();
            if (source != null)
            {
                model.UpdateSource(source);
            }
        }
        private void OnChooseTarget(object sender, RoutedEventArgs e)
        {
            string? target = FileSystem.OpenSaveFileDialog("PZPack File (.pzpk)|*.pzpk");
            if (target != null)
            {
                model.UpdateTarget(target);
            }
        }
        private void OnStart(object sender, RoutedEventArgs e)
        {
            model.UpdateProgress(0, 1, 0, 1);
            StartPacking();
        }
        private bool CheckSetting()
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                Alert.ShowWarning(Translate.MSG_Password_empty);
                return false;
            }
            if (string.IsNullOrWhiteSpace(model.Source))
            {
                Alert.ShowWarning(Translate.MSG_Source_directory_empty);
                return false;
            }
            if (!Directory.Exists(model.Source))
            {
                Alert.ShowWarning(string.Format(Translate.EX_DirectoryNotFound, model.Source));
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

            model.UpdateState(PackState.Packing);
            cancelSource = new CancellationTokenSource();
            CancellationToken token = cancelSource.Token;

            ProgressReporter<(int, int, long, long)> reporter = new((n) =>
            {
                (int count, int total, long fileUsed, long fileTotal) = n;
                model.UpdateProgress(count, total, fileUsed, fileTotal);
            });
            PZPackInfo info = new(model.Password, model.Remark);

            try
            {
                var startTime = DateTime.Now;
                long size = await PZPacker.Pack(model.Source, model.Target, info, reporter, token);
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

        public string ProgressText { get; private set; } = "";
        public string FileProgressText { get; private set; } = "";
        public double ProgressValue { get; private set; } = 0;
        public void UpdateProgress(int count, int total, long fileUsed, long fileTotal)
        {
            total = total == 0 ? 1 : total;
            ProgressValue = ((double)count / total) * 100;
            ProgressText = $"{count} / {total} ({ProgressValue:f0}%)";

            fileTotal = fileTotal == 0 ? 1 : fileTotal;
            double filePercent = ((double)fileUsed / fileTotal) * 100;
            FileProgressText = $"{FileSystem.ComputeFileSize(fileUsed)} / {FileSystem.ComputeFileSize(fileTotal)} ({filePercent:f0}%)";
            NotifyPropertyChanged(nameof(ProgressText));
            NotifyPropertyChanged(nameof(ProgressValue));
            NotifyPropertyChanged(nameof(FileProgressText));
        }

        private string _password = "";
        public string Password { get => _password; set { _password = value; NotifyPropertyChanged(nameof(Password)); } }

        private string _remark = "";
        public string Remark { get => _remark; set { _remark = value; NotifyPropertyChanged(nameof(Remark)); } }

        public string CompleteSizeInfo { get; private set; } = "0.0 MB";
        public string CompleteTimeInfo { get; private set; } = "00:00:00.0";
        public string CompleteSpeedInfo { get; private set; } = "0.0 MB/s";
        public void UpdateComplete(long size, TimeSpan time)
        {
            CompleteSizeInfo = FileSystem.ComputeFileSize(size);
            CompleteTimeInfo = Math.Floor(time.TotalHours).ToString("f0") + time.ToString(@"\:mm\:ss\.f");
            CompleteSpeedInfo = FileSystem.ComputeFileSize(size / time.TotalSeconds) + "/s";

            NotifyPropertyChanged(nameof(CompleteSizeInfo));
            NotifyPropertyChanged(nameof(CompleteTimeInfo));
            NotifyPropertyChanged(nameof(CompleteSpeedInfo));
            UpdateState(PackState.Complete);
        }

        public string Target { get; private set; } = "";
        public string Source { get; private set; } = "";
        public void UpdateTarget(string target)
        {
            Target = target;
            NotifyPropertyChanged(nameof(Target));
        }
        public void UpdateSource(string source)
        {
            Source = source;
            NotifyPropertyChanged(nameof(Source));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
