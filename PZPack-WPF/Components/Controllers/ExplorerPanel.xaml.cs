using PZPack.Core.Index;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using PZPack.View.Utils;

namespace PZPack.View.Controllers
{
    /// <summary>
    /// ExplorerPanel.xaml 的交互逻辑
    /// </summary>
    public partial class ExplorerPanel : UserControl
    {
        [Description("Item right click context menu"), Category("ItemMenu")]
        public ContextMenu ItemMenu { get; set; }

        public event Action<int>? OnFolderOpen;
        public event Action<int>? OnFileOpen;

        public ExplorerPanel()
        {
            InitializeComponent();
            ItemMenu = new ContextMenu();
        }

        public void UpdateItems(List<IPZFile> files, List<IPZFolder> folders)
        {
            List<object> items = new();
            files.Sort(NaturalPZFileComparer.Instance);
            folders.Sort(NaturalPZFolderComparer.Instance);
            items.AddRange(folders);
            items.AddRange(files);

            filesContent.ItemsSource = items;
        }

        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is IPZFolder folder)
                {
                    OnFolderOpen?.Invoke(folder.Id);
                }
                else if (sp.DataContext is IPZFile file)
                {
                    OnFileOpen?.Invoke(file.Id);
                }
            }
        }
    }
}
