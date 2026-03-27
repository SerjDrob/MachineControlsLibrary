using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace MachineControlsLibrary.Classes.SkEditor;


public class SimpleSkEditor : SKElement, IDisposable
{
    public enum Tool
    {
        Select,
        Line,
        Circle,
        Polyline,
        Text,
        Fillet
    }

    // Fillet state
    PolyShape _filletPoly;
    int _filletVertex = -1;
    float _filletRadius = 0;
    bool _filletPreview = false;
    bool _filletFlip = false;
    int _filletSegBeforeIndex = -1;
    int _filletSegAfterIndex = -1;
    bool _polyClosedPending = false; // Флаг: можно замкнуть (курсор рядом с первой точкой)
    const float CLOSE_THRESHOLD = 15f; // Пиксели для подсветки замыкания

    // Пaints для индикации замыкания
    readonly SKPaint _closeHintPaint = new()
    {
        Color = new SKColor(0, 150, 0),
        StrokeWidth = 2,
        IsStroke = true,
        PathEffect = SKPathEffect.CreateDash(new[] { 5f, 5f }, 0)
    };
    readonly SKPaint _closePointPaint = new()
    {
        Color = new SKColor(0, 200, 0),
        IsStroke = false
    };
    public Tool CurrentTool { get; set; }
    public bool SnapEnabled { get; set; } = false;

    // Viewport
    float _zoom = 1f;
    SKPoint _viewOffset;
    float _gridStep = 20;

    // Drawing state
    bool _drawing;
    SKPoint _start;
    SKPoint _current;

    // Shapes and selection
    readonly List<Shape> _shapes = new();
    readonly List<SKPoint> _poly = new();
    Shape _selected;
    Handle _activeHandle;
    public Action OnMouseRightClicked;
    const float HANDLE_SIZE = 6;
    const float HIT_DIST = 16;

    // Paints
    readonly SKPaint _gridPaint = new() { Color = new SKColor(220, 220, 220), StrokeWidth = 1 };
    readonly SKPaint _shapePaint = new() { Color = SKColors.Black, StrokeWidth = 2, IsStroke = true, IsAntialias = true };
    readonly SKPaint _previewPaint = new() { Color = SKColors.Red, StrokeWidth = 2, IsStroke = true };
    readonly SKPaint _selectPaint = new() { Color = SKColors.Orange, StrokeWidth = 3, IsStroke = true };
    readonly SKPaint _handlePaint = new() { Color = SKColors.Blue, IsStroke = false };
    readonly SKPaint _textPaint = new() { Color = SKColors.Black, TextSize = 14, IsAntialias = true };

    public SimpleSkEditor()
    {
        PaintSurface += OnPaint;
    }
    SKPoint GetScreenCenter() => new((float)ActualWidth / 2, (float)ActualHeight / 2);
    // ✅ Начало координат (0,0) в мировых координатах — всегда кратно шагу сетки
    // При _zoom=1: (0,0) совпадает с узлом сетки
    // При масштабировании: привязка к сетке сохраняется

    void CenterOrigin()
    {
        _viewOffset = GetScreenCenter();
    }
    public void DeleteAllShapes()
    {
        // Удаляем все созданные фигуры
        _shapes.Clear();

        // Сбрасываем выделение
        _selected = null;
        _activeHandle = null;

        // Отменяем текущее рисование
        _drawing = false;
        _poly.Clear();
        _start = _current = default;

        // Сбрасываем превью fillet
        ResetFilletState();

        // Перерисовываем
        InvalidateVisual();
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        // ✅ Просто перецентрируем (0,0) в новый центр
        CenterOrigin();
        InvalidateVisual();
    }


    SKPoint ScreenToWorld(SKPoint p) => new((p.X - _viewOffset.X) / _zoom, (p.Y - _viewOffset.Y) / _zoom);
    SKPoint WorldToScreen(SKPoint p) => new(p.X * _zoom + _viewOffset.X, p.Y * _zoom + _viewOffset.Y);

    SKPoint Snap(SKPoint p)
    {
        if (!SnapEnabled) return p;
        return new SKPoint(
            (float)Math.Round(p.X / _gridStep) * _gridStep,
            (float)Math.Round(p.Y / _gridStep) * _gridStep);
    }

