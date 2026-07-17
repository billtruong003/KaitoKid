using UnityEngine;

namespace Stratton.Core
{
    public static class MonoBehaviourExtensions
    {
        public static void StopCoroutineAndClear(this MonoBehaviour monoBehaviour, ref Coroutine coroutine)
        {
            if (coroutine != null)
            {
                monoBehaviour.StopCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}
