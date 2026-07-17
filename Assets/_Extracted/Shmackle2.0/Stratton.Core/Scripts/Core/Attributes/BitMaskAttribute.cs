using System;
using UnityEngine;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BitMaskAttribute : PropertyAttribute
    {
        #region Constructors

        public BitMaskAttribute(Type type, bool elementaryMaskOnly = false, bool zeroMaskIncluded = false)
        {
            PropType = type;
            ElementaryMaskOnly = elementaryMaskOnly;
            ZeroMaskIncluded = zeroMaskIncluded;
        }

        #endregion

        #region Properties

        public Type PropType { get; private set; }
        public bool ElementaryMaskOnly { get; private set; }
        public bool ZeroMaskIncluded { get; private set; }

        #endregion
    }
}