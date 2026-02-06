using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineControlsLibrary.Classes.SkEditor;

public static class SkEditor
{
    private static (SKPath path, uint argb) BuildLine(LineEntity l)
    {
        var p = new SKPath();
        p.MoveTo(l.P1);
        p.LineTo(l.P2);
        return (p, l.entityColor);
    }
    public static void AddPath(this SkiaScene scene, (SKPath path, uint argb) poly)
    {
        scene.Geometry.Add(poly);

        if (scene.Bounds.IsEmpty)
            scene.Bounds = poly.path.Bounds;
        else
            scene.Bounds = SKRect.Union(scene.Bounds, poly.path.Bounds);
    }
    public static void AddPath(this SkiaScene scene, CadEntity cadEntity)
    {
        var poly = cadEntity switch
        {
            PolylineEntity pl => BuildPolyline(pl),
            LineEntity l => BuildLine(l),
            ArcEntity arc => BuildArc(arc),
            CircleEntity circle => BuildCircle(circle),
            _ => throw new NotImplementedException(typeof(CadEntity).Name)
        };
        scene.Geometry.Add(poly);

        if (scene.Bounds.IsEmpty)
            scene.Bounds = poly.path.Bounds;
        else
            scene.Bounds = SKRect.Union(scene.Bounds, poly.path.Bounds);
    }
    private static (SKPath path, uint argb) BuildPolyline(PolylineEntity pl)
    {
        var path = new SKPath();

        if (pl.Vertices.Count < 2)
            return (path, pl.entityColor);

        path.MoveTo(pl.Vertices[0].Point);

        int count = pl.Vertices.Count;
        int limit = pl.Closed ? count : count - 1;

        for (int i = 0; i < limit; i++)
        {
            var v1 = pl.Vertices[i];
            var v2 = pl.Vertices[(i + 1) % count];

            if (Math.Abs(v1.Bulge) < 1e-6f)
            {
                // обычная линия
                path.LineTo(v2.Point);
            }
            else
            {
                AddBulgeArc(path, v1.Point, v2.Point, v1.Bulge);
            }
        }

        if (pl.Closed)
            path.Close();

        return (path, pl.entityColor);
    }
    private static void AddBulgeArc(SKPath path, SKPoint p1, SKPoint p2, float bulge)
    {
        // Центральный угол
        float theta = 4f * MathF.Atan(bulge);

        // Длина хорды
        float chord = Distance(p1, p2);

        // Радиус
        float radius = chord / (2f * MathF.Sin(MathF.Abs(theta) / 2f));

        // Середина хорды
        var mid = new SKPoint(
            (p1.X + p2.X) / 2f,
            (p1.Y + p2.Y) / 2f);

        // Направление хорды
        var dx = p2.X - p1.X;
        var dy = p2.Y - p1.Y;

        float len = MathF.Sqrt(dx * dx + dy * dy);
        dx /= len;
        dy /= len;

        // Перпендикуляр
        float perpX = -dy;
        float perpY = dx;

        // Смещение центра
        float h = radius * MathF.Cos(theta / 2f);

        if (bulge < 0)
            h = -h;

        var center = new SKPoint(
            mid.X + perpX * h,
            mid.Y + perpY * h);

        // Углы
        float startAngle = MathF.Atan2(p1.Y - center.Y, p1.X - center.X) * 180f / MathF.PI;
        float endAngle = MathF.Atan2(p2.Y - center.Y, p2.X - center.X) * 180f / MathF.PI;

        float sweepAngle = endAngle - startAngle;
        if (bulge > 0 && sweepAngle < 0) sweepAngle += 360f;
        if (bulge < 0 && sweepAngle > 0) sweepAngle -= 360f;

        var oval = new SKRect(
            center.X - radius,
            center.Y - radius,
            center.X + radius,
            center.Y + radius);

        path.ArcTo(oval, startAngle, sweepAngle, false);
    }
    private static float Distance(SKPoint a, SKPoint b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static (SKPath path, uint argb) BuildArc(ArcEntity a)
    {
        var path = new SKPath();

        float startRad = DegreesToRadians(a.StartAngleDeg);
        float endRad = DegreesToRadians(a.EndAngleDeg);

        var start = new SKPoint(
            a.Center.X + a.Radius * MathF.Cos(startRad),
            a.Center.Y + a.Radius * MathF.Sin(startRad));

        var sweepDeg = a.EndAngleDeg - a.StartAngleDeg;
        if (sweepDeg <= 0)
            sweepDeg += 360;

        var oval = new SKRect(
            a.Center.X - a.Radius,
            a.Center.Y - a.Radius,
            a.Center.X + a.Radius,
            a.Center.Y + a.Radius);

        path.MoveTo(start);
        path.ArcTo(oval, a.StartAngleDeg, sweepDeg, false);

        return (path,a.entityColor);
    }
    private static (SKPath path, uint argb) BuildCircle(CircleEntity c)
    {
        var path = new SKPath();

        path.AddCircle(c.Center.X, c.Center.Y, c.Radius);

        return (path, c.entityColor);
    }
    private static float DegreesToRadians(float deg) =>
           deg * MathF.PI / 180f;
}
