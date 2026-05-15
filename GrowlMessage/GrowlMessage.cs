using System;
using System.ComponentModel;
using System.Windows.Media;

namespace MachineControlsLibrary.GrowlMessage;

public class GrowlMessage : INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Text { get; set; }
    public GrowlType Type { get; set; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    public Brush TypeBrush => Type switch
    {
        GrowlType.Success => Brushes.Green,
        GrowlType.Warning => Brushes.Orange,
        GrowlType.Error => Brushes.Red,
        _ => Brushes.Blue
    };

    public event PropertyChangedEventHandler PropertyChanged;
}
