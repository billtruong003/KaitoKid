#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
// The module_bindings will generate a DbVector2 type (or similar) for
// SpacetimeDB vector fields. This file provides conversion and utility
// extensions between Unity's Vector2/Vector3 and the DB vector types.

using UnityEngine;

namespace SpumOnline
{
    /// <summary>
    /// Extension methods for converting between SpacetimeDB vector types and
    /// Unity vector types, plus utility calculations.
    /// </summary>
    public static class VectorExtensions
    {
        // -------------------------------------------------------
        // DbVector2 <-> Vector2 conversions
        // -------------------------------------------------------

        // Note: DbVector2 is a placeholder for whatever the auto-generated
        // SpacetimeDB module bindings produce. The actual type name may differ
        // (e.g., DbVector2, StdbVector2, or a simple struct with X/Y fields).
        // Uncomment and adjust these when module_bindings are generated:

        /*
        /// <summary>Convert a SpacetimeDB DbVector2 to a Unity Vector2.</summary>
        public static Vector2 ToVector2(this DbVector2 dbVec)
        {
            return new Vector2(dbVec.X, dbVec.Y);
        }

        /// <summary>Convert a Unity Vector2 to a SpacetimeDB DbVector2.</summary>
        public static DbVector2 ToDbVector2(this Vector2 vec)
        {
            return new DbVector2 { X = vec.x, Y = vec.y };
        }

        /// <summary>Convert a SpacetimeDB DbVector2 to a Unity Vector3 (z = 0).</summary>
        public static Vector3 ToVector3(this DbVector2 dbVec, float z = 0f)
        {
            return new Vector3(dbVec.X, dbVec.Y, z);
        }

        /// <summary>Distance between two DbVector2 points.</summary>
        public static float DistanceTo(this DbVector2 a, DbVector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Squared distance between two DbVector2 points (avoids sqrt).</summary>
        public static float SqrDistanceTo(this DbVector2 a, DbVector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }
        */

        // -------------------------------------------------------
        // Float-pair conversions (server sends X,Y as separate floats)
        // -------------------------------------------------------

        /// <summary>
        /// Create a Vector2 from separate X,Y float fields (common pattern
        /// when SpacetimeDB tables store position as two float columns).
        /// </summary>
        public static Vector2 ToVector2(float x, float y)
        {
            return new Vector2(x, y);
        }

        /// <summary>
        /// Create a Vector3 from separate X,Y float fields with an optional Z.
        /// </summary>
        public static Vector3 ToVector3(float x, float y, float z = 0f)
        {
            return new Vector3(x, y, z);
        }

        // -------------------------------------------------------
        // Unity Vector2 utilities
        // -------------------------------------------------------

        /// <summary>Squared distance between two Vector2 points.</summary>
        public static float SqrDistanceTo(this Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude;
        }

        /// <summary>Manhattan distance between two Vector2 points.</summary>
        public static float ManhattanDistance(this Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Clamp a Vector2 to within a rectangular boundary.
        /// Useful for constraining positions within a world map.
        /// </summary>
        public static Vector2 ClampToRect(this Vector2 pos, float minX, float minY, float maxX, float maxY)
        {
            return new Vector2(
                Mathf.Clamp(pos.x, minX, maxX),
                Mathf.Clamp(pos.y, minY, maxY)
            );
        }

        /// <summary>
        /// Get a direction vector from one position to another, normalized.
        /// Returns Vector2.zero if the positions are the same.
        /// </summary>
        public static Vector2 DirectionTo(this Vector2 from, Vector2 to)
        {
            Vector2 dir = to - from;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        }

        /// <summary>
        /// Check if a position is within a given range of another position.
        /// Uses squared distance for performance.
        /// </summary>
        public static bool IsWithinRange(this Vector2 pos, Vector2 target, float range)
        {
            return (pos - target).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// Convert a Vector2 to a Vector3 for world-space positioning.
        /// Z is set based on Y for sprite sorting (further up = behind).
        /// </summary>
        public static Vector3 ToWorldPosition(this Vector2 pos)
        {
            return new Vector3(pos.x, pos.y, pos.y * 0.01f);
        }

        /// <summary>
        /// Smooth step interpolation between two positions.
        /// Useful for network position interpolation.
        /// </summary>
        public static Vector2 SmoothLerp(this Vector2 current, Vector2 target, float speed, float deltaTime)
        {
            if (Vector2.Distance(current, target) < 0.001f) return target;
            return Vector2.Lerp(current, target, 1f - Mathf.Exp(-speed * deltaTime));
        }

        /// <summary>
        /// Snap a position to a grid with the given cell size.
        /// </summary>
        public static Vector2 SnapToGrid(this Vector2 pos, float cellSize)
        {
            return new Vector2(
                Mathf.Round(pos.x / cellSize) * cellSize,
                Mathf.Round(pos.y / cellSize) * cellSize
            );
        }
    }
}

#endif // STDB_BINDINGS
