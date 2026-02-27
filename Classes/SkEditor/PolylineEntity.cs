using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace MachineControlsLibrary.Classes.SkEditor;

public record PolylineVertex(SKPoint Point, float Bulge);

public record PolylineEntity(
    IReadOnlyList<PolylineVertex> Vertices,
    bool Closed
) : CadEntity("", 0, 0, true)
{
    public override SKRect GetWorldBounds()
    {
        float minX = Vertices.Min(p => p.Point.X);
        float minY = Vertices.Min(p => p.Point.Y);
        float maxX = Vertices.Max(p => p.Point.X);
        float maxY = Vertices.Max(p => p.Point.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }
}