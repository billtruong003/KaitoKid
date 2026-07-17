using UnityEngine;
using System;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TrippleRangeAttribute : PropertyAttribute
    {
        #region Fields

        public readonly float MaxX;
        public readonly float MinX;
        public readonly float MaxY;
        public readonly float MinY;
        public readonly float MaxZ;
        public readonly float MinZ;

        #endregion

        #region Constructors

        public TrippleRangeAttribute(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        #endregion
    }
}