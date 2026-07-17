namespace Stratton.Core
{
    public class LoadContentResult : CommonResult
    {
        public DataLoadErrorCode ErrorCode { get; protected set; }

        public new static LoadContentResult Success => new LoadContentResult();
        public new static LoadContentResult Cancelled => new LoadContentResult() { IsCancelled = true };
        public LoadContentResult() { }

        public LoadContentResult(DataLoadErrorCode errorCode, string errorMessage) : base(errorMessage)
        {
            ErrorCode = errorCode;
        }

        public static LoadContentResult Error(DataLoadErrorCode errorCode, string errorMsg)
        {
            Log.Error(BaseLogChannel.Core, $"Code={errorCode}, {errorMsg}");
            return new LoadContentResult(errorCode, errorMsg);
        }
    }
}