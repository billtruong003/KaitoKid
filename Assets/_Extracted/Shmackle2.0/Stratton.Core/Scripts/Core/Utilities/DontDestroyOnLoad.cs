using UnityEngine;

namespace Stratton.Core
{
    public class DontDestroyOnLoad : MonoBehaviour, IDontDestroyOnLoad
    {
        #region Public Methods

        public void MarkAsDontDestroyOnLoad()
        {
            DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            MarkAsDontDestroyOnLoad();
        }

        #endregion
    }
}