using UnityEngine;

namespace Stratton.Core
{
    public static class Vector2Extensions
    {
        #region Public Methods

        public static Vector2 Divide(this Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x / v2.x, v1.y / v2.y);
        }

        public static Vector3 ToVec3XZ(this Vector2 vec)
        {
            return new Vector3(vec.x, 0f, vec.y);
        }

        public static Vector3 ToVec3XY(this Vector2 vec)
        {
            return new Vector3(vec.x, vec.y, 0f);
        }

        public static Vector3 ToVec3XZAndY(this Vector2 vec, float y)
        {
            return new Vector3(vec.x, y, vec.y);
        }

        public static Vector2 Min(this Vector2 vec1, Vector2 vec2)
        {
            return new Vector2(Mathf.Min(vec1.x, vec2.x), Mathf.Min(vec1.y, vec2.y));
        }

        public static Vector2 Max(this Vector2 vec1, Vector2 vec2)
        {
            return new Vector2(Mathf.Max(vec1.x, vec2.x), Mathf.Max(vec1.y, vec2.y));
        }

        public static float AngleFromZero(this Vector2 v)
        {
            var ang = Mathf.Acos(v.normalized.x);
            return v.y >= 0 ? ang : -ang;
        }

        #endregion
    }
}