using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;

public record CircleEntity(SKPoint Center, float Radius) : CadEntity("",0,0,true);
