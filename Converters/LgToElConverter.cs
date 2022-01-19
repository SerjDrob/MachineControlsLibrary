using MachineControlsLibrary.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MachineControlsLibrary.Converters
{
    internal class LgToElConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var LgCollection = (ObservableCollection<LayerGeometryCollection>)value ?? throw new Exception();
                var result = new List<EnaLayer>(LgCollection.Count);
                foreach (var lg in LgCollection)
                {
                    result.Add(LgToElAdapter.NewEnaLayer(lg));
                }
                return new ObservableCollection<EnaLayer>(result);
                return LgCollection;
            }
            catch (Exception)
            {

                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                //var EnaLayers = (ObservableCollection<EnaLayer>)value ?? throw new Exception();
                var LayGeoms = (ObservableCollection<LayerGeometryCollection>)value ?? throw new Exception();

                //for (int i = 0; i < LayGeoms.Count; i++)
                //{
                //    var enable = EnaLayers.Where(e => e.Layer == LayGeoms[i].LayerName).First().Enable;
                //    if (enable ^ LayGeoms[i].LayerEnable)
                //    {
                //        var laygeom = LayGeoms[i];
                //        LayGeoms[i] = laygeom with { LayerEnable = enable };
                //    }
                //}
                return LayGeoms;
            }
            catch (Exception)
            {
                return (ObservableCollection<LayerGeometryCollection>)parameter ?? throw new Exception();
            }

        }
    }
}
