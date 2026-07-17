using UnityEngine;

namespace Stratton.Core
{
    public static class Vector4Extensions
    {
        #region Public Methods

        public static Vector4 Frac(this Vector4 vec)
        {
            vec.x = vec.x - Mathf.FloorToInt(vec.x);
            vec.y = vec.y - Mathf.FloorToInt(vec.y);
            vec.z = vec.z - Mathf.FloorToInt(vec.z);
            vec.w = vec.w - Mathf.FloorToInt(vec.w);
            return vec;
        }

        #endregion
    }
}