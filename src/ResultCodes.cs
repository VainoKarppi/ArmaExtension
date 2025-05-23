namespace ArmaExtension;

public static partial class Extension
{
    public enum ReturnCodes
    {
        Success = 0,
        Error = 1,
        InvalidMethod = 2,
        InvalidParameters = 3
    }
    public static class ResultMessages
    {
        public const string SUCCESS = "SUCCESS";
        public const string SUCCESS_VOID = "SUCCESS_VOID";
        public const string ERROR = "ERROR";
        public const string ASYNC_RESPONSE = "ASYNC_RESPONSE";
        public const string ASYNC_SENT = "ASYNC_SENT";
        public const string ASYNC_SENT_VOID = "ASYNC_SENT_VOID";
        public const string ASYNC_FAILED = "ASYNC_FAILED";
        public const string ASYNC_CANCEL_SUCCESS = "ASYNC_CANCEL_SUCCESS";
        public const string ASYNC_CANCEL = "ASYNC_CANCEL";
        public const string ASYNC_CANCEL_FAILED = "ASYNC_CANCEL_FAILED";
        public const string ASYNC_SUCCESS = "ASYNC_SUCCESS";
        public const string CALLFUNCTION = "CALLFUNCTION";
    }


}