using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Stratton.Core;

namespace Stratton.CI.Editor
{
    public interface IDeploymentConfig
    {
        void Init(DeployerType builderType, Dictionary<string, string> parameters);
        void OnPostInit();
        void OnPreDeploy();
        UniTask Deploy();
        void OnPostDeploy();
    }
}
