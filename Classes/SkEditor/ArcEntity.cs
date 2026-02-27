using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineControlsLibrary.Classes.SkEditor;

public record ArcEntity(SKPoint Center, float Radius, float StartAngleDeg, float EndAngleDeg) : CadEntity("", 0, 0, true)
{
    public override SKRect GetWorldBounds()
    {
        var points = new List<SKPoint>();

        float start = Normalize(StartAngleDeg);
        float end = Normalize(EndAngleDeg);

        bool IsAngleInside(float angle)
        {
            angle = Normalize(angle);

            if (start <= end)
                return angle >= start && angle <= end;
            else
                return angle >= start || angle <= end; // переход через 360
        }

        // стартовая и конечная точки
        points.Add(PointAt(start));
        points.Add(PointAt(end));

        // критические углы
        float[] critical = { 0f, 90f, 180f, 270f };

        foreach (var a in critical)
        {
            if (IsAngleInside(a))
                points.Add(PointAt(a));
        }

        float minX = points.Min(p => p.X);
        float minY = points.Min(p => p.Y);
        float maxX = points.Max(p => p.X);
        float maxY = points.Max(p => p.Y);

        return new SKRect(minX, minY, maxX, maxY);
    }

    private SKPoint PointAt(float angleDeg)
    {
        float rad = angleDeg * MathF.PI / 180f;

        return new SKPoint(
            Center.X + Radius * MathF.Cos(rad),
            Center.Y + Radius * MathF.Sin(rad));
    }

    private float Normalize(float angle)
    {
        angle %= 360f;
        if (angle < 0)
            angle += 360f;
        return angle;
    }
}