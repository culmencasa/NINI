using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NINI.Converters
{

    public class NegativeVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (bool.Parse(value.ToString()))
            {
                return Visibility.Hidden;

            }
            else
            {
                return Visibility.Visible;

            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = (Visibility)value;
            if (visible == Visibility.Visible)
            {
                return false;

            }
            else
            {
                return true;

            }
        }
    }
}
