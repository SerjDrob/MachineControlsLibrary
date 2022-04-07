using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.Converters
{
    public class GetTransformMatrixConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {

            double _offsetX = 0;
            double _offsetY = 0;
            bool _mirrorX = false;
            bool _angle90 = false;


            double _width = values[0].ConvertObject((double)1);
            double _height = values[1].ConvertObject((double)1);
            double _margin = values[2].ConvertObject((double)0);
            double _fieldSizeX = values[3].ConvertObject((double)1);
            double _fieldSizeY = values[4].ConvertObject((double)1);

            if (values.Length > 6)
            {
                _mirrorX = values[6].ConvertObject(false);
                _offsetX = values[7].ConvertObject((double)0);
                _offsetY = values[8].ConvertObject((double)0);
                _angle90 = values[9].ConvertObject(false);
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
}
