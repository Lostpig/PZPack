using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using PZPack.View.Service;
using System.Windows.Media;

namespace PZPack.View
{
    /// <summary>
    /// MainMenu.xaml 的交互逻辑
    /// </summary>
    public partial class MainMenu : UserControl
    {

        public MainMenu()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            languageCollection.Collection = Service.Language.GetLanguages().ToList();
        }

        private void OnLanguageSelect(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            if (element.Tag is string lang)
            {
                Service.Language.ChangeLanguage(lang);
            }
        }
        private void OnSelectOpen(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenReadOptionWindow();
        }
        private void OnSelectClose(object sender, RoutedEventArgs e)
        {
            Reader.Close();
        }
        private void OnSelectPack(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenPackWindow();
        }
        private void OnSelectExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void OnExtractPack(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenExtractAllWindow();
        }
        
    }
}
