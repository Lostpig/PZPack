using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using PZPack.Core;
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
            IFolderNode node = (IFolderNode)item.DataContext;

            if (node != null && Reader.Instance!=null)
            {
                var files = Reader.Instance.GetFiles(node.Id).ToList();
                files.Sort(NaturalPZFileComparer.Instance);
                filesContent.ItemsSource = files;
            }
        }
        private void OnFileSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    List<PZFile> list = (List<PZFile>)filesContent.ItemsSource;
                    int index = list.IndexOf(file);
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

            IFolderNode root = Reader.Instance!.GetFolderNode();
            TreeViewItem treeViewItem = new();
            treeViewItem.DataContext = root;
            treeViewItem.Header = "root";

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
            IFolderNode node = (IFolderNode)parent.DataContext;
            foreach(IFolderNode child in node.GetChildren())
            {
                TreeViewItem treeViewItem = new();
                treeViewItem.DataContext = child;
                treeViewItem.Header = child.Name;

                parent.Items.Add(treeViewItem);
                ExpandTree(treeViewItem);
            }
        }
    }
}
