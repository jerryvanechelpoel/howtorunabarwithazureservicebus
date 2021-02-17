namespace Azug.ServiceBar.Waiters
{
    public sealed class WaiteringActivityEventArgs
    {
        public string Message { get;  }

        public WaiteringActivityEventArgs(string message)
        {
            Message = message;
        }
    }
}