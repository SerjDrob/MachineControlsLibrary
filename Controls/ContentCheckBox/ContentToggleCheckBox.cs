using System.Windows;
using System.Windows.Controls;

namespace MachineControlsLibrary.Controls.ContentCheckBox;

public class ContentToggleCheckBox : CheckBox
{
    static ContentToggleCheckBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ContentToggleCheckBox),
            new FrameworkPropertyMetadata(typeof(ContentToggleCheckBox)));
    }

    public object CheckedContent
    {
        get => GetValue(CheckedContentProperty);
        set => SetValue(CheckedContentProperty, value);
    }
    public static readonly DependencyProperty CheckedContentProperty =
        DependencyProperty.Register(nameof(CheckedContent), typeof(object), typeof(ContentToggleCheckBox), new PropertyMetadata(null));

    public object UncheckedContent
    {
        get => GetValue(UncheckedContentProperty);
        set => SetValue(UncheckedContentProperty, value);
    }
    public static readonly DependencyProperty UncheckedContentProperty =
        DependencyProperty.Register(nameof(UncheckedContent), typeof(object), typeof(ContentToggleCheckBox), new PropertyMetadata(null));
}