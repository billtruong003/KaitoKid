using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Core
{
    public class MathSupport
    {
        #region Nested

        public class ThickLine : List<Vector2>
        {
            #region Constructors

            public ThickLine(Vector2 beg, Vector2 end, float width)
                : base(4)
            {
                Vector2 moveV = end - beg;
                Vector2 right = (new Vector2(moveV.y, -moveV.x)).normalized * width * 0.5f;
                Add(beg - right);
                Add(beg + right);
                Add(end + right);
                Add(end - right);
            }

            #endregion
        }

        #endregion

        #region Public Methods

        public static float FloatFloor(float amount, int precision)
        {
            if (precision < 0)
            {
                Debug.LogError("Precision in FloatFloor can't be less than 0!");
                return amount;
            }

            int multiplier = (int) Mathf.Pow(10.0f, precision);

            float multiplied = amount * multiplier;
            int integer = Mathf.RoundToInt(multiplied);
            float rounded = ((float) integer) / multiplier;
            return rounded;
        }

        // Using in HUDBars where value cant reach 0.
        public static float FloatCeil(float amount, int precision)
        {
            if (precision < 0)
            {
                Debug.LogError("Precision in FloatCeil can't be less than 0!");
                return amount;
            }

            int multiplier = (int) Mathf.Pow(10.0f, precision);

            float multiplied = amount * multiplier;
            int integer = Mathf.CeilToInt(multiplied);
            float rounded = (float) integer / multiplier;
            return rounded;
        }

        public static bool IsInPoly(IList<Vector2> poly, Vector2 point)
        {
            bool inside = false;

            if (poly.Count < 3)
                return false;

            Vector2 oldPt = poly[poly.Count - 1];
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 v1;
                Vector2 v2;
                Vector2 newPt = poly[i];
                if (newPt.x > oldPt.x)
                {
                    v1 = oldPt;
                    v2 = newPt;
                }
                else
                {
                    v1 = newPt;
                    v2 = oldPt;
                }

                if (newPt.x < point.x == point.x <= oldPt.x &&
                   (point.y - v1.y) * (v2.x - v1.x) < (v2.y - v1.y) * (point.x - v1.x))
                {
                    inside = !inside;
                }

                oldPt = newPt;
            }

            return inside;
        }

        public static bool IsInPoly(Vector2[] poly, Vector2 point)
        {
            bool inside = false;

            if (poly.Length < 3)
                return false;

            Vector2 oldPt = poly[poly.Length - 1];
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 newPt = poly[i];
                Vector2 v1;
                Vector2 v2;
                if (newPt.x > oldPt.x)
                {
                    v1 = oldPt;
                    v2 = newPt;
                }
                else
                {
                    v1 = newPt;
                    v2 = oldPt;
                }

                if (newPt.x < point.x == point.x <= oldPt.x &&
                    (point.y - v1.y) * (v2.x - v1.x) < (v2.y - v1.y) * (point.x - v1.x)
                )
                {
                    inside = !inside;
                }

                oldPt = newPt;
            }

            return inside;
        }

        public static Vector3 CalcHitPointWithGround(Ray ray, float groundY)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            float dist;
            plane.Raycast(ray, out dist);
            return ray.origin + dist * ray.direction;
        }

        public static bool IsBetween(float val, float min, float max)
        {
            return val <= max && val >= min;
        }

        public static int Sign(float val)
        {
            if (val > 0f)
                return 1;

            if (val < 0f)
                return -1;

            return 0;
        }


        public static float AngleBeetweenVectors(Vector2 start, Vector2 end)
        {
            float ang = Vector2.Angle(start, end);
            Vector3 cross = Vector3.Cross(start, end);

            if (cross.z > 0)
                ang = -ang;

            return ang;
        }

        public static float Frac(float loops)
        {
            return loops - Mathf.FloorToInt(loops);
        }

        #endregion
    }
}