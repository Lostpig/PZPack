using System.Windows;
using System.Windows.Controls;
using PZPack.View.Service;
using PZPack.Core;
using System;

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

        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)e.OriginalSource;
            IFolderNode node = (IFolderNode)item.DataContext;

            if (node != null && Reader.Instance!=null)
            {
                PZFile[] files = Reader.Instance.GetFiles(node.Id);
                filesContent.ItemsSource = files;
            }
        }
        private void OnFileSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    PZFile[] arr = (PZFile[])filesContent.ItemsSource;
                    int index = Array.IndexOf(arr, file);
                    index = index < 0 ? 0 : index;
                    Dialogs.OpenViewWindow(arr, index);
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
