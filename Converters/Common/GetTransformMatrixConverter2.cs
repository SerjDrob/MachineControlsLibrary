using MachineControlsLibrary.Controls;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.Converters
{
    public class GetTransformMatrixConverter2 : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 9)
            {
                var width = values[0].ConvertObject((double)1);
                var height = values[1].ConvertObject((double)1);
                var fieldSizeX = values[2].ConvertObject((double)1);
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

                double scalex;
                double scaley;
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

                var obj = values[10] as SpecimenWindow;
                if (obj is not null)
                {
                    obj.IsVisible = true;
                }

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
