using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Shmackle.Player.EyeAnimated
{
    /// <summary>
    /// Marks a GameObject as a valid focus point for the EyeTracking system.
    /// Maintains a static registry of active targets for performant querying without generic searches.
    /// Uses a priority system to determine which target should capture the eye's focus when multiple are visible.
    /// </summary>
    public class EyeTarget : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public byte Priority => _priority;
        public static IReadOnlyCollection<EyeTarget> ActiveTargets => _activeTargets;
        private static readonly HashSet<EyeTarget> _activeTargets = new HashSet<EyeTarget>();


        [Header("Settings")]
        [Tooltip("Higher value = Higher priority (0-255)")]
        [SerializeField] private byte _priority = 0;

        [Header("Debug")]
        [SerializeField] private Color _gizmoColor = new Color(0, 1, 1, 0.6f);
        [SerializeField] private float _gizmoSize = 0.15f;


        private void OnEnable() => _activeTargets.Add(this);
        private void OnDisable() => _activeTargets.Remove(this);

        public void SetPriority(byte newPriority) => _priority = newPriority;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoSize * 0.6f);
            Gizmos.DrawWireSphere(transform.position, _gizmoSize);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = _gizmoColor;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;

            Vector3 labelPos = transform.position + Vector3.up * (_gizmoSize + 0.2f);
            Handles.Label(labelPos, $"Priority: {_priority}", style);
        }
#endif
    }
}