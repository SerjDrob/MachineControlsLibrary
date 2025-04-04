﻿using PropertyChanged;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MachineControlsLibrary.Controls
{
    /// <summary>
    /// Interaction logic for ValveStateButton.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class ValveStateButton : UserControl
    {
        public ValveStateButton()
        {
            InitializeComponent();
            TheValveButton.DataContext = this;
        }




        public ICommand ValveCommand
        {
            get { return (ICommand)GetValue(ValveCommandProperty); }
            set { SetValue(ValveCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValveCommandProperty =
            DependencyProperty.Register("ValveCommand", typeof(ICommand), typeof(ValveStateButton), new PropertyMetadata(null));



        public Brush MyBackground
        {
            get { return (Brush)GetValue(MyBackgroundProperty); }
            set { SetValue(MyBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyBackgroundProperty =
            DependencyProperty.Register("MyBackground", typeof(Brush), typeof(ValveStateButton), new PropertyMetadata(null));




        public int CornerRadius
        {
            get { return (int)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CornerRadius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(int), typeof(ValveStateButton), new PropertyMetadata(0));



        public string ValveName
        {
            get { return (string)GetValue(ValveNameProperty); }
            set { SetValue(ValveNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValveName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValveNameProperty =
            DependencyProperty.Register("ValveName", typeof(string), typeof(ValveStateButton), new PropertyMetadata(string.Empty));



        public Brush BorderOnColor
        {
            get { return (Brush)GetValue(BorderOnColorProperty); }
            set { SetValue(BorderOnColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BorderOnColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderOnColorProperty =
            DependencyProperty.Register("BorderOnColor", typeof(Brush), typeof(ValveStateButton), new PropertyMetadata(Brushes.Green));




        public Brush BorderOffColor
        {
            get { return (Brush)GetValue(BorderOffColorProperty); }
            set { SetValue(BorderOffColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BorderOffColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BorderOffColorProperty =
            DependencyProperty.Register("BorderOffColor", typeof(Brush), typeof(ValveStateButton), new PropertyMetadata(Brushes.Red));


        public Brush SensorOnColor
        {
            get { return (Brush)GetValue(SensorOnColorProperty); }
            set { SetValue(SensorOnColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SensorOnColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SensorOnColorProperty =
            DependencyProperty.Register("SensorOnColor", typeof(Brush), typeof(ValveStateButton), new PropertyMetadata(Brushes.Green));


        public Brush SensorOffColor
        {
            get { return (Brush)GetValue(SensorOffColorProperty); }
            set { SetValue(SensorOffColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SensorOffColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SensorOffColorProperty =
            DependencyProperty.Register("SensorOffColor", typeof(Brush), typeof(ValveStateButton), new PropertyMetadata(Brushes.Red));



        public bool ValveIsOn
        {
            get { return (bool)GetValue(ValveIsOnProperty); }
            set { SetValue(ValveIsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValveIsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValveIsOnProperty =
            DependencyProperty.Register("ValveIsOn", typeof(bool), typeof(ValveStateButton), new PropertyMetadata(false));



        public bool SensorIsOn
        {
            get { return (bool)GetValue(SensorIsOnProperty); }
            set { SetValue(SensorIsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SensorIsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SensorIsOnProperty =
            DependencyProperty.Register("SensorIsOn", typeof(bool), typeof(ValveStateButton), new PropertyMetadata(false));

    }
}
