using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.Converters
{
    internal class BrushToColorConvereter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var color = (SolidColorBrush)value;
                return color.Color;
            }
            catch (Exception)
            {
                var color = (SolidColorBrush)Brushes.Green;
                return color.Color;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var color = new SolidColorBrush((Color)value);
                return color;
            }
            catch (Exception)
            {
                var color = (SolidColorBrush)Brushes.Green;
                return color;
            }
        }
    }
}
