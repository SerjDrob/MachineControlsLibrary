using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Controls
{
    internal class BoolToOpacityInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var isTransparent = (bool)value;
                return isTransparent ? 0d:1d;
            }
            catch (Exception)
            {
            }
            return 1d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
