using UnityEngine;

namespace Shmackle.Utilities
{
    public class DeactivateOnReleaseBuild : MonoBehaviour
    {
        #region Private Methods
        
        private void Awake()
        {
#if !DEBUG
            gameObject.SetActive(false);
#endif
        }

        #endregion
    }
}