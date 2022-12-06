using System.ComponentModel;
using System.Windows;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using PZPack.Core.Index;
using PZPack.Core.Utility;
using PZPack.View.Service;
using System.IO;
using System.Net.Security;
using System;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace PZPack.View
{
    /// <summary>
    /// VideoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoWindow : Window
    {
        static LibVLC? _libVLC = null;
        static MediaPlayer? _mediaPlayer = null;
        static MediaPlayer GetMediaPlayer()
        {
            if (_mediaPlayer is null)
            {
                LibVLCSharp.Shared.Core.Initialize();
                _libVLC = new LibVLC(enableDebugLogs: false);
                _mediaPlayer = new MediaPlayer(_libVLC);
            }

            return _mediaPlayer;
        }

        bool vlcLoaded = false;
        PZFile? _file;
        Stream? useStream;
        Media? useMedia;
        PlayModel vModle;

        public VideoWindow()
        {
            InitializeComponent();
            videoView.Loaded += VideoView_Loaded;

            vModle = new PlayModel();
            DataContext = vModle;
        }

        private void VideoView_Loaded(object sender, RoutedEventArgs e)
        {
            videoView.MediaPlayer = GetMediaPlayer();

            vlcLoaded = true;
            if (_file is not null)
            {
                PlayFile(_file);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (_mediaPlayer is not null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Media = null;
                _mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
            }

            useMedia?.Dispose();
            useStream?.Dispose();
            useMedia = null;
            useStream = null;
        }

        public void SetFile(PZFile file)
        {
            _file = file;
            if (vlcLoaded)
            {
                PlayFile(_file);
            }
        }
        private void PlayFile(PZFile file)
        {
            if (_mediaPlayer is not null && _libVLC is not null)
            {
                PZFileStream stream = Reader.Instance!.GetFileStream(file);
                useStream = stream;

                StreamMediaInput streamMedia = new StreamMediaInput(useStream);
                useMedia = new Media(_libVLC, streamMedia);
                useMedia.ParsedChanged += UseMedia_ParsedChanged;

                _mediaPlayer.Play(useMedia);
                _mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
            }
        }

        private void MediaPlayer_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            vModle.Time = e.Time / 1000.0;
        }
        private void UseMedia_ParsedChanged(object? sender, MediaParsedChangedEventArgs e)
        {
            if (e.ParsedStatus == MediaParsedStatus.Done)
            {
                Console.WriteLine("Parsed Media Duration: " + useMedia!.Duration);
                vModle.Duration = useMedia!.Duration / 1000.0;
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer is not null && !_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Play();
            }

        }
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer is not null && _mediaPlayer.CanPause)
            {
                _mediaPlayer.Pause();
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer is not null)
            {
                _mediaPlayer.Stop();
            }
        }

        private void ProgressBar_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_mediaPlayer is not null && _mediaPlayer.IsSeekable)
            {
                ProgressBar bar = (ProgressBar)sender;

                var point = e.GetPosition(bar);
                var perc = point.X / bar.ActualWidth;

                var time = vModle.Duration * perc;
                _mediaPlayer!.Time = (long)(time * 1000);
            }
        }
    }

    public class PlayModel : INotifyPropertyChanged
    {
        double duration = 10;
        public double Duration
        {
            get => duration;
            set {
                duration = value;
                minuteLen = duration > 6000 ? "d3" : "d2";
                durationText = formatTime(duration);
                NotifyPropertyChanged(nameof(Duration));
            }
        }
        double time = 0;
        public double Time
        {
            get => time;
            set
            {
                time = value;
                UpdateTime();
            }
        }

        string minuteLen = "d2";
        string durationText = "00:00";
        string timeText = "00:00";
        public string TimeText
        {
            get => $"{timeText} / {durationText}";
        }

        double oldTime = 0;
        private string formatTime(double val)
        {
            var intVal = (int)val;
            return (intVal / 60).ToString(minuteLen) + ":" + (intVal % 60).ToString("d2");
        }
        private void UpdateTime()
        {
            if (time < oldTime || time - oldTime > 0.33)
            {
                timeText = formatTime(Time); ;
                NotifyPropertyChanged(nameof(TimeText));
                NotifyPropertyChanged(nameof(Time));

                oldTime = time;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
