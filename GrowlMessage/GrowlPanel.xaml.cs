using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MachineControlsLibrary.GrowlMessage;

/// <summary>
/// Interaction logic for GrowlPanel.xaml
/// </summary>
public partial class GrowlPanel : UserControl
{
    public GrowlPanel()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            var view = CollectionViewSource.GetDefaultView(this.MyMessages.ItemsSource);
            view.CollectionChanged += (s, e) =>
            {

            };
        };
        
    }
    protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
    {
        return null;
    }
    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return null;
    }
}
