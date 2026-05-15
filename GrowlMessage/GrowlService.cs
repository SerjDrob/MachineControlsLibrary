using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MachineControlsLibrary.GrowlMessage;

public partial class GrowlService : ObservableObject
{
    private static readonly Lazy<GrowlService> _lazy =
        new(() => new GrowlService());
    public static GrowlService Instance => _lazy.Value;

    [ObservableProperty] public partial ObservableCollection<GrowlMessage> Messages { get; private set; } = new();

    public ICommand CloseMessageCommand { get; }

    private GrowlService()
    {
        CloseMessageCommand = new RelayCommand<GrowlMessage>(RemoveMessage);
    }

    public void Send(string text, GrowlType type = GrowlType.Info, TimeSpan? duration = null)
    {
        var message = new GrowlMessage { Text = text, Type = type };
        Messages.Add(message);

        // Авто-удаление через указанное время (по умолчанию 4 сек)
        var timeout = duration ?? TimeSpan.FromSeconds(4);
        _ = AutoRemoveAsync(message, timeout);
    }

    private async Task AutoRemoveAsync(GrowlMessage message, TimeSpan delay)
    {
        await Task.Delay(delay);
        RemoveMessage(message);
    }

    public void RemoveMessage(GrowlMessage message)
    {
        if (Messages.Contains(message))
            Messages.Remove(message);
    }

    public void Clear() => Messages.Clear();

}