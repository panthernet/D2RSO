using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace D2RSO.Classes
{
    public class BoolToOrientationConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Orientation.Vertical : Orientation.Horizontal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}