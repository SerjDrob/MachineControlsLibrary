using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    class MulConvereter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double n = 1;
            double m = 1;
            if (values.Length==2)
            {
                try
                {
                    n = (double)values[0];
                    m = (double)values[1];
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
}
