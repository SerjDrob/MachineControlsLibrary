using System.Windows;

namespace MachineControlsLibrary.Controls
{
    public class GeomClickEventArgs : RoutedEventArgs
    {
        public Point Coordinate
        {
            get; init;
        }

        public GeomClickEventArgs(Point coordinate)
        {
            Coordinate = coordinate;
        }
        public static explicit operator Point(GeomClickEventArgs e) => e.Coordinate;
        public static implicit operator GeomClickEventArgs(Point point) => new (point);
    }
}
