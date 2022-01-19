using System;
using System.Collections.Generic;
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
    /// Interaction logic for SpecCross.xaml
    /// </summary>
    public partial class SpecCross : UserControl
    {
        public SpecCross()
        {
            InitializeComponent();
            Cross.DataContext = this;
        }




        public double OffsetX
        {
            get { return (double)GetValue(OffsetXProperty); }
            set { SetValue(OffsetXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffsetX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetXProperty =
            DependencyProperty.Register("OffsetX", typeof(double), typeof(SpecCross), new PropertyMetadata((double)0));



        public double OffsetY
        {
            get { return (double)GetValue(OffsetYProperty); }
            set { SetValue(OffsetYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OffsetY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.Register("OffsetY", typeof(double), typeof(SpecCross), new PropertyMetadata((double)0));



        public double XScale
        {
            get { return (double)GetValue(XScaleProperty); }
            set { SetValue(XScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for XScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XScaleProperty =
            DependencyProperty.Register("XScale", typeof(double), typeof(SpecCross), new PropertyMetadata((double)1));


        public double YScale
        {
            get { return (double)GetValue(YScaleProperty); }
            set { SetValue(YScaleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for YScale.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YScaleProperty =
            DependencyProperty.Register("YScale", typeof(double), typeof(SpecCross), new PropertyMetadata((double)1));




        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        // Using a DependencyProperty as the backing store for X.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(SpecCross), new PropertyMetadata((double)0));


        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Y.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(SpecCross), new PropertyMetadata((double)0));


        public double Thickness
        {
            get { return (double)GetValue(ThicknessProperty); }
            set { SetValue(ThicknessProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Thickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register("Thickness", typeof(double), typeof(SpecCross), new PropertyMetadata((double)0));


        public Brush Color
        {
            get { return (Brush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Brush), typeof(SpecCross), new PropertyMetadata(Brushes.Black));


    }
}
