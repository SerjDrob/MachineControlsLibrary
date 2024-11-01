using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.Converters
{

    public class ShapeScalingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double width = 1;
            double height = 1;
            var fieldSizeX = width;
            var fieldSizeY = height;
            double specSizeX = 0;
            double specSizeY = 0;
            double margin = 0;
            bool xProportion;
            bool yProportion;
            bool autoProportion;
            var ScaledShapes = new GeometryCollection();
            if (values.Length >= 9)
            {
                try
                {
                    width = (double)values[1];
                    height = (double)values[2];
                    fieldSizeX = (double)values[3];
                    fieldSizeY = (double)values[4];
                    margin = (double)values[5];
                    if (margin > 1)
                    {
                        margin = 0;
                    }
                    xProportion = (bool)values[6];
                    yProportion = (bool)values[7];
                    autoProportion = (bool)values[8];
                    specSizeX = (double)values[9];
                    specSizeY = (double)values[10];
                    ScaledShapes = (GeometryCollection)values[0];
                }
                catch (Exception)
                {
                    return null;
                }


                var scalex = (double)0; //((width - 2 * margin * width) / sizex);
                var scaley = (double)0; //((height - 2 * margin * width) / sizey);
                var marginx = (double)0;
                var marginy = (double)0;
                var fieldmarginx = (double)0;
                var fieldmarginy = (double)0;
                GeometryCollection geometries;
                var calc = new ScaleCalc(width, height, fieldSizeX, fieldSizeY, margin, xProportion, yProportion, autoProportion, specSizeX, specSizeY);
                calc.Calc(out scalex, out scaley, out marginx, out marginy, out fieldmarginx, out fieldmarginy);


                var scaleTrans = new ScaleTransform(scalex, scaley);
                var translateTrans1 = new TranslateTransform(-specSizeX / 2, -specSizeY / 2);
                var translateTrans2 = new TranslateTransform(width / 2, height / 2);

                var transGroup = new TransformGroup();
                transGroup.Children.Add(translateTrans1);
                transGroup.Children.Add(scaleTrans);
                transGroup.Children.Add(translateTrans2);

                var points = ScaledShapes.OfType<EllipseGeometry>().Where(e => e.RadiusX == 0 | e.RadiusY == 0).ToList();
                var crosses = points?.Aggregate(new GeometryCollection(), (acc, p) => new GeometryCollection(acc.Concat(new Geometry[]
                             {
                                new LineGeometry(new System.Windows.Point(p.Center.X - 500,p.Center.Y), new System.Windows.Point(p.Center.X+500,p.Center.Y)),
                                new LineGeometry(new System.Windows.Point(p.Center.X,p.Center.Y - 500), new System.Windows.Point(p.Center.X,p.Center.Y + 500))
                             })));
                ScaledShapes = new GeometryCollection(ScaledShapes.Except(points).Concat(crosses).Select(s => { s.Transform = transGroup; return s; }));

            }

            return ScaledShapes;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
