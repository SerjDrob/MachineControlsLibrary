using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Converters.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
//using System.Windows.Forms;

namespace MachineControlsLibrary.Converters.Common
{

    public class KeyProcessorArgsConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is KeyEventArgs args)
            {
                var s = args.RoutedEvent == Keyboard.KeyDownEvent;
                try
                {
                    var isKeyDown = (bool)parameter;
                    return new KeyProcessorArgs(args, isKeyDown);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
