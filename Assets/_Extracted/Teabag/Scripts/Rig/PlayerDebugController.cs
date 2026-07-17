using Teabag.Core;
using UnityEngine;

namespace Teabag.Player
{
    public sealed class PlayerDebugController : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private GameObject _vrConsolePrefab;

        private void Start()
        {
            var gorilla = GetComponent<IGorilla>();
            if (gorilla is null || gorilla.HasStateAuthority is false)
                return;

            var console = GetComponent<DebugCommandConsole>();
            if (console is null)
                console = gameObject.AddComponent<DebugCommandConsole>();

            if (_vrConsolePrefab != null)
                console.SetVRConsolePrefab(_vrConsolePrefab);
        }
#endif
    }
}
