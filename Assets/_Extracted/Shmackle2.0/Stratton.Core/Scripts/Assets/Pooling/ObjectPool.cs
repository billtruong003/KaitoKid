using UnityEngine;
using Stratton.Core;

namespace Stratton.Assets
{
    public class ObjectPool : IObjectPool
    {
        #region Properties

        public bool IsReady { get; private set; }

        #endregion

        #region Public Methods

        public void Init()
        {
            IsReady = true;
        }

        public void DeInit()
        {
            IsReady = false;
        }

        public void Load(ILoadable iLoadable) { }

        public void Unload(IUnloadable iUnloadable) { }

        public void CreateInstance(IInstanceable iInstanceable) { }

        public GameObject GetInstance(IInstanceable iInstanceable)
        {
            return null;
        }

        public void ReturnInstance(GameObject gameObject, IInstanceable iInstanceable) { }

        public IAsset GetAsset(ILoadable iLoadable)
        {
            return null;
        }

        #endregion
    }
}