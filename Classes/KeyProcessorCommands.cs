using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Stateless;

namespace MachineControlsLibrary.Classes;

public enum KeyCommandExecutionMode
{
    SingleExecution,
    ConcurrentExecution
}
public static class KeyHelper
{
    [DllImport("user32.dll")]
    private static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    public static string KeyToChar(Key key)
    {
        int virtualKey = KeyInterop.VirtualKeyFromKey(key);
        byte[] keyboardState = new byte[256];
        GetKeyboardState(keyboardState);

        uint scanCode = MapVirtualKey((uint)virtualKey, 0);

        StringBuilder buffer = new StringBuilder(2);
        int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, buffer, buffer.Capacity, 0);

        if (result > 0)
            return buffer.ToString().ToUpper();

        return key.ToString();
    }
}
public class KeyProcessorCommands : ICommand
{
    private readonly Dictionary<(Key, ModifierKeys), (IAsyncRelayCommand command, bool isKeyRepeatProhibited)> DownKeys;
    private readonly Dictionary<Key, IAsyncRelayCommand> UpKeys;
    private readonly Dictionary<(Key key,ModifierKeys modifier), string> KeysDiscription;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Type[] notProcessingControls;
    IAsyncRelayCommand<KeyEventArgs>? _anyKeyDownCommand;
    IAsyncRelayCommand<KeyEventArgs>? _anyKeyUpCommand;
    public KeyProcessorCommands(Func<object?, bool>? canExecute = null, params Type[] notProcessingControls)
    {
        _canExecute = canExecute;
        DownKeys = new();
        UpKeys = new();
        KeysDiscription = new();
        this.notProcessingControls = notProcessingControls;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public KeyProcessorCommands CreateKeyDownCommand(Key key, ModifierKeys modifier, Func<Task> task,
        Func<bool> canExecute, bool isKeyRepeatProhibited = true,
        KeyCommandExecutionMode executionMode = KeyCommandExecutionMode.SingleExecution, string note = "")
    {
        var options = executionMode == KeyCommandExecutionMode.ConcurrentExecution
                                                       ? AsyncRelayCommandOptions.AllowConcurrentExecutions
                                                       : AsyncRelayCommandOptions.None;
        var command = new AsyncRelayCommand(task, canExecute, options);
        DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
        if(note!="") KeysDiscription[(key,modifier)] = note;
        return this;
    }
    public KeyProcessorCommands CreateKeyDownCommand(Key key, ModifierKeys modifier, IAsyncRelayCommand relayCommand, bool isKeyRepeatProhibited = true, string note = "")
    {
        var command = relayCommand;
        DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
        if (note != "") KeysDiscription[(key,modifier)] = note;
        return this;
    }
    public KeyProcessorCommands CreateKeyUpCommand(Key key, Func<Task> task, Func<bool> canExecute)
    {
        var command = new AsyncRelayCommand(task, canExecute);
        UpKeys[key] = command;
        return this;
    }

    public KeyProcessorCommands CreateAnyKeyDownCommand(Func<KeyEventArgs?, Task> task, Func<bool> canExecute)
    {
        Predicate<KeyEventArgs?> predicate = key => canExecute.Invoke();
        _anyKeyDownCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
        return this;
    }
    public KeyProcessorCommands CreateAnyKeyUpCommand(Func<KeyEventArgs?, Task> task, Func<bool> canExecute)
    {
        Predicate<KeyEventArgs?> predicate = key => canExecute.Invoke();
        _anyKeyUpCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
        return this;
    }


    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (parameter is KeyEventArgs args && !args.Handled)
        {
            if (args.OriginalSource is not null && notProcessingControls.Any(t => t.IsInstanceOfType(args.OriginalSource))) return;

            if (args.RoutedEvent == Keyboard.KeyDownEvent || args.RoutedEvent == Keyboard.PreviewKeyDownEvent)
            {
                var key = args.Key == Key.System ? args.SystemKey : args.Key;
                var clue = (key, args.KeyboardDevice.Modifiers);
                if (DownKeys.TryGetValue(clue, out var commandPair))
                {
                    if (!(args.IsRepeat && commandPair.isKeyRepeatProhibited))
                    {
                        var canExec = commandPair.command.CanExecute(null);
                        if (canExec)
                        {
                            args.Handled = true;
                            await commandPair.command.ExecuteAsync(null);
                        }
                    }
                }
                else if (_anyKeyDownCommand != null && _anyKeyDownCommand.CanExecute(args))
                {
                    args.Handled = true;
                    await _anyKeyDownCommand.ExecuteAsync(args);
                }
            }
            else if (args.RoutedEvent == Keyboard.KeyUpEvent || args.RoutedEvent == Keyboard.PreviewKeyUpEvent)
            {
                if (UpKeys.TryGetValue(args.Key, out var command))
                {
                    args.Handled = true;
                    await command.ExecuteAsync(null);
                }
                else if (_anyKeyUpCommand is not null && _anyKeyUpCommand.CanExecute(args))
                {
                    args.Handled = true;
                    await _anyKeyUpCommand.ExecuteAsync(args);
                }
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var item in KeysDiscription)
        {
            // 1. Модификаторы в верхний регистр
            var mod = item.Key.modifier != ModifierKeys.None
                ? item.Key.modifier.ToString().ToUpperInvariant()
                : "";

            if (!string.IsNullOrEmpty(mod))
            {
                sb.Append(mod).Append(" + ");
            }

            // 2. Получаем красивое латинское имя клавиши
            var keyName = GetFriendlyKeyName(item.Key.key);

            sb.Append(keyName)
              .Append(" - ")
              .Append(item.Value)
              .AppendLine();
        }
        return sb.ToString();
    }

    // Метод-маппер для перевода системных имен WPF в латинские символы
    private static string GetFriendlyKeyName(Key key)
    {
        return key switch
        {
            // Буквы на Numpad
            Key.NumPad0 => "NUMPAD 0",
            Key.NumPad1 => "NUMPAD 1",
            Key.NumPad2 => "NUMPAD 2",
            Key.NumPad3 => "NUMPAD 3",
            Key.NumPad4 => "NUMPAD 4",
            Key.NumPad5 => "NUMPAD 5",
            Key.NumPad6 => "NUMPAD 6",
            Key.NumPad7 => "NUMPAD 7",
            Key.NumPad8 => "NUMPAD 8",
            Key.NumPad9 => "NUMPAD 9",
            Key.Multiply => "*",
            Key.Add => "+",
            Key.Subtract => "-",
            Key.Decimal => ".",
            Key.Divide => "/",

            // Основные спецсимволы (Латиница)
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemQuestion => "/",
            Key.OemQuotes => "\"",
            Key.OemSemicolon => ";",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe => "\\",
            Key.OemTilde => "`",

            // Обычные цифры над буквами (убираем префикс "D")
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",

            // Для всех остальных (A-Z, F1-F12, Space, Enter, Escape и т.д.)
            _ => key.ToString().ToUpperInvariant()
        };
    }

}
public class KeyProcessorStateCommands<TState> : ICommand where TState : Enum
{
    private readonly Dictionary<(Key, ModifierKeys), (IAsyncRelayCommand command, bool isKeyRepeatProhibited)> DownKeys;
    private readonly Dictionary<Key, IAsyncRelayCommand> UpKeys;
    private readonly Dictionary<(Key key,ModifierKeys modifier), string> KeysDiscription;
    private readonly Func<object?, bool>? _canExecute;
    private readonly Type[] notProcessingControls;
    IAsyncRelayCommand<KeyEventArgs>? _anyKeyDownCommand;
    IAsyncRelayCommand<KeyEventArgs>? _anyKeyUpCommand;
    private TState _state;

