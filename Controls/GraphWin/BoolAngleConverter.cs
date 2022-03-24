using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Controls.GraphWin
{
    public class BoolAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value as bool?;
            return val ?? false ? 90 : 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
