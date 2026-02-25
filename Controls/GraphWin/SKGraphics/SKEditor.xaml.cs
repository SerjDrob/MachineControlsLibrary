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

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;


public partial class SKEditor : UserControl
{
    private float _zoom = 0.01f;

    private SKPoint _viewOffset = new SKPoint(0, 0);      // zoom + центрирование
    private SKPoint _topologyOffset = new SKPoint(0, 0);  // Паннинг топологии
    private SKPoint _lastMouse;
    private bool _panning;
    private readonly SkiaScene _scene = new();
    private Transform2D _modelTransform = Transform2D.Identity;

    private SKRect _substrateWorld;
    private SKSize _substrateSize;
    private SKPoint _pivotWorld = new(0, 0);

    private AlignState _alignState;
    private Anchor? _hoverAnchor;
    private Anchor? _sourceAnchor;
    private Anchor? _targetAnchor;
    private SKPoint _currentMouseWorld;

    private List<Anchor> _topologyAnchors = new();
    private List<Anchor> _substrateAnchors;

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
            new Anchor(new SKPoint(w, h), AnchorType.SubstrateCorner, null),
            new Anchor(new SKPoint(0, h), AnchorType.SubstrateCorner, null)
        };
    }

    public List<CadEntity> EntitiesView { get; set; }
    public Transform2D ResultTransform => _modelTransform;
    public SKPoint PivotWorld => _pivotWorld;

    public IList<CadEntity> Entities
    {
        get => (IList<CadEntity>)GetValue(EntitiesProperty);
        set => SetValue(EntitiesProperty, value);
    }

    public static readonly DependencyProperty EntitiesProperty =
        DependencyProperty.Register(nameof(Entities), typeof(IList<CadEntity>), typeof(SKEditor),
            new PropertyMetadata(null, OnEntitiesChanged));

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

    public void Fit() => FitToView(Canvas);

    public void FitToView(SKElement element)
    {
        if (_scene.Bounds.IsEmpty) return;

        var bounds = SKRect.Union(_scene.Bounds, _substrateWorld);
        float viewW = element.CanvasSize.Width;
        float viewH = element.CanvasSize.Height;
        if (viewW <= 0 || viewH <= 0) return;

        float scaleX = viewW / bounds.Width;
        float scaleY = viewH / bounds.Height;

        _zoom = MathF.Min(scaleX, scaleY) * 0.9f;

        float cx = (bounds.Left + bounds.Right) * 0.5f;
        float cy = (bounds.Top + bounds.Bottom) * 0.5f;

        float vx = viewW * 0.5f;
        float vy = viewH * 0.5f;

        _viewOffset = new SKPoint(vx - cx * _zoom, vy - cy * _zoom);
        _topologyOffset = SKPoint.Empty;

        element.InvalidateVisual();
    }

    private void DrawSubstrate(SKCanvas canvas)
    {
        using var fill = new SKPaint 
        {
            Color = new SKColor(245, 247, 250), 
            Style = SKPaintStyle.Fill 
        };
        using var stroke = new SKPaint 
        { 
            Color = SKColors.Gray, 
            Style = SKPaintStyle.Stroke, 
            StrokeWidth = 3f / _zoom 
        };

        canvas.DrawRect(_substrateWorld, fill);
        canvas.DrawRect(_substrateWorld, stroke);
    }

    private void DrawScene(SKCanvas canvas)
    {
        if (_scene.Geometry.Count == 0) return;

        foreach (var geometry in _scene.Geometry)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(geometry.argb),
                StrokeWidth = 1.2f / _zoom,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(geometry.path, paint);
        }
    }

    private void DrawSourceAnchor(SKCanvas canvas)
    {
        if (_hoverAnchor == null) return;
        using var paint = new SKPaint
        {
            Color = new SKColor(255, 196, 120),   // пастельный голубой
            IsAntialias = true,
            StrokeWidth = 2f / _zoom,
            Style = SKPaintStyle.Stroke
        };
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true
        };
        float r = 6f / _zoom;
        canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r, paint);
        canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r + 1, shadowPaint);
    }

    private void DrawRubberLine(SKCanvas canvas)
    {
        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s)
        {
            //using var paint = new SKPaint
            //{
            //    Color = SKColors.Yellow,
            //    StrokeWidth = 1f / _zoom,
            //    PathEffect = SKPathEffect.CreateDash([5/_zoom, 5/_zoom], 0)
            //};


            using var paint = new SKPaint
            {
                Color = new SKColor(100, 160, 220, 180), // голубой с прозрачностью
                StrokeWidth = 2 / _zoom,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round
            };

            canvas.DrawLine(s.WorldPoint, _currentMouseWorld, paint);
        }
    }

    private void DrawTargetAnchor(SKCanvas canvas)
    {
        if (_targetAnchor == null) return;

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(170, 150, 210),
            StrokeWidth = 2f / _zoom,
            IsAntialias = true
        };
        float r = 6f / _zoom;
        canvas.DrawRect(_targetAnchor.Value.WorldPoint.X - r, _targetAnchor.Value.WorldPoint.Y - r, r * 2, r * 2, paint);
    }

    private void DrawPivot(SKCanvas canvas)
    {
        const float r = 500f;
        using var paint = new SKPaint { Color = SKColors.Red, StrokeWidth = 1f / _zoom, Style = SKPaintStyle.Stroke, IsAntialias = true };
        canvas.DrawLine(_pivotWorld.X - r, _pivotWorld.Y, _pivotWorld.X + r, _pivotWorld.Y, paint);
        canvas.DrawLine(_pivotWorld.X, _pivotWorld.Y - r, _pivotWorld.X, _pivotWorld.Y + r, paint);
    }

    private void ApplyView(SKCanvas canvas)
    {
        canvas.Translate(_viewOffset);
        canvas.Scale(_zoom);
    }

    private void ApplyTopology(SKCanvas canvas)
    {
        canvas.Translate(_topologyOffset);
    }

    private void ApplyModel(SKCanvas canvas)
    {
        var matrix = _modelTransform.ToSKMatrix();
        canvas.Concat(in matrix);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        //var background = new SKColor(245, 247, 250);   // мягкий серо-голубой
         var background = new SKColor(250, 248, 244);   // теплый светло-бежевый
        //var background = new SKColor(120, 120, 100);   // мягкий bluish gray

        canvas.Clear(background);

        // общий переворот Y
        canvas.Translate(0, info.Height);
        canvas.Scale(1, -1);

        // --- Substrate ---
        canvas.Save();
        ApplyView(canvas);
        DrawSubstrate(canvas);
        DrawTargetAnchor(canvas);
        canvas.Restore();

        // --- Topology ---
        canvas.Save();
        ApplyView(canvas);
        ApplyModel(canvas);
        DrawSourceAnchor(canvas);
        DrawScene(canvas);
        canvas.Restore();

        // --- Overlay ---
        canvas.Save();
        ApplyView(canvas);
        DrawRubberLine(canvas);
        DrawPivot(canvas);
        canvas.Restore();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        float scale = e.Delta > 0 ? 1.2f : 0.8f;
        var mouse = ToSk(e.GetPosition(Canvas));
        ZoomAt(mouse, scale);
    }


    private void ZoomAt(SKPoint screenPoint, float scale)
    {
        var worldBefore = ScreenToWorldRefTopology(screenPoint);

        _zoom *= scale;
        _zoom = Math.Clamp(_zoom, 0.001f, 1000f);

        var worldAfter = ScreenToWorldRefTopology(screenPoint);

        var deltaWorld = worldAfter - worldBefore;

        _viewOffset.X += deltaWorld.X * _zoom;
        _viewOffset.Y += deltaWorld.Y * _zoom;

        Canvas.InvalidateVisual();
    }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _panning = true;
            _lastMouse = ToSk(e.GetPosition(Canvas));
        }

        if (e.RightButton == MouseButtonState.Pressed)
        {
            _pivotWorld = ScreenToWorldRefTopology(ToSk(e.GetPosition(Canvas)));
            Canvas.InvalidateVisual();
        }

        if (_hoverAnchor is Anchor a && e.LeftButton == MouseButtonState.Pressed)
        {
            var transformed = _modelTransform.Apply(a.WorldPoint);
            _sourceAnchor = a with { WorldPoint = transformed };
            _alignState = AlignState.Dragging;
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _panning = false;

        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s && _targetAnchor is Anchor t)
        {
            var delta = t.WorldPoint - s.WorldPoint;
            _topologyOffset += delta;
            ApplyTransform(Transform2D.Translate(delta.X, delta.Y));
        }

        _alignState = AlignState.Idle;
        _sourceAnchor = null;
        _targetAnchor = null;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var mouse = ToSk(e.GetPosition(Canvas));
        _currentMouseWorld = ScreenToWorld(mouse);
        if (_panning)
        {
            var deltaScreen = mouse - _lastMouse;

            // переводим экранное смещение в мировое
            var deltaWorld = new SKPoint(
                deltaScreen.X / _zoom,
                -deltaScreen.Y / _zoom
            );

            _topologyOffset += deltaWorld;
            _lastMouse = mouse;

            var tr = Transform2D.Translate(deltaWorld.X, deltaWorld.Y);
            _modelTransform = _modelTransform.Then(tr);
            Canvas.InvalidateVisual();
        }
        else
        {
            float tol = 5f / _zoom;

            if (_alignState == AlignState.Idle)
            {
                _hoverAnchor = FindTopologyAnchor(_currentMouseWorld, tol);
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
    private Anchor? FindTopologyAnchor(SKPoint mouseWorld, float tol)
    {
        float tol2 = tol * tol;

        Anchor? best = null;
        float bestDist2 = float.MaxValue;

        foreach (var a in _topologyAnchors)
        {
            var transformed = _modelTransform.Apply(a.WorldPoint);

            float dx = transformed.X - mouseWorld.X;
            float dy = transformed.Y - mouseWorld.Y;
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
        return new SKPoint((float)(p.X * dpi.DpiScaleX), (float)(p.Y * dpi.DpiScaleY));
    }

    private SKPoint ScreenToWorldRefTopology(SKPoint screen)
    {
        var world = ScreenToWorld(screen);
        return world - _topologyOffset;
    }
    private SKPoint ScreenToWorld(SKPoint screen)
    {
        float h = Canvas.CanvasSize.Height;
        float skY = h - screen.Y;

        return new SKPoint(
            (screen.X - _viewOffset.X) / _zoom,
            (skY - _viewOffset.Y) / _zoom
        );
    }
    private void ApplyTransform(Transform2D t)
    {
        _modelTransform = _modelTransform.Then(t);
        BuildScene(EntitiesView);
        Canvas.InvalidateVisual();
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
    private void RebuildAnchors()
    {
        _topologyAnchors = GetTopologyAnchors(EntitiesView).ToList();
    }
    public void BuildScene(IEnumerable<CadEntity> cadEntities)
    {
        _scene.Clear();
        foreach (var e in cadEntities.Where(l => l.layerEnable))
            _scene.AddPath(e);
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
                    yield return new Anchor(a.Center, AnchorType.Center, a);
                    break;
            }
        }
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
}

