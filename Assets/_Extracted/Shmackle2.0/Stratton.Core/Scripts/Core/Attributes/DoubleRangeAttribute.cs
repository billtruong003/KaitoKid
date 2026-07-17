using UnityEngine;
using System;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DoubleRangeAttribute : PropertyAttribute
    {
        #region Fields

        public readonly float MaxX;
        public readonly float MinX;
        public readonly float MaxY;
        public readonly float MinY;
        public readonly string[] ForceNames;

        #endregion

        #region Constructors

        public DoubleRangeAttribute(float minX, float maxX, float minY, float maxY)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
        }

        public DoubleRangeAttribute(float minX, float maxX, float minY, float maxY, string[] names)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            ForceNames = names;
        }

        #endregion
    }
}