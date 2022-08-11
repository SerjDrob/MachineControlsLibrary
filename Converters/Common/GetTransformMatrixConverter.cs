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

            var _offsetX = 0d;
            var _offsetY = 0d;
            var _mirrorX = false;
            var _angle90 = false;


            var _width = values[0].ConvertObject(1d);
            var _height = values[1].ConvertObject(1d);
            var _margin = values[2].ConvertObject(0d);
            var _fieldSizeX = values[3].ConvertObject(1d);
            var _fieldSizeY = values[4].ConvertObject(1d);

            if (values.Length > 6)
            {
                _mirrorX = values[6].ConvertObject(false);
                _offsetX = values[7].ConvertObject(0);
                _offsetY = values[8].ConvertObject(0);
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


            var fieldMarginX = (_width - _scale * _fieldSizeX) / 2;
            var fieldMarginY = (_height - _scale * _fieldSizeY) / 2;
            var fileMarginX = _offsetX + fieldMarginX;
            var fileMarginY = _offsetY + fieldMarginY;


            var obj = values[5] as Controls.GraphWindow;
            obj?.SetMargins(fileMarginX, fileMarginY);
            obj?.SetFieldMargins(fieldMarginX, fieldMarginY);
            obj?.SetScale(_scale, _scale);
            return transGroup.Value;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
