using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;
using PZPack.Core;
using PZPack.View.Service;
using System.IO;
using System.ComponentModel;
using System;
using System.Windows.Input;

namespace PZPack.View
{
    /// <summary>
    /// ViewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ViewWindow : Window
    {
        private PZFile? current;
        private bool fileChangeFlag = false;
        private int index = 0;
        private readonly List<PZFile> list;
        private readonly ViewWindowModel model;
        private double scale = 1;
        static readonly double[] scalelist = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5 };
        public ViewWindow()
        {
            InitializeComponent();
            model = new ViewWindowModel();
            list = new List<PZFile>();

            this.DataContext = model;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            scrollContent.LayoutUpdated += ScrollContent_LayoutUpdated;
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            scrollContent.LayoutUpdated -= ScrollContent_LayoutUpdated;
        }

        private void ScrollContent_LayoutUpdated(object? sender, EventArgs e)
        {
            if (!fileChangeFlag) return;

            scrollContent.ScrollToVerticalOffset(0);
            if (scrollContent.ScrollableWidth > 0)
            {
                scrollContent.ScrollToHorizontalOffset(scrollContent.ScrollableWidth / 2);
            }
            fileChangeFlag = false;
        }

        public void BindFiles(PZFile file, PZFile[] files)
        {
            index = Array.IndexOf(files, file);
            index = index < 0 ? 0 : index;

            list.Clear();
            list.AddRange(files);
            LoadImage();
            UpdateButtonState();
        }
        async void LoadImage()
        {
            PZFile newFile = list[index];
            if (current != null)
            {
                if (current.FolderId == newFile.FolderId && current.Name == newFile.Name) return;
            }

            current = newFile;
            if (Reader.Instance != null)
            {
                byte[] bytes = await Reader.Instance.ReadFile(newFile);
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.EndInit();

                viewImage.Source = bitmap;
                model.OriginSize = new Size(bitmap.PixelWidth, bitmap.PixelHeight);

                scale = model.LockScale ? scale : 1;
                UpdateImageScale();
                model.ChangeFile(newFile.Name, index + 1, list.Count, bytes.Length);

                fileChangeFlag = true;
            }
        }
        void Move(int offset)
        {
            int after = index + offset;
            if (after < 0) after = 0;
            if (after >= list.Count) after = list.Count - 1;

            if (index != after)
            {
                index = after;
                LoadImage();
                UpdateButtonState();
            }
        }
        void UpdateButtonState()
        {
            btnPrev.IsEnabled = index > 0;
            btnNext.IsEnabled = index < list.Count - 1;
        }
        void UpdateImageScale()
        {
            double newWidth = model.OriginSize.Width * scale;
            double newHeight = model.OriginSize.Height * scale;

            Size newSize = new(newWidth, newHeight);
            viewImage.Width = newSize.Width;
            viewImage.Height = newSize.Height;
            model.Size = newSize;
            scaleBox.Text = (scale * 100).ToString("f0");
        }

