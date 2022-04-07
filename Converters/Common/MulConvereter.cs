using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    public class MulConvereter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double n = 1;
            double m = 1;
            if (values.Length == 2)
            {
                try
                {
                    n = values[0] switch
                    {
                        double d => d,
                        int i => i,
                        float f => f
                    };
                    m = values[1] switch
                    {
                        double d => d,
                        int i => i,
                        float f => f
                    };
                }
                catch (Exception)
                {
                }
            }
            return n * m;

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MulParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var m1 = (double)value;
            var m2 = (double)parameter;
            return m1 * m2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
