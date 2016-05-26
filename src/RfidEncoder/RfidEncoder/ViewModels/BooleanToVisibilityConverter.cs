using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RfidEncoder.ViewModels
{
    [ValueConversion(typeof (bool), typeof (Visibility))]
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public bool IsReversed { get; set; }
        public bool UseHidden { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            if (IsReversed)
            {
                val = !val;
            }
            if (val)
            {
                return Visibility.Visible;
            }
            return UseHidden ? Visibility.Hidden : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}