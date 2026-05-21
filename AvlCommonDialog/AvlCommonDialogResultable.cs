using CommunityToolkit.Mvvm.ComponentModel;
using MachineControlsLibrary.CommonDialog;
using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace MachineControlsLibrary.AvlCommonDialog;



public interface IDialogResultable<T>
{
    T Result { get; set; }

    Action CloseAction { get; set; }
}
/// <summary>
/// Inherits CommunityToolkit.Mvvm.ComponentModel.ObservableValidator
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AvlCommonDialogResultable<T> : ObservableValidator, ICommonDialog, IDialogResultable<CommonDialogResult<T>>
{
    [Browsable(false)]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CommonDialogResult<T> Result { get; set; }
    
    [Browsable(false)]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Action CloseAction { get; set; }
    public void CloseWithSuccess()
    {
        Result = new CommonDialogResult<T> { Success = true };
        SetResult();
        CloseAction?.Invoke();
    }
    public void CloseWithCancel()
    {
        Result = new CommonDialogResult<T> { Success = false };
        SetResult();
        CloseAction?.Invoke();
    }
    public abstract void SetResult();
    protected void SetResult(T result)
    {
        Result.CommonResult = result;
    }
}