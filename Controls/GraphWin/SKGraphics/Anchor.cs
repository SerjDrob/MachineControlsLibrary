using MachineControlsLibrary.Classes.SkEditor;
using SkiaSharp;

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;

public readonly record struct Anchor(
    SKPoint WorldPoint,
    AnchorType Type,
    CadEntity? Entity // null для подложки
);
