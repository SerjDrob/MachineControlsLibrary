namespace MachineControlsLibrary.CommonDialog
{
    public class CommonDialogResult<T>
    {
        public bool Success
        {
            get;
            set;
        }
        public T CommonResult
        {
            get;
            set;
        }
    }


}
