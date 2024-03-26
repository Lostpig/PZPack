using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Unosquare.FFME;

namespace PZPack.View.Windows
{
    /// <summary>
    /// MediaPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MediaPlayerWindow : Window
    {
        readonly PZFile videoFile;
        readonly PZPKMediaStream stream;
        readonly MediaPlayerWindowModel viewModel;

        public MediaPlayerWindow(PZFile file)
        {
            videoFile = file;
            if (Reader.Instance == null)
            {
                throw new Exception(Translate.EX_PZFileNotOpened);
            }

            InitializeComponent();

            stream = new PZPKMediaStream(videoFile, Reader.Instance);

            viewModel = new MediaPlayerWindowModel(Media);
            DataContext = viewModel;

            InitializeMedia();
            InitializeController();
        }

        private async void InitializeMedia()
        {
            // Media.LoadedBehavior = Unosquare.FFME.Common.MediaPlaybackState.Play;
            await Media.Open(stream);

            viewModel.UpdateState();
            Title = videoFile.Name;
        }

        #region controller
        private Storyboard HideControllerAnimation => FindResource("HideControlOpacity") as Storyboard;
        private Storyboard ShowControllerAnimation => FindResource("ShowControlOpacity") as Storyboard;

        private bool IsControllerHideCompleted;
        private DateTime LastMouseMoveTime;
        private Point LastMousePosition;
        private DispatcherTimer MouseMoveTimer;
        private void InitializeController()
        {
            Loaded += (s, e) =>
            {
                Storyboard.SetTarget(HideControllerAnimation, ControllerPanel);
                Storyboard.SetTarget(ShowControllerAnimation, ControllerPanel);

                HideControllerAnimation.Completed += (es, ee) =>
                {
                    ControllerPanel.Visibility = Visibility.Hidden;
                    IsControllerHideCompleted = true;
                };

                ShowControllerAnimation.Completed += (es, ee) =>
                {
                    IsControllerHideCompleted = false;
                };
            };

            MouseMove += (s, e) =>
            {
                var currentPosition = e.GetPosition(window);
                if (Math.Abs(currentPosition.X - LastMousePosition.X) > double.Epsilon ||
                    Math.Abs(currentPosition.Y - LastMousePosition.Y) > double.Epsilon)
                    LastMouseMoveTime = DateTime.UtcNow;

                LastMousePosition = currentPosition;
            };

            MouseLeave += (s, e) =>
            {
                LastMouseMoveTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(10));
            };

            MouseMoveTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150),
                IsEnabled = true
            };

            MouseMoveTimer.Tick += (s, e) =>
            {
                var elapsedSinceMouseMove = DateTime.UtcNow.Subtract(LastMouseMoveTime);
                if (elapsedSinceMouseMove.TotalMilliseconds >= 3000 && Media.IsOpen && ControllerPanel.IsMouseOver == false)
                {
                    if (IsControllerHideCompleted) return;
                    Cursor = Cursors.None;
                    HideControllerAnimation?.Begin();
                    IsControllerHideCompleted = false;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                    ControllerPanel.Visibility = Visibility.Visible;
                    ShowControllerAnimation?.Begin();
                }
            };

            MouseMoveTimer.Start();
        }
        #endregion

        protected override async void OnClosing(CancelEventArgs e)
        {
            await Media.Close();
            Media.Dispose();
            stream?.Dispose();

            base.OnClosing(e);
        }
    }

    internal class MediaPlayerWindowModel: INotifyPropertyChanged
    {
        private readonly MediaElement m_MediaElement;
        public MediaElement MediaElement => m_MediaElement;

        private bool m_IsPlaying = false;
        public bool IsPlaying => m_IsPlaying;

        public MediaPlayerWindowModel(MediaElement media)
        {
            m_MediaElement = media;
            m_MediaElement.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(m_MediaElement.IsPlaying))
                {
                    m_IsPlaying = m_MediaElement.IsPlaying;
                    NotifyPropertyChanged(nameof(IsPlaying));
                }
            };

            // media.Volume
        }

        public void UpdateState()
        {
            m_IsPlaying = m_MediaElement.IsPlaying;
            NotifyPropertyChanged(nameof(IsPlaying));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
