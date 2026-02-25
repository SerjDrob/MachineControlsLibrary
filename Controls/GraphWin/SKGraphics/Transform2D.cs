using SkiaSharp;
using System;

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;

public readonly struct Transform2D
{
    public readonly float M11, M12;
    public readonly float M21, M22;
    public readonly float DX, DY;

    public Transform2D(
        float m11, float m12,
        float m21, float m22,
        float dx, float dy)
    {
        M11 = m11; M12 = m12;
        M21 = m21; M22 = m22;
        DX = dx; DY = dy;
    }


    public static readonly Transform2D Identity =
        new(1, 0, 0, 1, 0, 0);

    public SKPoint Apply(SKPoint p)
    {
        return new SKPoint(
            p.X * M11 + p.Y * M12 + DX,
            p.X * M21 + p.Y * M22 + DY
        );
    }

    public SKMatrix ToSKMatrix()
    {
        return new SKMatrix
        {
            ScaleX = M11,
            SkewX = M12,
            TransX = DX,

            SkewY = M21,
            ScaleY = M22,
            TransY = DY,

            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };
    }

    public Transform2D Then(Transform2D next)
    {
        return new Transform2D(
            M11 * next.M11 + M12 * next.M21,
            M11 * next.M12 + M12 * next.M22,

            M21 * next.M11 + M22 * next.M21,
            M21 * next.M12 + M22 * next.M22,

            DX * next.M11 + DY * next.M21 + next.DX,
            DX * next.M12 + DY * next.M22 + next.DY
        );
    }
    public static Transform2D Translate(float dx, float dy) =>
        new(1, 0, 0, 1, dx, dy);
    public static Transform2D Scale(
        float sx, float sy, SKPoint center)
    {
        return Translate(-center.X, -center.Y)
            .Then(new Transform2D(sx, 0, 0, sy, 0, 0))
            .Then(Translate(center.X, center.Y));
    }
    public static Transform2D Rotate(
        float angleDeg, SKPoint center)
    {
        float a = angleDeg * MathF.PI / 180f;
        float c = MathF.Cos(a);
        float s = MathF.Sin(a);

        return Translate(-center.X, -center.Y)
            .Then(new Transform2D(c, s, -s, c, 0, 0))
            .Then(Translate(center.X, center.Y));
    }
    public static Transform2D MirrorX(SKPoint center) =>
        Scale(1, -1, center);

    public static Transform2D MirrorY(SKPoint center) =>
        Scale(-1, 1, center);

}
