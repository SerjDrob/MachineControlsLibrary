using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;

public abstract record CadEntity(string LayerName, uint layerColor, uint entityColor, bool layerEnable)
{
    public abstract SKRect GetWorldBounds();
}