    void OnPaint(object sender, SKPaintSurfaceEventArgs e)
    {
        // Инициализация при первой отрисовке
        if (ActualWidth > 0 && ActualHeight > 0 &&
            (_viewOffset.X == 0 && _viewOffset.Y == 0 || _zoom == 1f && _shapes.Count == 0))
        {
            CenterOrigin();
        }

        var canvas = e.Surface.Canvas;
        canvas.Clear(/*SKColors.White*/SKColors.Transparent);
        DrawGrid(canvas);

        canvas.Save();
        canvas.Translate(_viewOffset);
        canvas.Scale(_zoom);

        foreach (var s in _shapes)
            s.Draw(canvas, s == _selected ? _selectPaint : _shapePaint);

        if (_drawing) DrawPreview(canvas);
        if (_selected != null) DrawHandles(canvas);
        if (_filletPreview) DrawFilletPreview(canvas);
        canvas.Restore();

        if (_drawing) DrawMeasurement(canvas);
    }
    void DrawFilletPreview(SKCanvas canvas)
    {
        if (_filletPoly == null || _filletSegBeforeIndex < 0 || _filletSegAfterIndex < 0)
            return;

        // ✅ Берём точки из сегментов, а не из Points
        var segBefore = _filletPoly.Segments[_filletSegBeforeIndex];
        var segAfter = _filletPoly.Segments[_filletSegAfterIndex];

        if (segBefore.IsArc || segAfter.IsArc)
            return;

        var A = segBefore.A;      // ✅ Актуальное начало сегмента "до"
        var B = _filletPoly.Points[_filletVertex];  // Вершина угла
        var C = segAfter.B;       // ✅ Актуальный конец сегмента "после"

        var BA = GeometryHelper.Normalize(A - B);
        var BC = GeometryHelper.Normalize(C - B);

        float dot = BA.X * BC.X + BA.Y * BC.Y;
        dot = Math.Clamp(dot, -1f, 1f);
        float angle = MathF.Acos(dot);
        float d = _filletRadius / MathF.Tan(angle / 2);

        // Точки касания
        var T1 = new SKPoint(B.X + BA.X * d, B.Y + BA.Y * d);
        var T2 = new SKPoint(B.X + BC.X * d, B.Y + BC.Y * d);

        // Центр дуги (всегда через биссектрису!)
        var center = GeometryHelper.ComputeFilletCenter(A, B, C, _filletRadius);

        // Углы для дуги
        float startAngle = GeometryHelper.Angle(center, T1);
        float sweep = GeometryHelper.ComputeSweep(center, T1, T2, _filletFlip);

        // Рисуем обрезанные сегменты
        canvas.DrawLine(A, T1, _previewPaint);
        canvas.DrawLine(T2, C, _previewPaint);

        // Рисуем дугу
        var rect = new SKRect(
            center.X - _filletRadius,
            center.Y - _filletRadius,
            center.X + _filletRadius,
            center.Y + _filletRadius);

        using var path = new SKPath();
        path.AddArc(rect, startAngle, sweep);
        canvas.DrawPath(path, _previewPaint);
    }

    void DrawGrid(SKCanvas canvas)
    {
        if(!SnapEnabled) return;
        float step = _gridStep * _zoom;
        var origin = WorldToScreen(new SKPoint(0, 0));

        int w = (int)ActualWidth, h = (int)ActualHeight;

        // Линии от начала координат
        for (float x = origin.X; x < w; x += step) canvas.DrawLine(x, 0, x, h, _gridPaint);
        for (float x = origin.X; x > 0; x -= step) canvas.DrawLine(x, 0, x, h, _gridPaint);
        for (float y = origin.Y; y < h; y += step) canvas.DrawLine(0, y, w, y, _gridPaint);
        for (float y = origin.Y; y > 0; y -= step) canvas.DrawLine(0, y, w, y, _gridPaint);

        // Оси координат
        using var axisPaint = new SKPaint { StrokeWidth = 2, IsStroke = true };
        axisPaint.Color = SKColors.Red;
        canvas.DrawLine(origin.X, 0, origin.X, h, axisPaint);
        axisPaint.Color = SKColors.Green;
        canvas.DrawLine(0, origin.Y, w, origin.Y, axisPaint);
    }

    void DrawPreview(SKCanvas canvas)
    {
        switch (CurrentTool)
        {
            case Tool.Line:
                canvas.DrawLine(_start, _current, _previewPaint);
                break;
            case Tool.Circle:
                float r = Distance(_start, _current);
                canvas.DrawCircle(_start, r, _previewPaint);
                DrawCross(canvas, _start);
                break;
            case Tool.Polyline:
                // Рисуем существующие сегменты полилинии
                for (int i = 1; i < _poly.Count; i++)
                    canvas.DrawLine(_poly[i - 1], _poly[i], _previewPaint);

                if (_poly.Count > 0)
                {
                    // Линия от последней точки к курсору
                    canvas.DrawLine(_poly[^1], _current, _previewPaint);

                    // Если можно замкнуть — рисуем линию к первой точке
                    if (_polyClosedPending && _poly.Count >= 3)
                    {
                        canvas.DrawLine(_current, _poly[0], _closeHintPaint);
                        // Подсвечиваем первую точку
                        canvas.DrawCircle(_poly[0], HANDLE_SIZE * 1.5f / _zoom, _closePointPaint);
                    }
                }
            break;
        }
    }

    void DrawCross(SKCanvas canvas, SKPoint p)
    {
        float s = 6 / _zoom;
        canvas.DrawLine(p.X - s, p.Y, p.X + s, p.Y, _previewPaint);
        canvas.DrawLine(p.X, p.Y - s, p.X, p.Y + s, _previewPaint);
    }

    void DrawHandles(SKCanvas canvas)
    {
        foreach (var h in _selected?.GetHandles() ?? new List<Handle>())
            canvas.DrawCircle(h.X, h.Y, HANDLE_SIZE / _zoom, _handlePaint);
    }

