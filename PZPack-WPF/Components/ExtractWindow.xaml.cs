using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using PZPack.Core;
using PZPack.Core.Index;
using PZPack.View.Service;

namespace PZPack.View
{
    /// <summary>
    /// ExtractWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ExtractWindow : Window
    {
        CancellationTokenSource? cancelSource;
        readonly ExtractWindowModel model;
        public ExtractWindow()
        {
            InitializeComponent();

            model = new();
            DataContext = model;
        }

        public async void StartExtractFolder(PZFolder folder, string output)
        {
            if (Reader.Instance == null)
            {
                NoFileOpened();
                return;
            }

            ProgressReporter<ExtractProgressArg> reporter = new(model.UpdateProgress);
            cancelSource = new CancellationTokenSource();
            CancellationToken ctoken = cancelSource.Token;

            try
            {
                var startTime = DateTime.Now;
                long size = await Reader.Instance.ExtractBatchAsync(folder, output, reporter, ctoken);
                var usedTime = DateTime.Now - startTime;

                Alert.ShowMessage(Translate.Extracted_complete);
                model.UpdateComplete(size, usedTime);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Extract task canceled");
                Alert.ShowMessage(Translate.Extracted_canceled);
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Extract task failed");
                Alert.ShowException(ex);
                Close();
            }
            finally
            {
                cancelSource.Dispose();
                cancelSource = null;
            }
        }
        public async void StartExtractFile(PZFile file, string output)
        {
            if (Reader.Instance == null)
            {
                NoFileOpened();
                return;
            }

            ProgressReporter<ExtractProgressArg> reporter = new(model.UpdateProgress);
            cancelSource = new CancellationTokenSource();
            CancellationToken ctoken = cancelSource.Token;

            try
            {
                var startTime = DateTime.Now;
                long length = await Reader.Instance.ExtractFileAsync(file, output, reporter, ctoken);
                var usedTime = DateTime.Now - startTime;

                Alert.ShowMessage(Translate.Extracted_complete);
                model.UpdateComplete(file.Size, usedTime);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Extract task canceled");
                Alert.ShowMessage(Translate.Extracted_canceled);
                Close();
            }
            catch(Exception ex)
            {
                Alert.ShowException(ex);
                Close();
            }
            finally
            {
                cancelSource.Dispose();
                cancelSource = null;
            }
        }
        private void NoFileOpened()
        {
            Alert.ShowWarning(Translate.EX_PZFileNotOpened);
            Close();
        }

        private void OnStop(object sender, RoutedEventArgs e)
        {
            cancelSource?.Cancel();
        }
        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    internal class ExtractWindowModel : INotifyPropertyChanged
    {
        public bool IsComplete { get; private set; }
        public Visibility ExtractingVisible { get => IsComplete ? Visibility.Collapsed : Visibility.Visible; }
        public Visibility CompleteVisible { get => IsComplete ? Visibility.Visible : Visibility.Collapsed; }
        public void UpdateState(bool complete)
        {
            IsComplete = complete;
            NotifyPropertyChanged(nameof(ExtractingVisible));
            NotifyPropertyChanged(nameof(CompleteVisible));
        }

        public string ProgressText { get; private set; } = "";
        public string FileProgressText { get; private set; } = "";
        public double ProgressValue { get; private set; } = 0;
        public double FileProgressValue { get; private set; } = 0;

        public void UpdateProgress(ExtractProgressArg arg)
        {
            var currentTotal = arg.CurrentBytes == 0 ? 1 : arg.CurrentBytes;
            ProgressValue = (double)arg.TotalProcessedBytes / arg.TotalBytes * 100;
            FileProgressValue = (double)arg.CurrentProcessedBytes / currentTotal * 100;

            ProgressText = $"{arg.ProcessedFileCount} / {arg.TotalFileCount} ({ProgressValue:f0}%)";
            FileProgressText = $"{FileSystem.ComputeFileSize(arg.CurrentProcessedBytes)} / {FileSystem.ComputeFileSize(arg.CurrentBytes)} ({FileProgressValue:f0}%)";
            
            NotifyPropertyChanged(nameof(ProgressText));
            NotifyPropertyChanged(nameof(ProgressValue));
            NotifyPropertyChanged(nameof(FileProgressText));
        }

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
            UpdateState(true);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
