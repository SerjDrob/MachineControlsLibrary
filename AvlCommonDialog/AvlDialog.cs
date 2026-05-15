using MachineControlsLibrary.CommonDialog;
using System;
using System.Windows;
using System.Windows.Input;

namespace MachineControlsLibrary.AvlCommonDialog;

public class AvlDialog
{
    private object? _context;
    private Action? _closeAction;
    private Action? _confirmAction;
    private Action? _initializeAction;
    private string _title = string.Empty;

    public AvlDialog SetDataContext<TContext>(Action<TContext> action) where TContext : ICommonDialog, new() 
    {
        _context = new TContext();
        var commonDialog = (ICommonDialog)_context;

        _confirmAction += commonDialog.CloseWithSuccess;
        _closeAction += commonDialog.CloseWithCancel;
        _initializeAction += () => action.Invoke((TContext)_context);

        return this;
    }
    public AvlDialog SetDataContext<TContext>(TContext instance, Action<TContext> action)
    {
        _context = instance;
        var commonDialog = (ICommonDialog)instance!;

        _confirmAction += commonDialog.CloseWithSuccess;
        _closeAction += commonDialog.CloseWithCancel;
        _initializeAction += () => action.Invoke(instance);

        return this;
    }
    public AvlDialog SetDialogTitle(string title)
    {
       _title = title;
        return this;
    }
    public CommonDialogResult<TResult> Show<TResult>(Window? owner = null)
    {
        if (_context is not AvlCommonDialogResultable<TResult> context)
            throw new ArgumentException($"Context must be AvlCommonDialogResultable<{typeof(TResult).Name}>");

        var _dialog = new AvlCommonDialog
        {
            DataContext = context,
            Owner = owner ?? Application.Current?.MainWindow            
        };

        _dialog.SetTitle(_title);

        context.CloseAction = () => _dialog.Close();

        _dialog.CommandBindings.Add(new CommandBinding(AvlCommands.Confirm, (s, e) =>
        {
            _confirmAction?.Invoke();
            _dialog.Close();
        }));

        _dialog.CommandBindings.Add(new CommandBinding(AvlCommands.Close, (s, e) =>
        {
            _closeAction?.Invoke();
            _dialog.Close();
        }));

        _dialog.Closing += (s, e) =>
        {
            if (context.Result == null)
                context.CloseWithCancel();
        };

        _initializeAction?.Invoke();
        _dialog.ShowDialog();

        return context.Result ?? new CommonDialogResult<TResult> { Success = false };
    }
}

public static class AvlCommands
{
    public static RoutedCommand Confirm { get; } = new RoutedCommand("Confirm", typeof(AvlCommands));
    public static RoutedCommand Close { get; } = new RoutedCommand("Close", typeof(AvlCommands));
}