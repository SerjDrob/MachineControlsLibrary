using SkiaSharp;
using System.Collections.Generic;

namespace MachineControlsLibrary.Classes.SkEditor;

public sealed class SkiaScene
{
    public readonly List<(SKPath path,uint argb)> Geometry = new();
    public SKRect Bounds;

    public void Clear()
    {
        Geometry.Clear();
        Bounds = SKRect.Empty;
    }
}
