using UnityEngine;

namespace Stratton.Core
{
    public static class Vector3Extensions
    {
        #region Fields

        private static readonly Quaternion _displace = Quaternion.Euler(0, 107, 0);
        private static readonly Quaternion _rot90Right = Quaternion.Euler(0, 90, 0);
        private static readonly Quaternion _rot90Left = Quaternion.Euler(0, -90, 0);
        private static int? _walkableArea;

        #endregion

        #region Public Methods

        public static void AddXZ(this Vector3 origin, Vector3 another)
        {
            origin.x += another.x;
            origin.z += another.z;
        }

        public static Vector3 NeutralizeY(this Vector3 vec, Vector3 sample)
        {
            vec.y = sample.y;
            return vec;
        }

        public static Vector3 Left(this Vector3 vec)
        {
            return _rot90Left * vec;
        }

        public static Vector3 Right(this Vector3 vec)
        {
            return _rot90Right * vec;
        }

        public static Vector3 Displace(this Vector3 vec)
        {
            return _displace * vec;
        }

        public static Vector3 Abs(this Vector3 vec)
        {
            return new Vector3(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
        }

        public static float SqrMagnitudeXZ(this Vector3 vec)
        {
            return vec.x * vec.x + vec.z * vec.z;
        }

        public static float MagnitudeXZ(this Vector3 vec)
        {
            return Mathf.Sqrt(vec.x * vec.x + vec.z * vec.z);
        }

        public static Vector4 ToVec4(this Vector3 vec, float w = 0f)
        {
            return new Vector4(vec.x, vec.y, vec.z, w);
        }

        public static Vector2 ToVec2XZ(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        public static float DotXZ(this Vector3 vec, Vector3 vec2)
        {
            return vec.x * vec2.x + vec.z * vec2.z;
        }

        public static Vector2 ToVec2XY(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }

        public static Vector3 Min(this Vector3 vec1, Vector3 vec2)
        {
            return new Vector3(Mathf.Min(vec1.x, vec2.x), Mathf.Min(vec1.y, vec2.y), Mathf.Min(vec1.z, vec2.z));
        }

        public static Vector3 Max(this Vector3 vec1, Vector3 vec2)
        {
            return new Vector3(Mathf.Max(vec1.x, vec2.x), Mathf.Max(vec1.y, vec2.y), Mathf.Max(vec1.z, vec2.z));
        }

        #endregion
    }
}