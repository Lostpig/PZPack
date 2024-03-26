using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PZPack.View.Service.Converters
{
    internal class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                TimeSpan span => span.TotalSeconds,
                Duration duration => duration.HasTimeSpan ? duration.TimeSpan.TotalSeconds : 0d,
                _ => (object)0d,
            };
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double == false) return 0d;
            var result = TimeSpan.FromTicks(System.Convert.ToInt64(TimeSpan.TicksPerSecond * (double)value));

            // Do the conversion from visibility to bool
            if (targetType == typeof(TimeSpan)) return result;
            if (targetType == typeof(Duration)) return new Duration(result);

            throw new ArgumentException("Convert type not defined");
        }
    }
}
