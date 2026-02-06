using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;

public record LineEntity(SKPoint P1, SKPoint P2) : CadEntity("", 0, 0, true);
