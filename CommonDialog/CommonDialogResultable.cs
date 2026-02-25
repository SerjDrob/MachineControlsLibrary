using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Tools.Extension;

namespace MachineControlsLibrary.CommonDialog;
/// <summary>
/// Implements ObservableValidator so implements INotifyPropertyCanged(-ing) as well
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class CommonDialogResultable<T> : ObservableValidator, ICommonDialog, IDialogResultable<CommonDialogResult<T>>
{
    [Browsable(false)]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CommonDialogResult<T> Result
    {
        get;
        set;
    }
    [Browsable(false)]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public Action CloseAction
    {
        get;
        set;
    }
    public void CloseWithSuccess()
    {
        Result = new CommonDialogResult<T> { Success = true };
        SetResult();
        CloseAction();
    }
    public void CloseWithCancel()
    {
        Result = new CommonDialogResult<T> { Success = false };
        SetResult();
        CloseAction();
    }
    public abstract void SetResult();
    protected void SetResult(T result)
    {
        Result.CommonResult = result;
    }
}
