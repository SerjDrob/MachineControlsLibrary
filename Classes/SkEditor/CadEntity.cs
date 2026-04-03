using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;

public abstract record CadEntity(string LayerName, uint LayerColor, uint EntityColor, bool LayerEnable)
{
    public abstract SKRect GetWorldBounds();
}
