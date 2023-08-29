using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using MachineControlsLibrary.Classes;
using Microsoft.Toolkit.Mvvm.Input;
using PropertyChanged;

namespace MachineControlsLibrary.Controls
{
    /// <summary>
    /// Interaction logic for EditorSettings.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class EditorSettings : UserControl
    {

        public EditorSettings()
        {
            InitializeComponent();
            SettingsGrid.DataContext = this;
        }
        public string FilterItem
        {
            get; set;
        }
        public bool FilterItemEnable { get; set; } = true;
        public ObservableCollection<LayerFilter> Filters { get; private set; } = new();
        public event EventHandler<IEnumerable<LayerFilter>> FiltersChanged;
        [ICommand]
        public void AddFilter()
        {
            if (FilterItem is not null)
            {
                var filter = FilterItem.Trim();
                if (filter.Count() > 0)
                {
                    Filters.Add(new LayerFilter(filter, FilterItemEnable));
                    FilterItem = string.Empty;
                    FilterItemEnable = true;
                    FiltersChanged?.Invoke(this, Filters);
                }
            }
        }
        [ICommand]
        public void DeleteFilter(object? filter)
        {
            if (Filters.Count > 0)
            {
                if (filter is not null)
                {
                    var myFilter = (LayerFilter)filter;
                    var result = Filters.Remove(myFilter);
                    FiltersChanged?.Invoke(this, Filters);
                }
            }
        }

    }
}
