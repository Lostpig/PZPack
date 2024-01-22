using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;
using System;
using System.ComponentModel;
using System.Windows;

namespace PZPack.View.Components
{
    /// <summary>
    /// VideoPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPlayerWindow : Window
    {
        PZFile videoFile;
        PZPKMediaStream stream;

        public VideoPlayerWindow(PZFile file)
        {
            videoFile = file;
            if (Reader.Instance == null)
            {
                Alert.ShowException(new Exception(Translate.EX_PZFileNotOpened));
                Close();
                return;
            }

            InitializeComponent();

            stream = new PZPKMediaStream(videoFile, Reader.Instance);
            Media.Open(stream);
        }

        protected override async void OnClosing(CancelEventArgs e)
        {
            await Media.Close();
            Media.Dispose();
            if (stream != null)
            {
                stream.Dispose();
            }

            base.OnClosing(e);
        }
    }
}
