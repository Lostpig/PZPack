using System.ComponentModel;

namespace PZPack.View.Utils
{
    internal abstract class BaseNotifyPropertyChangedModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
