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


public enum CutStyle
{
    Contains,
    Intersect
}

public record CutZone(SKRect Rect, CutStyle Style, string[] ForLayers);

public partial class SKEditor : UserControl
{
    private float _zoom = 1f;
    private readonly List<CutZone> _cutZones = new();
    private readonly Stack<IEditorCommand> _undo = new();
    private readonly Stack<IEditorCommand> _redo = new();
    private SKPoint _viewOffset = new SKPoint(0, 0);      // zoom + центрирование
    private SKPoint _topologyOffset = new SKPoint(0, 0);  // Паннинг топологии
    private SKPoint _lastMouse;
    private bool _panning;
    private bool _cutting;
    private bool _cutEnable;
    private bool _motionEnable;
    private CutStyle _cutStyle = CutStyle.Contains;
    private readonly SkiaScene _scene = new();
    private float _currentModelScale = 1;
    private Transform2D _modelTransform = Transform2D.Identity;
    private SKPoint _selectionStartWorld;
    private SKRect? _currentSelectionWorld;
    private SKRect _substrateWorld;
    private SKSize _substrateSize;
    private SKPoint _pivotWorld = new(0, 0);
    private SKPoint _cameraViewfinder = new(0, 0);
    private SKPoint _laserViewfinder = new(0, 0);


    private AlignState _alignState;
    private Anchor? _hoverAnchor;
    private Anchor? _sourceAnchor;
    private Anchor? _targetAnchor;
    private SKPoint _currentMouseWorld;

    private List<Anchor> _topologyAnchors = new();
    private List<Anchor> _substrateAnchors;
    private List<string> _enabledLayers;

    public float W { get; set; }
    public float H { get; set; }

    public SKEditor()
    {
        InitializeComponent();
        W = 60;
        H = 48;
        _substrateWorld = new SKRect(0, 0, W, H);
        _substrateSize = new SKSize(W, H);
        RebuildSubstrateAnchors();
    }
    public event Action<float[]>? TransformChanged;
    public event Action<IList<CutZone>>? CutZoneChanged;
    public event Action<(float x, float y)>? OnSubstrateClicked;
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
        //if (_scene.Bounds.IsEmpty) return;

        var scaledBounds = _modelTransform.Apply(_scene.Bounds);

