using CommunityToolkit.Mvvm.Input;
using MachineControlsLibrary.Classes;
using MachineControlsLibrary.Classes.SkEditor;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;

/// <summary>
/// Interaction logic for SKEditor.xaml
/// </summary>
public partial class SKEditor : UserControl
{
    private float _zoom = 0.01f;
    private SKPoint _pan = new(0, 0);
    private SKPoint _lastMouse;
    private bool _panning;
    private int _modelHeight;
    private readonly SkiaScene _scene = new();

    public SKEditor()
    {
        InitializeComponent();
    }


    public List<CadEntity> EntitiesView { get; set; }

    public IList<CadEntity> Entities
    {
        get { return (IList<CadEntity>)GetValue(EntitiesProperty); }
        set { SetValue(EntitiesProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Entities.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EntitiesProperty =
        DependencyProperty.Register(nameof(Entities), typeof(IList<CadEntity>), typeof(SKEditor), new PropertyMetadata(null, OnEntitiesChanged));

    private static void OnEntitiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SKEditor editor && e.NewValue is IEnumerable<CadEntity> entities)
        {
            editor.EntitiesView = new(entities);
            editor.BuildScene(entities);
            editor.Canvas.InvalidateVisual();
        }
    }
    public void Fit()
    {
        FitToView(Canvas);
    }

    
    public void FitToView(SKElement element)
    {
        if (_scene.Bounds.IsEmpty)
            return;

        var bounds = _scene.Bounds;

        float viewW = element.CanvasSize.Width;
        float viewH = element.CanvasSize.Height;

        if (viewW <= 0 || viewH <= 0)
            return;

        // масштаб, чтобы влезло целиком
        float scaleX = viewW / bounds.Width;
        float scaleY = viewH / bounds.Height;

        _zoom = MathF.Min(scaleX, scaleY) * 0.9f; // 10% поля

        // центр bounds в мировых координатах
        float cx = (bounds.Left + bounds.Right) * 0.5f;
        float cy = (bounds.Top + bounds.Bottom) * 0.5f;

        // центр экрана в экранных координатах
        float vx = viewW * 0.5f;
        float vy = viewH * 0.5f;

        // pan так, чтобы центры совпали
        _pan = new SKPoint(
            vx - cx * _zoom,
            vy - cy * _zoom
        );

        element.InvalidateVisual();
    }


    public void BuildScene(IEnumerable<CadEntity> cadEntities)
    {
        _scene.Clear();
        foreach (var e in cadEntities.Where(l => l.layerEnable))
            _scene.AddPath(e);
    }
    private SKRect GetVisibleWorldRect(SKCanvas canvas)
    {
        var w = canvas.DeviceClipBounds.Width;
        var h = canvas.DeviceClipBounds.Height;

        var p1 = ScreenToWorld(new SKPoint(0, 0));
        var p2 = ScreenToWorld(new SKPoint(w, h));

        return new SKRect(
            Math.Min(p1.X, p2.X),
            Math.Min(p1.Y, p2.Y),
            Math.Max(p1.X, p2.X),
            Math.Max(p1.Y, p2.Y));
    }
    SKPoint ScreenToWorld(SKPoint p)
    {
        return new SKPoint(
            (p.X - _pan.X) / _zoom,
            (_modelHeight - p.Y + _pan.Y) / _zoom
        );
    }
    private void DrawScene(SKCanvas canvas)
    {
        if (_scene.Geometry.Count == 0)
            return;

        // Видимая область в координатах модели
        var visible = GetVisibleWorldRect(canvas);

        foreach (var geometry in _scene.Geometry)
        {
            //     ❗ отсечение невидимого
            //if (!path.Bounds.IntersectsWith(visible))
            //    continue;
            using var paint = new SKPaint
            {
                Color = new SKColor(geometry.argb),
                // ColorF = SKColors.Black,
                StrokeWidth = 1f / _zoom,
                Style = SKPaintStyle.Stroke,
                IsAntialias = false
            };
            canvas.DrawPath(geometry.path, paint);

        }
    }
    void ZoomAt(SKPoint screenPoint, float scale)
    {
        var before = ScreenToWorld(screenPoint);

        _zoom *= scale;
        _zoom = Math.Clamp(_zoom, 0.001f, 1000f);

        var after = ScreenToWorld(screenPoint);

        _pan = new SKPoint(
            _pan.X + (after.X - before.X) * _zoom,
            _pan.Y - (after.Y - before.Y) * _zoom
        );
    }
    private SKPoint ToSk(Point p)
    {
        var dpi = VisualTreeHelper.GetDpi(Canvas);
        return new SKPoint(
            (float)(p.X * dpi.DpiScaleX),
            (float)(p.Y * dpi.DpiScaleY)
        );
    }


    static CadEntity Transform(CadEntity e, Transform2D t) => e switch
    {
        PolylineEntity pl => Transform(pl, t),
        CircleEntity c => Transform(c, t),
        ArcEntity a => Transform(a, t),
        _ => e
    };
    static PolylineEntity Transform(PolylineEntity pl, Transform2D t)
    {
        bool mirror =
            t.M11 * t.M22 - t.M12 * t.M21 < 0; // det < 0

        var verts = pl.Vertices
            .Select(v =>
                new PolylineVertex(
                    t.Apply(v.Point),
                    mirror ? -v.Bulge : v.Bulge
                ))
            .ToList();

        return pl with { Vertices = verts };
    }

    static CircleEntity Transform(CircleEntity c, Transform2D t)
    {
        var center = t.Apply(c.Center);

        // предполагаем равномерный scale
        float scale =
            MathF.Sqrt(t.M11 * t.M11 + t.M21 * t.M21);

        return c with
        {
            Center = center,
            Radius = c.Radius * scale
        };
    }
    static ArcEntity Transform(ArcEntity a, Transform2D t)
    {
        var center = t.Apply(a.Center);

        bool mirror =
            t.M11 * t.M22 - t.M12 * t.M21 < 0;

        float rot =
            MathF.Atan2(t.M12, t.M11) * 180f / MathF.PI;

        return a with
        {
            Center = center,
            StartAngleDeg = mirror
                ? -(a.StartAngleDeg + rot)
                : (a.StartAngleDeg + rot),

            EndAngleDeg = mirror
                ? -(a.EndAngleDeg + rot)
                : (a.EndAngleDeg + rot)
        };
    }


    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.DarkSlateGray);

