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
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace PZPack.View.Windows
{
    /// <summary>
    /// MediaPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MediaPlayerWindow : Window
    {
        readonly MediaPlayerWindowModel viewModel;
        List<PZFile> files;
        PZFile currentFile;
        PZPKMediaStream stream;

        public MediaPlayerWindow()
        {
            InitializeComponent();

            viewModel = new MediaPlayerWindowModel(Media);
            DataContext = viewModel;

            InitializeController();
            BindHotkey();
        }

        public void OpenMediaFile(PZFile file, List<PZFile> files)
        {
            if (Reader.Instance == null)
            {
                throw new Exception(Translate.EX_PZFileNotOpened);
            }


            this.files = files;

            PlayFile(file);
        }

        public async void PlayFile(PZFile file)
        {
            if (stream != null)
            {
                await Media.Close();
                stream.Dispose();
            }

            currentFile = file;
            stream = new PZPKMediaStream(currentFile, Reader.Instance);
            await Media.Open(stream);

            int currentIndex = files.IndexOf(currentFile);

            if (WindowState != WindowState.Maximized)
            {
                Height = Media.NaturalVideoHeight;
                Width = Media.NaturalVideoWidth;
            }

            viewModel.UpdateState(currentIndex + 1, files.Count);
            Title = currentFile.Name;

            Media.Play();
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
            ControllerPanel.ChangeFile += OnChangeFile;

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

        private void OnChangeFile(int move)
        {
            if (files.Count <= 1) return;

            int currentIndex = files.IndexOf(currentFile);

            if (move > 0)
            {
                int next = currentIndex + 1;
                if (next < files.Count) PlayFile(files[next]);
            }
            else if (move < 0)
            {
                int prev = currentIndex - 1;
                if (prev >= 0) PlayFile(files[prev]);
            }
        }
        #endregion


        private DispatcherTimer KeyDownTimer;
        private Key? CurrentDownKey;
        private void BindHotkey()
        {
            KeyDownTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(333),
                IsEnabled = true,
            };

            KeyDown += (e, s) =>
            {
                if (s.Key == Key.Left || s.Key == Key.Right)
                {
                    CurrentDownKey = s.Key;
                    PlayMove(CurrentDownKey.Value);
                    KeyDownTimer.Start();
                }
            };
            KeyUp += (e, s) =>
            {
                if (s.Key == Key.Left || s.Key == Key.Right)
                {
                    if (s.Key == CurrentDownKey)
                    {
                        CurrentDownKey = null;
                        KeyDownTimer.Stop();
                    }
                }
            };
            KeyDownTimer.Tick += (e, s) =>
            {
                if (CurrentDownKey.HasValue)
                {
                    PlayMove(CurrentDownKey.Value);
                }
                else
                {
                    KeyDownTimer.Stop();
                }
            };
            LostFocus += (e, s) =>
            {
                if (CurrentDownKey != null)
                {
                    CurrentDownKey = null;
                    KeyDownTimer.Stop();
                }
            };
        }
        private void PlayMove(Key key)
        {
            if (!Media.IsOpen) return;

            TimeSpan? total = Media.NaturalDuration;
            if (total.HasValue)
            {
                var current = Media.Position;

                if (key == Key.Right) current += TimeSpan.FromSeconds(5);
                else if (key == Key.Left) current -= TimeSpan.FromSeconds(5);

                if (current < TimeSpan.Zero) current = TimeSpan.Zero;
                else if (current > total) current = total.Value;

                Media.Seek(current);
            }
        }


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

        public string PlayMedias { get; set; }

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

        public void UpdateState(int current, int total)
        {
            m_IsPlaying = m_MediaElement.IsPlaying;
            PlayMedias = $"{current} / {total}";

            NotifyPropertyChanged(nameof(IsPlaying));
            NotifyPropertyChanged(nameof(PlayMedias));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
