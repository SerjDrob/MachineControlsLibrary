﻿using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MachineControlsLibrary.Classes
{
    public class KeyProcessorCommands : ICommand
    {
        private Dictionary<Key, (AsyncRelayCommand command, bool isKeyRepeatProhibited)> DownKeys;
        private Dictionary<Key, AsyncRelayCommand> UpKeys;
        private readonly Func<object?, bool>? _canExecute;
        AsyncRelayCommand<KeyEventArgs> _anyKeyDownCommand;
        AsyncRelayCommand<KeyEventArgs> _anyKeyUpCommand;
        public KeyProcessorCommands(Func<object?, bool>? canExecute = null)
        {
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public KeyProcessorCommands CreateKeyDownCommand(Key key, Func<Task> task, Func<bool> canExecute, bool isKeyRepeatProhibited = true)
        {
            DownKeys ??= new();
            var command = new AsyncRelayCommand(task, canExecute);
            DownKeys[key] = (command, isKeyRepeatProhibited);
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

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            if (parameter is KeyProcessorArgs args)
            {
                if (args.IsKeyDown)
                {
                    if (!(DownKeys?.Keys.Any(key => key == args.KeyEventArgs.Key) ?? false) & _anyKeyDownCommand is not null)
                    {
                        await _anyKeyDownCommand.ExecuteAsync(args.KeyEventArgs);
                        return;
                    }
                    DownKeys.TryGetValue(args.KeyEventArgs.Key, out var commandPair);

                    if (commandPair != default && !(args.KeyEventArgs.IsRepeat & commandPair.isKeyRepeatProhibited))
                        await commandPair.command.ExecuteAsync(null);
                }
                else
                {
                    if (!(UpKeys?.Keys.Any(key => key == args.KeyEventArgs.Key) ?? false) & _anyKeyUpCommand is not null)
                    {
                        await _anyKeyUpCommand.ExecuteAsync(args.KeyEventArgs);
                        return;
                    }
                    UpKeys.TryGetValue(args.KeyEventArgs.Key, out var command);
                    if (command is not null) await command.ExecuteAsync(null);
                }
                args.KeyEventArgs.Handled = true;
            }
        }
    }
}

