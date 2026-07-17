using System;

namespace Stratton.Assets.Requests
{
    public interface ILoadRequest
    {
        public float Progress { get; }
    }

    public class LoadRequest : ILoadRequest, IProgress<float>
    {
        private Action _reportProgress;

        public float Progress { get; private set; }

        public LoadRequest(Action reportProgress)
        {
            _reportProgress = reportProgress;
        }

        public void Report(float value)
        {
            Progress = value;
            _reportProgress.Invoke();
        }
    }
}