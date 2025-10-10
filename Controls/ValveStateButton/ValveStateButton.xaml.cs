using CommunityToolkit.Mvvm.Input;
using PropertyChanged;
using System;
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


        public HorizontalAlignment SensorAlignment { get; set; } = HorizontalAlignment.Center;

        public static readonly DependencyProperty CommandProperty =
           DependencyProperty.Register(
               nameof(Command),
               typeof(ICommand),
               typeof(ValveStateButton),
               new PropertyMetadata(null));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(ValveStateButton),
                new PropertyMetadata(null));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }


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



        public string Valve_1_Name
        {
            get { return (string)GetValue(Valve_1_NameProperty); }
            set { SetValue(Valve_1_NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValveName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Valve_1_NameProperty =
            DependencyProperty.Register("Valve_1_Name", typeof(string), typeof(ValveStateButton), new PropertyMetadata(string.Empty));

        public string Valve_2_Name
        {
            get { return (string)GetValue(Valve_2_NameProperty); }
            set { SetValue(Valve_2_NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValveName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Valve_2_NameProperty =
            DependencyProperty.Register("Valve_2_Name", typeof(string), typeof(ValveStateButton), new PropertyMetadata(string.Empty, OnValveNameChanged));

        private static void OnValveNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValveStateButton button && (string)e.NewValue != string.Empty) 
            {
                button.SensorAlignment = HorizontalAlignment.Left;
                button.LeftSensor.HorizontalAlignment = HorizontalAlignment.Left;
                button.LeftSensor.Margin = new Thickness(20,3,3,3);
                button.RightSensor.Visibility = Visibility.Visible;
                button.RightSensor.Margin = new Thickness(3,3,20,3);
                button.SecondNameName.Visibility = Visibility.Visible;
                button.SecondNameSlash.Visibility = Visibility.Visible;
            }
        }

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



        public bool Sensor_1_IsOn
        {
            get { return (bool)GetValue(Sensor_1_IsOnProperty); }
            set { SetValue(Sensor_1_IsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SensorIsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Sensor_1_IsOnProperty =
            DependencyProperty.Register("Sensor_1_IsOn", typeof(bool), typeof(ValveStateButton), new PropertyMetadata(false));
        
        public bool Sensor_2_IsOn
        {
            get { return (bool)GetValue(Sensor_2_IsOnProperty); }
            set { SetValue(Sensor_2_IsOnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SensorIsOn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Sensor_2_IsOnProperty =
            DependencyProperty.Register("Sensor_2_IsOn", typeof(bool), typeof(ValveStateButton), new PropertyMetadata(false));
                     
    }
}
