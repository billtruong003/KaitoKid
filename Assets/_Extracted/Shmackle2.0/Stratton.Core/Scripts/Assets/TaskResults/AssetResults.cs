using Stratton.Core;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Stratton.Assets
{
    public class DownloadAssetsResult : LoadContentResult
    {
        public new static DownloadAssetsResult Success => new DownloadAssetsResult();
        public new static DownloadAssetsResult Cancelled => new DownloadAssetsResult() { IsCancelled = true };

        public DownloadAssetsResult() : base() { }

        public DownloadAssetsResult(DataLoadErrorCode errorCode, string errorMessage) : base(errorCode, errorMessage) { }

        public new static DownloadAssetsResult Error(DataLoadErrorCode errorCode, string errorMsg)
        {
            Log.Error(BaseLogChannel.Assets, $"Code={errorCode}, {errorMsg}");
            return new DownloadAssetsResult(errorCode, errorMsg);
        }
    }

    public class AssetResult<T> : CommonResult where T : UnityEngine.Object
    {
        public T Asset { get; set; }
        public T AssetInstance { get; set; }
        public AsyncOperationHandle OperationHandle { get; set; }

        public new static AssetResult<T> Success => new AssetResult<T>();
        public new static AssetResult<T> Cancelled => new AssetResult<T>() { IsCancelled = true };
        public AssetResult() : base() { }

        public AssetResult(string errorMessage) : base(errorMessage) { }

        public new static AssetResult<T> Error(string errorMsg)
        {
            Log.Error(BaseLogChannel.Assets, errorMsg);
            return new AssetResult<T>(errorMsg);
        }
    }

    public class SceneResult : CommonResult
    {
        public SceneInstance SceneInstance { get; set; }
        public AsyncOperationHandle OperationHandle { get; set; }

        public new static SceneResult Success => new SceneResult();
        public new static SceneResult Cancelled => new SceneResult() { IsCancelled = true };

        public SceneResult() : base() { }

        public SceneResult(string errorMessage) : base(errorMessage) { }

        public new static SceneResult Error(string errorMsg)
        {
            Log.Error(BaseLogChannel.Assets, errorMsg);
            return new SceneResult(errorMsg);
        }

        public static SceneResult Warning(string warningMsg)
        {
            Log.Warning(BaseLogChannel.Assets, warningMsg);
            return Success;
        }
    }
}