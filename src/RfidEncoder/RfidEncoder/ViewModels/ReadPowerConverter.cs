using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RfidEncoder.ViewModels
{
    /// <summary>
    /// Convertor to convert read power values between slider and textbox
    /// </summary>
    public class ReadPowerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                double dblValue = (double)value;
                return Math.Round(dblValue, 1);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                try
                {
                    if (value.ToString() != "")
                    {
                        double ret = double.Parse(value.ToString());
                        return Math.Round(ret, 1);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            return 0;
        }
    }
}