        private void ChangeSizeUp(object sender, RoutedEventArgs e)
        {
            if (scale > scalelist[^1]) return;

            double newScale = 0;
            for (int i = 0; i < scalelist.Length; i++)
            {
                if (i == scalelist.Length - 1)
                {
                    newScale = scalelist[i];
                    break;
                }
                if (scale < scalelist[i] - 0.001)
                {
                    newScale = scalelist[i];
                    break;
                }
            }
            if (Math.Abs(newScale - scale) < 0.001) return;
            scale = newScale;
            UpdateImageScale();
        }
        private void ChangeSizeDown(object sender, RoutedEventArgs e)
        {
            if (scale < scalelist[0]) return;

            double newScale = 0;
            for (int i = scalelist.Length - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    newScale = scalelist[i];
                    break;
                }
                if (scale > scalelist[i] + 0.001)
                {
                    newScale = scalelist[i];
                    break;
                }
            }
            if (Math.Abs(newScale - scale) < 0.001) return;
            scale = newScale;
            UpdateImageScale();
        }
        private void ChangeSizeToOriginal(object sender, RoutedEventArgs e)
        {
            scale = 1;
            UpdateImageScale();
        }
        
        private void NextFile(object sender, RoutedEventArgs e)
        {
            Move(1);
        }
        private void PrevFile(object sender, RoutedEventArgs e)
        {
            Move(-1);
        }

        struct MouseState
        {
            public double LastX = 0;
            public double LastY = 0;
            public bool Active = false;
        }
        private MouseState Mstate = new();
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.XButton1://Back button
                    PrevFile(sender, e);
                    break;
                case MouseButton.XButton2://forward button
                    NextFile(sender, e);
                    break;
                case MouseButton.Right:
                    Mstate.Active = true;
                    Point pos = e.GetPosition(scrollContent);
                    Mstate.LastX = pos.X;
                    Mstate.LastY = pos.Y;
                    break;
                default:
                    break;
            }
        }
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                case MouseButton.Right:
                    Mstate.Active = false; break;
                default:
                    break;
            }
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!Mstate.Active) return;
            Point pos = e.GetPosition(scrollContent);

            double hmove = pos.X - Mstate.LastX;
            double hnext = scrollContent.HorizontalOffset - hmove;
            if (hnext > 0 && hnext < scrollContent.ScrollableWidth)
            {
                scrollContent.ScrollToHorizontalOffset(hnext);
            }

            double vmove = pos.Y - Mstate.LastY;
            double vnext = scrollContent.VerticalOffset - vmove;
            if (vnext > 0 && vnext < scrollContent.ScrollableHeight)
            {
                scrollContent.ScrollToVerticalOffset(vnext);
            }

            Mstate.LastX = pos.X;
            Mstate.LastY = pos.Y;
        }

        private void EnterScale()
        {
            string scaleStr = scaleBox.Text;
            bool success = Double.TryParse(scaleStr, out double scalePrec);
            if (success)
            {
                double newScale = scalePrec / 100;
                if (Math.Abs(newScale - scale) > 0.0001)
                {
                    scale = newScale;
                    UpdateImageScale();
                }
            }
            else
            {
                scaleBox.Text = (scale * 100).ToString("f0");
            }
        }
        private void OnScaleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EnterScale();
            }
        }
        private void OnScaleLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            scaleBox.Text = (scale * 100).ToString("f0");
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (e.Delta > 0)
            {
                ChangeSizeUp(sender, e);
            }
            else
            {
                ChangeSizeDown(sender, e);
            }
        }
    }

    internal class ViewWindowModel : INotifyPropertyChanged
    {
        private Size _size;
        private Size _originSize;
        public Size Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    UpdateSize();
                }
            }
        }
        public Size OriginSize
        {
            get => _originSize;
            set
            {
                if (_originSize != value)
                {
                    _originSize = value;
                    UpdateSize();
                }
            }
        }
        public double Scale
        {
            get
            {
                if (_size.IsEmpty || _originSize.IsEmpty) return 0;
                if (_originSize.Width == 0) return 0;
                return _size.Width / _originSize.Width;
            }
        }
        public string ScalePercent
        {
            get => (Scale * 100).ToString("f1");
        }
        public string FileName { get; set; }
        public int CurrentIndex { get; set; }
        public int FileCount { get; set; }
        public string SizeText { get; set; } = "0 * 0";
        public string FileSize { get; set; } = "0MB";

        private bool _lockScale = false;
        public bool LockScale
        {
            get => _lockScale;
            set
            {
                if (_lockScale != value)
                {
                    _lockScale = value;
                    NotifyPropertyChanged(nameof(LockScale));
                }
            }
        }

        public ViewWindowModel()
        {
            _size = new Size(0, 0);
            _originSize = new Size(0, 0);

            FileName = "";
            CurrentIndex = 0;
            FileCount = 0;
        }
        private void UpdateSize()
        {
            SizeText = OriginSize.Width.ToString("f0") + " * " + OriginSize.Height.ToString("f0");

            NotifyPropertyChanged(nameof(Size));
            NotifyPropertyChanged(nameof(OriginSize));
            NotifyPropertyChanged(nameof(Scale));
            NotifyPropertyChanged(nameof(ScalePercent));
            NotifyPropertyChanged(nameof(SizeText));
        }
        public void ChangeFile(string name, int index, int count, int bufferLength)
        {
            FileName = name;
            CurrentIndex = index;
            FileCount = count;
            FileSize = FileSystem.ComputeFileSize(bufferLength);

            NotifyPropertyChanged(nameof(FileName));
            NotifyPropertyChanged(nameof(CurrentIndex));
            NotifyPropertyChanged(nameof(FileCount));
            NotifyPropertyChanged(nameof(FileSize));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
