using SkiaSharp;
using System;
using System.Collections.Generic;

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
    public Transform2D(float[] elements) : this(elements[0], elements[1],
                                                elements[2], elements[3],
                                                elements[4], elements[5])
    {
    }
    
        public float[] GetTransformation() => [M11, M12, M21, M22, DX, DY];

    public static readonly Transform2D Identity =
        new(1, 0, 0, 1, 0, 0);

    public SKPoint Apply(SKPoint p)
    {
        return new SKPoint(
            p.X * M11 + p.Y * M12 + DX,
            p.X * M21 + p.Y * M22 + DY
        );
    }
    public float Determinant => M11 * M22 - M12 * M21;
    public bool ReversesOrientation => Determinant < 0;
    public (float X, float Y) Apply(float x, float y)
    {
        return (x * M11 + y * M12 + DX,
                x * M21 + y * M22 + DY );
    }
    public SKRect Apply(SKRect rect)
    {
        var leftTop = new SKPoint(rect.Left, rect.Top);
        var rightBot = new SKPoint(rect.Right, rect.Bottom);
        var trLeftTop = Apply(leftTop);
        var trRightBot = Apply(rightBot);
        (var left, var right) = trLeftTop.X < trRightBot.X ? (trLeftTop.X, trRightBot.X) : (trRightBot.X, trLeftTop.X);
        (var bot, var top) = trLeftTop.Y > trRightBot.Y ? (trLeftTop.Y, trRightBot.Y) : (trRightBot.Y, trLeftTop.Y);
        return new SKRect(left, top, right, bot);
    }
    public SKPoint ApplyInverse(SKPoint p)
    {
        // 1. Убираем перенос
        float x = p.X - DX;
        float y = p.Y - DY;

        // 2. Вычисляем детерминант
        float det = M11 * M22 - M12 * M21;

        if (Math.Abs(det) < 1e-8f) throw new InvalidOperationException("Matrix is not invertible.");

        float invDet = 1f / det;

        // 3. Применяем обратную матрицу
        return new SKPoint(
            (x * M22 - y * M12) * invDet,
            (-x * M21 + y * M11) * invDet
        );
    }
    public (float x, float y) ApplyInverse(float pX, float pY)
    {
        // 1. Убираем перенос
        float x = pX - DX;
        float y = pY - DY;

        // 2. Вычисляем детерминант
        float det = M11 * M22 - M12 * M21;

        if (Math.Abs(det) < 1e-8f) throw new InvalidOperationException("Matrix is not invertible.");

        float invDet = 1f / det;

        // 3. Применяем обратную матрицу
        return (
            (x * M22 - y * M12) * invDet,
            (-x * M21 + y * M11) * invDet
        );
    }
    public SKRect ApplyInverse(SKRect rect)
    {
        var leftTop = new SKPoint(rect.Left, rect.Top);
        var rightBot = new SKPoint(rect.Right, rect.Bottom);
        var trLeftTop = ApplyInverse(leftTop);
        var trRightBot = ApplyInverse(rightBot);
        (var left, var right) = trLeftTop.X < trRightBot.X ? (trLeftTop.X, trRightBot.X) : (trRightBot.X, trLeftTop.X);
        (var bot, var top) = trLeftTop.Y > trRightBot.Y ? (trLeftTop.Y, trRightBot.Y) : (trRightBot.Y, trLeftTop.Y);
        return new SKRect(left, top, right, bot);
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

    //public Transform2D Then(Transform2D next)
    //{
    //    return new Transform2D(
    //        M11 * next.M11 + M12 * next.M21,
    //        M11 * next.M12 + M12 * next.M22,

    //        M21 * next.M11 + M22 * next.M21,
    //        M21 * next.M12 + M22 * next.M22,

    //        DX * next.M11 + DY * next.M21 + next.DX,
    //        DX * next.M12 + DY * next.M22 + next.DY
    //    );
    //}

    public Transform2D Then(Transform2D next)
    {
        // Матрица: next.M × this.M (умножение слева!)
        float m11 = next.M11 * M11 + next.M12 * M21;
        float m12 = next.M11 * M12 + next.M12 * M22;
        float m21 = next.M21 * M11 + next.M22 * M21;
        float m22 = next.M21 * M12 + next.M22 * M22;

        // Перенос: next.M × this.t + next.t
        float dx = next.M11 * DX + next.M12 * DY + next.DX;
        float dy = next.M21 * DX + next.M22 * DY + next.DY;

        return new Transform2D(m11, m12, m21, m22, dx, dy);
    }

    public Transform2D Remove(Transform2D t) => Then(t.Inverse());
    public Transform2D Inverse()
    {
        float det = M11 * M22 - M12 * M21;

        if (Math.Abs(det) < 1e-12)
            throw new InvalidOperationException("Non invertible transform");

        float inv = 1.0f / det;

        float m11 = M22 * inv;
        float m12 = -M12 * inv;
        float m21 = -M21 * inv;
        float m22 = M11 * inv;

        float dx = -(m11 * DX + m12 * DY);
        float dy = -(m21 * DX + m22 * DY);

        return new Transform2D(m11, m12, m21, m22, dx, dy);
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
    public static Transform2D RotateRadians(
        float angleRad, SKPoint center)
    {
        float c = MathF.Cos(angleRad);
        float s = MathF.Sin(angleRad);

        return Translate(-center.X, -center.Y)
            .Then(new Transform2D(c, s, -s, c, 0, 0))
            .Then(Translate(center.X, center.Y));
    }

    public static Transform2D MirrorX(SKPoint center) =>
        Scale(1, -1, center);

    public static Transform2D MirrorY(SKPoint center) =>
        Scale(-1, 1, center);

    /// <summary>
    /// It doesn't take in account a shear transformation
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="q1"></param>
    /// <param name="q2"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Transform2D FromTwoPoints(SKPoint p1, SKPoint p2, SKPoint q1, SKPoint q2)
    {
        var v = p2 - p1;
        var w = q2 - q1;

        float lenV = v.Length;
        float lenW = w.Length;

        if (lenV < 1e-6f)  throw new ArgumentException("Source points are too close");

        float scale = lenW / lenV;

        float angleV = MathF.Atan2(v.Y, v.X);
        float angleW = MathF.Atan2(w.Y, w.X);

        float rot = angleW - angleV;

        float c = MathF.Cos(rot) * scale;
        float s = MathF.Sin(rot) * scale;

        // матрица вращения + scale
        var rs = new Transform2D(
            c, -s,
            s, c,
            0, 0
        );

        // вычисляем translation
        var p1Transformed = rs.Apply(p1);

        var dx = q1.X - p1Transformed.X;
        var dy = q1.Y - p1Transformed.Y;

        return rs.Then(Transform2D.Translate(dx, dy));
    }
    public static Transform2D FromThreePoints(
    SKPoint p1, SKPoint p2, SKPoint p3,
    SKPoint q1, SKPoint q2, SKPoint q3)
    {
        // Матрица исходных точек (с однородной координатой)
        // | p1.x  p1.y  1 |
        // | p2.x  p2.y  1 |
        // | p3.x  p3.y  1 |

        float det =
            p1.X * (p2.Y - p3.Y) -
            p1.Y * (p2.X - p3.X) +
            (p2.X * p3.Y - p2.Y * p3.X);

        if (MathF.Abs(det) < 1e-6f)
            throw new ArgumentException("Source points are collinear");

        float invDet = 1f / det;

        // ✅ Правильные коэффициенты обратной матрицы (транспонированная матрица алгебраических дополнений)
        float a11 = (p2.Y - p3.Y) * invDet;
        float a12 = (p3.Y - p1.Y) * invDet;  // ❗ Было (p3.X - p2.X)
        float a13 = (p1.Y - p2.Y) * invDet;  // ❗ Было (p1.Y - p2.Y)

        float a21 = (p3.X - p2.X) * invDet;  // ❗ Было (p3.Y - p1.Y)
        float a22 = (p1.X - p3.X) * invDet;
        float a23 = (p2.X - p1.X) * invDet;  // ❗ Было (p2.X - p1.X)

        float a31 = (p2.X * p3.Y - p2.Y * p3.X) * invDet;
        float a32 = (p3.X * p1.Y - p3.Y * p1.X) * invDet;  // ❗ Было (p3.X*p1.Y - p1.X*p3.Y)
        float a33 = (p1.X * p2.Y - p1.Y * p2.X) * invDet;  // ❗ Было (p1.X*p2.Y - p2.X*p1.Y)

        // ✅ Правильное сопоставление
        float m11 = a11 * q1.X + a12 * q2.X + a13 * q3.X;
        float m12 = a21 * q1.X + a22 * q2.X + a23 * q3.X;
        float m21 = a11 * q1.Y + a12 * q2.Y + a13 * q3.Y;
        float m22 = a21 * q1.Y + a22 * q2.Y + a23 * q3.Y;
        float dx = a31 * q1.X + a32 * q2.X + a33 * q3.X;
        float dy = a31 * q1.Y + a32 * q2.Y + a33 * q3.Y;

        return new Transform2D(m11, m12, m21, m22, dx, dy);
    }

    public static Transform2D FromPointSets(
    IReadOnlyList<SKPoint> src,
    IReadOnlyList<SKPoint> dst)
    {
        if (src.Count != dst.Count || src.Count < 2)
            throw new ArgumentException("Point sets must match");

        int n = src.Count;

        // 1️⃣ центры
        SKPoint pc = new();
        SKPoint qc = new();

        for (int i = 0; i < n; i++)
        {
            pc += src[i];
            qc += dst[i];
        }

        pc = new SKPoint(pc.X / n, pc.Y / n);
        qc = new SKPoint(qc.X / n, qc.Y / n);

        float sxx = 0;
        float sxy = 0;
        float syx = 0;
        float syy = 0;
        float norm = 0;

        for (int i = 0; i < n; i++)
        {
            var p = src[i] - pc;
            var q = dst[i] - qc;

            sxx += p.X * q.X;
            sxy += p.X * q.Y;
            syx += p.Y * q.X;
            syy += p.Y * q.Y;

            norm += p.X * p.X + p.Y * p.Y;
        }

        float a = sxx + syy;
        float b = sxy - syx;

        float scale = MathF.Sqrt(a * a + b * b) / norm;

        float angle = MathF.Atan2(b, a);

        float c = MathF.Cos(angle) * scale;
        float s = MathF.Sin(angle) * scale;

        var rs = new Transform2D(
            c, -s,
            s, c,
            0, 0
        );

        var pcTransformed = rs.Apply(pc);

        var dx = qc.X - pcTransformed.X;
        var dy = qc.Y - pcTransformed.Y;

        return rs.Then(Transform2D.Translate(dx, dy));
    }

    public static Transform2D RansacTransform(
    IReadOnlyList<SKPoint> src,
    IReadOnlyList<SKPoint> dst,
    int iterations = 200,
    float threshold = 5f)
    {
        if (src.Count != dst.Count || src.Count < 2)
            throw new ArgumentException();

        var rnd = new Random();

        int bestInliers = -1;
        Transform2D bestTransform = Transform2D.Identity;

        for (int it = 0; it < iterations; it++)
        {
            int i1 = rnd.Next(src.Count);
            int i2 = rnd.Next(src.Count);

            if (i1 == i2) continue;

            var s = new[] { src[i1], src[i2] };
            var d = new[] { dst[i1], dst[i2] };

            var t = Transform2D.FromTwoPoints(s[0], s[1], d[0], d[1]);

            int inliers = 0;

            for (int i = 0; i < src.Count; i++)
            {
                var p = t.Apply(src[i]);
                var err = Distance(p, dst[i]);

                if (err < threshold)
                    inliers++;
            }

            if (inliers > bestInliers)
            {
                bestInliers = inliers;
                bestTransform = t;
            }
        }

        // финальная подгонка по всем inliers
        var srcIn = new List<SKPoint>();
        var dstIn = new List<SKPoint>();

        for (int i = 0; i < src.Count; i++)
        {
            var p = bestTransform.Apply(src[i]);
            if (Distance(p, dst[i]) < threshold)
            {
                srcIn.Add(src[i]);
                dstIn.Add(dst[i]);
            }
        }

        if (srcIn.Count >= 2)
            return FromPointSets(srcIn, dstIn);

        return bestTransform;
    }

    static float Distance(SKPoint a, SKPoint b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static Transform2D WeightedAlignment(
    IReadOnlyList<SKPoint> src,
    IReadOnlyList<SKPoint> dst,
    IReadOnlyList<float> weights)
    {
        if (src.Count != dst.Count || src.Count != weights.Count)
            throw new ArgumentException();

        int n = src.Count;

        float wsum = 0;
        float sx = 0, sy = 0;
        float dx = 0, dy = 0;

        // центры масс
        for (int i = 0; i < n; i++)
        {
            float w = weights[i];

            wsum += w;

            sx += src[i].X * w;
            sy += src[i].Y * w;

            dx += dst[i].X * w;
            dy += dst[i].Y * w;
        }

        sx /= wsum;
        sy /= wsum;

        dx /= wsum;
        dy /= wsum;

        float Sxx = 0;
        float Sxy = 0;
        float Syx = 0;
        float Syy = 0;

        float srcVar = 0;

        for (int i = 0; i < n; i++)
        {
            float w = weights[i];

            float xs = src[i].X - sx;
            float ys = src[i].Y - sy;

            float xd = dst[i].X - dx;
            float yd = dst[i].Y - dy;

            Sxx += w * xs * xd;
            Sxy += w * xs * yd;
            Syx += w * ys * xd;
            Syy += w * ys * yd;

            srcVar += w * (xs * xs + ys * ys);
        }

        float a = Sxx + Syy;
        float b = Sxy - Syx;

        float angle = MathF.Atan2(b, a);

        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);

        float scale = MathF.Sqrt(a * a + b * b) / srcVar;

        float m11 = scale * c;
        float m12 = scale * -s;

        float m21 = scale * s;
        float m22 = scale * c;

        float tx = dx - (m11 * sx + m12 * sy);
        float ty = dy - (m21 * sx + m22 * sy);

        return new Transform2D(
            m11, m12,
            m21, m22,
            tx, ty);
    }

    public static float AlignmentError(
    Transform2D t,
    SKPoint src,
    SKPoint dst)
    {
        var p = t.Apply(src);

        float dx = p.X - dst.X;
        float dy = p.Y - dst.Y;

        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public static void SelfTest()
    {
        var p = new SKPoint(10, 20);

        var t1 = Translate(100, 50);
        var t2 = Rotate(30, new SKPoint(0, 0));
        var a = t2.Apply(t1.Apply(p));
        var b = t1.Then(t2).Apply(p);

        if (Math.Abs(a.X - b.X) > 0.001 ||
            Math.Abs(a.Y - b.Y) > 0.001)
            throw new Exception("Transform composition broken");
    }
}
