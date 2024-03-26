using System.Windows;
using System.Collections.Generic;
using PZPack.Core.Index;
using PZPack.View.Utils;
using System;
using PZPack.View.Service;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;

namespace PZPack.View.Windows
{
    /// <summary>
    /// BuilderWindow.xaml 的交互逻辑
    /// </summary>
    public partial class BuilderWindow : Window
    {
        private IndexDesigner _designer;
        private PZDesigningFolder _current;
        private PZDesigningFolder Current
        {
            get => _current;
            set
            {
                if (_current != value)
                {
                    _current = value;
                    UpdateList();
                    UpdateStack();
                }
            }
        }

        public BuilderWindow()
        {
            InitializeComponent();
            _designer = new IndexDesigner();
            _current = _designer.Root;
            UpdateList();
            UpdateStack();
        }

        private void UpdateStack()
        {
            var stacks = _designer.GetFolderResolveList(Current, null);

            folderStack.Children.Clear();
            Button rootBtn = new() { Content = "Root", Tag = _designer.Root.Id };
            rootBtn.Click += FolderStackClick;
            TextBlock arrow = new() { Text = " > " };
            folderStack.Children.Add(rootBtn);
            folderStack.Children.Add(arrow);

            foreach (var item in stacks)
            {
                Button itemBtn = new() { Content = item.Name, Tag = item.Id };
                TextBlock arrowBtn = new() { Text = " > " };
                itemBtn.Click += FolderStackClick;
                folderStack.Children.Add(itemBtn);
                folderStack.Children.Add(arrowBtn);
            }
        }
        private void UpdateList()
        {
            List<object> items = new();

            var files = _designer.GetFiles(Current);
            var folders = _designer.GetFolders(Current);

            files.Sort(NaturalPZFileComparer.Instance);
            folders.Sort(NaturalPZFolderComparer.Instance);

            items.AddRange(folders);
            items.AddRange(files);

            filesContent.ItemsSource = items;
        }
        private void ScanAndAddFolder(string folderPath, PZDesigningFolder parent)
        {
            string folderName = Path.GetFileName(folderPath);
            PZDesigningFolder current = _designer.AddFolder(folderName, parent);

            string[] childDirs = Directory.GetDirectories(folderPath);
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                _designer.AddFile(file, name, current);
            }
            foreach (string dir in childDirs)
            {
                ScanAndAddFolder(dir, current);
            }
        }

        private string? MakeNewName(string defaultName)
        {
            MakeNameWindow win = new(defaultName) { Owner = this };
            bool? ok = win.ShowDialog();

            if (ok == true)
            {
                return win.ResultName;
            }
            return null;
        }

        private void FolderStackClick(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                var id = (int)btn.Tag;
                var folder = _designer.GetFolder(id);
                Current = folder;
            }
        }
        private void OnPrev(object sender, EventArgs e)
        {
            if (Current.Id == _designer.Root.Id) return;
            Current = _designer.GetFolder(Current.Pid);
        }
        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZDesigningFolder folder)
                {
                    Current = folder;
                }
            }
        }
        private void OnItemDelete(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZDesigningFolder folder)
                {
                    bool removed = _designer.RemoveFolder(folder);
                    if (removed)
                    {
                        UpdateList();
                    }
                }
                else if (sp.DataContext is PZDesigningFile file)
                {
                    bool removed = _designer.RemoveFile(file);
                    if (removed)
                    {
                        UpdateList();
                    }
                }
            }
        }
        private void OnItemRename(object sender, RoutedEventArgs e)
        {
            try
            {
                if (e.OriginalSource is FrameworkElement sp)
                {
                    if (sp.DataContext is PZDesigningFolder folder)
                    {
                        string? newName = MakeNewName(folder.Name);
                        if (newName is not null)
                        {
                            _designer.RenameFolder(folder, newName);
                            UpdateList();
                        }
                    }
                    else if (sp.DataContext is PZDesigningFile file)
                    {
                        string? newName = MakeNewName(file.Name);
                        if (newName is not null)
                        {
                            _designer.RenameFile(file, newName);
                            UpdateList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Alert.ShowException(ex);
            }
        }
        private void OnItemExtract(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement sp)
            {
                if (sp.DataContext is PZDesigningFolder folder)
                {
                    var childFolders = _designer.GetFolders(folder);
                    var childFiles = _designer.GetFiles(folder);
                    var parent = _designer.GetFolder(folder.Pid);

                    try
                    {
                        foreach (var cfolder in childFolders)
                        {
                            if (cfolder.Name == folder.Name) continue;
                            _designer.CheckDuplicateNameFolder(cfolder.Name, parent);
                        }
                        foreach (var cfile in childFiles)
                        {
                            _designer.CheckDuplicateNameFile(cfile.Name, parent);
                        }
                    } 
                    catch(Exception ex)
                    {
                        Alert.ShowException(ex);
                        return;
                    }

                    _designer.RenameFolder(folder, "______temp_before_delete_" + DateTime.Now.Millisecond.ToString());

                    foreach (var cfolder in childFolders)
                    {
                        _designer.MoveFolder(cfolder, parent);
                    }
                    foreach (var cfile in childFiles)
                    {
                        _designer.MoveFile(cfile, parent);
                    }

                    _designer.RemoveFolder(folder);
                    UpdateList();
                }
            }
        }
        private void OnAddFiles(object sender, EventArgs e)
        {
            string[]? files = FileSystem.OpenSelectFilesDialog("");
            if (files is not null)
            {
                bool added = false;
                try
                {
                    foreach (var f in files)
                    {
                        string name = Path.GetFileName(f);
                        _designer.AddFile(f, name, Current);
                        added = true;
                    }
                }
                catch (Exception ex)
                {
                    Alert.ShowException(ex);
                }
                finally
                {
                    if (added)
                    {
                        UpdateList();
                    }
                }
            }
        }
        private void OnAddFolder(object sender, EventArgs e)
        {
            string? folder = FileSystem.OpenSelectDirectryDialog();

            if (folder is not null)
            {
                try
                {
                    Debug.WriteLine(folder);
                    ScanAndAddFolder(folder, Current);
                }
                catch (Exception ex)
                {
                    Alert.ShowException(ex);
                }
                finally
                {
                    UpdateList();
                }
            }
        }
        private void OnMakeFolder(object sender, EventArgs e)
        {
            string? name = MakeNewName("");

            if (name is not null)
            {
                try
                {
                    _designer.AddFolder(name, Current);
                    UpdateList();
                }
                catch (Exception ex)
                {
                    Alert.ShowException(ex);
                }
            }
        }
        private void OnRenameFiles(object sender, EventArgs e)
        {
            var files = _designer.GetFiles(Current);
            files.Sort(NaturalPZFileComparer.Instance);

            int i = 0;
            foreach (var file in files)
            {
                i++;
                string idx = i.ToString("D5");
                _designer.RenameFile(file, idx + file.Extension);
            }

            UpdateList();
        }
        private void OnCreatePack(object sender, EventArgs e)
        {
            if (_designer.IsEmpty)
            {
                return;
            }

            PackWindow packWin = new(_designer);
            packWin.ShowDialog();
        }
        private void OnDeleteAllFiles(object sender, EventArgs e)
        {
            var files = _designer.GetFiles(Current);
            foreach (var file in files)
            {
                _designer.RemoveFile(file);
            }
            UpdateList();
        }
    }
}
