using System;
using System.Globalization;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    internal class MulZeroConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double x = 0;
            bool res = false;
            try
            {
                x = System.Convert.ToDouble(values[0]);
                res = System.Convert.ToBoolean(values[1]);
            }
            catch { }
            return res ? x : 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var width = System.Convert.ToDouble(values[0]);
                var height = System.Convert.ToDouble(values[1]);
                return new System.Windows.Rect(0, -height, width, height);
            }
            catch(Exception ex) 
            {
                return new System.Windows.Rect();
            }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
