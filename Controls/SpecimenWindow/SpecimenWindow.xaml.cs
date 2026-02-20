using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MachineControlsLibrary.Classes;
using Xceed.Wpf.AvalonDock.Controls;

namespace MachineControlsLibrary.Controls;



/// <summary>
/// Interaction logic for SpecimenWindow.xaml
/// </summary>

[INotifyPropertyChanged]
public partial class SpecimenWindow : UserControl
{
    public SpecimenWindow()
    {
        InitializeComponent();
        SpecWin.DataContext = this;
        //MouseWheel += SpecimenWindow_MouseWheel;

    }
    public float Zoomfactor { get; set; } = 1.1f;
    private readonly MatrixTransform _transform = new();
    private void SpecimenWindow_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        //var scaleFactor = Zoomfactor;
        //if (e.Delta < 0)
        //{
        //    scaleFactor = 1f / scaleFactor;
        //}

        //Point mousePosition = e.GetPosition(this);

        //Matrix scaleMatrix = _transform.Matrix;
        //scaleMatrix.ScaleAt(scaleFactor, scaleFactor, mousePosition.X, mousePosition.Y);
        //_transform.Matrix = scaleMatrix;

        //foreach (UIElement child in this.MainCanvas.Children)
        //{
        //    var x = Canvas.GetLeft(child);
        //    var y = Canvas.GetTop(child);

        //    var sx = x * scaleFactor;
        //    var sy = y * scaleFactor;

        //    Canvas.SetLeft(child, sx);
        //    Canvas.SetTop(child, sy);