    void DrawMeasurement(SKCanvas canvas)
    {
        var getMeasure = () =>
        {
            float perimeter = 0;
            for (int i = 1; i < _poly.Count; i++)
                perimeter += Distance(_poly[i - 1], _poly[i]);
            if (_polyClosedPending)
                perimeter += Distance(_poly[^1], _poly[0]); // Замкнутая длина

             return _polyClosedPending
                ? $"Close: L={perimeter:0.##}"
                : $"L={perimeter:0.##} | {Distance(_poly[^1], _current):0.##}";
        };
        string text = CurrentTool switch
        {
            Tool.Line => Distance(_start, _current).ToString("0.##"),
            Tool.Circle => "Ø " + (Distance(_start, _current) * 2).ToString("0.##"),
            //Tool.Polyline when _poly.Count > 0 => Distance(_poly[^1], _current).ToString("0.##"),
            Tool.Polyline when _poly.Count >= 3 => getMeasure(),
            Tool.Polyline when _poly.Count > 0 =>
                _polyClosedPending
                    ? $"Close: {Distance(_poly[^1], _current):0.##}"
                    : Distance(_poly[^1], _current).ToString("0.##"),
            _ => " "
        };
        var p = WorldToScreen(_current);
        canvas.DrawText(text, p.X + 15, p.Y + 15, _textPaint);
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        var p = Snap(ScreenToWorld(ToSK(e.GetPosition(this))));
        if (e.ClickCount == 2 && (e.ChangedButton == MouseButton.Middle ||
            (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.LeftCtrl))))
        {
            _zoom = 1f;
            CenterOrigin();
            InvalidateVisual();
            return;
        }
        // ✅ ИСПРАВЛЕНО: ПКМ во время fillet — переключение направления дуги
        // Проверяем ChangedButton, не RightButton
        if (CurrentTool == Tool.Fillet && _filletPreview && e.ChangedButton == MouseButton.Right)
        {
            _filletFlip = !_filletFlip;
            InvalidateVisual();
            return;
        }

        if (CurrentTool == Tool.Select)
        {
            _activeHandle = GetHandle(p);
            SelectShape(p);
            InvalidateVisual();
            return;
        }

        if (CurrentTool == Tool.Fillet && e.ChangedButton == MouseButton.Left)
        {
            if (!_filletPreview)
                StartFillet(p);
            else 
                ConfirmFillet();

            InvalidateVisual();
            return;
        }

        // ✅ ИСПРАВЛЕНО: Проверка ПКМ для завершения полилинии
        if (CurrentTool == Tool.Polyline && _drawing)
        {
            // Проверка замыкания (клик по первой точке) — ЛКМ
            if (e.ChangedButton == MouseButton.Left && _poly.Count >= 3 && _polyClosedPending)
            {
                ClosePolyline();
                return;
            }

            // ✅ Завершение разомкнутой полилинии — ПКМ
            if (e.ChangedButton == MouseButton.Right)
            {
                // Минимум 2 точки для линии или 1 для точки (но смысла нет)
                if (_poly.Count >= 2)
                {
                    var poly = new PolyShape([.. _poly], closed: false);
                    _shapes.Add(poly);
                    _drawing = false;
                    _poly.Clear();
                    _polyClosedPending = false;
                    InvalidateVisual();
                }
                else
                {
                    // Менее 2 точек — просто отменяем
                    CancelDrawing();
                }
                return;
            }

            // Обычная точка полилинии — ЛКМ
            if (e.ChangedButton == MouseButton.Left)
            {
                _poly.Add(p);
                InvalidateVisual();
            }

            return;
        }

