using SkiaSharp;
using System;

namespace MachineControlsLibrary.Classes.SkEditor;

public record LineEntity(SKPoint P1, SKPoint P2) : CadEntity("", 0, 0, true)
{
    public override SKRect GetWorldBounds()
    {
        float minX = Math.Min(P1.X, P2.X);
        float minY = Math.Min(P1.Y, P2.Y);
        float maxX = Math.Max(P1.X, P2.X);
        float maxY = Math.Max(P1.Y, P2.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }
}