﻿using System;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Controls;
using MachineControlsLibrary.CommonDialog;

namespace HandyControl.Tools.Extension
{
    public static class DialogExtensions
    {
        public static Dialog SetDialogTitle(this Dialog dialog, string title)
        {
            if (dialog.Content is IHasTitle commonDialog) commonDialog.SetTitle(title);
            return dialog;
        }

        public static Dialog SetDataContext<TContext>(this Dialog dialog, Action<TContext> action) where TContext : new()
        {
            var instance = new TContext();
            if (instance is ICommonDialog commonDialog)
            {
                dialog.CommandBindings.Add(new System.Windows.Input.CommandBinding(HandyControl.Interactivity.ControlCommands.Confirm, (e, a) =>
                {
                    commonDialog.CloseWithSuccess();
                }));
                dialog.CommandBindings.Add(new System.Windows.Input.CommandBinding(HandyControl.Interactivity.ControlCommands.Cancel, (e, a) =>
                {
                    commonDialog.CloseWithCancel();
                }));
            }
            dialog.DataContext = instance;
            dialog.Initialize(action);
            return dialog;
        }

        public static Dialog SetDataContext<TContext>(this Dialog dialog, TContext instance, Action<TContext> action)
        {
            if (instance is ICommonDialog commonDialog)
            {
                try
                {
                    dialog.CommandBindings.Add(new System.Windows.Input.CommandBinding(HandyControl.Interactivity.ControlCommands.Confirm, (e, a) =>
                            {
                                commonDialog.CloseWithSuccess();
                            }));
                    dialog.CommandBindings.Add(new System.Windows.Input.CommandBinding(HandyControl.Interactivity.ControlCommands.Cancel, (e, a) =>
                    {
                        commonDialog.CloseWithCancel();
                    }));
                }
                catch (Exception)
                {
                    throw;
                }
            }
            dialog.DataContext = instance;
            dialog.Initialize(action);
            return dialog;
        }

        public static Task<CommonDialogResult<TResult>> GetCommonResultAsync<TResult>(this Dialog dialog, Action? blockAction = null)
        {
            TaskCompletionSource<CommonDialogResult<TResult>> tcs = new TaskCompletionSource<CommonDialogResult<TResult>>();
            try
            {
                if (dialog.IsClosed)
                {
                    SetResult();
                }
                else
                {
                    blockAction?.Invoke();
                    dialog.Unloaded += OnUnloaded;
                    dialog.GetViewModel<IDialogResultable<CommonDialogResult<TResult>>>().CloseAction = dialog.Close;
                }
            }
            catch (Exception exception)
            {
                tcs.TrySetException(exception);
            }

            return tcs.Task;
            void OnUnloaded(object sender, RoutedEventArgs args)
            {
                dialog.Unloaded -= OnUnloaded;
                blockAction?.Invoke();
                SetResult();
            }

            void SetResult()
            {
                try
                {
                    tcs.TrySetResult(dialog.GetViewModel<IDialogResultable<CommonDialogResult<TResult>>>().Result);
                }
                catch (Exception exception2)
                {
                    tcs.TrySetException(exception2);
                }
            }
        }

    }
}
