using UnityEngine;

namespace Stratton.Core
{
    /// <summary>
    ///     The support class for creating Singleton GameObjects, which are marked as DontDestroyOnLoad.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour, IDontDestroyOnLoad
        where T : Singleton<T>
    {
        #region Properties

        public static T Instance { get; private set; }

        #endregion

        #region Unity Methods

        protected virtual void Awake()
        {
            Instance = InstanceCheck(Instance) as T;
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        public void MarkAsDontDestroyOnLoad()
        {
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Private Methods

        protected Singleton<T> InstanceCheck(Singleton<T> instanceCheck)
        {
            if (instanceCheck != null && instanceCheck != this)
            {
                // Destroy new instance of Singleton GameObject.
                DestroyImmediate(gameObject);

                return instanceCheck;
            }

            MarkAsDontDestroyOnLoad();
            return this;
        }

        #endregion
    }
}