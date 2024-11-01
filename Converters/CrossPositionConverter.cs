using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    internal class CrossPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 1)
            {
                var length = System.Convert.ToDouble(values[0]);
                var pointNum = System.Convert.ToInt32(values[1]);
                var lengthRatio = System.Convert.ToDouble(parameter);
                return pointNum switch
                {
                    1 => length * (1 - lengthRatio) / 2,
                    2 => length * (1 + lengthRatio) / 2,
                    _ => 0,
                };
            }

            return System.Convert.ToDouble(values[0]) / 2;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