        // Начало рисования
        if (!_drawing && e.ChangedButton == MouseButton.Left)
        {
            _start = _current = p;
            _drawing = true;

            if (CurrentTool == Tool.Polyline)
            {
                _poly.Add(p);
                _polyClosedPending = false;
            }

            InvalidateVisual();
        }
        if (e.ChangedButton == MouseButton.Right)
        {
            OnMouseRightClicked?.Invoke();
        }
    }
    //protected override void OnMouseDown(MouseButtonEventArgs e)
    //{
    //    var p = Snap(ScreenToWorld(ToSK(e.GetPosition(this))));
    //    // Сброс вида по двойному клику колесом или Ctrl+двойной ЛКМ
    //    if (e.ClickCount == 2 && (e.ChangedButton == MouseButton.Middle ||
    //        (e.ChangedButton == MouseButton.Left && Keyboard.IsKeyDown(Key.LeftCtrl))))
    //    {
    //        _zoom = 1f;
    //        CenterOrigin();
    //        InvalidateVisual();
    //        return;
    //    }
    //    // Проверка на замыкание полилинии (клик по первой точке или Ctrl+клик)
    //    if (CurrentTool == Tool.Polyline && _drawing && _polyClosedPending)
    //    {
    //        // Замыкаем полилинию
    //        ClosePolyline();
    //        return;
    //    }
    //    if (CurrentTool == Tool.Fillet && _filletPreview && e.ChangedButton == MouseButton.Right)
    //    {
    //        _filletFlip = !_filletFlip;
    //        InvalidateVisual();
    //        return;
    //    }
    //    else if (e.ChangedButton == MouseButton.Right)
    //    {
    //        OnMouseRightClicked?.Invoke();
    //    }

    //    if (CurrentTool == Tool.Select)
    //    {
    //        _activeHandle = GetHandle(p);
    //        SelectShape(p);
    //        InvalidateVisual();
    //        return;
    //    }

    //    if (CurrentTool == Tool.Fillet)
    //    {
    //        if (!_filletPreview)
    //        {
    //            StartFillet(p);
    //        }
    //        else
    //        {
    //            // ЛКМ подтверждает скругление
    //            if (e.ChangedButton == MouseButton.Left)
    //            {
    //                ConfirmFillet();
    //            }
    //        }
    //        InvalidateVisual();
    //        return;
    //    }
    //    if (!_drawing)
    //    {
    //        _start = _current = p;
    //        _drawing = true;
    //        if (CurrentTool == Tool.Polyline)
    //        {
    //            _poly.Add(p);
    //            _polyClosedPending = false;
    //        }
    //    }
    //    else if (CurrentTool == Tool.Polyline)
    //    {
    //        // Проверяем, не кликнули ли по первой точке для замыкания
    //        if (_poly.Count >= 3 && Distance(p, _poly[0]) < HIT_DIST / _zoom)
    //        {
    //            ClosePolyline();
    //            return;
    //        }

    //        _poly.Add(p);
    //    }
    //    InvalidateVisual();
    //}
    void ClosePolyline()
    {
        if (_poly.Count < 3) return; // Минимум треугольник

        var poly = new PolyShape([.. _poly], closed: true); // <-- замкнутая
        _shapes.Add(poly);
        _drawing = false;
        _poly.Clear();
        _polyClosedPending = false;
        InvalidateVisual();
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var mousePos = ToSK(e.GetPosition(this));
        var worldBefore = ScreenToWorld(mousePos);

        float oldZoom = _zoom;
        _zoom *= e.Delta > 0 ? 1.1f : 0.9f;
        _zoom = Math.Clamp(_zoom, 0.01f, 100f);

        // Zoom к курсору: сохраняем мировую точку под курсором
        var worldAfter = ScreenToWorld(mousePos);
        _viewOffset.X += (worldAfter.X - worldBefore.X) * _zoom;
        _viewOffset.Y += (worldAfter.Y - worldBefore.Y) * _zoom;

        InvalidateVisual();
    }
    void StartFillet(SKPoint p)
    {
        foreach (var s in _shapes)
        {
            if (s is PolyShape poly)
            {
                int v = FindVertex(poly, p);

                // ✅ Исправлено: для замкнутой полилинии допускаем v = 0 и v = Points.Count - 1
                bool validVertex = poly.IsClosed
                    ? (v >= 0 && v < poly.Points.Count)
                    : (v > 0 && v < poly.Points.Count - 1);

                if (!validVertex) continue;

                var segBefore = poly.GetSegmentBeforeVertex(v, out int idxBefore);
                var segAfter = poly.GetSegmentAfterVertex(v, out int idxAfter);

                if (segBefore != null && segAfter != null &&
                    !segBefore.IsArc && !segAfter.IsArc)
                {
                    _filletPoly = poly;
                    _filletVertex = v;
                    _filletSegBeforeIndex = idxBefore;
                    _filletSegAfterIndex = idxAfter;
                    _filletPreview = true;
                    _filletRadius = 0;
                    _filletFlip = false;
                    return;
                }
            }
        }
    }

    void ConfirmFillet()
    {
        if (_filletPoly == null || _filletSegBeforeIndex < 0 || _filletSegAfterIndex < 0)
            return;

        var segBefore = _filletPoly.Segments[_filletSegBeforeIndex];
        var segAfter = _filletPoly.Segments[_filletSegAfterIndex];

        if (segBefore.IsArc || segAfter.IsArc)
        {
            ResetFilletState();
            return;
        }

        var A = segBefore.A;
        var B = _filletPoly.Points[_filletVertex];
        var C = segAfter.B;

        if (!GeometryHelper.ComputeFillet(A, B, C, _filletRadius, out var center, out var t1, out var t2))
        {
            ResetFilletState();
            return;
        }

        float startAngle = GeometryHelper.Angle(center, t1);

        // ✅ ИСПРАВЛЕНО: используем _filletFlip для вычисления sweep
        float sweep = GeometryHelper.ComputeSweep(center, t1, t2, _filletFlip);

        _filletPoly.AddFilletFromPreview(
            _filletSegBeforeIndex,
            _filletSegAfterIndex,
            t1, t2, center, _filletRadius, startAngle, sweep);

        ResetFilletState();
    }

    void ResetFilletState()
    {
        _filletPreview = false;
        _filletPoly = null;
        _filletVertex = -1;
        _filletSegBeforeIndex = -1;
        _filletSegAfterIndex = -1;
        _filletRadius = 0;
        _filletFlip = false; // ✅ Сбрасываем flip при сбросе состояния
    }

    int FindVertex(PolyShape poly, SKPoint p)
    {
        float tol = 10 / _zoom;
        for (int i = 0; i < poly.Points.Count; i++)
        {
            var v = poly.Points[i];
            if (Distance(v, p) < tol) return i;
        }
        return -1;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Enter или C — замкнуть полилинию
        if (CurrentTool == Tool.Polyline && _drawing &&
            (e.Key == Key.Enter || e.Key == Key.C))
        {
            if (_poly.Count >= 3)
            {
                ClosePolyline();
                e.Handled = true;
            }
        }

        // Escape — отмена рисования полилинии
        if (e.Key == Key.Escape && _drawing)
        {
            CancelDrawing();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    void CancelDrawing()
    {
        _drawing = false;
        _poly.Clear();
        _polyClosedPending = false;
        _filletPreview = false;
        ResetFilletState();
        InvalidateVisual();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        var p = Snap(ScreenToWorld(ToSK(e.GetPosition(this))));
        _current = p;

        // Проверка возможности замыкания полилинии
        if (CurrentTool == Tool.Polyline && _drawing && _poly.Count >= 3)
        {
            float distToFirst = Distance(p, _poly[0]);
            float threshold = CLOSE_THRESHOLD / _zoom;
            bool canClose = distToFirst < threshold;

            if (canClose != _polyClosedPending)
            {
                _polyClosedPending = canClose;
                InvalidateVisual();
            }
        }

        if (_activeHandle != null && _selected is PolyShape poly)
        {
            _activeHandle.Move(p);
            poly.BuildPoints();  // ✅ Обновляем Points в реальном времени
            InvalidateVisual();
            return;
        }

        if (_filletPreview)
        {
            var v = _filletPoly.Points[_filletVertex];
            float dx = p.X - v.X;
            float dy = p.Y - v.Y;
            _filletRadius = (float)Math.Sqrt(dx * dx + dy * dy);
            InvalidateVisual();
            return;
        }
        if (_activeHandle != null) { _activeHandle.Move(p); InvalidateVisual(); return; }
        if (_drawing) InvalidateVisual();
    }
   
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        var p = Snap(ScreenToWorld(ToSK(e.GetPosition(this))));

        // ✅ Если перемещали handle — обновляем Points у полилинии
        if (_activeHandle != null && _selected is PolyShape poly)
        {
            poly.BuildPoints();
        }

        _activeHandle = null;

        if (!_drawing) return;

        if (CurrentTool == Tool.Line)
        {
            _shapes.Add(new LineShape(_start, p));
            _drawing = false;
        }

        if (CurrentTool == Tool.Circle)
        {
            _shapes.Add(new CircleShape(_start, Distance(_start, p)));
            _drawing = false;
        }

        InvalidateVisual();
    }

    void SelectShape(SKPoint p)
    {
        _selected = null;
        foreach (var s in _shapes)
            if (s.HitTest(p, HIT_DIST / _zoom)) { _selected = s; break; }
    }

    Handle GetHandle(SKPoint p)
    {
        if (_selected == null) return null;
        foreach (var h in _selected.GetHandles())
            if (Distance(h.Position, p) < HIT_DIST / _zoom) return h;
        return null;
    }

    static float Distance(SKPoint a, SKPoint b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    SKPoint ToSK(Point p) => new((float)p.X, (float)p.Y);

    // ==================== Shape hierarchy ====================

    abstract class Shape
    {
        public abstract void Draw(SKCanvas c, SKPaint p);
        public abstract bool HitTest(SKPoint p, float tol);
        public abstract List<Handle> GetHandles();
    }

    class LineShape(SKPoint a, SKPoint b) : Shape
    {
        public SKPoint A = a, B = b;
        public override void Draw(SKCanvas c, SKPaint p) => c.DrawLine(A, B, p);
        public override bool HitTest(SKPoint p, float tol) => DistanceToSegment(p, A, B) < tol;
        public override List<Handle> GetHandles() => new()
        {
            new Handle(() => A, v => A = v),
            new Handle(() => B, v => B = v),
            new Handle(() => Mid(), MoveMid)
        };
        SKPoint Mid() => new((A.X + B.X) / 2, (A.Y + B.Y) / 2);
        void MoveMid(SKPoint p)
        {
            var mid = Mid();
            var dx = p.X - mid.X;
            var dy = p.Y - mid.Y;
            A = new SKPoint(A.X + dx, A.Y + dy);
            B = new SKPoint(B.X + dx, B.Y + dy);
        }
        static float DistanceToSegment(SKPoint p, SKPoint a, SKPoint b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Clamp(t, 0, 1);
            var proj = new SKPoint(a.X + t * dx, a.Y + t * dy);
            return Distance(p, proj);
        }
    }

    class CircleShape(SKPoint c, float r) : Shape
    {
        public SKPoint C = c;
        public float R = r;
        public override void Draw(SKCanvas c, SKPaint p) => c.DrawCircle(C, R, p);
        public override bool HitTest(SKPoint p, float tol) => Math.Abs(Distance(p, C) - R) < tol;
        public override List<Handle> GetHandles() => new()
        {
            new Handle(() => C, v => C = v),
            new Handle(() => new SKPoint(C.X + R, C.Y), p => R = Distance(C, p))
        };
    }

    class PolyShape : Shape
    {
        public bool IsClosed { get; }
        public class Segment
        {
            public bool IsArc;
            public SKPoint A, B;
            public SKPoint Center;
            public float Radius, StartAngle, Sweep;
        }

        public List<SKPoint> Points { get; } = new();
        readonly List<Segment> _segments = new();
        public List<Segment> Segments => _segments;

        public PolyShape(List<SKPoint> points)
        {
            Points.AddRange(points);
            for (int i = 1; i < points.Count; i++) AddLine(points[i - 1], points[i]);
        }
        public PolyShape(List<SKPoint> points, bool closed = false) : this(points)
        {
            IsClosed = closed;
            if (closed && points.Count > 2)
            {
                // Добавляем сегмент замыкания
                AddLine(points[^1], points[0]);
            }
        }
        public void AddLine(SKPoint a, SKPoint b) => _segments.Add(new Segment { IsArc = false, A = a, B = b });

        public override void Draw(SKCanvas c, SKPaint p)
        {
            foreach (var s in _segments)
            {
                if (!s.IsArc) c.DrawLine(s.A, s.B, p);
                else
                {
                    using var path = new SKPath();
                    var rect = new SKRect(s.Center.X - s.Radius, s.Center.Y - s.Radius,
                                          s.Center.X + s.Radius, s.Center.Y + s.Radius);
                    path.AddArc(rect, s.StartAngle, s.Sweep);
                    c.DrawPath(path, p);
                }
            }
        }

        public void AddFilletFromPreview(
       int segBeforeIndex,
       int segAfterIndex,
       SKPoint t1, SKPoint t2, SKPoint center,
       float radius, float startAngle, float sweep)
        {
            if (segBeforeIndex < 0 || segAfterIndex >= _segments.Count) return;

            var segBefore = _segments[segBeforeIndex];
            var segAfter = _segments[segAfterIndex];

            if (segBefore.IsArc || segAfter.IsArc) return;

            // Обрезаем сегменты
            segBefore.B = t1;
            segAfter.A = t2;

            // Создаём дугу
            var arc = new Segment
            {
                IsArc = true,
                A = t1,
                B = t2,
                Center = center,
                Radius = radius,
                StartAngle = startAngle,
                Sweep = sweep
            };

            // ✅ Для замкнутой полилинии: если segBeforeIndex > segAfterIndex 
            // (замыкание через 0), корректно вставляем
            if (IsClosed && segBeforeIndex > segAfterIndex)
            {
                // Случай: последний сегмент -> первый сегмент
                // Вставляем после segBefore (в конец) — он фактически перед segAfter
                _segments.Add(arc);
            }
            else
            {
                // Обычный случай
                _segments.Insert(segAfterIndex, arc);
            }

            RebuildPoints();
        }
        //public override bool HitTest(SKPoint p, float tol)
        //{
        //    foreach (var s in _segments)
        //    {
        //        if (!s.IsArc) { if (DistToSegment(p, s.A, s.B) < tol) return true; }
        //        else if (Math.Abs(SKPoint.Distance(p, s.Center) - s.Radius) < tol) return true;
        //    }
        //    return false;
        //}

        public override bool HitTest(SKPoint p, float tol)
        {
            // Проверка ребер (без base.HitTest)
            foreach (var s in _segments)
            {
                if (!s.IsArc)
                {
                    if (DistToSegment(p, s.A, s.B) < tol) return true;
                }
                else if (Math.Abs(SKPoint.Distance(p, s.Center) - s.Radius) < tol)
                    return true;
            }

            // Для замкнутой — проверяем внутренность
            if (IsClosed && IsPointInPolygon(p)) return true;

            return false;
        }
        bool IsPointInPolygon(SKPoint p)
        {
            bool inside = false;
            for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
            {
                var pi = Points[i];
                var pj = Points[j];
                if (((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                    (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                    inside = !inside;
            }
            return inside;
        }
        //public override List<Handle> GetHandles()
        //{
        //    var handles = new List<Handle>();
        //    var uniquePoints = new HashSet<(float x, float y)>();

        //    void AddHandle(SKPoint pt, Action<SKPoint> setter, int segIndex, bool isStart)
        //    {
        //        var key = (pt.X, pt.Y);
        //        if (!uniquePoints.Add(key)) return; // Пропускаем дубликаты

        //        handles.Add(new Handle(() => pt, p =>
        //        {
        //            setter(p);
        //            // Синхронизируем соседние сегменты
        //            if (isStart && segIndex > 0)
        //                _segments[segIndex - 1].B = p;
        //            if (!isStart && segIndex + 1 < _segments.Count)
        //                _segments[segIndex + 1].A = p;
        //        }));
        //    }

        //    for (int i = 0; i < _segments.Count; i++)
        //    {
        //        var s = _segments[i];
        //        int idx = i;
        //        if (i == 0)
        //            AddHandle(s.A, p => _segments[idx].A = p, i, true);
        //        AddHandle(s.B, p => _segments[idx].B = p, i, false);
        //    }
        //    return handles;
        //}
        public override List<Handle> GetHandles()
        {
            var handles = new List<Handle>();
            var uniquePoints = new HashSet<(float x, float y)>();

            void AddHandle(SKPoint pt, Action<SKPoint> setter, int segIndex, bool isStart)
            {
                var key = (pt.X, pt.Y);
                if (!uniquePoints.Add(key)) return;

                handles.Add(new Handle(() => pt, p =>
                {
                    setter(p);
                    // Синхронизируем соседние сегменты
                    if (isStart && segIndex > 0)
                        _segments[segIndex - 1].B = p;
                    if (!isStart && segIndex + 1 < _segments.Count)
                        _segments[segIndex + 1].A = p;

                    // ✅ Для замкнутой полилинии обновляем замыкающие сегменты
                    if (IsClosed)
                    {
                        if (isStart && segIndex == 0)
                            _segments[^1].B = p; // Последний сегмент заканчивается на первой точке
                        if (!isStart && segIndex == _segments.Count - 1)
                            _segments[0].A = p; // Первый сегмент начинается на последней точке
                    }

                    RebuildPoints();
                }));
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                var s = _segments[i];
                int idx = i;
                if (i == 0)
                    AddHandle(s.A, p => _segments[idx].A = p, i, true);
                AddHandle(s.B, p => _segments[idx].B = p, i, false);
            }
            return handles;
        }
        static bool PointsAreEqual(SKPoint a, SKPoint b, float tol = 1e-3f)
        {
            return MathF.Abs(a.X - b.X) < tol && MathF.Abs(a.Y - b.Y) < tol;
        }

        void RebuildPoints()
        {
            Points.Clear();
            if (_segments.Count == 0) return;

            Points.Add(_segments[0].A);
            foreach (var s in _segments)
                Points.Add(s.B);

            // Для замкнутой удаляем дубликат последней точки (она совпадает с первой)
            if (IsClosed && Points.Count > 1 &&
                PointsAreEqual(Points[0], Points[^1]))
            {
                Points.RemoveAt(Points.Count - 1);
            }
        }
        public Segment GetSegmentBeforeVertex(int vertexIndex, out int segmentIndex)
        {
            segmentIndex = -1;
            if (Points.Count == 0) return null;

            // Для замкнутой полилинии индексы цикличны
            if (IsClosed)
            {
                if (vertexIndex < 0 || vertexIndex >= Points.Count) return null;

                float tol = 10f;
                var targetPoint = Points[vertexIndex];

                // Ищем сегмент, который заканчивается на targetPoint
                for (int i = 0; i < _segments.Count; i++)
                {
                    var seg = _segments[i];
                    if (!seg.IsArc && Distance(seg.B, targetPoint) < tol)
                    {
                        segmentIndex = i;
                        return seg;
                    }
                }
                return null;
            }
            else
            {
                // Оригинальная логика для разомкнутой
                if (vertexIndex < 1 || vertexIndex >= Points.Count) return null;

                float tol = 10f;
                var B = Points[vertexIndex];

                for (int i = 0; i < _segments.Count; i++)
                {
                    var seg = _segments[i];
                    if (!seg.IsArc && Distance(seg.B, B) < tol)
                    {
                        segmentIndex = i;
                        return seg;
                    }
                }
                return null;
            }
        }
        public void BuildPoints()
        {
            Points.Clear();
            if (_segments.Count == 0) return;

            // Добавляем первую точку
            Points.Add(_segments[0].A);

            // Добавляем конечные точки каждого сегмента
            for (int i = 0; i < _segments.Count; i++)
            {
                Points.Add(_segments[i].B);
            }
        }
        public Segment GetSegmentAfterVertex(int vertexIndex, out int segmentIndex)
        {
            segmentIndex = -1;
            if (Points.Count == 0) return null;

            // Для замкнутой полилинии индексы цикличны
            if (IsClosed)
            {
                if (vertexIndex < 0 || vertexIndex >= Points.Count) return null;

                float tol = 10f;
                var targetPoint = Points[vertexIndex];

                // Ищем сегмент, который начинается на targetPoint
                for (int i = 0; i < _segments.Count; i++)
                {
                    var seg = _segments[i];
                    if (!seg.IsArc && Distance(seg.A, targetPoint) < tol)
                    {
                        segmentIndex = i;
                        return seg;
                    }
                }
                return null;
            }
            else
            {
                // Оригинальная логика для разомкнутой
                if (vertexIndex < 0 || vertexIndex >= Points.Count - 1) return null;

                float tol = 10f;
                var B = Points[vertexIndex];

                for (int i = 0; i < _segments.Count; i++)
                {
                    var seg = _segments[i];
                    if (!seg.IsArc && Distance(seg.A, B) < tol)
                    {
                        segmentIndex = i;
                        return seg;
                    }
                }
                return null;
            }
        }


        // Вспомогательный метод для расстояния (если ещё нет в классе)
        private static float Distance(SKPoint a, SKPoint b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        static float DistToSegment(SKPoint p, SKPoint a, SKPoint b)
        {
            var ab = new SKPoint(b.X - a.X, b.Y - a.Y);
            var ap = new SKPoint(p.X - a.X, p.Y - a.Y);
            float ab2 = ab.X * ab.X + ab.Y * ab.Y;
            if (ab2 < 1e-6f) return Distance(p, a);
            float t = Math.Max(0, Math.Min(1, (ap.X * ab.X + ap.Y * ab.Y) / ab2));
            var proj = new SKPoint(a.X + ab.X * t, a.Y + ab.Y * t);
            return Distance(p, proj);
        }
    }

    // ==================== Handle ====================

    class Handle
    {
        readonly Func<SKPoint> _getter;
        readonly Action<SKPoint> _setter;
        public SKPoint Position => _getter();
        public float X => Position.X;
        public float Y => Position.Y;
        public Handle(Func<SKPoint> g, Action<SKPoint> s) { _getter = g; _setter = s; }
        public void Move(SKPoint p) => _setter(p);
    }

    // ==================== Geometry Helper ====================

    public static class GeometryHelper
    {
        public static SKPoint Normalize(SKPoint v)
        {
            float len = MathF.Sqrt(v.X * v.X + v.Y * v.Y);
            return len < 1e-6f ? new SKPoint(0, 0) : new SKPoint(v.X / len, v.Y / len);
        }

        public static float Dot(SKPoint a, SKPoint b) => a.X * b.X + a.Y * b.Y;
        public static SKPoint Sub(SKPoint a, SKPoint b) => new(a.X - b.X, a.Y - b.Y);
        public static SKPoint Add(SKPoint a, SKPoint b) => new(a.X + b.X, a.Y + b.Y);
        public static SKPoint Mul(SKPoint v, float k) => new(v.X * k, v.Y * k);
        public static float Distance(SKPoint a, SKPoint b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static float Angle(SKPoint center, SKPoint p)
        {
            float dx = p.X - center.X, dy = p.Y - center.Y;
            return MathF.Atan2(dy, dx) * 180f / MathF.PI;
        }

        public static float Sweep(SKPoint c, SKPoint a, SKPoint b)
        {
            float a1 = Angle(c, a), a2 = Angle(c, b);
            float sweep = a2 - a1;
            return sweep < 0 ? sweep + 360 : sweep;
        }
        public static SKPoint ComputeFilletCenter(SKPoint A, SKPoint B, SKPoint C, float radius)
        {
            var v1 = Normalize(Sub(A, B));  // от B к A
            var v2 = Normalize(Sub(C, B));  // от B к C

            float dot = Dot(v1, v2);
            dot = Math.Clamp(dot, -1f, 1f);
            float angle = MathF.Acos(dot);

            if (angle < 0.01f || angle > MathF.PI - 0.01f)
                return default;

            var bisector = Normalize(Add(v1, v2));
            float h = radius / MathF.Sin(angle / 2f);

            return Add(B, Mul(bisector, h));
        }
        public static bool ComputeFillet(
    SKPoint A,  // точка на первом сегменте (до вершины)
    SKPoint B,  // вершина угла
    SKPoint C,  // точка на втором сегменте (после вершины)
    float radius,
    out SKPoint center,
    out SKPoint t1,
    out SKPoint t2)
        {
            center = default;
            t1 = default;
            t2 = default;

            // Векторы ОТ вершины B к соседним точкам (вдоль реальных сегментов)
            var v1 = Normalize(Sub(A, B));  // от B к A
            var v2 = Normalize(Sub(C, B));  // от B к C

            float dot = Dot(v1, v2);
            dot = Math.Clamp(dot, -1f, 1f);
            float angle = MathF.Acos(dot);

            // Слишком острый или прямой угол — филлет невозможен
            if (angle < 0.01f || angle > MathF.PI - 0.01f)
                return false;

            // Расстояние от вершины до точек касания вдоль сегментов
            float dist = radius / MathF.Tan(angle / 2f);

            // Точки касания на РЕАЛЬНЫХ частях сегментов (между A-B и B-C)
            t1 = Add(B, Mul(v1, dist));  // от B в сторону A
            t2 = Add(B, Mul(v2, dist));  // от B в сторону C

            // ✅ ВНУТРЕННЯЯ биссектриса (всегда в сторону "реальных" сегментов)
            // НЕ зависит от flip — центр всегда внутри угла!
            var bisector = Normalize(Add(v1, v2));

            // Расстояние от вершины до центра дуги вдоль биссектрисы
            float h = radius / MathF.Sin(angle / 2f);
            center = Add(B, Mul(bisector, h));

            return true;
        }

        // ✅ НОВЫЙ метод для вычисления sweep с учётом выбора дуги
        public static float ComputeSweep(SKPoint center, SKPoint t1, SKPoint t2, bool useLargeArc)
        {
            float startAngle = Angle(center, t1);
            float endAngle = Angle(center, t2);

            float sweep = endAngle - startAngle;

            // Нормализуем к диапазону -180..180 (короткая дуга)
            while (sweep > 180) sweep -= 360;
            while (sweep <= -180) sweep += 360;

            // Если запрошена длинная дуга (вогнутая) — инвертируем
            if (useLargeArc)
                sweep = (sweep > 0) ? sweep - 360 : sweep + 360;

            return sweep;
        }


        public static SKPoint ComputeArcCenter(SKPoint a, SKPoint b, float r, bool flip)
        {
            var mid = new SKPoint((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < 1e-6f) return mid;
            float h = MathF.Sqrt(MathF.Max(0, r * r - (d * d) / 4));
            var nx = -dy / d;
            var ny = dx / d;
            if (flip) { nx = -nx; ny = -ny; }
            return new SKPoint(mid.X + nx * h, mid.Y + ny * h);
        }

    }
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gridPaint?.Dispose();
            _shapePaint?.Dispose();
            _previewPaint?.Dispose();
            _selectPaint?.Dispose();
            _handlePaint?.Dispose();
            _textPaint?.Dispose();
        }
    }
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}