using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;

namespace PZPack.View
{
    /// <summary>
    /// VideoExplorer.xaml 的交互逻辑
    /// </summary>
    public partial class VideoExplorer : UserControl
    {
        public VideoExplorer()
        {
            InitializeComponent();
            DataContext = new VideoExplorerModel();
        }

        public void Update(bool fileOpened)
        {
            if (fileOpened)
            {
                BuildList();
            }
            else
            {
                ClearList();
            }
        }

        private void BuildList()
        {
            if (!Reader.IsOpened || Reader.Instance == null)
            {
                return;
            }

            var root = Reader.Instance.Index.Root;
            Reader.Instance.Index.GetChildren(root, out var videos, out _);
            Array.Sort(videos, NaturalPZFolderComparer.Instance);

            videosContent.ItemsSource = videos;
        }
        private void ClearList()
        {
            videosContent.ItemsSource = null;
        }

        private void OnVideoOpen(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFolder folder)
                {
                    ExPlayer.Play(folder);
                }
            }
        }

        private void OnPlayAllClick(object sender, RoutedEventArgs e)
        {
            ExPlayer.PlayAll();
        }
    }

    internal class VideoExplorerModel
    {
        public BitmapImage _videoIcon;
        public BitmapImage VideoIcon { get { return _videoIcon; } }

        public VideoExplorerModel()
        {
            _videoIcon = Service.Converters.ExtIconConverter.GetExtensionIcon(".mp4");
        }
    }
}
