using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace GTAGameFilter
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object isActive, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (isActive is bool)
            {
                if ((bool)isActive)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility)
            {
                switch ((Visibility)value)
                {
                    case Visibility.Visible:
                        return true;
                    case Visibility.Collapsed:
                    case Visibility.Hidden:
                    default:
                        return false;
                }
            }
            return false;
        }
    }
    public class LastSeenConverter : IValueConverter
    {
        public object Convert(object timestamp, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (timestamp is long)
            {
                var span = DateTimeOffset.Now.ToUnixTimeMilliseconds() - (long)timestamp;
                span /= 1000;
                return string.Format("{0}s ago", span);
            }
            return "??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }
}
