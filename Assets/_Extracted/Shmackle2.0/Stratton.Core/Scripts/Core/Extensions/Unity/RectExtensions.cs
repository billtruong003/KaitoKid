using UnityEngine;

namespace Stratton.Core
{
    public static class RectExtensions
    {
        #region Public Methods

        public static Rect Offset(this Rect vec, float x)
        {
            vec.x += x;
            return vec;
        }

        public static Rect Offset(this Rect vec, Vector2 offset)
        {
            vec.x += offset.x;
            vec.y += offset.y;
            return vec;
        }

        #endregion
    }
}