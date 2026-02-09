using MachineControlsLibrary.Classes.SkEditor;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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
    private Transform2D _modelTransform = Transform2D.Identity;
    private SKRect _substrateWorld; // в мировых координатах
    private SKPoint _pivotWorld = new(0, 0);
    private AlignState _alignState;
    private Anchor? _hoverAnchor;
    private Anchor? _sourceAnchor;
    private SKPoint _currentMouseWorld;
    private List<Anchor> _topologyAnchors = new();
    private SKSize _substrateSize;
    private Transform2D _topologyTransform = Transform2D.Identity;
    private List<Anchor> _substrateAnchors;
    private Anchor? _targetAnchor;

    public float W { get; set; }
    public float H { get; set; }


    public SKEditor()
    {
        InitializeComponent();
        W = 60000;
        H = 48000;
        _substrateWorld = new SKRect(0, 0, W, H);
        _substrateSize = new SKSize(W, H);
        RebuildSubstrateAnchors();
    }
    private void RebuildSubstrateAnchors()
    {
        float w = _substrateSize.Width;
        float h = _substrateSize.Height;

        _substrateAnchors = new List<Anchor>
        {
            new Anchor(new SKPoint(0, 0), AnchorType.SubstrateCorner, null),
            new Anchor(new SKPoint(w, 0), AnchorType.SubstrateCorner, null),
            new Anchor( new SKPoint(w, h), AnchorType.SubstrateCorner, null),
            new Anchor(new SKPoint(0, h), AnchorType.SubstrateCorner, null)
        };
    }


    public List<CadEntity> EntitiesView { get; set; }
    public Transform2D ResultTransform => _modelTransform;
    public SKPoint PivotWorld => _pivotWorld;

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
            editor.RebuildAnchors();
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

        var bounds = SKRect.Union(_scene.Bounds, _substrateWorld);


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

    private void DrawPivot(SKCanvas canvas)
    {
        const float r = 500f;

        using var paint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 1f / _zoom,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        canvas.DrawLine(
            _pivotWorld.X - r, _pivotWorld.Y,
            _pivotWorld.X + r, _pivotWorld.Y,
            paint);

        canvas.DrawLine(
            _pivotWorld.X, _pivotWorld.Y - r,
            _pivotWorld.X, _pivotWorld.Y + r,
            paint);
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
    private void RebuildAnchors()
    {
        _topologyAnchors = GetTopologyAnchors(EntitiesView).ToList();
    }

    SKPoint ScreenToWorld(SKPoint screen)
    {
        float h = Canvas.CanvasSize.Height;

        // переводим WPF-screen → Skia-screen (с инверсией Y)
        float skY = h - screen.Y;

        return new SKPoint(
            (screen.X - _pan.X) / _zoom,
            (skY - _pan.Y) / _zoom
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
        var world = ScreenToWorld(screenPoint);

        _zoom *= scale;
        _zoom = Math.Clamp(_zoom, 0.001f, 1000f);

        float h = Canvas.CanvasSize.Height;
        float skY = h - screenPoint.Y;

        _pan = new SKPoint(
            screenPoint.X - world.X * _zoom,
            skY - world.Y * _zoom
        );
    }

    static IEnumerable<Anchor> GetTopologyAnchors(IEnumerable<CadEntity> entities)
    {
        foreach (var e in entities)
        {
            switch (e)
            {
                case PolylineEntity pl:
                    foreach (var v in pl.Vertices)
                        yield return new Anchor(v.Point, AnchorType.Vertex, pl);
                    break;

                case CircleEntity c:
                    yield return new Anchor(c.Center, AnchorType.Center, c);
                    break;

                case ArcEntity a:
                    yield return new Anchor(a.Center, AnchorType.Center,  a);
                    break;
            }
        }
    }
    private void DrawTargetAnchor(SKCanvas canvas)
    {
        if (_targetAnchor == null)
            return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.LimeGreen,
            StrokeWidth = 1f / _zoom,
            IsAntialias = true
        };

        float r = 7f / _zoom;

        canvas.DrawRect(
            _targetAnchor.Value.WorldPoint.X - r,
            _targetAnchor.Value.WorldPoint.Y - r,
            r * 2,
            r * 2,
            paint);
    }

    private Anchor? FindTopologyAnchor(SKPoint mouseWorld)
    {
        float tolWorld = 8f / _zoom;
        float tol2 = tolWorld * tolWorld;

        Anchor? best = null;
        float bestDist2 = float.MaxValue;

        foreach (var a in _topologyAnchors)
        {
            float dx = a.WorldPoint.X - mouseWorld.X;
            float dy = a.WorldPoint.Y - mouseWorld.Y;
            float d2 = dx * dx + dy * dy;

            if (d2 <= tol2 && d2 < bestDist2)
            {
                best = a;
                bestDist2 = d2;
            }
        }

        return best;
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

        // 1️⃣ начало координат в левом нижнем углу экрана
        canvas.Translate(0, Canvas.CanvasSize.Height);

        // 2️⃣ инверсия Y
        canvas.Scale(1, -1);

        // 3️⃣ камера
        canvas.Translate(_pan);
        canvas.Scale(_zoom, _zoom);

        DrawSubstrate(canvas);
        ApplyTransform(canvas, _modelTransform);
        DrawScene(canvas);
        DrawAnchors(canvas);
        DrawTargetAnchor(canvas);
        DrawPivot(canvas);
    }

    private void DrawSubstrate(SKCanvas canvas)
    {
        using var fill = new SKPaint
        {
            Color = new SKColor(40, 40, 40),
            Style = SKPaintStyle.Fill
        };

        using var stroke = new SKPaint
        {
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f / _zoom
        };

        canvas.DrawRect(_substrateWorld, fill);
        canvas.DrawRect(_substrateWorld, stroke);
    }

    static void ApplyTransform(SKCanvas canvas, Transform2D t)
    {
        canvas.Concat(new SKMatrix
        {
            ScaleX = t.M11,
            SkewX = t.M21,
            TransX = t.DX,
            SkewY = t.M12,
            ScaleY = t.M22,
            TransY = t.DY,
            Persp2 = 1
        });
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
        if (e.RightButton == MouseButtonState.Pressed)
        {
            var sk = ToSk(e.GetPosition(Canvas));
            _pivotWorld = ScreenToWorld(sk);
            Canvas.InvalidateVisual();
            return;
        }

        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _panning = true;
            _lastMouse = ToSk(e.GetPosition(Canvas));
        }

        if (_hoverAnchor is Anchor a)
        {
            _sourceAnchor = a;
            _alignState = AlignState.Dragging;
        }

    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _panning = false;

        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s && _targetAnchor is Anchor t)
        {
            var delta = t.WorldPoint - s.WorldPoint;

            ApplyTransform( Transform2D.Translate(delta.X, delta.Y));
        }

        _alignState = AlignState.Idle;
        _sourceAnchor = null;
        _targetAnchor = null;

    }
   

    private void ApplyTransform(Transform2D t)
    {
        // накапливаем итоговую матрицу
        _topologyTransform = _topologyTransform.Then(t);

        // применяем к геометрии (preview)
        EntitiesView = new List<CadEntity>(EntitiesView.Select(e => Transform(e, t)));

        BuildScene(EntitiesView);
        RebuildAnchors();
        Canvas.InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var mouse = ToSk(e.GetPosition(Canvas));
        _currentMouseWorld = ScreenToWorld(mouse);

        if (_panning)
        {

            var deltaScreen = mouse - _lastMouse;
            var deltaWorld = new SKPoint(deltaScreen.X / _zoom, -deltaScreen.Y / _zoom);
            _modelTransform = _modelTransform.Then(Transform2D.Translate(deltaWorld.X, deltaWorld.Y));
            _lastMouse = mouse;
            Canvas.InvalidateVisual();
        }
        else
        {
            float tol = 5f / _zoom;


            if (_alignState == AlignState.Idle)
            {
                _hoverAnchor = FindNearestVertex(_currentMouseWorld, tol);// ?? FindNearestSegment(_currentMouseWorld, tol);
                Canvas.InvalidateVisual();
            }
            else if (_alignState == AlignState.Dragging)
            {
                if (TryFindSubstrateCornerAnchor(_currentMouseWorld, tol, out var corner))
                {
                    _targetAnchor = corner;
                    Canvas.InvalidateVisual();
                }
                else
                {
                    _targetAnchor = null;
                    Canvas.InvalidateVisual();
                }
            }
        }
    }
    private bool TryFindSubstrateCornerAnchor(SKPoint worldPoint, float tolerance, out Anchor? anchor)
    {
        anchor = null;
        var tol2 = tolerance * tolerance;

        foreach (var a in _substrateAnchors)
        {
            var dx = a.WorldPoint.X - worldPoint.X;
            var dy = a.WorldPoint.Y - worldPoint.Y;

            if (dx * dx + dy * dy <= tol2)
            {
                anchor = a;
                return true;
            }
        }

        return false;
    }


    private void DrawAnchors(SKCanvas canvas)
    {
        if (_hoverAnchor == null)
            return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColors.OrangeRed,
            StrokeWidth = 1f / _zoom,
            IsAntialias = true
        };

        float r = 6f / _zoom;

        canvas.DrawCircle(
            _hoverAnchor.Value.WorldPoint,
            r,
            paint);
    }


    public void Rotate90(SKPoint worldCenter)
    {
        _modelTransform = _modelTransform.Then(Transform2D.Rotate(90, worldCenter));
        Canvas.InvalidateVisual();
    }

    public void MirrorX(SKPoint worldCenter)
    {
        _modelTransform = _modelTransform.Then(Transform2D.MirrorX(worldCenter));
        Canvas.InvalidateVisual();
    }
    public void MirrorY(SKPoint worldCenter)
    {
        _modelTransform = _modelTransform.Then(Transform2D.MirrorY(worldCenter));
        Canvas.InvalidateVisual();
    }
    private Anchor? FindNearestVertex(SKPoint world, float tol)
    {
        float tol2 = tol * tol;

        foreach (var e in EntitiesView.OfType<PolylineEntity>())
        {
            foreach (var v in e.Vertices)
            {
                if ((v.Point - world).LengthSquared <= tol2)
                    return new Anchor(v.Point, AnchorType.Vertex, e);
            }
        }
        return null;
    }
    //private Anchor? FindNearestSegment(SKPoint world, float tol)
    //{
    //    foreach (var e in EntitiesView.OfType<PolylineEntity>())
    //    {
    //        var pts = e.Vertices;
    //        for (int i = 0; i < pts.Count - 1; i++)
    //        {
    //            if (DistancePointToSegment(world, pts[i].Point, pts[i + 1].Point) < tol)
    //            {
    //                return new Anchor(
    //                    ProjectPointToSegment(world, pts[i].Point, pts[i + 1].Point),
    //                    AnchorType.Intersection,
    //                    e
    //                );
    //            }
    //        }
    //    }
    //    return null;
    //}

    private void DrawOverlay(SKCanvas canvas)
    {
        if (_hoverAnchor is Anchor a)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Orange,
                StrokeWidth = 2 / _zoom,
                IsAntialias = true
            };

            canvas.DrawCircle(a.WorldPoint, 6 / _zoom, paint);
        }

        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s)
        {
            using var paint = new SKPaint
            {
                Color = SKColors.Yellow,
                StrokeWidth = 1 / _zoom,
                PathEffect = SKPathEffect.CreateDash([5, 5], 0)
            };

            canvas.DrawLine(s.WorldPoint, _currentMouseWorld, paint);
        }
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

public enum AnchorType
{
    Vertex,
    Intersection,
    SubstrateCorner,
    Center
}

public readonly record struct Anchor(
    SKPoint WorldPoint,
    AnchorType Type,
    CadEntity? Entity // null для подложки
);
enum AlignState
{
    Idle,
    HoverSource,
    Dragging,
    HoverTarget
}
