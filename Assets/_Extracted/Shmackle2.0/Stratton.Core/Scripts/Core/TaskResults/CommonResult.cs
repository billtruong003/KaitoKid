namespace Stratton.Core
{
    public class CommonResult
    {
        public static CommonResult Success => new CommonResult();
        public static CommonResult Cancelled => new CommonResult() { IsCancelled = true };

        public CommonResult() { }

        public CommonResult(string errorMessage)
        {
            IsError = true;
            ErrorMessage = errorMessage;
        }

        public static CommonResult Error(string errorMsg)
        {
            Log.Error(BaseLogChannel.Core, errorMsg);
            return new CommonResult(errorMsg);
        }

        public string ErrorMessage { get; protected set; }
        public bool IsSuccess => !IsError;
        public bool IsError { get; protected set; }
        public bool IsCancelled { get; protected set; }
    }
}