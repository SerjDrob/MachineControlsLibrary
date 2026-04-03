using MachineControlsLibrary.Classes.SkEditor;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
public sealed class SubstrateText
{
    public string Text { get; set; } = "";

    public LabelSide Side { get; set; }

    public LabelAlignment Alignment { get; set; }

    public float OffsetFromEdge { get; set; }

    public float OffsetAlongEdge { get; set; }

    public float FontSize { get; set; } = 16;

    public SKTypeface Typeface { get; set; }

    public SKColor Color { get; set; } = SKColors.White;
}
public enum LabelSide
{
    Top,
    Bottom,
    Left,
    Right
}
public enum LabelAlignment
{
    Start,
    Center,
    End
}
public record CutZone(SKRect Rect, CutStyle Style, string[] ForLayers);

public partial class SKEditor : UserControl
{
    const int FRAME_TIME_MS = 16; // ~60 FPS

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
    private readonly SkiaScene _substrateLayerScene = new();
    private float _currentModelScale = 1;
    private long _lastRedrawTime;
    private Transform2D _modelTransform = Transform2D.Identity;
    private Transform2D _modelTransformWithoutTranslation = Transform2D.Identity;

    private SKPoint _selectionStartWorld;
    private SKRect? _currentSelectionWorld;
    private SKRect _substrateWorld;
    private SKSize _substrateSize;
    private SKPoint _pivotWorld = new(0, 0);
    private SKPoint _cameraViewfinder = new(0, 0);
    private SKPoint _laserViewfinder = new(0, 0);
    private SKRect _cameraViewRegion = new();
    private AlignState _alignState;
    private Anchor? _hoverAnchor;
    private Anchor? _sourceAnchor;
    private Anchor? _targetAnchor;
    private SKPoint _currentMouseWorld;
    private SKPicture? _scenePicture;//topology cash
    private Dictionary<uint, SKPath> _scenePaths = new();
    private List<SKPath> _substrateLayerPaths = new();
    private List<Anchor> _topologyAnchors = new();
    private List<Anchor> _substrateAnchors;
    private List<string> _enabledLayers;
    private bool _teachPointsEnable;
    private bool _redrawPending;
    private bool _modelRotated = false;
    private bool _canBeAnchored = false;

    public List<SubstrateText> SubstrateTexts { get; } = new();
    public float W { get; set; }
    public float H { get; set; }

    public SKEditor()
    {
        InitializeComponent();
        SizeChanged += (s, e) =>
        {
            Fit();
        };
        //Canvas.RenderContinuously = false;
        W = 60;
        H = 48;
        _substrateWorld = new SKRect(0, 0, W, H);
        _substrateSize = new SKSize(W, H);

        Transform2D.SelfTest();
        //SubstrateTexts.Add(new SubstrateText
        //{
        //    Text = "SN 001234",
        //    Side = LabelSide.Top,
        //    Alignment = LabelAlignment.Center,
        //    OffsetFromEdge = 0,
        //    FontSize = 60*1.4f,
        //    Color = new SKColor(255, 0, 0)
        //});
        SetViewfindersCoordinates((0, 0), (0, 0));

        RebuildSubstrateAnchors();
    }
    public event Action<(float[] full, float[] withoutTranslation)>? TransformChanged;
    public event Action<IList<CutZone>>? CutZoneChanged;
    public event Action<(float x, float y)>? OnSubstrateClicked;
    public event Action<(float x, float y)>? OnTopologyPointClicked;
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
            //editor._modelTransform = new(0f, -0.001f, 0.001f, 0f, 48f, 0f);
            editor.EntitiesView = [.. entities];
            editor.BuildScene(entities);
            //editor.RebuildScenePicture();
            editor.RebuildScenePaths();
            editor.RebuildAnchors();
            editor.InvalidateCanvas(true);
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

