using System;
using UnityEngine;
using BillGameCore;

namespace Mythfall.Player
{
    /// <summary>
    /// Periodic nearest-enemy scanner. Uses Physics.OverlapSphereNonAlloc on the
    /// "Enemy" layer to avoid per-tick allocations. Bill.Timer.Repeat drives the
    /// 5 Hz refresh so individual players don't need their own Update poll.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [SerializeField] float searchRadius = 12f;
        [SerializeField] string enemyLayerName = "Enemy";
        [SerializeField] float refreshInterval = 0.2f;

        public Transform CurrentTarget { get; private set; }
        public float SearchRadius => searchRadius;

        public event Action<Transform> OnTargetChanged;

        readonly Collider[] _hitBuffer = new Collider[32];
        TimerHandle _timerHandle;
        int _enemyMask;

        void Start()
        {
            _enemyMask = LayerMask.GetMask(enemyLayerName);
            if (_enemyMask == 0)
                Debug.LogWarning($"[TargetSelector] Layer '{enemyLayerName}' not defined — add it via Project Settings → Tags and Layers.");

            _timerHandle = Bill.Timer.Repeat(refreshInterval, UpdateTarget);
        }

        void OnDestroy()
        {
            // Guard: Bill may not be initialized yet (Phase1 unloading scenes during bounce)
            // or may have been Reset (play mode exit). Either way, the timer is already gone.
            if (_timerHandle == null || !Bill.IsReady) return;
            Bill.Timer.Cancel(_timerHandle);
        }

        void UpdateTarget()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, searchRadius, _hitBuffer, _enemyMask, QueryTriggerInteraction.Ignore);

            Transform nearest = null;
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var col = _hitBuffer[i];
                if (col == null || !col.gameObject.activeInHierarchy) continue;

                float d = (col.transform.position - transform.position).sqrMagnitude;
                if (d < nearestSqr)
                {
                    nearestSqr = d;
                    nearest = col.transform;
                }
            }

            if (nearest != CurrentTarget)
            {
                CurrentTarget = nearest;
                OnTargetChanged?.Invoke(CurrentTarget);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, searchRadius);

            if (CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, CurrentTarget.position);
            }
        }
    }
}
