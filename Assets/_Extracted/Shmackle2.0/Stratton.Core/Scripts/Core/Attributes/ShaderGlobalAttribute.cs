using UnityEngine;
using System;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShaderGlobalAttribute : PropertyAttribute
    {
        #region Fields

        public readonly string propertyName;

        #endregion

        #region Constructors

        public ShaderGlobalAttribute(string propertyName)
        {
            this.propertyName = propertyName;
        }

        #endregion
    }
}