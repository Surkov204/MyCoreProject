namespace Com.GameDev.Module.UISystem
{
    public class UIToastData : ViewData
    {
        public string Message { get; }
        public ToastType ToastType { get; }

        public UIToastData(string message, ToastType toastType = ToastType.Gray)
        {
            Message = message;
            ToastType = toastType;
        }
    }
}