        //    child.RenderTransform = _transform;
        //}
    }

    private double _scalex;
    private double _scaley;
    private double _marginx;
    private double _marginy; 
    private bool _isSelectionInitiated = false;
    private Point _sbStartPoint;
    private ItemsControl? _itemsControl;

    public event EventHandler<Rect> GotSelectionEvent;
    public event EventHandler<Point> GotSpecimenClickedEvent;
    [ObservableProperty] public partial bool IsSelectionBoxVisible { get; set; }
    [ObservableProperty] public partial double SelectionBoxX { get; set; }
    [ObservableProperty] public partial double SelectionBoxY { get; set; }
    [ObservableProperty] public partial double SelectionBoxWidth { get; set; }
    [ObservableProperty] public partial double SelectionBoxHeight { get; set; }
    [ObservableProperty] public partial ScaleTransform SelectionBoxScaleTransform { get; set; } = new ScaleTransform(1, 1);



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
        DependencyProperty.Register("IsLoading", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata(false));



    public bool IsFillPath
    {
        get
        {
            return (bool)GetValue(IsFillPathProperty);
        }
        set
        {
            SetValue(IsFillPathProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for IsFillPath.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsFillPathProperty =
        DependencyProperty.Register("IsFillPath", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata(false));



    public bool LightPathModeOn
    {
        get
        {
            return (bool)GetValue(LightPathModeOnProperty);
        }
        set
        {
            SetValue(LightPathModeOnProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for LightPathModeOn.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty LightPathModeOnProperty =
        DependencyProperty.Register("LightPathModeOn", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata(false));



    public bool CutCursor
    {
        get
        {
            return (bool)GetValue(CutCursorProperty);
        }
        set
        {
            SetValue(CutCursorProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for CutCursor.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CutCursorProperty =
        DependencyProperty.Register("CutCursor", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata(false));


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
        DependencyProperty.Register("LayGeoms", typeof(ObservableCollection<LayerGeometryCollection>), typeof(SpecimenWindow),
            new PropertyMetadata(null, new PropertyChangedCallback(LayGeomsChanged)));

    private static void LayGeomsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var spec = d as SpecimenWindow;
        if (spec is not null)
        {
            spec.IsLoading = false;
        }
    }



    public bool IsVisible
    {
        get
        {
            return (bool)GetValue(IsVisibleProperty);
        }
        set
        {
            SetValue(IsVisibleProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for IsVisible.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsVisibleProperty =
        DependencyProperty.Register("IsVisible", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata(false));



    public double MarginX
    {
        get
        {
            return (double)GetValue(MarginXProperty);
        }
        protected set
        {
            SetValue(MarginXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for MarginX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MarginXProperty =
        DependencyProperty.Register("MarginX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));


    public double MarginY
    {
        get
        {
            return (double)GetValue(MarginYProperty);
        }
        protected set
        {
            SetValue(MarginYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for MarginY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MarginYProperty =
        DependencyProperty.Register("MarginY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double ScaleY
    {
        get
        {
            return (double)GetValue(ScaleYProperty);
        }
        protected set
        {
            SetValue(ScaleYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for ScaleY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ScaleYProperty =
        DependencyProperty.Register("ScaleY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public double ScaleX
    {
        get
        {
            return (double)GetValue(ScaleXProperty);
        }
        protected set
        {
            SetValue(ScaleXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for ScaleX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ScaleXProperty =
        DependencyProperty.Register("ScaleX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public double StrokeThickness
    {
        get
        {
            return (double)GetValue(StrokeThicknessProperty);
        }
        set
        {
            SetValue(StrokeThicknessProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for StrokeThickness.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register("StrokeThickness", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public bool XProportion
    {
        get
        {
            return (bool)GetValue(XProportionProperty);
        }
        set
        {
            SetValue(XProportionProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for XProportion.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty XProportionProperty =
        DependencyProperty.Register("XProportion", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata((bool)false));



    public bool YProportion
    {
        get
        {
            return (bool)GetValue(YProportionProperty);
        }
        set
        {
            SetValue(YProportionProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for YProportion.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty YProportionProperty =
        DependencyProperty.Register("YProportion", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata((bool)false));

    public bool AutoProportion
    {
        get
        {
            return (bool)GetValue(AutoProportionProperty);
        }
        set
        {
            SetValue(AutoProportionProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for AutoProportion.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AutoProportionProperty =
        DependencyProperty.Register("AutoProportion", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata((bool)true));

    public double SpecMargin
    {
        get
        {
            return (double)GetValue(SpecMarginProperty);
        }
        set
        {
            SetValue(SpecMarginProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for SpecMargin.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SpecMarginProperty =
        DependencyProperty.Register("SpecMargin", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double SpecSizeX
    {
        get
        {
            return (double)GetValue(SpecSizeXProperty);
        }
        set
        {
            SetValue(SpecSizeXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for SpecSizeX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SpecSizeXProperty =
        DependencyProperty.Register("SpecSizeX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public double SpecSizeY
    {
        get
        {
            return (double)GetValue(SpecSizeYProperty);
        }
        set
        {
            SetValue(SpecSizeYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for SpecSizeY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SpecSizeYProperty =
        DependencyProperty.Register("SpecSizeY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));

    public double FieldSizeX
    {
        get
        {
            return (double)GetValue(FieldSizeXProperty);
        }
        set
        {
            SetValue(FieldSizeXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for FieldSizeX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FieldSizeXProperty =
        DependencyProperty.Register("FieldSizeX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public double FieldSizeY
    {
        get
        {
            return (double)GetValue(FieldSizeYProperty);
        }
        set
        {
            SetValue(FieldSizeYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for SpecSizeY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty FieldSizeYProperty =
        DependencyProperty.Register("FieldSizeY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));

    public ObservableCollection<GeometryCollection> Shapes
    {
        get
        {
            return (ObservableCollection<GeometryCollection>)GetValue(ShapesProperty);
        }
        set
        {
            SetValue(ShapesProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for Shapes.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ShapesProperty =
        DependencyProperty.Register("Shapes", typeof(ObservableCollection<GeometryCollection>),
            typeof(SpecimenWindow), new PropertyMetadata(null));


    public double MirrorX
    {
        get
        {
            return (double)GetValue(MirrorXProperty);
        }
        set
        {
            SetValue(MirrorXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for MirrorX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty MirrorXProperty =
        DependencyProperty.Register("MirrorX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)1));


    public double Angle
    {
        get
        {
            return (double)GetValue(AngleProperty);
        }
        set
        {
            SetValue(AngleProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AngleProperty =
        DependencyProperty.Register("Angle", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));


    public double OffsetX
    {
        get
        {
            return (double)GetValue(OffsetXProperty);
        }
        set
        {
            SetValue(OffsetXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for OffsetX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OffsetXProperty =
        DependencyProperty.Register("OffsetX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double OffsetY
    {
        get
        {
            return (double)GetValue(OffsetYProperty);
        }
        set
        {
            SetValue(OffsetYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for OffsetY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty OffsetYProperty =
        DependencyProperty.Register("OffsetY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));


    public bool PointerVisibility
    {
        get
        {
            return (bool)GetValue(PointerVisibilityProperty);
        }
        set
        {
            SetValue(PointerVisibilityProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for PointerVisibility.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PointerVisibilityProperty =
        DependencyProperty.Register("PointerVisibility", typeof(bool), typeof(SpecimenWindow), new PropertyMetadata((bool)false));




    public double PointerDiameter
    {
        get
        {
            return (double)GetValue(PointerDiameterProperty);
        }
        set
        {
            SetValue(PointerDiameterProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for PointerDiameter.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PointerDiameterProperty =
        DependencyProperty.Register("PointerDiameter", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double PointerThickness
    {
        get
        {
            return (double)GetValue(PointerThicknessProperty);
        }
        set
        {
            SetValue(PointerThicknessProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for PointerThickness.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PointerThicknessProperty =
        DependencyProperty.Register("PointerThickness", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double PointerX
    {
        get
        {
            return (double)GetValue(PointerXProperty);
        }
        set
        {
            SetValue(PointerXProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for PointerX.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PointerXProperty =
        DependencyProperty.Register("PointerX", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));



    public double PointerY
    {
        get
        {
            return (double)GetValue(PointerYProperty);
        }
        set
        {
            SetValue(PointerYProperty, value);
        }
    }

    // Using a DependencyProperty as the backing store for PointerY.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty PointerYProperty =
        DependencyProperty.Register("PointerY", typeof(double), typeof(SpecimenWindow), new PropertyMetadata((double)0));


    private void SpecWin_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CutCursor)
        {
            _isSelectionInitiated = true;
            IsSelectionBoxVisible = true;
            var grid = sender as Grid;
            var position = e.GetPosition(grid);
            SelectionBoxX = position.X;
            SelectionBoxY = position.Y;

            _itemsControl = grid.FindVisualChildren<ItemsControl>().SingleOrDefault(ch => ch.Name == "DxfItems");
            if (_itemsControl is not null) _sbStartPoint = e.GetPosition(_itemsControl);
        }
        else
        {
            /*
            var grid = sender as Grid;
            var matrix = grid?.Resources["MTrans"] as MatrixTransform;
            var invMatrix = matrix?.Inverse;
            _itemsControl = grid.FindVisualChildren<ItemsControl>().SingleOrDefault(ch => ch.Name == "DxfItems");
            if (invMatrix is not null && _itemsControl is not null)
            {
                var point = e.GetPosition(_itemsControl);
                var resultPoint = invMatrix.Transform(point);
                GotSpecimenClickedEvent?.Invoke(this, resultPoint);
            }*/
            var point2 = e.GetPosition(fieldGrid);
            var mirror = new ScaleTransform(1, -1, 0, FieldSizeY / 2);
            var mirPoint = mirror.Transform(point2);
            GotSpecimenClickedEvent?.Invoke(this, mirPoint);
        }
        e.Handled = true;
    }


    private void SpecWin_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isSelectionInitiated)
        {
            var grid = sender as Grid;
            var position = e.GetPosition(grid);
            var width = position.X - SelectionBoxX;
            var height = position.Y - SelectionBoxY;
            SelectionBoxWidth = Math.Abs(width);
            SelectionBoxHeight = Math.Abs(height);
            SelectionBoxScaleTransform = new ScaleTransform(Math.Sign(width), Math.Sign(height));
        }
        e.Handled = true;
    }

    private void SpecWin_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CutCursor)
        {
            var grid = sender as Grid;
            var matrix = grid?.Resources["MTrans"] as MatrixTransform;
            var invMatrix = matrix?.Inverse;

            if (invMatrix is not null && _itemsControl is not null)
            {
                var endPoint = e.GetPosition(_itemsControl);
                var rect = new Rect(_sbStartPoint, endPoint);
                var rect1 = invMatrix.TransformBounds(rect);

                GotSelectionEvent?.Invoke(this, rect1);
            }

            _isSelectionInitiated = false;
            IsSelectionBoxVisible = false;
            SelectionBoxWidth = 0;
            SelectionBoxHeight = 0;
            e.Handled = true;
        }
    }

    [RelayCommand]
    private void LayGeomClicked(object? obj)
    {
            if(obj is Point point) OnGeometryClickEvent?.Invoke(this, point);
    }

    public event RoutedGeomClickEventHandler OnGeometryClickEvent;

    //private void Path_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    //{
    //    try
    //    {
    //        var point = (Point)e.GetPosition();
    //        OnGeometryClickEvent?.Invoke(this, point);
    //    }
    //    catch (Exception)
    //    {

    //        //throw;
    //    }
    //}
}
public delegate void RoutedGeomClickEventHandler(object? obj, GeomClickEventArgs clickEventArgs);
