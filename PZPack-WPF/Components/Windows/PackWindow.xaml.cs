using System.Windows;
using PZPack.View.Service;
using System.IO;
using PZPack.Core;
using System.Threading;
using System;
using System.Diagnostics;
using PZPack.Core.Index;
using PZPack.View.Utils;
using PZPack.Core.Core;

namespace PZPack.View.Windows;

enum PackState
{
    Setting,
    Packing,
    Complete
}

/// <summary>
/// PackWindow.xaml 的交互逻辑
/// </summary>
public partial class PackWindow : Window
{
    private CancellationTokenSource? cancelSource;
    private readonly IndexDesigner designer;
    private readonly PackWindowModel model;

    public PackWindow(IndexDesigner designer)
    {
        InitializeComponent();

        model = new();
        this.DataContext = model;
        this.designer = designer;

        model.UpdateState(PackState.Setting);
    }

    private void OnChooseTarget(object sender, RoutedEventArgs e)
    {
        string? target = FileSystem.OpenSaveFileDialog("PZPack File (.pzpk)|*.pzpk", Config.Instance.LastSaveDirectory);
        if (target != null)
        {
            model.Options.UpdateTarget(target);
        }
    }
    private void OnStart(object sender, RoutedEventArgs e)
    {
        model.Packing.UpdateStartTime(DateTime.Now);
        StartPacking();
    }