    public KeyProcessorStateCommands(Func<object?, bool>? canExecute = null, params Type[] notProcessingControls)
    {
        _canExecute = canExecute;
        DownKeys = new();
        UpKeys = new();
        KeysDiscription = new();
        this.notProcessingControls = notProcessingControls;
    }

    public KeyProcessorStateCommands<TState> Subscribe<TTrigger>(StateMachine<TState,TTrigger> stateMachine)
    {
        stateMachine.OnTransitioned(t => ChangeCurrentState(t.Destination));
        return this;
    }

    private void ChangeCurrentState(TState state)=>_state = state;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke(parameter) ?? true;
    }

    public KeyProcessorStateCommands<TState> CreateKeyDownCommand(Key key,
                                                                  ModifierKeys modifier,
                                                                  Func<Task> task,
                                                                  IList<TState>? allowingStates = null,
                                                                  IList<TState>? forbidingStates = null,
                                                                  Func<bool>? canExecFunc = null,
                                                                  bool isKeyRepeatProhibited = true,
                                                                  KeyCommandExecutionMode executionMode = KeyCommandExecutionMode.SingleExecution,
                                                                  string note = "")
    {
        var options = executionMode == KeyCommandExecutionMode.ConcurrentExecution
                                                       ? AsyncRelayCommandOptions.AllowConcurrentExecutions
                                                       : AsyncRelayCommandOptions.None;
        var canExecute = () => { return GetPredicate(allowingStates, forbidingStates).Invoke() && (canExecFunc?.Invoke() ?? true); };
        var command = new AsyncRelayCommand(task, canExecute, options);
        DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
        if(note!="") KeysDiscription[(key,modifier)] = note;
        return this;
    }
    public KeyProcessorStateCommands<TState> CreateKeyDownCommand(Key key, ModifierKeys modifier, IAsyncRelayCommand relayCommand, bool isKeyRepeatProhibited = true, string note = "")
    {
        var command = relayCommand;
        DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
        if (note != "") KeysDiscription[(key,modifier)] = note;
        return this;
    }
    public KeyProcessorStateCommands<TState> CreateKeyUpCommand(Key key, Func<Task> task, IList<TState>? allowingStates = null, IList<TState>? forbidingStates = null)
    {
        var canExecute = GetPredicate(allowingStates, forbidingStates);
        var command = new AsyncRelayCommand(task, canExecute);
        UpKeys[key] = command;
        return this;
    }  


