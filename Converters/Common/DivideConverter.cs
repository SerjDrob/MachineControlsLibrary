using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    public class DivideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val;
            double par;
            try
            {
                val = System.Convert.ToDouble(value);
                par = System.Convert.ToDouble(parameter, NumberFormatInfo.InvariantInfo);
                if (par==0)
                {
                    par = 1;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return val / par;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DivideConverterInvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val;
            double par;
            try
            {
                val = System.Convert.ToDouble(value);
                par = System.Convert.ToDouble(parameter, NumberFormatInfo.InvariantInfo);
                if (val == 0)
                {
                    val = 1;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return par / val;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
