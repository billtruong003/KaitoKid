using UnityEngine;

namespace Stratton.AppReloading
{
    [CreateAssetMenu(fileName = "AppReloadSettings", menuName = "Settings/App Reload Settings")]
    public class AppReloadSettings : ScriptableObject
    {
        public float OnPauseTimeInSeconds;
        public float OnFocusTimeInSeconds;
        public float NoCommunicationWithServerTimeInSeconds;
    }
}
