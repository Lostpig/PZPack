using System;
using System.Windows.Controls;
using System.Collections.Generic;
using PZPack.Core.Index;


namespace PZPack.View.Controllers
{
    /// <summary>
    /// FolderStackPanel.xaml 的交互逻辑
    /// </summary>
    public partial class FolderStackPanel : UserControl
    {
        public event Action? OnPrevClick;
        public event Action<int>? OnFolderClick;

        public FolderStackPanel()
        {
            InitializeComponent();
        }

        public void UpdateStack(IList<IPZFolder> folders, int rootId)
        {
            folderStack.Children.Clear();
            Button rootBtn = new() { Content = "Root", Tag = rootId };
            rootBtn.Click += FolderStackClick;
            TextBlock arrow = new() { Text = "\uE76C" };
            folderStack.Children.Add(rootBtn);
            folderStack.Children.Add(arrow);

            foreach (var item in folders)
            {
                Button itemBtn = new() { Content = item.Name, Tag = item.Id };
                TextBlock arrowBtn = new() { Text = "\uE76C" };
                itemBtn.Click += FolderStackClick;
                folderStack.Children.Add(itemBtn);
                folderStack.Children.Add(arrowBtn);
            }
        }

        private void OnPrev(object sender, EventArgs e)
        {
            OnPrevClick?.Invoke();
        }
        private void FolderStackClick(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                var id = (int)btn.Tag;
                OnFolderClick?.Invoke(id);
            }
        }
    }
}
