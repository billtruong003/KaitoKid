using UnityEngine;

namespace Stratton.Effects
{
    public static class PriorityCalculator
    {
        public static bool IsWithinMaxDistanceToCamera(Transform playerTransform, ObjectEmitter objectEmitter)
        {
            return objectEmitter.ObjectEmitterData.MaxDistanceToCamera <= CalculateDistanceWithPriority(playerTransform, objectEmitter);
        }

        public static float CalculateDistanceWithPriority(Transform playerTransform, ObjectEmitter objectEmitter)
        {
            return  Vector3.Distance(playerTransform.position, objectEmitter.transform.position) * objectEmitter.ObjectEmitterData.Priority;
        }
    }
}