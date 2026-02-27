namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;

public interface IEditorCommand
{
    void Execute();
    void Undo();
}