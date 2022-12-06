using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;
using System.Xml.Linq;

namespace PZPack.View
{
    /// <summary>
    /// MainContent.xaml 的交互逻辑
    /// </summary>
    public partial class MainContent : UserControl
    {
        private PZFolder? currentFolder;

        public MainContent()
        {
            InitializeComponent();
        }

        public void Update(bool fileOpened)
        {
            if (fileOpened && Reader.Instance is not null)
            {
                OpenFolder(Reader.Instance.Index.Root);
            } 
            else
            {
                currentFolder = null;
            }
        }

        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    Dialogs.OpenViewWindow(file);
                }
                else if (sp.DataContext is PZFolder folder)
                {
                    OpenFolder(folder);
                }
            }
        }
        private void OnItemExtrect(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZFile file)
                {
                    Dialogs.OpenExtractWindow(file);
                }
                else if (sp.DataContext is PZFolder folder)
                {
                    Dialogs.OpenExtractFolderWindow(folder);
                }
            }
        }

        private void OpenFolder(PZFolder folder)
        {
            if (folder != null && Reader.Instance != null)
            {
                Reader.Instance.Index.GetChildren(folder, out var folders, out var files);

                Array.Sort(folders, NaturalPZFolderComparer.Instance);
                Array.Sort(files, NaturalPZFileComparer.Instance);

                object[] items = folders.Concat<object>(files).ToArray();
                filesContent.ItemsSource = items;
                CreateFolderStack(folder);

                currentFolder = folder;
            }
        }

        private void CreateFolderStack(PZFolder folder)
        {
            if (Reader.Instance is null)
            {
                return;
            }

            var stacks = Reader.Instance.Index.GetFolderResolveList(folder, null);

            folderStack.Children.Clear();
            Button rootBtn = new() { Content = "Root", Tag = Reader.Instance.Index.Root.Id };
            rootBtn.Click += FolderStackClick;
            TextBlock arrow = new() { Text = " > " };
            folderStack.Children.Add(rootBtn);
            folderStack.Children.Add(arrow);

            foreach(var item in stacks)
            {
                Button itemBtn = new() { Content = item.Name, Tag = item.Id };
                TextBlock arrowBtn = new() { Text = " > " };
                itemBtn.Click += FolderStackClick;
                folderStack.Children.Add(itemBtn);
                folderStack.Children.Add(arrowBtn);
            }
        }

        private void FolderStackClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && Reader.Instance is not null)
            {
                var id = (int)btn.Tag;
                var folder = Reader.Instance.Index.GetFolder(id);
                OpenFolder(folder);
            }
        }

        private void OnPrev(object sender, EventArgs e)
        {
            if (currentFolder is null || Reader.Instance is null || currentFolder.Id == Reader.Instance.Index.Root.Id)
            {
                return;
            }

            var parentFolder = Reader.Instance.Index.GetFolder(currentFolder.Pid);
            OpenFolder(parentFolder);
        }
    }
}