        var bounds = SKRect.Union(scaledBounds, _substrateWorld);
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
    public void FitToCameraViewfinder(SKElement element, SKPoint clickPoint)
    {
        var bounds = SKRect.Create(_cameraViewfinder - new SKPoint(2.5f,2.5f), new(5, 5));
        if(!bounds.Contains(clickPoint)) return;
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
    public void SetSubstrateDim(float w, float h)
    {
        W = w;
        H = h;
        _substrateSize = new SKSize(w, h);
        _substrateWorld = new SKRect(0, 0, w, h);
        RebuildSubstrateAnchors();
        Application.Current.Dispatcher.Invoke(Canvas.InvalidateVisual);
    }
    public void SetTopologyScale((float oldScale, float newScale) scales)
    {
        _currentModelScale = scales.newScale;
        _modelTransform = Transform2D.Scale(scales.oldScale, scales.oldScale, new SKPoint(0, 0))
            .Then(_modelTransform);
        _modelTransform = Transform2D.Scale(1f / scales.newScale, 1f / scales.newScale, new SKPoint(0, 0))
            .Then(_modelTransform);
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        Application.Current.Dispatcher.Invoke(Canvas.InvalidateVisual);
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
    private void DrawSelection(SKCanvas canvas)
    {
        if (_currentSelectionWorld is SKRect selection)
        {
            var fillColor = _cutStyle == CutStyle.Contains ?
                new SKColor(120, 170, 220, 80) :
                new SKColor(220, 170, 220, 80);
            var fill = new SKPaint
            {
                Color = fillColor,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            var stroke = new SKPaint
            {
                Color = new SKColor(120, 170, 220),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 / _zoom,
                PathEffect = SKPathEffect.CreateDash([6 / _zoom, 4 / _zoom], 0),
                IsAntialias = true
            };

            canvas.DrawRect(selection, fill);
            canvas.DrawRect(selection, stroke);
        }
    }

    private void DrawScene(SKCanvas canvas)
    {
        if (_scene.Geometry.Count == 0) return;

        foreach (var geometry in _scene.Geometry)
        {
            var color = new SKColor(geometry.argb);
            var trueColor = color == SKColors.White ? SKColors.Black : color;
            using var paint = new SKPaint
            {
                Color = trueColor,
                StrokeWidth = 1.2f * _currentModelScale / _zoom,
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
            StrokeWidth = 2f * _currentModelScale / _zoom,
            Style = SKPaintStyle.Stroke
        };
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true
        };
        float r = 6f * _currentModelScale / _zoom;
        canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r, paint);
        canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r, shadowPaint);
    }

    private void DrawRubberLine(SKCanvas canvas)
    {
        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s)
        {
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
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 40),
            IsAntialias = true
        };
        float r = 6f / _zoom;
        canvas.DrawRect(_targetAnchor.Value.WorldPoint.X - r, _targetAnchor.Value.WorldPoint.Y - r, r * 2, r * 2, shadowPaint);
        canvas.DrawRect(_targetAnchor.Value.WorldPoint.X - r, _targetAnchor.Value.WorldPoint.Y - r, r * 2, r * 2, paint);
    }

    private void DrawPivot(SKCanvas canvas)
    {
        const float r = 0.5f;
        using var paint = new SKPaint { Color = SKColors.Red, StrokeWidth = 1f / _zoom, Style = SKPaintStyle.Stroke, IsAntialias = true };
        canvas.DrawLine(_pivotWorld.X - r, _pivotWorld.Y, _pivotWorld.X + r, _pivotWorld.Y, paint);
        canvas.DrawLine(_pivotWorld.X, _pivotWorld.Y - r, _pivotWorld.X, _pivotWorld.Y + r, paint);
    }
    private void DrawViewfinders(SKCanvas canvas)
    {
        //if (!_motionEnable) return;
        const float r = 5f;
        using var cameraPaint = new SKPaint { Color = SKColors.Green, StrokeWidth = 1f / _zoom, Style = SKPaintStyle.Stroke, IsAntialias = true };
        using var cameraRegion = new SKPaint
        {
            Color = new SKColor(255, 255, 0, 50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var laserPaint = new SKPaint { Color = SKColors.Violet, StrokeWidth = 1f / _zoom, Style = SKPaintStyle.Stroke, IsAntialias = true };



        if (_motionEnable)
        {
            canvas.DrawLine(_cameraViewfinder.X - r, _cameraViewfinder.Y, _cameraViewfinder.X - 2.5f, _cameraViewfinder.Y, cameraPaint);
            canvas.DrawLine(_cameraViewfinder.X + 2.5f, _cameraViewfinder.Y, _cameraViewfinder.X + r, _cameraViewfinder.Y, cameraPaint);

            canvas.DrawLine(_cameraViewfinder.X, _cameraViewfinder.Y - r, _cameraViewfinder.X, _cameraViewfinder.Y - 2.5f, cameraPaint);
            canvas.DrawLine(_cameraViewfinder.X, _cameraViewfinder.Y + 2.5f, _cameraViewfinder.X, _cameraViewfinder.Y + r, cameraPaint);

            canvas.DrawRect(_cameraViewfinder.X - 2.5f, _cameraViewfinder.Y - 2.5f, 5f, 5f, cameraRegion);
            canvas.DrawRect(_cameraViewfinder.X - 2.5f, _cameraViewfinder.Y - 2.5f, 5f, 5f, cameraPaint);
        }
        else
        {
            canvas.DrawLine(_cameraViewfinder.X - r, _cameraViewfinder.Y, _cameraViewfinder.X + r, _cameraViewfinder.Y, cameraPaint);
            canvas.DrawLine(_cameraViewfinder.X, _cameraViewfinder.Y - r, _cameraViewfinder.X, _cameraViewfinder.Y + r, cameraPaint);
        }

        canvas.DrawLine(_laserViewfinder.X - r, _laserViewfinder.Y, _laserViewfinder.X + r, _laserViewfinder.Y, laserPaint);
        canvas.DrawLine(_laserViewfinder.X, _laserViewfinder.Y - r, _laserViewfinder.X, _laserViewfinder.Y + r, laserPaint);
    }

    private void ApplyView(SKCanvas canvas)
    {
        canvas.Translate(_viewOffset);
        canvas.Scale(_zoom);
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
        DrawSelection(canvas);
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
        DrawViewfinders(canvas);
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
        if (e.LeftButton == MouseButtonState.Pressed && _motionEnable)
        {
            var coors = ScreenToWorld(ToSk(e.GetPosition(Canvas)));
            var contains = SKRect.Create(_substrateSize).Contains(coors);
            if (contains)
            {
                OnSubstrateClicked?.Invoke((coors.X, coors.Y));
            }
            return;
        }
        if (e.MiddleButton == MouseButtonState.Pressed && !_motionEnable)
        {
            Cursor = Cursors.Hand;
            _panning = true;
            _lastMouse = ToSk(e.GetPosition(Canvas));
        }
        if (e.RightButton == MouseButtonState.Pressed)
        {
            if (_motionEnable)
            {
                var clickPoint = ScreenToWorld(ToSk(e.GetPosition(Canvas)));
                FitToCameraViewfinder(Canvas, clickPoint);
            }
            else
            {
                _pivotWorld = ScreenToWorldRefTopology(ToSk(e.GetPosition(Canvas)));
                Canvas.InvalidateVisual();
            }
        }

        if (_hoverAnchor is Anchor a && e.LeftButton == MouseButtonState.Pressed && !_cutEnable)
        {
            var transformed = _modelTransform.Apply(a.WorldPoint);
            _sourceAnchor = a with { WorldPoint = transformed };
            _alignState = AlignState.Dragging;
        }

        if (_cutEnable && e.LeftButton == MouseButtonState.Pressed)
        {
            Cursor = Cursors.Cross;
            _selectionStartWorld = ScreenToWorld(ToSk(e.GetPosition(Canvas)));
            _cutting = true;
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _panning = false;
        if (_motionEnable) return;
        if (_alignState == AlignState.Dragging && _sourceAnchor is Anchor s && _targetAnchor is Anchor t && !_cutEnable)
        {
            var delta = t.WorldPoint - s.WorldPoint;
            _topologyOffset += delta;
            ApplyTransform(Transform2D.Translate(delta.X, delta.Y));
        }

        _alignState = AlignState.Idle;
        _sourceAnchor = null;
        _targetAnchor = null;
        if (_cutting)
        {
            _cutting = false;
            if (_currentSelectionWorld.HasValue)
            {
                var zone = _currentSelectionWorld.Value;
                var cutZone = _modelTransform.ApplyInverse(zone);
                var cmd = new AddCutZoneCommand(_cutZones, new(cutZone, _cutStyle, _enabledLayers.ToArray()));
                cmd.Execute();
                _undo.Push(cmd);
                _redo.Clear();
                CanUndo = true;
                CanRedo = false;
                CutZoneChanged?.Invoke(_cutZones);
                _currentSelectionWorld = null;
                ApplyTransform(Transform2D.Identity);
            }
        }
        Cursor = Cursors.Arrow;
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_motionEnable) return;
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
            TransformChanged?.Invoke(_modelTransform.GetTransformation());
            Canvas.InvalidateVisual();
        }
        else if (!_cutEnable)
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
        if (_cutting && e.LeftButton == MouseButtonState.Pressed)
        {
            var currentWorld = ScreenToWorld(ToSk(e.GetPosition(Canvas)));
            _cutStyle = _selectionStartWorld.X < currentWorld.X ? CutStyle.Contains : CutStyle.Intersect;
            _currentSelectionWorld = SKRect.Create(
                Math.Min(_selectionStartWorld.X, currentWorld.X),
                Math.Min(_selectionStartWorld.Y, currentWorld.Y),
                Math.Abs(currentWorld.X - _selectionStartWorld.X),
                Math.Abs(currentWorld.Y - _selectionStartWorld.Y));
            Canvas.InvalidateVisual();
        }
    }
    public void Undo()
    {
        if (_undo.Count == 0) return;

        var cmd = _undo.Pop();
        cmd.Undo();
        _redo.Push(cmd);
        CanUndo = _undo.Any();
        CanRedo = true;
        CutZoneChanged?.Invoke(_cutZones);
        ApplyTransform(Transform2D.Identity);
    }

    public void Redo()
    {
        if (_redo.Count == 0) return;

        var cmd = _redo.Pop();
        cmd.Execute();
        _undo.Push(cmd);
        CanRedo = _redo.Any();
        CanUndo = true;
        CutZoneChanged?.Invoke(_cutZones);
        ApplyTransform(Transform2D.Identity);
    }




    public bool CanUndo
    {
        get { return (bool)GetValue(CanUndoProperty); }
        set { SetValue(CanUndoProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CanUndo.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanUndoProperty =
        DependencyProperty.Register(nameof(CanUndo), typeof(bool), typeof(SKEditor), new PropertyMetadata(false, UndoCallBack));

    private static void UndoCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    public bool CanRedo
    {
        get { return (bool)GetValue(CanRedoProperty); }
        set { SetValue(CanRedoProperty, value); }
    }

    // Using a DependencyProperty as the backing store for CanRedo.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CanRedoProperty =
        DependencyProperty.Register(nameof(CanRedo), typeof(bool), typeof(SKEditor), new PropertyMetadata(false));


    public void SetMotionEnable(bool enable)
    {
        _motionEnable = enable;
        Canvas.InvalidateVisual();
    }

    public void SetViewfindersCoordinates((float x, float y) cameraVfCoors, (float x, float y) laserVfCoors)
    {
        _cameraViewfinder = new SKPoint(cameraVfCoors.x, cameraVfCoors.y);
        _laserViewfinder = new SKPoint(laserVfCoors.x, laserVfCoors.y);
        Canvas.InvalidateVisual();
    }
    public void SwitchScissors(bool cutEnable) => _cutEnable = cutEnable;

    private bool IsCut(CadEntity entity)
    {
        var bounds = entity.GetWorldBounds();
        foreach (var zone in _cutZones)
        {
            if (zone.Style == CutStyle.Contains && zone.Rect.Contains(bounds) && zone.ForLayers.Contains(entity.LayerName)) return true;
            if (zone.Style == CutStyle.Intersect && zone.Rect.IntersectsWithInclusive(bounds) && zone.ForLayers.Contains(entity.LayerName)) return true;
        }

        return false;
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
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        BuildScene(EntitiesView);
        Canvas.InvalidateVisual();
    }

    public void RotateCW(SKPoint worldCenter)
    {
        _modelTransform = Transform2D.Rotate(90, worldCenter).Then(_modelTransform);
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        Canvas.InvalidateVisual();
    }
    public void RotateCCW(SKPoint worldCenter)
    {
        _modelTransform = Transform2D.Rotate(-90, worldCenter).Then(_modelTransform);
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        Canvas.InvalidateVisual();
    }

    public void MirrorX(SKPoint worldCenter)
    {
        _modelTransform = Transform2D.MirrorX(worldCenter).Then(_modelTransform);
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        Canvas.InvalidateVisual();
    }
    public void MirrorY(SKPoint worldCenter)
    {
        _modelTransform = Transform2D.MirrorY(worldCenter).Then(_modelTransform);
        TransformChanged?.Invoke(_modelTransform.GetTransformation());
        Canvas.InvalidateVisual();
    }
    private void RebuildAnchors()
    {
        _topologyAnchors = GetTopologyAnchors(EntitiesView).ToList();
    }
    public void BuildScene(IEnumerable<CadEntity> cadEntities)
    {
        var enabledEnts = cadEntities.Where(l => l.layerEnable);
        _enabledLayers = enabledEnts.Select(l => l.LayerName).Distinct().ToList();
        _scene.Clear();
        foreach (var e in enabledEnts)
        {
            if (IsCut(e)) continue;
            _scene.AddPath(e);
        }
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

