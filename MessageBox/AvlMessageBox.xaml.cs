using MahApps.Metro.IconPacks;
using System.Windows;
using System.Windows.Media;

namespace MachineControlsLibrary.MessageBox;

/// <summary>
/// Interaction logic for AvlMessageBox.xaml
/// </summary>
public partial class AvlMessageBox : Window
{
    enum MsgKind
    {
        Ask,
        Info,
        Warn,
        Error,
        Success,
        Fatal
    }
    private AvlMessageBox(string message, string caption, MsgKind msgKind)
    {
        InitializeComponent();
        TxtMessage.Text = message;
        Caption.Text = caption;
        switch (msgKind)
        {
            case MsgKind.Ask:
                {
                    Icon.Content = new PackIconBootstrapIcons
                    {
                        Kind = PackIconBootstrapIconsKind.QuestionSquareFill,
                        Foreground = Brushes.LightSeaGreen,
                        Width = 30,
                        Height = 30,
                    };
                }
                break;
            case MsgKind.Info:
                {
                    Icon.Content = new PackIconBootstrapIcons
                    {
                        Kind = PackIconBootstrapIconsKind.ExclamationSquareFill,
                        Foreground = Brushes.Blue,
                        Width = 30,
                        Height = 30,
                    };
                    CancellButton.Visibility = Visibility.Collapsed;
                }
                break;
            case MsgKind.Warn:
                {
                    Icon.Content = new PackIconBootstrapIcons
                    {
                        Kind = PackIconBootstrapIconsKind.ExclamationTriangleFill,
                        Foreground = Brushes.OrangeRed,
                        Width = 30,
                        Height = 30
                    };
                    CancellButton.Visibility = Visibility.Collapsed;
                }
                break;
            case MsgKind.Error:
                {
                    Icon.Content = new PackIconBootstrapIcons
                    {
                        Kind = PackIconBootstrapIconsKind.XOctagonFill,
                        Foreground = Brushes.Red,
                        Width = 30,
                        Height = 30
                    };
                    CancellButton.Visibility = Visibility.Collapsed;
                }
                break;
            case MsgKind.Success:
                {
                    Icon.Content = new PackIconBootstrapIcons
                    {
                        Kind = PackIconBootstrapIconsKind.ExclamationSquareFill,
                        Foreground = Brushes.LightSeaGreen,
                        Width = 30,
                        Height = 30
                    };
                    CancellButton.Visibility = Visibility.Collapsed;
                }
                break;
            case MsgKind.Fatal:
                {
                    Icon.Content = new PackIconGameIcons
                    {
                        Kind = PackIconGameIconsKind.ExecutionerHood,
                        Foreground = Brushes.White,
                        Width = 30,
                        Height = 30
                    };
                    CancellButton.Visibility = Visibility.Collapsed;
                }
                break;
            default:
                break;
        }

    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = true; // Returns true to the caller
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false; // Returns false to the caller
    }

    // Static helper to make it easy to call
    public static MessageBoxResult Ask(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Ask, owner);
    }
    public static MessageBoxResult Info(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Info, owner);
    }
    public static MessageBoxResult Warning(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Warn, owner);
    }
    public static MessageBoxResult Error(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Error, owner);
    }
    public static MessageBoxResult Success(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Success, owner);
    }
    public static MessageBoxResult Fatal(string message, string caption = "", Window? owner = null)
    {
        return IvokeMessage(message, caption, MsgKind.Fatal, owner);
    }

    private static MessageBoxResult IvokeMessage(string message, string caption, MsgKind msgKind, Window? owner)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            var msg = new AvlMessageBox(message, caption, msgKind);
            owner ??= Application.Current.MainWindow;
            if (owner is not AvlMessageBox) msg.Owner = owner;
            var result = msg.ShowDialog();
            if (result is null) return MessageBoxResult.None;
            return result.Value ? MessageBoxResult.OK : MessageBoxResult.Cancel;
        });
    }
}

