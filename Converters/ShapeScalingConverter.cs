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
                var crosses = points?.Aggregate(new GeometryCollection(),(acc,p) => new GeometryCollection(acc.Concat(new Geometry[]
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
            double _width = 1;
            double _height = 1;
            double _fieldSizeX = _width;
            double _fieldSizeY = _height;
            double _margin = 0;
            double _scale = 1;
            if (values.Length >= 5)
            {
                try
                {
                    _width = (double)values[0];
                    _height = (double)values[1];
                    _margin = (double)values[2];
                    _fieldSizeX = (double)values[3];
                    _fieldSizeY = (double)values[4];


                    if (_margin > 1)
                    {
                        _margin = 0;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
                var scalex = (_width - _margin * _width) / _fieldSizeX;
                var scaley = (_height - _margin * _height) / _fieldSizeY;
                var selector = (_fieldSizeX / _width) > (_fieldSizeY / _height);
                _scale = selector ? scalex : scaley;
            }
            var scaleTrans = new ScaleTransform(_scale, _scale, _width / 2, _height / 2);
            var translateTrans2 = new TranslateTransform(_width / 2, _height / 2);
            var translateTrans0 = new TranslateTransform(-_fieldSizeX / 2, -_fieldSizeY / 2);
            var transGroup = new TransformGroup();

            transGroup.Children.Add(translateTrans0);
            transGroup.Children.Add(translateTrans2);
            transGroup.Children.Add(scaleTrans);
            var obj = values[5] as Controls.GraphWindow;
            var marginX = (_width - _scale * _fieldSizeX) / 2;
            var marginY = (_height - _scale * _fieldSizeY) / 2;
            obj?.SetMargins(marginX, marginY);
            obj?.SetScale(_scale, _scale);
            return transGroup.Value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class GetScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double _width = 1;
            double _height = 1;
            double _fieldSizeX = _width;
            double _fieldSizeY = _height;
            double _margin = 0;
            bool _xProportion;
            bool _yProportion;
            bool _autoProportion;
            var scale = (double)1;

            if (values.Length >= 8)
            {
                try
                {
                    _width = (double)values[0];
                    _height = (double)values[1];
                    _fieldSizeX = (double)values[2];
                    _fieldSizeY = (double)values[3];
                    _margin = (double)values[4];
                    if (_margin > 1)
                    {
                        _margin = 0;
                    }
                    _xProportion = (bool)values[5];
                    _yProportion = (bool)values[6];
                    _autoProportion = (bool)values[7];
                }
                catch (Exception)
                {
                    return null;
                }

                var scalex = (_width - _margin * _width) / _fieldSizeX;
                var scaley = (_height - _margin * _height) / _fieldSizeY;
                if (_autoProportion & !(_xProportion | _yProportion))
                {
                    var selector = (_fieldSizeX / _width) > (_fieldSizeY / _height);
                    scale = selector ? scalex : scaley;
                }
                else if (!_xProportion & _yProportion)
                {
                    if (_xProportion)
                    {
                        scale = scalex;
                    }
                    if (_yProportion)
                    {
                        scale = scaley;
                    }
                }

            }
            return scale;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
