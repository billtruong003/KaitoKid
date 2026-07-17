using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.XR.Shared.Utils
{
    public static class TransformManipulations
    {
        // Return the position and rotation of a referential referenceTransform so that a offsetedTransform already placed properly will have the required positionOffset/rotationOffset
        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectChildPositionOffset(
            Transform referenceTransform, 
            Vector3 offsetedTransformPosition, Quaternion offsetedTransformRotation, 
            Vector3 positionOffset, Quaternion rotationOffset,
            bool acceptLossyScale = false,
            Vector3? forcedScale = null
            )
        {
            var rotation = offsetedTransformRotation * Quaternion.Inverse(rotationOffset);
            // We do not apply the rotation to the transform right now, so to use the rotated transform, we can't rely on it and have to use a matrix to emulate in advance the new transform position
            Vector3 scale;
            if (forcedScale != null)
            {
                scale = forcedScale.GetValueOrDefault();
            }
            else if (referenceTransform.parent != null)
            {
                if (acceptLossyScale)
                {
                    scale = referenceTransform.lossyScale;
                } 
                else
                {
                    throw new System.Exception("[ReferentialPositionToRespectChildPositionOffset] Lossy scale not accepted while the reference transform has a parent");
                }
            }
            else
            {
                scale = referenceTransform.localScale;
            }
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransform.position, rotation, scale);
            // If the transform was already rotated, it would be equivalent to Equivalent to:
            //     var offsetInRotatedReference = referenceTransform.TransformPoint(positionOffset);
            var offsetedInRotatedReference = referenceTransformMatrix.MultiplyPoint(positionOffset);
            var position = offsetedTransformPosition - (offsetedInRotatedReference - referenceTransform.transform.position);
            var movedReferenceTransformMatrix = Matrix4x4.TRS(position, rotation, referenceTransform.localScale);
            var appliedOffsetInFixedRef = movedReferenceTransformMatrix.MultiplyPoint(positionOffset);
            return (position, rotation);
        }

        public static (Vector3 newReferencePosition, Quaternion newReferencerotation) ReferentialPositionToRespectChildPositionUnscaledOffset(
            Transform referenceTransform,
            Vector3 offsetedTransformPosition, Quaternion offsetedTransformRotation,
            Vector3 positionOffset, Quaternion rotationOffset
        )
        {
            return ReferentialPositionToRespectChildPositionOffset(referenceTransform, offsetedTransformPosition, offsetedTransformRotation, positionOffset, rotationOffset, forcedScale: Vector3.one);
        }

        /// <summary>
        /// Return a transform position/rotation offset relative to another transform
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Transform referenceTransform, Transform transformToOffset)
        {
            // Equivalent to "offset = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the one of its parents
            return UnscaledOffset(referenceTransform.position, referenceTransform.rotation, transformToOffset);
        }

        /// <summary>
        /// Return a transform position/rotation offset relative to another virtual transform, with referenceTransform.position=referenceTransformPosition, referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// For the position, equivalent to "offsetPosition = referenceTransform.InverseTransformPoint(transformToOffset.position)" when the referenceTransform scale is Vector3.one, as well as the ones of its parents
        /// </summary>
        /// <returns></returns>
        public static (Vector3 offset, Quaternion rotationOffset) UnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Transform transformToOffset)
        { 
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, referenceTransformRotation, Vector3.one);
            var offset = referenceTransformMatrix.inverse.MultiplyPoint(transformToOffset.position);

            var rotationOffset = Quaternion.Inverse(referenceTransformRotation) * transformToOffset.rotation;
            return (offset, rotationOffset);
        }

        /// <summary>
        /// Return an offseted position/rotation relatively to a referenceTransform
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ApplyUnscaledOffset(Transform referenceTransform, Vector3 offset, Quaternion rotationOffset)
        { 
            return ApplyUnscaledOffset(referenceTransform.position, referenceTransform.rotation, offset, rotationOffset);
        }

        /// <summary>
        /// Return an offseted position/rotation relatively to a virtual referenceTrasnform, with referenceTransform.position=referenceTransformPosition, referenceTransform.position=referenceTransformPosition, with referenceTransform.rotation=referenceTransformRotation, referenceTransform.scale=Vector3.one (as well as its parents' scales)
        /// </summary>
        public static (Vector3 position, Quaternion rotation) ApplyUnscaledOffset(Vector3 referenceTransformPosition, Quaternion referenceTransformRotation, Vector3 offset, Quaternion rotationOffset)
        {
            var rotation = referenceTransformRotation * rotationOffset;
            var referenceTransformMatrix = Matrix4x4.TRS(referenceTransformPosition, referenceTransformRotation, Vector3.one);
            var position = referenceTransformMatrix.MultiplyPoint(offset);
            return (position, rotation);
        }
    }
}
