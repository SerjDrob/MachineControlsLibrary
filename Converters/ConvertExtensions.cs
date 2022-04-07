namespace MachineControlsLibrary.Converters
{
    public static class ConvertExtensions
    {
        public static T ConvertObject<T>(this object obj, T defaultValue)
        {
            return obj is T ? (T)obj : defaultValue;
        }
    }
}
