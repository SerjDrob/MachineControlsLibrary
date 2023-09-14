using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Controls.GraphWin;

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
        }

        public event RoutedSelectionEventHandler GotSelectionEvent;
        public event RoutedPointClickedEventHandler GotPointClickedEvent;
        public float Zoomfactor { get; set; } = 1.1f;
        private readonly MatrixTransform _transform = new MatrixTransform();
        public bool IsFillPath
        {
            get => (bool)GetValue(IsFillPathProperty);
            set => SetValue(IsFillPathProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsFillPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsFillPathProperty =
            DependencyProperty.Register("IsFillPath", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public bool IsMarkTextVisible
        {
            get
            {
                return (bool)GetValue(IsMarkTextVisibleProperty);
            }
            set
            {
                SetValue(IsMarkTextVisibleProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for IsMarkTextVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMarkTextVisibleProperty =
            DependencyProperty.Register("IsMarkTextVisible", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public bool LightPathModeOn
        {
            get => (bool)GetValue(LightPathModeOnProperty);
            set => SetValue(LightPathModeOnProperty, value);
        }

        // Using a DependencyProperty as the backing store for LightPathModeOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LightPathModeOnProperty =
            DependencyProperty.Register("LightPathModeOn", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public TextPosition TextPosition
        {
            get => (TextPosition)GetValue(TextPositionProperty);
            set => SetValue(TextPositionProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextPosition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextPositionProperty =
            DependencyProperty.Register("TextPosition", typeof(TextPosition), typeof(GraphWindow), new PropertyMetadata(TextPosition.W));



        public bool CutCursor
        {
            get => (bool)GetValue(CutCursorProperty);
            set => SetValue(CutCursorProperty, value);
        }

        // Using a DependencyProperty as the backing store for CutCursor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CutCursorProperty =
            DependencyProperty.Register("CutCursor", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));




        public string MarkText
        {
            get => (string)GetValue(MarkTextProperty);
            set => SetValue(MarkTextProperty, value);
        }

        // Using a DependencyProperty as the backing store for MarkText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarkTextProperty =
            DependencyProperty.Register("MarkText", typeof(string), typeof(GraphWindow), new PropertyMetadata(null));



        public int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // Using a DependencyProperty as the backing store for FontSize.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(int), typeof(GraphWindow), new PropertyMetadata(0));



        public Brush SelectedColor
        {
            get => (Brush)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(GraphWindow), new PropertyMetadata(Brushes.Gray));

        public ObservableCollection<LayerGeometryCollection> LayGeoms
        {
            get => (ObservableCollection<LayerGeometryCollection>)GetValue(LayGeomsProperty);
            set => SetValue(LayGeomsProperty, value);
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




        public string FileName
        {
            get => (string)GetValue(FileNameProperty);
            set => SetValue(FileNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for FileName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FileNameProperty =
            DependencyProperty.Register("FileName", typeof(string), typeof(GraphWindow), new PropertyMetadata(null));




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

        public Dictionary<string, bool> IgnoredLayers
        {
            get => (Dictionary<string, bool>)GetValue(IgnoredLayersProperty);
            set => SetValue(IgnoredLayersProperty, value);
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
            get => (bool)GetValue(PointerVisibilityProperty);
            set => SetValue(PointerVisibilityProperty, value);
        }

        // Using a DependencyProperty as the backing store for PointerVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerVisibilityProperty =
            DependencyProperty.Register("PointerVisibility", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));




        public double PointerDiameter
        {
            get => (double)GetValue(PointerDiameterProperty);
            set => SetValue(PointerDiameterProperty, value);
        }

        // Using a DependencyProperty as the backing store for PointerDiameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerDiameterProperty =
            DependencyProperty.Register("PointerDiameter", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerThickness
        {
            get => (double)GetValue(PointerThicknessProperty);
            set => SetValue(PointerThicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for PointerThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerThicknessProperty =
            DependencyProperty.Register("PointerThickness", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerX
        {
            get => (double)GetValue(PointerXProperty);
            set => SetValue(PointerXProperty, value);
        }

        // Using a DependencyProperty as the backing store for PointerX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PointerXProperty =
            DependencyProperty.Register("PointerX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double PointerY
        {
            get => (double)GetValue(PointerYProperty);
            set => SetValue(PointerYProperty, value);
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
            get => (double)GetValue(MarginXProperty);
            protected set => SetValue(MarginXProperty, value);
        }

        // Using a DependencyProperty as the backing store for MarginX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarginXProperty =
            DependencyProperty.Register("MarginX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));


        public double MarginY
        {
            get => (double)GetValue(MarginYProperty);
            protected set => SetValue(MarginYProperty, value);
        }

        // Using a DependencyProperty as the backing store for MarginY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MarginYProperty =
            DependencyProperty.Register("MarginY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));

        public double FieldMarginX
        {
            get => (double)GetValue(FieldMarginXProperty);
            protected set => SetValue(FieldMarginXProperty, value);
        }

        // Using a DependencyProperty as the backing store for FieldMarginX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldMarginXProperty =
            DependencyProperty.Register("FieldMarginX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));


        public double FieldMarginY
        {
            get => (double)GetValue(FieldMarginYProperty);
            protected set => SetValue(FieldMarginYProperty, value);
        }

        // Using a DependencyProperty as the backing store for FieldMarginY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldMarginYProperty =
            DependencyProperty.Register("FieldMarginY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));


        public void SetFieldMargins(double marginX, double marginY)
        {
            FieldMarginX = marginX;
            FieldMarginY = marginY;
        }


        public double ScaleY
        {
            get => (double)GetValue(ScaleYProperty);
            protected set => SetValue(ScaleYProperty, value);
        }

        // Using a DependencyProperty as the backing store for ScaleY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleYProperty =
            DependencyProperty.Register("ScaleY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double ScaleX
        {
            get => (double)GetValue(ScaleXProperty);
            protected set => SetValue(ScaleXProperty, value);
        }

        // Using a DependencyProperty as the backing store for ScaleX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScaleXProperty =
            DependencyProperty.Register("ScaleX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        // Using a DependencyProperty as the backing store for StrokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public bool XProportion
        {
            get => (bool)GetValue(XProportionProperty);
            set => SetValue(XProportionProperty, value);
        }

        // Using a DependencyProperty as the backing store for XProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XProportionProperty =
            DependencyProperty.Register("XProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));



        public bool YProportion
        {
            get => (bool)GetValue(YProportionProperty);
            set => SetValue(YProportionProperty, value);
        }

        // Using a DependencyProperty as the backing store for YProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YProportionProperty =
            DependencyProperty.Register("YProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)false));





        public bool AutoProportion
        {
            get
            => (bool)GetValue(AutoProportionProperty);
            set
            {
                SetValue(AutoProportionProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for AutoProportion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoProportionProperty =
            DependencyProperty.Register("AutoProportion", typeof(bool), typeof(GraphWindow), new PropertyMetadata((bool)true));



        public double SpecMargin
        {
            get
            => (double)GetValue(SpecMarginProperty);
            set
            {
                SetValue(SpecMarginProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SpecMargin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecMarginProperty =
            DependencyProperty.Register("SpecMargin", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double SpecSizeX
        {
            get => (double)GetValue(SpecSizeXProperty);
            set => SetValue(SpecSizeXProperty, value);
        }

        // Using a DependencyProperty as the backing store for SpecSizeX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecSizeXProperty =
            DependencyProperty.Register("SpecSizeX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double SpecSizeY
        {
            get => (double)GetValue(SpecSizeYProperty);
            set => SetValue(SpecSizeYProperty, value);
        }

        // Using a DependencyProperty as the backing store for SpecSizeY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpecSizeYProperty =
            DependencyProperty.Register("SpecSizeY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));

        public double FieldSizeX
        {
            get => (double)GetValue(FieldSizeXProperty);
            set => SetValue(FieldSizeXProperty, value);
        }

        // Using a DependencyProperty as the backing store for FieldSizeX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldSizeXProperty =
            DependencyProperty.Register("FieldSizeX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));


        public double FieldSizeY
        {
            get => (double)GetValue(FieldSizeYProperty);
            set => SetValue(FieldSizeYProperty, value);
        }

        // Using a DependencyProperty as the backing store for FieldSizeY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FieldSizeYProperty =
            DependencyProperty.Register("FieldSizeY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)1));

        public bool MirrorX
        {
            get => (bool)GetValue(MirrorXProperty);
            set => SetValue(MirrorXProperty, value);
        }

        // Using a DependencyProperty as the backing store for MirrorX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MirrorXProperty =
            DependencyProperty.Register("MirrorX", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public bool Angle90
        {
            get => (bool)GetValue(Angle90Property);
            set => SetValue(Angle90Property, value);
        }

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Angle90Property =
            DependencyProperty.Register("Angle90", typeof(bool), typeof(GraphWindow), new PropertyMetadata(false));



        public double OffsetX
        {
            get => (double)GetValue(OffsetXProperty);
            set => SetValue(OffsetXProperty, value);
        }

        // Using a DependencyProperty as the backing store for OffsetX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetXProperty =
            DependencyProperty.Register("OffsetX", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));



        public double OffsetY
        {
            get => (double)GetValue(OffsetYProperty);
            set => SetValue(OffsetYProperty, value);
        }

        // Using a DependencyProperty as the backing store for OffsetY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.Register("OffsetY", typeof(double), typeof(GraphWindow), new PropertyMetadata((double)0));

        private void GraphEditorMenu_TheItemChecked(object sender, EventArgs e)
        {
            var args = (ItemArgs)e;
            var temp = LayGeoms[args.LayerName];
            LayGeoms[args.LayerName] = temp with { LayerEnable = args.Enable };
            IgnoredLayers[LayGeoms[args.LayerName].LayerName] = args.Enable;
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

        private void Specimen_GotSelectionEvent(object sender, Rect e)
        {
            GotSelectionEvent?.Invoke(sender, e);
        }

        private void CanvasView_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var viewBox = sender as Viewbox;
            var scaleFactor = e.Delta < 0 ? 1f / Zoomfactor : Zoomfactor;

            var mousePosition = e.GetPosition(viewBox);

            var matrix = viewBox.Child.RenderTransform;
            var scaleMatrix = matrix.Value;
            scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePosition.X, mousePosition.Y);

            _transform.Matrix = scaleMatrix;
            viewBox.Child.RenderTransform = _transform;
        }

        private void Specimen_GotSpecimenClickedEvent(object sender, Point e)
        {
            GotPointClickedEvent?.Invoke(this, e);
        }
    }

    public delegate void RoutedSelectionEventHandler(object sender, RoutedSelectionEventArgs e);
    public delegate void RoutedPointClickedEventHandler(object sender, RoutedPointClickedEventArgs e);

    public class RoutedPointClickedEventArgs : RoutedEventArgs
    {
        public Point Point
        {
            get;
            set;
        }

        public RoutedPointClickedEventArgs(Point point)
        {
            Point = point;
        }

        public static implicit operator RoutedPointClickedEventArgs(Point point) => new(point);
        public static implicit operator Point(RoutedPointClickedEventArgs args) => args.Point;
     }


    public class RoutedSelectionEventArgs : RoutedEventArgs
    {
        public Rect Selection
        {
            get; init;
        }

        public RoutedSelectionEventArgs(Rect selection)
        {
            Selection = selection;
        }
        public static explicit operator Rect(RoutedSelectionEventArgs e) => e.Selection;
        public static implicit operator RoutedSelectionEventArgs(Rect rect) => new RoutedSelectionEventArgs(rect);
    }
}
