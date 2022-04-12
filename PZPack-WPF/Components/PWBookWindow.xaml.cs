using System.ComponentModel;
using System.Windows;
using PZPack.View.Service;

namespace PZPack.View
{
    /// <summary>
    /// PWBookWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PWBookWindow : Window
    {
        private readonly PWBWindowModel VModel;
        public PWBookWindow()
        {
            InitializeComponent();
            VModel = new PWBWindowModel();
            DataContext = VModel;

            if (CheckPwBookOpened() is PWBook book)
            {
                string[] hashList = book.GetKeys();
                pwRecordsContent.ItemsSource = hashList;
            }
        }

        private PWBook? CheckPwBookOpened ()
        {
            if (PWBook.Current == null)
            {
                Alert.ShowWarning(Translate.Password_Book_Not_Opened);
                Close();
                return null;
            }

            return PWBook.Current;
        }

        private void OnAddPassword(object sender, RoutedEventArgs e)
        {
            string password = VModel.NewPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                Alert.ShowMessage(Translate.MSG_Password_empty);
                return;
            }

            if (CheckPwBookOpened() is PWBook book)
            {
                book.AddPassword(password);
                string[] hashList = book.GetKeys();
                pwRecordsContent.ItemsSource = hashList;

                VModel.NewPassword = "";
            }
        }
        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void OnSavePwBook(object sender, RoutedEventArgs e)
        {
            if (CheckPwBookOpened() is PWBook book)
            {
                PWBook.Save(book);
                Alert.ShowMessage(Translate.Save_Completed);
            }
        }
    }

    internal class PWBWindowModel : INotifyPropertyChanged
    {
        private string _newPassword = "";
        public string NewPassword { get => _newPassword; set { _newPassword = value; NotifyPropertyChanged(nameof(NewPassword)); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
