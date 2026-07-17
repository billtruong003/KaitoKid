using System.Collections;
using Squido.JungleXRKit.Core;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Attaches to pooled VFX prefabs (like muzzle flashes and impacts).
    /// Automatically returns the object to the pool after a set delay.
    /// For LOCAL-ONLY pooled VFX objects. Should NOT be used on NetworkObject prefabs.
    /// </summary>
    public class PoolAutoReturn : MonoBehaviour
    {
        [Tooltip("Time in seconds before returning to the pool.")]
        public float delay = 2f;

        private WaitForSeconds _waitDelay;

        private void Awake()
        {
            _waitDelay = new WaitForSeconds(delay);
        }

        private void OnEnable()
        {
            StartCoroutine(IEReturnAfterDelay());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator IEReturnAfterDelay()
        {
            yield return _waitDelay;

            var poolObject = GetComponent<PoolObject>();
            if (poolObject != null)
            {
                poolObject.Return();
            }
            else
            {
                Destroy(gameObject);
            }
        }

    }
}
