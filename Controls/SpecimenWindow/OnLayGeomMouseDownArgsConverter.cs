using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MachineControlsLibrary.Classes;

namespace MachineControlsLibrary.Controls
{
    internal class OnLayGeomMouseDownArgsConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MouseEventArgs mouseDownArgs)
            {
                var source = mouseDownArgs.Source as System.Windows.Shapes.Path;
                var lgc = source?.DataContext as LayerGeometryCollection;
                var ellipse = lgc?.Geometries.OfType<EllipseGeometry>().FirstOrDefault();
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
