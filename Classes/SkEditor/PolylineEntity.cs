using SkiaSharp;
using System.Collections.Generic;

namespace MachineControlsLibrary.Classes.SkEditor;

public record PolylineVertex(SKPoint Point, float Bulge);

public record PolylineEntity(
    IReadOnlyList<PolylineVertex> Vertices,
    bool Closed
) : CadEntity("", 0, 0, true);

