using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using HandyControl.Tools.Extension;

namespace MachineControlsLibrary.CommonDialog
{
    public abstract class CommonDialogResultable<T> : ICommonDialog, IDialogResultable<CommonDialogResult<T>>
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


}
