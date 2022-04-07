using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Converters;
using PropertyChanged;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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

namespace MachineControlsLibrary.Controls
{
    /// <summary>
    /// Interaction logic for GraphWindow.xaml
    /// </summary>
    //[AddINotifyPropertyChangedInterface]
    public partial class GraphWindow : UserControl
    {
        public GraphWindow()
        {
            InitializeComponent();            
            GraphWin.DataContext = this;
            DefaultStyleKey = typeof(GraphWindow);
        }
        //private double _scalex;
        //private double _scaley;
        //private double _marginx;
        //private double _marginy;
        //private double _fieldmarginx;
        //private double _fieldmarginy;

        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(GraphWindow), new PropertyMetadata(Brushes.Gray));

        public ObservableCollection<LayerGeometryCollection> LayGeoms
        {
            get { return (ObservableCollection<LayerGeometryCollection>)GetValue(LayGeomsProperty); }
            set { SetValue(LayGeomsProperty, value); }
        }


        // Using a DependencyProperty as the backing store for LayGeoms.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayGeomsProperty =
            DependencyProperty.Register("LayGeoms", typeof(ObservableCollection<LayerGeometryCollection>), typeof(GraphWindow),
                new PropertyMetadata(null, new PropertyChangedCallback(LayGeomsChanged)));

