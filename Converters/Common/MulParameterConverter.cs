using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    public class MulParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {

                var m1 = (double)value;
                var m2 = System.Convert.ToDouble(parameter);
                return m1 * m2;
            }
            catch (Exception)
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