    private async void StartPacking()
    {
        if (!model.Options.CheckSetting()) return;
        Config.Instance.LastSaveDirectory = Path.GetDirectoryName(model.Options.Target) ?? "";

        model.UpdateState(PackState.Packing);
        cancelSource = new CancellationTokenSource();
        CancellationToken token = cancelSource.Token;

        ProgressReporter<PackProgressArg> reporter = new((args) =>
        {
            model.Packing.UpdateProgress(args);
        });

        try
        {
            var startTime = DateTime.Now;
            var resizer = model.Options.GetImageResizer();
            long size = await PZPacker.Pack(model.Options.Target, designer, model.Options.Password, model.Options.BlockSize, resizer, reporter, token);
            var usedTime = DateTime.Now - startTime;

            Alert.ShowMessage(Translate.Packing_complete);
            model.UpdateState(PackState.Complete);
            model.Completed.UpdateComplete(size, usedTime);
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

internal class PackWindowModel : BaseNotifyPropertyChangedModel
{
    public PackState State { get; private set; } = PackState.Setting;
    public PackOptionsModel Options { get; init; }
    public PackingModel Packing { get; init; }
    public CompletedModel Completed { get; init; }

    public PackWindowModel()
    {
        Options = new();
        Packing = new();
        Completed = new();
    }

    public void UpdateState(PackState state)
    {
        State = state;
        if (State == PackState.Setting)
        {
            Options.SetPanelVisible(Visibility.Visible);
            Packing.SetPanelVisible(Visibility.Collapsed);
            Completed.SetPanelVisible(Visibility.Collapsed);
        }
        else if (State == PackState.Packing)
        {
            Options.SetPanelVisible(Visibility.Collapsed);
            Packing.SetPanelVisible(Visibility.Visible);
            Completed.SetPanelVisible(Visibility.Collapsed);
        }
        else
        {
            Options.SetPanelVisible(Visibility.Collapsed);
            Packing.SetPanelVisible(Visibility.Collapsed);
            Completed.SetPanelVisible(Visibility.Visible);
        }
    }
}

internal class PackOptionsModel : BaseNotifyPropertyChangedModel
{
    public Visibility PanelVisible { get; private set; } = Visibility.Collapsed;
    public void SetPanelVisible(Visibility visible)
    {
        PanelVisible = visible;
        NotifyPropertyChanged(nameof(PanelVisible));
    }

    public string Target { get; private set; } = "";
    public void UpdateTarget(string target)
    {
        Target = target;
        NotifyPropertyChanged(nameof(Target));
    }

    private string _password = "";
    public string Password { get => _password; set { _password = value; NotifyPropertyChanged(nameof(Password)); } }

    private int _blockSize = 4 * 1024 * 1024;
    public int BlockSize { get => _blockSize; set { _blockSize = value; NotifyPropertyChanged(nameof(BlockSize)); } }

    private ResizeImageFormat _resizeImage = ResizeImageFormat.Never;
    public ResizeImageFormat ResizeImage 
    { 
        get => _resizeImage; 
        set 
        { 
            _resizeImage = value; 
            NotifyPropertyChanged(nameof(ResizeImage));
            NotifyPropertyChanged(nameof(ImageMaxSizeVisible));
            NotifyPropertyChanged(nameof(QualityVisible));
            NotifyPropertyChanged(nameof(LosslessVisible));
        } 
    }

    private string _imageMaxSize = "2160";
    public string ImageMaxSize { get => _imageMaxSize; set { _imageMaxSize = value; NotifyPropertyChanged(nameof(ImageMaxSize)); } }

    private string _quality = "90";
    public string Quality { get => _quality; set { _quality = value; NotifyPropertyChanged(nameof(Quality)); } }

    private bool _lossless = false;
    public bool Lossless { get => _lossless; set { _lossless = value; NotifyPropertyChanged(nameof(Lossless)); } }

    // visibility properties
    public Visibility ImageMaxSizeVisible => ResizeImage != ResizeImageFormat.Never ? Visibility.Visible : Visibility.Hidden;
    public Visibility QualityVisible => (ResizeImage == ResizeImageFormat.JPEG || ResizeImage == ResizeImageFormat.WEBP) ? Visibility.Visible : Visibility.Hidden;
    public Visibility LosslessVisible => ResizeImage == ResizeImageFormat.WEBP ? Visibility.Visible : Visibility.Hidden;

    public bool CheckSetting()
    {
        if (string.IsNullOrWhiteSpace(Password))
        {
            Alert.ShowWarning(Translate.MSG_Password_empty);
            return false;
        }
        if (string.IsNullOrWhiteSpace(Target))
        {
            Alert.ShowWarning(Translate.MSG_Output_file_empty);
            return false;
        }
        if (File.Exists(Target))
        {
            Alert.ShowWarning(string.Format(Translate.EX_OutputFileAlreadyExists, Target));
            return false;
        }
        
        if (ResizeImage != ResizeImageFormat.Never)
        {
            if (GetMaxSize() == -1)
            {
                Alert.ShowWarning(Translate.MSG_Maxsize_Number);
                return false;
            }
            if ((ResizeImage == ResizeImageFormat.JPEG || ResizeImage == ResizeImageFormat.WEBP) && GetQuality() == -1)
            {
                Alert.ShowWarning(Translate.MSG_Quality_Number);
                return false;
            }
        }

        return true;
    }
    public int GetMaxSize()
    {
        if (int.TryParse(ImageMaxSize, out int maxSize))
        {
            return maxSize;
        }
        return -1;
    }
    public int GetQuality()
    {
        if (int.TryParse(Quality, out int quality))
        {
            if (quality >= 1 && quality <= 100)
            {
                return quality;
            }
        }
        return -1;
    }

    public ImageResizer? GetImageResizer()
    {
        if (ResizeImage == ResizeImageFormat.Never)
        {
            return null;
        }
        if (ResizeImage == ResizeImageFormat.JPEG) {
            return ImageResizer.CreateJpegResizer(GetMaxSize(), GetQuality());
        }
        if (ResizeImage == ResizeImageFormat.PNG)
        {
            return ImageResizer.CreatePngResizer(GetMaxSize());
        }
        if (ResizeImage == ResizeImageFormat.WEBP)
        {
            return ImageResizer.CreateWebpResizer(GetMaxSize(), Lossless, GetQuality());
        }

        return null;
    }
}

internal class PackingModel: BaseNotifyPropertyChangedModel
{
    public Visibility PanelVisible { get; private set; } = Visibility.Collapsed;
    public void SetPanelVisible(Visibility visible)
    {
        PanelVisible = visible;
        NotifyPropertyChanged(nameof(PanelVisible));
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

    public void UpdateStartTime(DateTime startTime)
    {
        this.startTime = startTime;
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
}

internal class CompletedModel : BaseNotifyPropertyChangedModel
{
    public Visibility PanelVisible { get; private set; } = Visibility.Collapsed;
    public void SetPanelVisible(Visibility visible)
    {
        PanelVisible = visible;
        NotifyPropertyChanged(nameof(PanelVisible));
    }

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
    }
}
