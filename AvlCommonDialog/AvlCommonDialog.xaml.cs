using System.Windows;
using System.Windows.Input;

namespace MachineControlsLibrary.AvlCommonDialog;

/// <summary>
/// Interaction logic for AvlCommonDialog.xaml
/// </summary>
public partial class AvlCommonDialog : Window
{
    public AvlCommonDialog()
    {
        InitializeComponent();
    }
    public void SetTitle(string title)=>Title.Text = title;
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Двойной клик — можно развернуть (если нужно)
            // WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
}