        canvas.Translate(_pan);
        canvas.Scale(_zoom, _zoom);   // инверсия Y как в CAD
                                      // canvas.Translate(0, _modelHeight);

        DrawScene(canvas);
    }
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        float scale = e.Delta > 0 ? 1.2f : 0.8f;

        var mouse = e.GetPosition(Canvas);
        var mouseSk = new SKPoint((float)mouse.X, (float)mouse.Y);

        ZoomAt(mouseSk, scale);
        Canvas.InvalidateVisual();
    }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _panning = true;
            _lastMouse = ToSk(e.GetPosition(Canvas));
        }
    }
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _panning = false;
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;

        var cur = ToSk(e.GetPosition(Canvas));
        _pan += cur - _lastMouse;
        _lastMouse = cur;

        Canvas.InvalidateVisual();
    }
    public void Rotate90(SKPoint center)
    {
        var t = Transform2D.Rotate(90, center);

        EntitiesView = new List<CadEntity>(EntitiesView.Select(e => Transform(e, t)));
        BuildScene(EntitiesView);
        Canvas.InvalidateVisual();
    }
    public void MirrorX(SKPoint center)
    {
        var t = Transform2D.MirrorX(center);

        EntitiesView = new List<CadEntity>(EntitiesView.Select(e => Transform(e, t)));
        BuildScene(EntitiesView);
        Canvas.InvalidateVisual();
    }

}


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
            p.X * M11 + p.Y * M21 + DX,
            p.X * M12 + p.Y * M22 + DY
        );
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
