using System.Windows.Input;

namespace MachineControlsLibrary.Classes
{
    /// <summary>
    /// Arguments for KeyProcessorCommands
    /// </summary>
    /// <param name="KeyEventArgs">event arguments</param>
    /// <param name="IsKeyDown">true if there's KeyDown event, false if there's KeyUp event</param>
    public record KeyProcessorArgs(KeyEventArgs KeyEventArgs, bool IsKeyDown);
}

