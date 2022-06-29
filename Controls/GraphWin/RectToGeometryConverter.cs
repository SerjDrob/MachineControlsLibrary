using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using MachineControlsLibrary.Converters;

namespace MachineControlsLibrary.Controls.GraphWin
{
    internal class RectToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var rectangle = value.ConvertObject(new Rectangle());
            var rectGeom = new RectangleGeometry(new Rect(0,0, rectangle.Width, rectangle.Height));
            return rectGeom;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class RectToGeometryConverter2 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var width = values[0].ConvertObject(0d);
            var height = values[1].ConvertObject(0d);
            var rectGeom = new RectangleGeometry(new Rect(0,0, width, height));
            return rectGeom;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
