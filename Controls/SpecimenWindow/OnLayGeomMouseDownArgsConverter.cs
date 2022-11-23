using HandyControl.Interactivity;
using MachineControlsLibrary.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace MachineControlsLibrary.Controls
{
    internal class OnLayGeomMouseDownArgsConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mouseDownArgs = value as MouseEventArgs;
            if (mouseDownArgs != null)
            {
                var source = mouseDownArgs.Source as System.Windows.Shapes.Path;

                var lgc = source?.DataContext as LayerGeometryCollection;
                var ellipse = lgc?.Geometries.SingleOrDefault() as EllipseGeometry;//TODO can be more entities. Exception occured.
                var center = ellipse?.Center;
                return center;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
