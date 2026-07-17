using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Stratton.Core
{
    public class AppBootstrap : MonoBehaviour
    {
        #region Private Methods

        private void Start()
        {
            InstallMessagePipe();
            Init();
        }

        protected void OnDestroy()
        {
            DeInit();
        }

        private void InstallMessagePipe()
        {
            var builder = new BuiltinContainerBuilder();
            builder.AddMessagePipe();

            AppServicesManager.Instance.InstallMessageBrokers(builder);
            GameSystemsManager.Instance.InstallMessageBrokers(builder);

            var provider = builder.BuildServiceProvider();
            GlobalMessagePipe.SetProvider(provider);
        }

        private void Init()
        {
            AppServicesManager.Instance.Init();
            GameSystemsManager.Instance.Init().Forget();
        }

        private async void DeInit()
        {
            await GameSystemsManager.Instance.DeInit();
            AppServicesManager.Instance.DeInit();
        }

        #endregion
    }
}