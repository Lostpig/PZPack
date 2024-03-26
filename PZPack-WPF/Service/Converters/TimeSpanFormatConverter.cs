using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PZPack.View.Service.Converters
{
    internal class TimeSpanFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TimeSpan? p;

            switch (value)
            {
                case TimeSpan position:
                    p = position;
                    break;
                case Duration duration when duration.HasTimeSpan:
                    p = duration.TimeSpan;
                    break;
                default:
                    return string.Empty;
            }

            if (p.Value == TimeSpan.MinValue)
                return "N/A";

            return $"{(int)p.Value.TotalHours:00}:{p.Value.Minutes:00}:{p.Value.Seconds:00}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
