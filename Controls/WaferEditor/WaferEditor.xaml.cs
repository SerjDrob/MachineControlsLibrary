using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MachineControlsLibrary.Classes;

namespace MachineControlsLibrary.Controls.WaferEditor
{
    /// <summary>
    /// Interaction logic for WaferEditor.xaml
    /// </summary>
    public partial class WaferEditor : UserControl
    {
        public WaferEditor()
        {
            InitializeComponent();
            Editor.DataContext = this;
        }
        public ObservableCollection<LayerGeometryCollection> LayGeoms
        {
            get
            {
                return (ObservableCollection<LayerGeometryCollection>)GetValue(LayGeomsProperty);
            }
            set
            {
                SetValue(LayGeomsProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for LayGeoms.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayGeomsProperty =
            DependencyProperty.Register("LayGeoms", typeof(ObservableCollection<LayerGeometryCollection>), typeof(WaferEditor),
                new PropertyMetadata(null, new PropertyChangedCallback(LayGeomsChanged)));

        private static void LayGeomsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spec = d as WaferEditor;
            if (spec is not null)
            {
                spec.IsLoading = false;
            }
        }

        public bool IsLoading
        {
            get
            {
                return (bool)GetValue(IsLoadingProperty);
            }
            set
            {
                SetValue(IsLoadingProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(WaferEditor), new PropertyMetadata(false));
    }
}
