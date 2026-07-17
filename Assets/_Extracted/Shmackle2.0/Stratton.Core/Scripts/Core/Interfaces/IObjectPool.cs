using UnityEngine;

namespace Stratton.Core
{
    /// <summary>
    /// WIP... some methods will be moved probably...
    /// </summary>
    public interface IObjectPool : IInitializable
    {
        #region Public Methods

        /// <summary>
        ///     Load Asset to memory and prepare it to be instantiated.
        /// </summary>
        /// <param name="iLoadable"></param>
        void Load(ILoadable iLoadable);

        /// <summary>
        ///     Unload Asset from memory and make it unable to be instantiated.
        /// </summary>
        /// <param name="iUnloadable"></param>
        void Unload(IUnloadable iUnloadable);

        /// <summary>
        ///     Create Instance of a loaded Asset and put it into pool. If Asset is not loaded, first it'll be loaded.
        /// </summary>
        /// <param name="iInstanceable"></param>
        void CreateInstance(IInstanceable iInstanceable);

        /// <summary>
        ///     Get Instance of loaded Asset from pool and remove it from pool. If there is no more instances in pool, it'll be
        ///     created. If Asset is not loaded, it'll be loaded first.
        /// </summary>
        /// <param name="iInstanceable"></param>
        GameObject GetInstance(IInstanceable iInstanceable);

        /// <summary>
        ///     Return Instance of a loaded Asset to pool.
        /// </summary>
        void ReturnInstance(GameObject gameObject, IInstanceable iInstanceable);

        #endregion
    }
}