        InvalidateElement(element, true);
    }
    public void FitToCameraViewfinder(SKElement element, SKPoint clickPoint)
    {
        if (!_cameraViewRegion.Contains(clickPoint)) return;
        float viewW = element.CanvasSize.Width;
        float viewH = element.CanvasSize.Height;
        if (viewW <= 0 || viewH <= 0) return;

        float scaleX = viewW / _cameraViewRegion.Width;
        float scaleY = viewH / _cameraViewRegion.Height;

        _zoom = MathF.Min(scaleX, scaleY) * 0.9f;

        float cx = (_cameraViewRegion.Left + _cameraViewRegion.Right) * 0.5f;
        float cy = (_cameraViewRegion.Top + _cameraViewRegion.Bottom) * 0.5f;

        float vx = viewW * 0.5f;
        float vy = viewH * 0.5f;

        _viewOffset = new SKPoint(vx - cx * _zoom, vy - cy * _zoom);
        _topologyOffset = SKPoint.Empty;

        InvalidateElement(element, true);
    }
    public void SetSubstrateDim(float w, float h)
    {
        W = w;
        H = h;
        _substrateSize = new SKSize(w, h);
        _substrateWorld = new SKRect(0, 0, w, h);
        RebuildSubstrateAnchors();
        Application.Current.Dispatcher.Invoke(() => InvalidateCanvas(true));
    }
    public void SetTopologyScale((float oldScale, float newScale) scales)
    {
        _currentModelScale = scales.newScale;
        _modelTransform = Transform2D.Scale(scales.oldScale, scales.oldScale, new SKPoint(0, 0))
            .Then(_modelTransform);
        _modelTransform = Transform2D.Scale(1f / scales.newScale, 1f / scales.newScale, new SKPoint(0, 0))
            .Then(_modelTransform);

        _modelTransformWithoutTranslation = Transform2D.Scale(scales.oldScale, scales.oldScale, new SKPoint(0, 0))
            .Then(_modelTransformWithoutTranslation);
        _modelTransformWithoutTranslation = Transform2D.Scale(1f / scales.newScale, 1f / scales.newScale, new SKPoint(0, 0))
            .Then(_modelTransformWithoutTranslation);
        InvokeTransformationsChangedEvent();
        Fit();
    }

    private void InvokeTransformationsChangedEvent() => TransformChanged?.Invoke((_modelTransform.GetTransformation(), _modelTransformWithoutTranslation.GetTransformation()));
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

    private void DrawSuabstrateLayer(SKCanvas canvas)
    {
        using var stroke = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f / _zoom
        };
        foreach (var path in _substrateLayerPaths)
        {
            canvas.DrawPath(path, stroke);
        }
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
    private readonly SKPaint _scenePaint = new()
    {
        Style = SKPaintStyle.Stroke,
        IsAntialias = true,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round
    };

    private void RebuildScenePicture()
    {
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(SKRect.Create(-1_000_000, -1_000_000, 2_000_000, 2_000_000));

        // DrawSceneGeometry(canvas);
        DrawScenePaths(canvas);
        _scenePicture = recorder.EndRecording();
    }
    private void RebuildScenePaths()
    {
        if (_scenePaths != null)
        {
            foreach (var p in _scenePaths.Values)
                p.Dispose();
        }

        _scenePaths = _scene.Geometry
            .GroupBy(g => g.argb)
            .ToDictionary(
                o => o.Key,
                o =>
                {
                    var path = new SKPath();

                    foreach (var g in o)
                        path.AddPath(g.path.Simplify() ?? g.path);

                    return path;
                });
    }

    private void DrawScenePaths(SKCanvas canvas)
    {
        if (!_scenePaths.Any()) return;
        foreach (var (argb, path) in _scenePaths)
        {
            var color = new SKColor(argb);
            var trueColor = color == SKColors.White ? SKColors.Black : color;

            _scenePaint.Color = trueColor;
            _scenePaint.StrokeWidth = 1.2f * _currentModelScale / _zoom;

            canvas.DrawPath(path, _scenePaint);
        }
    }
    private void DrawSourceAnchor(SKCanvas canvas)
    {
        if (_hoverAnchor == null) return;
        if (!_teachPointsEnable)
        {
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
        else
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(0, 255, 20),   // пастельный голубой
                IsAntialias = true,
                StrokeWidth = 3f * _currentModelScale / _zoom,
                Style = SKPaintStyle.Stroke
            };
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0, 0, 0, 40),
                IsAntialias = true
            };
            float r = 6f * _currentModelScale / _zoom;
            //canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r, paint);
            //canvas.DrawCircle(_hoverAnchor.Value.WorldPoint, r, shadowPaint);
            var p = new SKPoint(5 * _currentModelScale / _zoom, 5 * _currentModelScale / _zoom);
            var p1 = new SKPoint(5 * _currentModelScale / _zoom, -5 * _currentModelScale / _zoom);

            canvas.DrawLine(_hoverAnchor.Value.WorldPoint - p, _hoverAnchor.Value.WorldPoint + p, paint);
            canvas.DrawLine(_hoverAnchor.Value.WorldPoint - p1, _hoverAnchor.Value.WorldPoint + p1, paint);
        }
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
        if (_targetAnchor == null || _teachPointsEnable) return;

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
    private SKPoint SubstrateToScreen(SKPoint p)
    {
        return new SKPoint(
            p.X * _zoom + _viewOffset.X,
            p.Y * _zoom + _viewOffset.Y
        );
    }
    private void DrawViewfinders(SKCanvas canvas)
    {
        //if (!_motionEnable) return;
        const float r = 5f;
        using var cameraPaint = new SKPaint
        {
            Color = SKColors.Green,
            StrokeWidth = 1f / _zoom,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var cameraRegion = new SKPaint
        {
            Color = new SKColor(255, 255, 0, 50),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var laserPaint = new SKPaint { Color = SKColors.Violet, StrokeWidth = 1f / _zoom, Style = SKPaintStyle.Stroke, IsAntialias = true };

        var cameraVF = _cameraViewfinder;// SubstrateToScreen(_cameraViewfinder);
        var laserVF = _laserViewfinder;// SubstrateToScreen(_laserViewfinder);

        if (_motionEnable)
        {
            canvas.DrawLine(cameraVF.X - r, cameraVF.Y, cameraVF.X - 2.5f, cameraVF.Y, cameraPaint);
            canvas.DrawLine(cameraVF.X + 2.5f, cameraVF.Y, cameraVF.X + r, cameraVF.Y, cameraPaint);

            canvas.DrawLine(cameraVF.X, cameraVF.Y - r, cameraVF.X, cameraVF.Y - 2.5f, cameraPaint);
            canvas.DrawLine(cameraVF.X, cameraVF.Y + 2.5f, cameraVF.X, cameraVF.Y + r, cameraPaint);

            canvas.DrawRect(_cameraViewRegion, cameraRegion);
            canvas.DrawRect(_cameraViewRegion, cameraPaint);
        }
        else
        {
            canvas.DrawLine(cameraVF.X - r, cameraVF.Y, cameraVF.X + r, cameraVF.Y, cameraPaint);
            canvas.DrawLine(cameraVF.X, cameraVF.Y - r, cameraVF.X, cameraVF.Y + r, cameraPaint);
        }

        canvas.DrawLine(laserVF.X - r, laserVF.Y, laserVF.X + r, laserVF.Y, laserPaint);
        canvas.DrawLine(laserVF.X, laserVF.Y - r, laserVF.X, laserVF.Y + r, laserPaint);
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
        var background = new SKColor(250, 248, 244);   // теплый светло-бежевый

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
        DrawSubstrateTexts(canvas);
        DrawSuabstrateLayer(canvas);
        canvas.Restore();


        // --- Topology ---
        canvas.Save();
        ApplyView(canvas);
        ApplyModel(canvas);
        DrawSourceAnchor(canvas);
        // DrawScene(canvas);
        if (_redrawTopology || _scenePicture is null)
        {
            RebuildScenePicture();
            _redrawTopology = false;
        }
        canvas.DrawPicture(_scenePicture);
        canvas.Restore();

        // --- Overlay (world space) ---
        canvas.Save();
        ApplyView(canvas);
        DrawRubberLine(canvas);
        DrawViewfinders(canvas);
        DrawPivot(canvas);
        canvas.Restore();

        // --- Overlay (screen space) ---


    }
    private bool _redrawTopology;

    private void InvalidateCanvas(bool withTopology = false)
    {
        _redrawTopology = withTopology;
        Canvas.InvalidateVisual();
    }
    private void InvalidateElement(SKElement element, bool withTopology = false)
    {
        _redrawTopology = withTopology;
        element.InvalidateVisual();
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

        InvalidateCanvas(true);
    }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _teachPointsEnable)
        {
            var coors = ScreenToWorld(ToSk(e.GetPosition(Canvas)));
            if (_cameraViewRegion.Contains(coors))
            {
                if (_hoverAnchor is not null) OnTopologyPointClicked?.Invoke((_hoverAnchor.Value.WorldPoint.X, _hoverAnchor.Value.WorldPoint.Y));
                return;
            }
        }
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
                InvalidateCanvas();
            }
        }

        if (_hoverAnchor is Anchor a && e.LeftButton == MouseButtonState.Pressed && !_cutEnable && _canBeAnchored)
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
        // if (_motionEnable) return;
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
            InvokeTransformationsChangedEvent();
            InvalidateCanvas(true);
        }
        else if (!_cutEnable)
        {
            float tol = 5f / _zoom;

            if (_alignState == AlignState.Idle)
            {
                if ((!_teachPointsEnable || _cameraViewRegion.Contains(_currentMouseWorld)) && (!_motionEnable || _teachPointsEnable) && (_canBeAnchored || _teachPointsEnable))
                {
                    _hoverAnchor = FindTopologyAnchor(_currentMouseWorld, tol);
                    InvalidateCanvas();
                }
            }
            else if (_alignState == AlignState.Dragging)
            {
                if (TryFindSubstrateCornerAnchor(_currentMouseWorld, tol, out var corner))
                {
                    _targetAnchor = corner;
                    InvalidateCanvas();
                }
                else
                {
                    _targetAnchor = null;
                    InvalidateCanvas();
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
            InvalidateCanvas();
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
        InvalidateCanvas();
    }
    public void SetTeachPointsEnable(bool enable) => _teachPointsEnable = enable;

    private (float x, float y) _cameraVfCoorsTemp = (0f, 0f);
    public void SetViewfindersCoordinates((float x, float y) cameraVfCoors, (float x, float y) laserVfCoors)
    {
        if (Math.Abs(_cameraVfCoorsTemp.x - cameraVfCoors.x) < 0.003f && Math.Abs(_cameraVfCoorsTemp.y - cameraVfCoors.y) < 0.003f) return;
        _cameraVfCoorsTemp = cameraVfCoors;
        _cameraViewfinder = new SKPoint(cameraVfCoors.x, cameraVfCoors.y);
        _laserViewfinder = new SKPoint(laserVfCoors.x, laserVfCoors.y);
        _cameraViewRegion = SKRect.Create(_cameraViewfinder - new SKPoint(2.5f, 2.5f), new(5, 5));


        long now = Environment.TickCount64;

        if (now - _lastRedrawTime > FRAME_TIME_MS)
        {
            _lastRedrawTime = now;
            Canvas.Dispatcher.Invoke(() => InvalidateCanvas());
        }
        else if (!_redrawPending)
        {
            _redrawPending = true;

            Task.Delay(FRAME_TIME_MS).ContinueWith(_ =>
            {
                _redrawPending = false;
                Canvas.Dispatcher.Invoke(() => InvalidateCanvas());
            });
        }
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
        InvokeTransformationsChangedEvent();
        BuildScene(EntitiesView);
        InvalidateCanvas(true);
    }
    public void EnableAnchors(bool enable) => _canBeAnchored = enable;
    public void RotateCW(SKPoint worldCenter, bool relCenter = false)
    {
        if (relCenter)
        {
            var bounds = _scene.Bounds;
            worldCenter = new SKPoint(bounds.MidX, bounds.MidY);
        }
        _modelTransform = Transform2D.Rotate(90, worldCenter).Then(_modelTransform);
        _modelTransformWithoutTranslation = Transform2D.Rotate(90, new SKPoint(0f, 0f)).Then(_modelTransformWithoutTranslation);
        InvokeTransformationsChangedEvent();
        _modelRotated ^= true;
        InvalidateCanvas(true);
    }
    public void RotateCCW(SKPoint worldCenter, bool relCenter = false)
    {
        if (relCenter)
        {
            var bounds = _scene.Bounds;
            worldCenter = new SKPoint(bounds.MidX, bounds.MidY);
        }
        _modelTransform = Transform2D.Rotate(-90, worldCenter).Then(_modelTransform);
        _modelTransformWithoutTranslation = Transform2D.Rotate(-90, new SKPoint(0f, 0f)).Then(_modelTransformWithoutTranslation);
        InvokeTransformationsChangedEvent();
        _modelRotated ^= true;
        InvalidateCanvas(true);
    }

    public void MirrorX(SKPoint worldCenter, bool relCenter = false)
    {
        if (relCenter && _scenePicture is not null)
        {
            var bounds = _scene.Bounds;
            worldCenter = new SKPoint(bounds.MidX, bounds.MidY);
        }
        if (!_modelRotated)
        {
            _modelTransform = Transform2D.MirrorX(worldCenter).Then(_modelTransform);
        }
        else
        {
            _modelTransform = Transform2D.MirrorY(worldCenter).Then(_modelTransform);
        }
        _modelTransformWithoutTranslation = Transform2D.MirrorX(new SKPoint(0f, 0f)).Then(_modelTransformWithoutTranslation);
        InvokeTransformationsChangedEvent();
        InvalidateCanvas(true);
    }
    public void MirrorY(SKPoint worldCenter, bool relCenter = false)
    {
        if (relCenter && _scenePicture is not null)
        {
            var bounds = _scene.Bounds;
            worldCenter = new SKPoint(bounds.MidX, bounds.MidY);
        }
        if (!_modelRotated)
        {
            _modelTransform = Transform2D.MirrorY(worldCenter).Then(_modelTransform);
        }
        else
        {
            _modelTransform = Transform2D.MirrorX(worldCenter).Then(_modelTransform);
        }
        _modelTransformWithoutTranslation = Transform2D.MirrorY(new SKPoint(0f, 0f)).Then(_modelTransformWithoutTranslation);
        InvokeTransformationsChangedEvent();
        InvalidateCanvas(true);
    }
    public void PushToCenter()
    {
        var bounds = _scene.Bounds;

        _modelTransform = Transform2D.Translate(-bounds.MidX, -bounds.MidY)
            .Then(_modelTransformWithoutTranslation)
            .Then(Transform2D.Translate(W / 2, H / 2));

        InvokeTransformationsChangedEvent();
        InvalidateCanvas(true);
    }
    private void RebuildAnchors()
    {
        _topologyAnchors = GetTopologyAnchors(EntitiesView).ToList();
    }
    public void BuildScene(IEnumerable<CadEntity> cadEntities)
    {
        var enabledEnts = cadEntities.Where(l => l.LayerEnable);
        _enabledLayers = enabledEnts.Select(l => l.LayerName).Distinct().ToList();
        _scene.Clear();
        foreach (var e in enabledEnts)
        {
            if (IsCut(e))
                continue;
            _scene.AddPath(e);
        }
        RebuildScenePaths();
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

    (SKPoint pos, float angle) GetLabelTransform(SubstrateText label)
    {
        var r = SKRect.Create(_substrateSize);

        float cx = (float)(r.Left + r.Right) * 0.5f;
        float cy = (float)(r.Top + r.Bottom) * 0.5f;

        return label.Side switch
        {
            LabelSide.Top =>
                (new SKPoint(cx + label.OffsetAlongEdge,
                             (float)r.Top - label.OffsetFromEdge), 0),

            LabelSide.Bottom =>
                (new SKPoint(cx + label.OffsetAlongEdge,
                             (float)r.Bottom + label.OffsetFromEdge), 0),

            LabelSide.Left =>
                (new SKPoint((float)r.Left - label.OffsetFromEdge,
                             cy + label.OffsetAlongEdge), -MathF.PI / 2),

            LabelSide.Right =>
                (new SKPoint((float)r.Right + label.OffsetFromEdge,
                             cy + label.OffsetAlongEdge), MathF.PI / 2),
        };
    }

    void DrawSubstrateTexts(SKCanvas canvas)
    {
        foreach (var label in SubstrateTexts)
        {
            DrawLabel(canvas, label);
        }
    }

    //void DrawLabel(SKCanvas canvas, SubstrateText label)
    //{
    //    var (pos, angle) = GetLabelTransform(label);

    //    using var paint = new SKPaint
    //    {
    //        IsAntialias = true,
    //        Color = label.Color
    //    };
    //    using var font = new SKFont
    //    {
    //        Typeface = label.Typeface,
    //        Size = label.FontSize
    //    };
    //    font.MeasureText(label.Text, out var bounds);

    //    float offsetX = label.Alignment switch
    //    {
    //        LabelAlignment.Start => 0,
    //        LabelAlignment.Center => -bounds.Width / 2,
    //        LabelAlignment.End => -bounds.Width,
    //        _ => 0
    //    };

    //    canvas.Save();

    //    canvas.Translate(pos.X, pos.Y);
    //    canvas.RotateRadians(angle);

    //    canvas.DrawText(label.Text, new SKPoint(offsetX, 0), font, paint);
    //    canvas.Restore();
    //}

    void DrawLabel(SKCanvas canvas, SubstrateText label)
    {
        var (pos, angle) = GetLabelTransform(label);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = label.Color
        };

        using var font = new SKFont
        {
            Typeface = label.Typeface,
            Size = label.FontSize
        };

        font.MeasureText(label.Text, out var bounds);

        float offsetX = label.Alignment switch
        {
            LabelAlignment.Start => 0,
            LabelAlignment.Center => -bounds.Width / 2,
            LabelAlignment.End => -bounds.Width,
            _ => 0
        };

        var metrics = font.Metrics;
        float baseline = -metrics.Ascent;

        canvas.Save();

        canvas.Translate(pos.X, pos.Y);
        canvas.RotateRadians(angle);

        canvas.DrawText(label.Text, new SKPoint(offsetX, baseline), font, paint);

        canvas.Restore();
    }

    public void AddSubstrateText(string text)
    {
        SubstrateTexts.Add(new SubstrateText
        {
            Text = text,
            Side = LabelSide.Top,
            Alignment = LabelAlignment.Center,
            OffsetFromEdge = 5
        });

        InvalidateCanvas();
    }

    public void AddEntitiesToSubstrateLayer(IEnumerable<CadEntity> cadEntities)
    {
        //var enabledEnts = cadEntities.Where(l => l.LayerEnable);
        //_enabledLayers = enabledEnts.Select(l => l.LayerName).Distinct().ToList();
        _substrateLayerScene.Clear();
        foreach (var e in cadEntities)
        {
            _substrateLayerScene.AddPath(e);
        }
        if (_substrateLayerPaths != null)
        {
            foreach (var p in _substrateLayerPaths)
                p.Dispose();
        }

        _substrateLayerPaths = _substrateLayerScene.Geometry.Select(g => g.path).ToList();
    }
}

