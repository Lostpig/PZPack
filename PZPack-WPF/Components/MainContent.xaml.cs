using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;

namespace PZPack.View
{
    /// <summary>
    /// MainContent.xaml 的交互逻辑
    /// </summary>
    public partial class MainContent : UserControl
    {
        public MainContent()
        {
            InitializeComponent();
        }

        public void Update(bool fileOpened)
        {
            if (fileOpened)
            {
                BuildTree();
            }
            else
            {
                ClearTree();
            }
        }

        private void OnFolderSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)e.OriginalSource;
            PZFolder node = (PZFolder)item.DataContext;

            if (node != null && Reader.Instance!=null)
            {
                Reader.Instance.Index.GetChildren(node, out _, out var files);

                Array.Sort(files, NaturalPZFileComparer.Instance);
                filesContent.ItemsSource = files;
            }
        }
        private void OnFileSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    PZFile[] list = (PZFile[])filesContent.ItemsSource;
                    int index = Array.IndexOf(list, file);
                    index = index < 0 ? 0 : index;
                    Dialogs.OpenViewWindow(list, index);
                }
            }
        }
        private void OnFileExtrect(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    Dialogs.OpenExtractWindow(file);
                }
            }
        }

        private void BuildTree ()
        {
            if (!Reader.IsOpened)
            {
                return;
            }

            PZFolder root = Reader.Instance!.Index.Root;
            TreeViewItem treeViewItem = new()
            {
                DataContext = root,
                Header = "root"
            };

            folderTree.Items.Add(treeViewItem);
            ExpandTree(treeViewItem);

            treeViewItem.IsSelected = true;
        }
        private void ClearTree()
        {
            folderTree.Items.Clear();
            filesContent.ItemsSource = null;
        }
        static private void ExpandTree (TreeViewItem parent)
        {
            PZFolder node = (PZFolder)parent.DataContext;
            Reader.Instance!.Index.GetChildren(node, out var folders, out _);

            foreach(var child in folders)
            {
                TreeViewItem treeViewItem = new()
                {
                    DataContext = child,
                    Header = child.Name
                };

                parent.Items.Add(treeViewItem);
                ExpandTree(treeViewItem);
            }
        }
    }
}
