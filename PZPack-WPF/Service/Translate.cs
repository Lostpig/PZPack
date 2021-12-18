using System;
using System.ComponentModel;

namespace PZPack.View.Service
{
    internal partial class Translate
    {
        public static void NotifyPropertyChanged(string propertyName)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }
        public static event EventHandler<PropertyChangedEventArgs>? StaticPropertyChanged;
    }
}