        private static void LayGeomsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ignoredLayers = ((GraphWindow)d).IgnoredLayers;
            var layers = ((GraphWindow)d).LayGeoms;
            ProcIgnored(ignoredLayers, layers);
        }
        private static void ProcIgnored(IDictionary<string, bool> igLayers, IList<LayerGeometryCollection> layers)
        {
            if ((igLayers is not null) && (layers is not null))
            {
                for (int i = 0; i < layers.Count(); i++)
                {
                    foreach (var key in igLayers.Keys)
                    {
                        if (layers[i].LayerName.Contains(key, StringComparison.InvariantCultureIgnoreCase))
                        {
                            layers[i] = layers[i] with { LayerEnable = igLayers[key] };
                        }
                    }
                }
            }
        }
        private class MyStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string? x, string? y)
            {
                return x.Contains(y, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode([DisallowNull] string obj)
            {
                throw new NotImplementedException();
            }
        }

        public Dictionary<string, bool> IgnoredLayers
        {
            get { return (Dictionary<string, bool>)GetValue(IgnoredLayersProperty); }
            set { SetValue(IgnoredLayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IgnoredLayers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IgnoredLayersProperty =
            DependencyProperty.Register("IgnoredLayers", typeof(Dictionary<string, bool>), typeof(GraphWindow), new PropertyMetadata(null,
                new PropertyChangedCallback(IgnoredLayersChanged)));

        private static void IgnoredLayersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LayGeomsChanged(d, e);
         //   ((GraphWindow)d).MyMenu.GetBindingExpression(GraphEditorMenu.LayersProperty).UpdateTarget();
        }

        public bool PointerVisibility
        {
            get { return (bool)GetValue(PointerVisibilityProperty); }
            set { SetValue(PointerVisibilityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointerVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerVisibilityProperty =
            DependencyProperty.Register("PointerVisibility", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));




        public double PointerDiameter
        {   
            get { return (double)GetValue(PointerDiameterProperty); }
            set { SetValue(PointerDiameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointerDiameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerDiameterProperty =
            DependencyProperty.Register("PointerDiameter", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerThickness
        {   
            get { return (double)GetValue(PointerThicknessProperty); }
            set { SetValue(PointerThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointerThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerThicknessProperty =
            DependencyProperty.Register("PointerThickness", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerX
        {
            get { return (double)GetValue(PointerXProperty); }
            set { SetValue(PointerXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointerX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerXProperty =
            DependencyProperty.Register("PointerX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerY
        {
            get { return (double)GetValue(PointerYProperty); }
            set { SetValue(PointerYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PointerY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerYProperty =
            DependencyProperty.Register("PointerY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));





        public void SetScale(double scaleX, double scaleY)
        {
            ScaleX = scaleX;
            ScaleY = scaleY;
        }
        public void SetMargins(double marginX, double marginY)
        {
            MarginX = marginX;
            MarginY = marginY;
        }
        public double MarginX
        {
            get { return (double)GetValue(MarginXProperty); }
            protected set { SetValue(MarginXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MarginX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarginXProperty =
            DependencyProperty.Register("MarginX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));


        public double MarginY
        {
            get { return (double)GetValue(MarginYProperty); }
            protected set { SetValue(MarginYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MarginY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarginYProperty =
            DependencyProperty.Register("MarginY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));

        public double FieldMarginX
        {
            get { return (double)GetValue(FieldMarginXProperty); }
            protected set { SetValue(FieldMarginXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FieldMarginX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldMarginXProperty =
            DependencyProperty.Register("FieldMarginX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));


        public double FieldMarginY
        {
            get { return (double)GetValue(FieldMarginYProperty); }
            protected set { SetValue(FieldMarginYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FieldMarginY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldMarginYProperty =
            DependencyProperty.Register("FieldMarginY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));

        public double ScaleY
        {
            get { return (double)GetValue(ScaleYProperty); }
            protected set { SetValue(ScaleYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScaleY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleYProperty =
            DependencyProperty.Register("ScaleY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double ScaleX
        {
            get { return (double)GetValue(ScaleXProperty); }
            protected set
            {
                SetValue(ScaleXProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for ScaleX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleXProperty =
            DependencyProperty.Register("ScaleX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StrokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public bool XProportion
        {
            get { return (bool)GetValue(XProportionProperty); }
            set { SetValue(XProportionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XProportionProperty =
            DependencyProperty.Register("XProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));



        public bool YProportion
        {
            get { return (bool)GetValue(YProportionProperty); }
            set { SetValue(YProportionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for YProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YProportionProperty =
            DependencyProperty.Register("YProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));





        public bool AutoProportion
        {
            get { return (bool)GetValue(AutoProportionProperty); }
            set { SetValue(AutoProportionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoProportionProperty =
            DependencyProperty.Register("AutoProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)true));



        public double SpecMargin
        {
            get { return (double)GetValue(SpecMarginProperty); }
            set { SetValue(SpecMarginProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpecMargin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecMarginProperty =
            DependencyProperty.Register("SpecMargin", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double SpecSizeX
        {
            get { return (double)GetValue(SpecSizeXProperty); }
            set { SetValue(SpecSizeXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpecSizeX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecSizeXProperty =
            DependencyProperty.Register("SpecSizeX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double SpecSizeY
        {
            get { return (double)GetValue(SpecSizeYProperty); }
            set { SetValue(SpecSizeYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpecSizeY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecSizeYProperty =
            DependencyProperty.Register("SpecSizeY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));

        public double FieldSizeX
        {
            get { return (double)GetValue(FieldSizeXProperty); }
            set { SetValue(FieldSizeXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FieldSizeX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldSizeXProperty =
            DependencyProperty.Register("FieldSizeX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double FieldSizeY
        {
            get { return (double)GetValue(FieldSizeYProperty); }
            set { SetValue(FieldSizeYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FieldSizeY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldSizeYProperty =
            DependencyProperty.Register("FieldSizeY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));

        public bool MirrorX
        {
            get { return (bool)GetValue(MirrorXProperty); }
            set { SetValue(MirrorXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MirrorX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MirrorXProperty =
            DependencyProperty.Register("MirrorX", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public bool Angle90
        {
            get { return (bool)GetValue(Angle90Property); }
            set { SetValue(Angle90Property, value); }
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Angle90Property =
            DependencyProperty.Register("Angle90", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public double OffsetX
        {
            get { return (double)GetValue(OffsetXProperty); }
            set { SetValue(OffsetXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffsetX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetXProperty =
            DependencyProperty.Register("OffsetX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double OffsetY
        {
            get { return (double)GetValue(OffsetYProperty); }
            set { SetValue(OffsetYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffsetY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.Register("OffsetY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));

        private void GraphEditorMenu_TheItemChecked(object sender, EventArgs e)
        {
            var args = (ItemArgs)e;
            var temp = LayGeoms[args.LayerName];
            LayGeoms[args.LayerName] = temp with { LayerEnable = args.Enable };
        }

        private void MyMenu_LayerFiltersChanged(object sender, IEnumerable<LayerFilter> e)
        {
            ProcIgnored(new LayerFilterAdapter(e), LayGeoms);
            ((GraphEditorMenu)sender).GetBindingExpression(GraphEditorMenu.LayersProperty).UpdateTarget();
        }
        private class LayerFilterAdapter : IDictionary<string, bool>
        {
            private readonly IDictionary<string, bool> keyValuePairs;
            public LayerFilterAdapter(IEnumerable<LayerFilter> layerFilters)
            {
                keyValuePairs = new Dictionary<string, bool>();
                foreach (var filter in layerFilters)
                {
                    keyValuePairs.Add(filter.name, filter.enable);
                }
            }

            public bool this[string key]
            {
                get => keyValuePairs[key];
                set => throw new NotImplementedException();
            }

            public ICollection<string> Keys => keyValuePairs.Keys;

            public ICollection<bool> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public bool IsReadOnly => throw new NotImplementedException();

            public void Add(string key, bool value)
            {
                throw new NotImplementedException();
            }

            public void Add(KeyValuePair<string, bool> item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, bool> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, bool>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, bool>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, bool> item)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out bool value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        private void MyMenu_Rotate90Changed()
        {
            Angle90 ^= true;
        }

        private void MyMenu_MirrorXChanged()
        {
            MirrorX ^= true;
        }
    }
}
