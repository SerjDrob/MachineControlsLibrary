using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MachineControlsLibrary.GrowlMessage;

public class GrowlAdorner : Adorner
{
    private readonly GrowlPanel _panel;
    private readonly VisualCollection _visuals;

    public GrowlAdorner(UIElement adornedElement) : base(adornedElement)
    {
        _visuals = new VisualCollection(this);

        _panel = new GrowlPanel
        {
            DataContext = GrowlService.Instance,
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), // Почти прозрачный, но hit-testable
                                                                          // Растягиваем на весь доступный размер
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        IsHitTestVisible = true;
        _visuals.Add(_panel);

        // Обновляем при изменении размера родителя
        if (adornedElement is FrameworkElement fe)
        {
            fe.SizeChanged += (s, e) => InvalidateMeasure();
        }
    }

    protected override int VisualChildrenCount => _visuals.Count;
    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override Size MeasureOverride(Size constraint)
    {
        // Не растягиваемся на бесконечность или размер родителя,
        // а измеряем контент по его желаемому размеру
        _panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return _panel.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Располагаем панель ровно в том размере, который она запросила
        var size = _panel.DesiredSize;
        _panel.Arrange(new Rect(size));
        return size;
    }
}

