using MachineControlsLibrary.Classes;
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
using Microsoft.Toolkit.Mvvm.Input;


namespace MachineControlsLibrary.Controls
{
    /// <summary>
    /// Interaction logic for GraphEditor.xaml
    /// </summary>
    public partial class GraphEditorMenu : UserControl
    {
        public GraphEditorMenu()
        {
            InitializeComponent();
            EditorMenu.DataContext = this;
        }
        [ICommand]
        private void ItemChecked(object obj)
        {
            var lg = (EnaLayer)obj;
            var index = Layers.IndexOf(lg);
            TheItemChecked?.Invoke(this, new ItemArgs(index,lg.Enable));
        }
        public event EventHandler TheItemChecked;   
        public event EventHandler<IEnumerable<LayerFilter>> LayerFiltersChanged;
        public ObservableCollection<EnaLayer> Layers
        {
            get { return (ObservableCollection<EnaLayer>)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register("Layers", typeof(ObservableCollection<EnaLayer>), typeof(GraphEditorMenu), new PropertyMetadata(null));

       
        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(GraphEditorMenu), new PropertyMetadata(Brushes.Gray));

        public void EditorSettings_FiltersChanged(object? obj, IEnumerable<LayerFilter> layerFilters)
        {
            LayerFiltersChanged(this, layerFilters);
        }
    }
}
