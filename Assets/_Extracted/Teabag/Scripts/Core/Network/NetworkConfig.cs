using UnityEngine;

namespace Teabag.Core
{
    // All settings in this config are used in production builds.
    // Simulation-only tools (SimulateTimeout, SimulateFailure) live on NetworkManager behind #if UNITY_EDITOR.
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Gorilla Royale/Network Config")]
    public class NetworkConfig : ScriptableObject
    {
        [Tooltip("How long to wait for a connection attempt before timing out.")]
        [SerializeField] private float _connectionTimeoutSeconds = 30f;

        [Tooltip("Number of retry attempts after the initial connection fails. Total attempts = MaxRetries + 1.")]
        [SerializeField] private int _maxRetries = 4;

        [Tooltip("Delay in seconds before the first retry. Subsequent retries double this value (exponential backoff).")]
        [SerializeField] private float _baseRetryDelaySeconds = 10f;

        [Tooltip("Maximum delay in seconds between retries. Caps the exponential backoff.")]
        [SerializeField] private float _maxRetryDelaySeconds = 60f;

        public float ConnectionTimeoutSeconds => _connectionTimeoutSeconds;
        public int MaxRetries => _maxRetries;
        public float BaseRetryDelaySeconds => _baseRetryDelaySeconds;
        public float MaxRetryDelaySeconds => _maxRetryDelaySeconds;

        public float GetRetryDelay(int attempt)
        {
            float delay = _baseRetryDelaySeconds * Mathf.Pow(2f, attempt);
            return Mathf.Min(delay, _maxRetryDelaySeconds);
        }
    }
}