    public KeyProcessorStateCommands<TState> CreateAnyKeyDownCommand(Func<KeyEventArgs?, Task> task, IList<TState>? allowingStates = null, IList<TState>? forbidingStates = null)
    {
        Predicate<KeyEventArgs?> predicate = key => GetPredicate(allowingStates,forbidingStates).Invoke();
        _anyKeyDownCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
        return this;
    }
    public KeyProcessorStateCommands<TState> CreateAnyKeyUpCommand(Func<KeyEventArgs?, Task> task, IList<TState>? allowingStates = null, IList<TState>? forbidingStates = null)
    {
        Predicate<KeyEventArgs?> predicate = key => GetPredicate(allowingStates, forbidingStates).Invoke();
        _anyKeyUpCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
        return this;
    }

    private Func<bool> GetPredicate(IList<TState>? allowingStates, IList<TState>? forbidingStates)
    {
        return () =>
        {
            return (allowingStates is null || allowingStates.Any(s => s.HasFlag(_state))) && (forbidingStates is null || !forbidingStates.Any(s => s.HasFlag(_state)));
        };
    }
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        //_ = Application.Current.Dispatcher.InvokeAsync(async () =>
        //{
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        //}, System.Windows.Threading.DispatcherPriority.Background);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (parameter is KeyEventArgs args)
        {
            if (args.Handled) return;
            //  Debug.WriteLine($"{args.Key} {args.RoutedEvent.Name}, repeat is {args.IsRepeat}");

            if (args.OriginalSource is not null && notProcessingControls.Any(t => t.IsAssignableFrom(args.OriginalSource.GetType()))) return;

            if (args.RoutedEvent == Keyboard.KeyDownEvent || args.RoutedEvent == Keyboard.PreviewKeyDownEvent)
            {
                var key = args.Key == Key.System ? args.SystemKey : args.Key;
                var clue = (key, args.KeyboardDevice.Modifiers);
                if (DownKeys.TryGetValue(clue, out var commandPair))
                {
                    if (!(args.IsRepeat && commandPair.isKeyRepeatProhibited))
                    {
                        var canExec = commandPair.command.CanExecute(null);
                        if (canExec)
                        {
                            args.Handled = true;
                            await commandPair.command.ExecuteAsync(null);
                        }
                    }
                }
                else if (_anyKeyDownCommand != null)
                {
                    if (_anyKeyDownCommand.CanExecute(args))
                    {
                        args.Handled = true;
                        await _anyKeyDownCommand.ExecuteAsync(args);
                    }
                }
            }
            else if (args.RoutedEvent == Keyboard.KeyUpEvent || args.RoutedEvent == Keyboard.PreviewKeyUpEvent)
            {
                if (UpKeys.TryGetValue(args.Key, out var command))
                {
                    args.Handled = true;
                    await command.ExecuteAsync(null);
                }
                else if (_anyKeyUpCommand is not null && _anyKeyUpCommand.CanExecute(args))
                {
                    args.Handled = true;
                    await _anyKeyUpCommand.ExecuteAsync(args);
                }
            }
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var item in KeysDiscription)
        {
            var mod = item.Key.modifier != ModifierKeys.None ? item.Key.modifier.ToString() : "";
            sb.Append(mod);
            if (mod != "") sb.Append(" + ");
            sb.Append(KeyHelper.KeyToChar(item.Key.key));
            sb.Append(" - ");
            sb.Append(item.Value);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}



