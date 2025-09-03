using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace MachineControlsLibrary.Controls
{
    /// <summary>
    /// Interaction logic for AxisState.xaml
    /// </summary>
    public partial class AxisState : UserControl
    {
        public AxisState()
        {
            InitializeComponent();
            TheAxis.DataContext = this;
        }


        public double CmdCoordinate
        {
            get => (double)GetValue(CmdCoordinateProperty);
            set => SetValue(CmdCoordinateProperty, value);
        }

        // Using a DependencyProperty as the backing store for CmdCoordinate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CmdCoordinateProperty =
            DependencyProperty.Register("CmdCoordinate", typeof(double), typeof(AxisState), new PropertyMetadata(default(double)));



        public double Coordinate
        {
            get => (double)GetValue(CoordinateProperty);
            set => SetValue(CoordinateProperty, value);
        }

        // Using a DependencyProperty as the backing store for Coordinate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordinateProperty =
            DependencyProperty.Register("Coordinate", typeof(double), typeof(AxisState), new PropertyMetadata(default(double)));


        public string CoordinateName
        {
            get => (string)GetValue(CoordinateNameProperty);
            set => SetValue(CoordinateNameProperty, value);
        }

        // Using a DependencyProperty as the backing store for CoordinateName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordinateNameProperty =
            DependencyProperty.Register("CoordinateName", typeof(string), typeof(AxisState), new PropertyMetadata(string.Empty));



        public bool LmtNeg
        {
            get => (bool)GetValue(LmtNegProperty);
            set => SetValue(LmtNegProperty, value);
        }

        // Using a DependencyProperty as the backing store for LmtNeg.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LmtNegProperty =
            DependencyProperty.Register("LmtNeg", typeof(bool), typeof(AxisState), new PropertyMetadata(false));


        public bool LmtPos
        {
            get => (bool)GetValue(LmtPosProperty);
            set => SetValue(LmtPosProperty, value);
        }

        // Using a DependencyProperty as the backing store for LmtPos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LmtPosProperty =
            DependencyProperty.Register("LmtPos", typeof(bool), typeof(AxisState), new PropertyMetadata(false));



        public bool MotionDone
        {
            get => (bool)GetValue(MotionDoneProperty);
            set => SetValue(MotionDoneProperty, value);
        }

        // Using a DependencyProperty as the backing store for MotionDone.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MotionDoneProperty =
            DependencyProperty.Register("MotionDone", typeof(bool), typeof(AxisState), new PropertyMetadata(true));



        public bool EZ
        {
            get { return (bool)GetValue(EZProperty); }
            set { SetValue(EZProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EZ.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EZProperty =
            DependencyProperty.Register("EZ", typeof(bool), typeof(AxisState), new PropertyMetadata(false));



        public Brush NegColor
        {
            get => (Brush)GetValue(NegColorProperty);
            set => SetValue(NegColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for NegColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NegColorProperty =
            DependencyProperty.Register("NegColor", typeof(Brush), typeof(AxisState), new PropertyMetadata(Brushes.Red));


        public Brush PosColor
        {
            get => (Brush)GetValue(PosColorProperty);
            set => SetValue(PosColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for PosColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PosColorProperty =
            DependencyProperty.Register("PosColor", typeof(Brush), typeof(AxisState), new PropertyMetadata(Brushes.Green));
        
        public Brush BusyColor
        {
            get => (Brush)GetValue(BusyColorProperty);
            set => SetValue(BusyColorProperty, value);
        }

        // Using a DependencyProperty as the backing store for PosColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BusyColorProperty =
            DependencyProperty.Register("BusyColor", typeof(Brush), typeof(AxisState), new PropertyMetadata(Brushes.Orange));



        public Brush TextBackground
        {
            get => (Brush)GetValue(TextBackgroundProperty);
            set => SetValue(TextBackgroundProperty, value);
        }

        // Using a DependencyProperty as the backing store for TextBackGround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBackgroundProperty =
            DependencyProperty.Register("TextBackground", typeof(Brush), typeof(AxisState), new PropertyMetadata(Brushes.AliceBlue));




        public AxStateLayout Layout
        {
            get => (AxStateLayout)GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        // Using a DependencyProperty as the backing store for Layout.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register("Layout", typeof(AxStateLayout), typeof(AxisState), new PropertyMetadata(AxStateLayout.Vertical));

    }
}
