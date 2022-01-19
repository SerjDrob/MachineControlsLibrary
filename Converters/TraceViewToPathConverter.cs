using MachineControlsLibrary.Converters;
using netDxf.Entities;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.Converters
{
    class TraceViewToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var tracesView = new ObservableCollection<TraceLine>();
            //try
            //{
            //    tracesView = (ObservableCollection<TraceLine>)value;
            //}
            //catch { }

            //var geometryGroup = new GeometryGroup();
            //foreach (var line in tracesView)
            //{
            //    geometryGroup.Children.Add(new LineGeometry(
            //        new System.Windows.Point(line.XStart, line.YStart),
            //        new System.Windows.Point(line.XEnd, line.YEnd)
            //        ));
            //}

            //return geometryGroup;
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
