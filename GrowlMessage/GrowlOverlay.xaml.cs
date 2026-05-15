using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MachineControlsLibrary.GrowlMessage;

/// <summary>
/// Interaction logic for GrowlOverlay.xaml
/// </summary>
public partial class GrowlOverlay : UserControl
{
    private GrowlAdorner _adorner;
    private FrameworkElement _adornedElement;

    public GrowlOverlay()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Ищем ближайший FrameworkElement выше в дереве, 
        // который НЕ является служебным контейнером (ContentPresenter)
        _adornedElement = this.Parent as FrameworkElement;

        // Если родитель - ContentPresenter, берем его родителя (обычно это панель размещения)
        if (_adornedElement is ContentPresenter)
        {
            _adornedElement = VisualTreeHelper.GetParent(_adornedElement) as FrameworkElement;
        }

        if (_adornedElement != null)
        {
            AttachAdorner();
        }
    }

    private void OnLayoutUpdated(object sender, EventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;

        var element = FindAdornedElement();
        if (element != null)
        {
            _adornedElement = element;
            AttachAdorner();
        }
    }

    private FrameworkElement FindAdornedElement()
    {
        // Идём вверх по визуальному дереву и ищем элемент с AdornerLayer
        DependencyObject current = this;

        while (current != null)
        {
            if (current is FrameworkElement fe)
            {
                var layer = AdornerLayer.GetAdornerLayer(fe);
                if (layer != null)
                {
                    // Проверяем, что этот слой действительно принадлежит этому элементу
                    // (а не окну или другому предку)
                    return fe;
                }
            }
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void AttachAdorner()
    {
        if (_adornedElement == null) return;

        var layer = AdornerLayer.GetAdornerLayer(_adornedElement);
        if (layer == null) return;

        // Удаляем старый адорнер если есть
        if (_adorner != null)
        {
            layer.Remove(_adorner);
        }

        // Создаём новый адорнер, привязанный к найденному элементу
        _adorner = new GrowlAdorner(_adornedElement);
        layer.Add(_adorner);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_adorner != null && _adornedElement != null)
        {
            var layer = AdornerLayer.GetAdornerLayer(_adornedElement);
            layer?.Remove(_adorner);
            _adorner = null;
        }
    }
}
