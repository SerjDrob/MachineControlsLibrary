using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Controls.GraphWin
{
    public class BoolMirrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value as bool?;
            return val ?? false ? -1 : 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
