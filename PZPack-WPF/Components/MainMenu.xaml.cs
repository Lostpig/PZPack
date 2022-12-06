using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PZPack.View.Service;
using System.Windows.Media;
using System.IO;
using System.Collections.Generic;

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
            Service.PZHistory.Instance.HistoryChanged += HistoryChanged;
            BuildHistory();
        }

        private void HistoryChanged(object? sender, System.EventArgs e)
        {
            BuildHistory();
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
            Dialogs.TryOpenPZFile();
        }
        private void OnSelectClose(object sender, RoutedEventArgs e)
        {
            Reader.Close();
        }
        private void OnSelectPack(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenBuilderWindow();
        }
        private void OnSelectExit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private void OnExtractPack(object sender, RoutedEventArgs e)
        {
            if (Reader.Instance is not null)
            {
                Dialogs.OpenExtractFolderWindow(Reader.Instance.Index.Root);
            }
        }

        private void OnSetiingOpen(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenSettingWindow();
        }

        private void OnCreatePwBook(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenPwBookOpenWindow(true);
        }
        private void OnOpenPwBook(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenPwBookOpenWindow(false);
        }
        private void OnClosePwBook(object sender, RoutedEventArgs e)
        {
            PWBook.Close();
        }
        private void OnManagePwBook(object sender, RoutedEventArgs e)
        {
            Dialogs.OpenPwBookManageWindow();
        }

        private void BuildHistory()
        {
            List<HistoryVModel> historyList = new();
            var items = PZHistory.Instance.Items;

            if (items.Count == 0)
            {
                historyList.Add(new HistoryVModel(HistroyVMType.Empty, -1, ""));
            }
            else
            {
                historyList.Add(new HistoryVModel(HistroyVMType.Clear, -1, ""));
                historyList.Add(new HistoryVModel(HistroyVMType.Separator, -1, ""));

                int idx = 0;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var item = items[i];
                    historyList.Add(new HistoryVModel(HistroyVMType.Normal, idx, item));
                    idx++;
                }
            }

            historyCollection.Collection = historyList;
        }
        private void OnClearHistory(object sender, RoutedEventArgs e)
        {
            Service.PZHistory.Instance.Clear();
        }
        private void OnHistorySelect(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)e.OriginalSource;
            if (element.Tag is HistoryVModel history)
            {
                if (history.Type == HistroyVMType.Clear)
                {
                    PZHistory.Instance.Clear();
                }
                else if (history.Type == HistroyVMType.Normal)
                {
                    if (!File.Exists(history.Filename))
                    {
                        var result = Alert.ShowWarningConfirm(
                            string.Format(Translate.CONFIRM_File_not_exists, history.Filename)
                        );
                        if (result == MessageBoxResult.Yes)
                        {
                            PZHistory.Instance.Remove(history.Filename);
                        }
                    }
                    else
                    {
                        bool success = Reader.TryOpen(history.Filename);
                        if (!success)
                        {
                            Dialogs.OpenReadOptionWindow(history.Filename);
                        }
                    }
                }
            }
        }
    }

    internal enum HistroyVMType
    {
        Empty = 0,
        Separator = 1,
        Clear = 2,
        Normal = 3
    }
    internal class HistoryVModel
    {
        public HistroyVMType Type { get; }
        public string Filename { get; }

        public int Index { get; }

        public string Caption { get => $"{Index + 1} {Filename}"; }

        public HistoryVModel(HistroyVMType type, int index, string filename)
        {
            Type = type;
            Filename = filename;
            Index = index;
        }
    }
}
