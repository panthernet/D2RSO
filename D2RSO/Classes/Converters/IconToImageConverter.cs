using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace D2RSO.Classes
{
    public class IconToImageConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is KeyValuePair<string, ImageSource> pair)
                return pair.Value;

            var fileName = (string)value;

            if (string.IsNullOrEmpty(fileName)) return null;

            return App.Data.Skillicons.ContainsKey(fileName) ? App.Data.Skillicons[fileName] : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}