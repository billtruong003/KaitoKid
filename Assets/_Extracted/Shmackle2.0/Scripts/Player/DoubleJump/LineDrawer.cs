using System;
using UnityEngine;
using NaughtyAttributes;

namespace Shmackle.Player.DoubleJump
{
    public class LineDrawer : MonoBehaviour
    {
        [SerializeField] private Material _lineMaterial;
        [SerializeField] private Material _centerMaterial;

        [Header("Segment Settings")]
        [SerializeField] [Tooltip("Minimum distance before spawning a new arrow segment")]
        private float _minSegmentLength = 0.05f;

        [SerializeField] [Tooltip("Width of the line between arrows")]
        private float _lineWidth = 0.02f;

        [Header("Left Position Pinning")]
        [ShowNonSerializedField] private Vector3 _leftPinnedStartPosition;

        [ShowNonSerializedField] private Vector3 _leftPinnedEndPosition;

        [Header("Right Position Pinning")]
        [ShowNonSerializedField] private Vector3 _rightPinnedStartPosition;

        [ShowNonSerializedField] private Vector3 _rightPinnedEndPosition;

        public void PinLeftStartPosition(Vector3 position)
        {
            _leftPinnedStartPosition = position;
        }

        public void PinLeftEndPosition(Vector3 position)
        {
            _leftPinnedEndPosition = position;
            TryDrawSegment(_leftPinnedStartPosition, _leftPinnedEndPosition);
        }

        public void PinRightStartPosition(Vector3 position)
        {
            _rightPinnedStartPosition = position;
        }

        public void PinRightEndPosition(Vector3 position)
        {
            _rightPinnedEndPosition = position;
            TryDrawSegment(_rightPinnedStartPosition, _rightPinnedEndPosition);
        }
        
        private void TryDrawSegment(Vector3 startPosition, Vector3 endPosition, bool centerSegment = false)
        {
            Vector3 worldStart = transform.TransformPoint(startPosition);
            Vector3 worldEnd = transform.TransformPoint(endPosition);

            Vector3 direction = worldEnd - worldStart;
            float distance = direction.magnitude;

            if (distance < _minSegmentLength)
                return;

            GameObject lineObject = new GameObject("ArrowLine");
            lineObject.transform.SetParent(transform); // Optional, keeps things organized
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

            lineRenderer.material = centerSegment ? _centerMaterial : _lineMaterial;
            lineRenderer.startWidth = _lineWidth;
            lineRenderer.endWidth = _lineWidth;
            lineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.02f),
                new Keyframe(1f, 0.15f)
            );

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, worldStart);
            lineRenderer.SetPosition(1, worldEnd);
        }
        public void GenerateCenterSegment(LeftData leftData, RightData rightData)
        {
            // Center point from all four positions
            Vector3 centerEnd = (leftData.LastPosition + leftData.Position + rightData.LastPosition + rightData.Position) * 0.25f;

            // Average movement direction
            Vector3 leftDirection = leftData.Position - leftData.LastPosition;
            Vector3 rightDirection = rightData.Position - rightData.LastPosition;
            Vector3 averageDirection = ((leftDirection + rightDirection) * 0.5f).normalized;

            // Use average segment length to extrude the start point backward
            float averageLength = ((leftDirection.magnitude + rightDirection.magnitude) * 0.5f);
            Vector3 centerStart = centerEnd - averageDirection * averageLength;

            TryDrawSegment(centerStart, centerEnd, true);
        }
    }

    [Serializable]
    public class LeftData
    {
        public Vector3 Position;
        public Vector3 LastPosition;
    }

    [Serializable]
    public class RightData
    {
        public Vector3 Position;
        public Vector3 LastPosition;
    }
}