using Cysharp.Threading.Tasks;

namespace Stratton.Core
{
    public enum InitializationStatus
    {
        Success,
        Cancelled,
        Paused,
        Failed
    }

    public enum DeinitializationStatus
    {
        Success,
        Failed
    }

    public enum InitializationErrorCode
    {
        Unknown,
        Logic,
        Timeout
    }

    /// <summary>
    /// Handles initialization stuff.
    /// </summary>
    public interface IInitializableAsync
    {
        #region Properties

        bool IsReady { get; }

        #endregion

        #region Public Methods

        UniTask<InitializationResult> Init();
        UniTask<DeinitializationResult> DeInit();

        #endregion
    }

    public class InitializationResult : CommonResult
    {
        public new static InitializationResult Success => new InitializationResult(InitializationStatus.Success);
        public new static InitializationResult Cancelled => new InitializationResult(InitializationStatus.Cancelled);

        public static InitializationResult Paused => new InitializationResult(InitializationStatus.Paused);

        public InitializationStatus Status { get; protected set; }
        public InitializationErrorCode ErrorCode { get; protected set; }

        public new bool IsSuccess => Status == InitializationStatus.Success;
        public new bool IsCancelled => Status == InitializationStatus.Cancelled;
        public bool IsPaused => Status == InitializationStatus.Paused;
        public bool IsFailed => Status == InitializationStatus.Failed;

        public InitializationResult(InitializationErrorCode errorCode, string errorMessage) : base(errorMessage)
        {
            Status = InitializationStatus.Failed;
            ErrorCode = errorCode;
        }

        public InitializationResult(InitializationStatus status)
        {
            Status = status;
        }

        public static InitializationResult Error(InitializationErrorCode errorCode, string errorMsg)
        {
            Log.Error(BaseLogChannel.Core, $"Code={errorCode}, {errorMsg}");
            return new InitializationResult(errorCode, errorMsg);
        }
    }

    public class DeinitializationResult : CommonResult
    {
        public DeinitializationStatus Status { get; protected set; }

        public DeinitializationResult(DeinitializationStatus status)
        {
            Status = status;
        }

        public new static DeinitializationResult Success => new DeinitializationResult(DeinitializationStatus.Success);

        public DeinitializationResult(string errorMessage) : base(errorMessage)
        {
            Status = DeinitializationStatus.Failed;
        }

        public new static DeinitializationResult Error(string errorMsg)
        {
            Log.Error(BaseLogChannel.Core, errorMsg);
            return new DeinitializationResult(errorMsg);
        }
    }
}