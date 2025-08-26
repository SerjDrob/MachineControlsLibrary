using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MachineControlsLibrary.Classes
{
    public class KeyProcessorCommands : ICommand
    {
        private Dictionary<(Key, ModifierKeys), (IAsyncRelayCommand command, bool isKeyRepeatProhibited)> DownKeys;
        private Dictionary<Key, IAsyncRelayCommand> UpKeys;
        private Func<object?, bool>? _canExecute;
        private readonly Type[] notProcessingControls;
        IAsyncRelayCommand<KeyEventArgs> _anyKeyDownCommand;
        IAsyncRelayCommand<KeyEventArgs> _anyKeyUpCommand;
        public KeyProcessorCommands(Func<object?, bool>? canExecute = null, params Type[] notProcessingControls)
        {
            _canExecute = canExecute;
            DownKeys = new();
            UpKeys = new();
            this.notProcessingControls = notProcessingControls;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public KeyProcessorCommands CreateKeyDownCommand(Key key, ModifierKeys modifier, Func<Task> task, Func<bool> canExecute, bool isKeyRepeatProhibited = true)
        {
            DownKeys ??= new();
            var command = new AsyncRelayCommand(task, canExecute);
            DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
            return this;
        } 
        public KeyProcessorCommands CreateKeyDownCommand(Key key, ModifierKeys modifier, IAsyncRelayCommand relayCommand, Func<bool> canExecute, bool isKeyRepeatProhibited = true)
        {
            DownKeys ??= new();
            var command = relayCommand;
            DownKeys[(key, modifier)] = (command, isKeyRepeatProhibited);
            return this;
        }
        public KeyProcessorCommands CreateKeyUpCommand(Key key, Func<Task> task, Func<bool> canExecute)
        {
            UpKeys ??= new();
            var command = new AsyncRelayCommand(task, canExecute);
            UpKeys[key] = command;
            return this;
        }

        public KeyProcessorCommands CreateAnyKeyDownCommand(Func<KeyEventArgs, Task> task, Func<bool> canExecute)
        {
            var predicate = new Predicate<KeyEventArgs>(kea => canExecute.Invoke());
            _anyKeyDownCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
            return this;
        }
        public KeyProcessorCommands CreateAnyKeyUpCommand(Func<KeyEventArgs, Task> task, Func<bool> canExecute)
        {
            var predicate = new Predicate<KeyEventArgs>(kea => canExecute.Invoke());
            _anyKeyUpCommand = new AsyncRelayCommand<KeyEventArgs>(task, predicate);
            return this;
        }


        public void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await ExecuteAsync(parameter);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        public async Task ExecuteAsync(object? parameter)
        {
            if (parameter is KeyEventArgs args)
            {
                Debug.WriteLine($"{args.Key} {args.RoutedEvent.Name}, repeat is {args.IsRepeat}");
                
                if (notProcessingControls.Any(t => t.IsEquivalentTo(args.OriginalSource.GetType().BaseType))) return;
                if (args.RoutedEvent == Keyboard.KeyDownEvent || args.RoutedEvent == Keyboard.PreviewKeyDownEvent)
                {
                    var clue = (args.Key, args.KeyboardDevice.Modifiers);
                    if (DownKeys.TryGetValue(clue, out var commandPair))
                    {
                        if (!(args.IsRepeat & commandPair.isKeyRepeatProhibited))
                        {
                            args.Handled = true;
                            await commandPair.command.ExecuteAsync(null);
                        }
                    }
                    else if (_anyKeyDownCommand != null)
                    {
                        args.Handled = true;
                        await _anyKeyDownCommand.ExecuteAsync(args);
                    }
                }
                else if (args.RoutedEvent == Keyboard.KeyUpEvent || args.RoutedEvent == Keyboard.PreviewKeyUpEvent)
                {
                    if (!(UpKeys.ContainsKey(args.Key)) && _anyKeyUpCommand is not null)
                    {
                        args.Handled = true;
                        await _anyKeyUpCommand.ExecuteAsync(args);
                        return;
                    }
                    if (UpKeys.TryGetValue(args.Key, out var command))
                    {
                        args.Handled = true;
                        await command.ExecuteAsync(null);
                    }
                }
            }
        }
    }
}

