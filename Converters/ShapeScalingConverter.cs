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
            double fieldSizeX = width;
            double fieldSizeY = height;
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


    public class GetTransformMatrixConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            double _offsetX = 0;
            double _offsetY = 0;
            bool _mirrorX = false;
            bool _angle90 = false;


            double _width = values[0] is double ? (double)values[0] : 1;
            double _height = values[1] is double ? (double)values[1] : 1;
            double _margin = values[2] is double ? (double)values[2] : 0;
            double _fieldSizeX = values[3] is double ? (double)values[3] : 1;
            double _fieldSizeY = values[4] is double ? (double)values[4] : 1;

            if (values.Length > 6)
            {
                _mirrorX = values[6] is bool ? (bool)values[6] : false;
                _offsetX = values[7] is double ? (double)values[7] : 0;
                _offsetY = values[8] is double ? (double)values[8] : 0;
                _angle90 = values[9] is bool ? (bool)values[9] : false;
            }


            var scalex = (_width - _margin * _width) / _fieldSizeX;
            var scaley = (_height - _margin * _height) / _fieldSizeY;
            var selector = (_fieldSizeX / _width) > (_fieldSizeY / _height);
            var _scale = selector ? scalex : scaley;

            var scaleTrans = new ScaleTransform(_scale * (_mirrorX ? -1 : 1), _scale, _width / 2, _height / 2);
            var rotateTransform = new RotateTransform(_angle90 ? 90 : 0, _width / 2, _height / 2);
            var translateTrans2 = new TranslateTransform(_width / 2, _height / 2);
            var translateTrans0 = new TranslateTransform(-_fieldSizeX / 2, -_fieldSizeY / 2);
            var transGroup = new TransformGroup();

            transGroup.Children.Add(translateTrans0);
            transGroup.Children.Add(translateTrans2);
            transGroup.Children.Add(scaleTrans);
            transGroup.Children.Add(rotateTransform);
            var marginX = _offsetX + (_width - _scale * _fieldSizeX) / 2;
            var marginY = _offsetY + (_height - _scale * _fieldSizeY) / 2;
            var obj = values[5] as Controls.GraphWindow;
            obj?.SetMargins(marginX, marginY);
            obj?.SetScale(_scale, _scale);
            return transGroup.Value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal static class ConvertExtensions
    {
        public static T ConvertObject<T>(this object obj, T defaultValue)
        {
            return obj is T ? (T)obj : defaultValue;
        }
    }

    public class GetTransformMatrixConverter2 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 9)
            {
                var width = values[0].ConvertObject((double)1);// is double ? (double)values[1] : 1;
                var height = values[1].ConvertObject((double)1);// is double ? (double)values[2] : 1;
                var fieldSizeX = values[2].ConvertObject((double)1);// is double ? (double)values[3] : 1;
                var fieldSizeY = values[3].ConvertObject((double)1);
                var margin = values[4].ConvertObject((double)0);
                if (margin > 1)
                {
                    margin = 0;
                }
                var xProportion = values[5].ConvertObject(false);
                var yProportion = values[6].ConvertObject(false);
                var autoProportion = values[7].ConvertObject(true);
                var specSizeX = values[8].ConvertObject((double)0);
                var specSizeY = values[9].ConvertObject((double)0);


                double scalex; //((width - 2 * margin * width) / sizex);
                double scaley; //((height - 2 * margin * width) / sizey);
                double marginx;
                double marginy;
                double fieldmarginx;
                double fieldmarginy;
                var calc = new ScaleCalc(width, height, fieldSizeX, fieldSizeY, margin, xProportion, yProportion, autoProportion, specSizeX, specSizeY);
                calc.Calc(out scalex, out scaley, out marginx, out marginy, out fieldmarginx, out fieldmarginy);


                var scaleTrans = new ScaleTransform(scalex, scaley);
                var translateTrans1 = new TranslateTransform(-specSizeX / 2, -specSizeY / 2);
                var translateTrans2 = new TranslateTransform(width / 2, height / 2);

                var transGroup = new TransformGroup();
                transGroup.Children.Add(translateTrans1);
                transGroup.Children.Add(scaleTrans);
                transGroup.Children.Add(translateTrans2);

                return transGroup.Value;
            }

            return new Matrix();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
