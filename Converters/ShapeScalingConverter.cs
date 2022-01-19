using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            double sizex = width;
            double sizey = height;
            double margin = 0;
            bool xProportion;
            bool yProportion;
            bool autoProportion;
            var ScaledShapes = new GeometryCollection();
            if (values.Length == 9)
            {
                try
                {
                    width = (double)values[1];
                    height = (double)values[2];
                    sizex = (double)values[3];
                    sizey = (double)values[4];
                    margin = (double)values[5];
                    if (margin > 1)
                    {
                        margin = 0;
                    }
                    xProportion = (bool)values[6];
                    yProportion = (bool)values[7];
                    autoProportion = (bool)values[8];
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

                var calc = new ScaleCalc(width, height, sizex, sizey, margin, xProportion, yProportion, autoProportion);
                calc.Calc(out scalex, out scaley, out marginx, out marginy);

                foreach (var item in ScaledShapes)
                {
                    var scaleTrans = new ScaleTransform(scalex, scaley);
                    var translateTrans = new TranslateTransform(marginx, marginy);
                    var transGroup = new TransformGroup();
                    transGroup.Children.Add(scaleTrans);
                    transGroup.Children.Add(translateTrans);
                    item.Transform = transGroup;
                }
            }
            //ScaledShapes.Freeze();
            return ScaledShapes;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
