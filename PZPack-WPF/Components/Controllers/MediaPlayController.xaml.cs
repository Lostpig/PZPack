using PZPack.View.Windows;
using System.Windows.Controls;
using Unosquare.FFME;

namespace PZPack.View.Controllers
{
    /// <summary>
    /// MediaPlayController.xaml 的交互逻辑
    /// </summary>
    public partial class MediaPlayController : UserControl
    {
        public MediaPlayController()
        {
            InitializeComponent();
        }

        public Unosquare.FFME.MediaElement? m_mediaElement;
        public Unosquare.FFME.MediaElement MediaElement
        {
            get
            {
                if (m_mediaElement == null)
                {
                    if (DataContext is MediaPlayerWindowModel vm)
                    {
                        m_mediaElement = vm.MediaElement;
                    } 
                    else
                    {
                        throw new System.Exception("DataContext not is a MediaPlayerWindowModel");
                    }
                }

                return m_mediaElement;
            }
        }

        private void PlayButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaElement.Play();
        }
        private void PauseButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaElement.Pause();
        }
        private void StopButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            MediaElement.Stop();
        }
    }
}
