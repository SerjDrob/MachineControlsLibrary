using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;

public record CircleEntity(SKPoint Center, float Radius) : CadEntity("", 0, 0, true)
{
    public override SKRect GetWorldBounds()
    {
        return new SKRect(
        Center.X - Radius,
        Center.Y - Radius,
        Center.X + Radius,
        Center.Y + Radius);
    }
}