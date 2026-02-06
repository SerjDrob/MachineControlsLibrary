using SkiaSharp;

namespace MachineControlsLibrary.Classes.SkEditor;
public record ArcEntity(SKPoint Center, float Radius, float StartAngleDeg, float EndAngleDeg) : CadEntity("",0,0,true);
