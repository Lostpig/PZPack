using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using PZPack.Core.Index;
using PZPack.View.Service;
using PZPack.View.Utils;
using System.Collections.Generic;

namespace PZPack.View.Controllers
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
            BindingExplorer();
            BindingFolderStack();
        }

        private void BindingFolderStack()
        {
            floderStackPanel.OnPrevClick += OnPrev;
            floderStackPanel.OnFolderClick += OpenFolder;
        }
        private void BindingExplorer()
        {
            explorePanel.OnFileOpen += OpenFile;
            explorePanel.OnFolderOpen += OpenFolder;
        }

        private void OpenFolder(int id)
        {
            if (Reader.Instance != null)
            {
                var folder = Reader.Instance.Index.GetFolder(id);
                if (folder == null) return;

                Reader.Instance.Index.GetChildren(folder, out var folders, out var files);
                explorePanel.UpdateItems([.. files], [.. folders]);

                var stacks = Reader.Instance.Index.GetFolderResolveList(folder, null);
                floderStackPanel.UpdateStack(stacks, Reader.Instance.Index.Root.Id);

                currentFolder = folder;
            }
        }
        private void OpenFile(int id)
        {
            if (Reader.Instance != null)
            {
                var file = Reader.Instance.Index.GetFile(id);
                Dialogs.OpenViewWindow(file);
            }
        }

        public void Update(bool fileOpened)
        {
            if (fileOpened && Reader.Instance is not null)
            {
                OpenFolder(Reader.Instance.Index.Root.Id);
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
                    OpenFile(file.Id);
                }
                else if (sp.DataContext is PZFolder folder)
                {
                    OpenFolder(folder.Id);
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

        private void OnPrev()
        {
            if (currentFolder is null || Reader.Instance is null || currentFolder.Id == Reader.Instance.Index.Root.Id)
            {
                return;
            }
            OpenFolder(currentFolder.Pid);
        }
    }
}
