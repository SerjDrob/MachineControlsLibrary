using MachineControlsLibrary.Classes.SkEditor;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MachineControlsLibrary.Controls.GraphWin.SKGraphics;


/// <summary>
/// Interaction logic for SKSimpleEditor.xaml
/// </summary>
public partial class SKSimpleEditor : UserControl
{
    // Вместо события — команда
    public static readonly DependencyProperty ShapesReadyCommandProperty =
        DependencyProperty.Register(
            "ShapesReadyCommand",
            typeof(ICommand),
            typeof(SKSimpleEditor));

    public ICommand ShapesReadyCommand
    {
        get => (ICommand)GetValue(ShapesReadyCommandProperty);
        set => SetValue(ShapesReadyCommandProperty, value);
    }

    public static readonly DependencyProperty ShapesReadyCommandParameterProperty =
        DependencyProperty.Register(
            "ShapesReadyCommandParameter",
            typeof(object),
            typeof(SKSimpleEditor));

    public object ShapesReadyCommandParameter
    {
        get => GetValue(ShapesReadyCommandParameterProperty);
        set => SetValue(ShapesReadyCommandParameterProperty, value);
    }
     


    public SKSimpleEditor()
    {
        InitializeComponent();
        editor.CurrentTool = SimpleSkEditor.Tool.Select;
        TurnOffButton(SnapTool);
        editor.OnMouseRightClicked += () =>
        {
            SwitchButton(exceptedButton: SnapTool);
            editor.CurrentTool = SimpleSkEditor.Tool.Select;
        };
        AddToLayerTool.Click += (s, e) =>
        {
            OnShapesReady(editor.GetShapes());
            editor.DeleteAllShapes();
        };
        WipeTool.Click += (s, e) =>
        {
            SwitchButton(exceptedButton: SnapTool);
            editor.CurrentTool = SimpleSkEditor.Tool.Select;
            editor.DeleteAllShapes();
        };
        SnapTool.Click += (s, e) =>
        {
            if (editor.SnapEnabled)
            {
                TurnOffButton(SnapTool);
                editor.SnapEnabled = false;
            }
            else
            {
                TurnOnButton(SnapTool);
                editor.SnapEnabled = true;
            }
            editor.InvalidateVisual();
        };
        CircleTool.Click += (s, e) =>
        {
            if (editor.CurrentTool is SimpleSkEditor.Tool.Circle)
            {
                TurnOffButton(CircleTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Select;
            }
            else
            {
                SwitchButton(CircleTool, SnapTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Circle;
            }

        };
        LineTool.Click += (s, e) =>
        {
            if (editor.CurrentTool is SimpleSkEditor.Tool.Line)
            {
                TurnOffButton(LineTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Select;
            }
            else
            {
                SwitchButton(LineTool, SnapTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Line;
            }

        };
        CurveTool.Click += (s, e) =>
        {
            if (editor.CurrentTool is SimpleSkEditor.Tool.Polyline)
            {
                TurnOffButton(CurveTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Select;
            }
            else
            {
                SwitchButton(CurveTool, SnapTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Polyline;
            }

        };
        SplineTool.Click += (s, e) =>
        {
            if (editor.CurrentTool is SimpleSkEditor.Tool.Fillet)
            {
                TurnOffButton(SplineTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Select;
            }
            else
            {
                SwitchButton(SplineTool, SnapTool);
                editor.CurrentTool = SimpleSkEditor.Tool.Fillet;
            }

        };
    }

    private void OnShapesReady((IList<Shape> shapes, float factor) shapes)
    {
        ShapesReadyCommandParameter = shapes;
        if (ShapesReadyCommand?.CanExecute(shapes) == true)
            ShapesReadyCommand.Execute(shapes);
    }
    private void SwitchButton(Button? turnOnButton = null, Button? exceptedButton = null)
    {
        if (turnOnButton is not null) TurnOnButton(turnOnButton);
        foreach (var tool in Tools.Items)
        {
            if (tool is Button button1 && button1 != turnOnButton && button1 != exceptedButton) TurnOffButton(button1);
        }
    }

    private void TurnOnButton(Button button)
    {
        button.Foreground = Brushes.Black;
        button.Background = Brushes.White;
    }
    private void TurnOffButton(Button button)
    {
        button.Foreground = Brushes.White;
        button.Background = Brushes.Black;
    }
}
