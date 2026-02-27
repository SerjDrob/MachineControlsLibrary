using System.Collections.Generic;

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;

public class AddCutZoneCommand : IEditorCommand
{
    private readonly List<CutZone> _zones;
    private readonly CutZone _zone;

    public AddCutZoneCommand(List<CutZone> zones, CutZone zone)
    {
        _zones = zones;
        _zone = zone;
    }

    public void Execute()
    {
        _zones.Add(_zone);
    }

    public void Undo()
    {
        _zones.Remove(_zone);
    }
}
