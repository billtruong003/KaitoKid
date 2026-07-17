#nullable enable
using UnityEngine;

namespace RadiantArena.Weapons
{
    /// <summary>
    /// Apply a hex color tint to every MeshRenderer under a weapon root via
    /// MaterialPropertyBlock — zero GC alloc, no shader variant explosion.
    /// </summary>
    public static class WeaponHueApplier
    {
        static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int _legacyColorId = Shader.PropertyToID("_Color");
        static MaterialPropertyBlock? _mpb;

        public static void Apply(GameObject root, string hex)
        {
            if (root == null) return;
            if (string.IsNullOrEmpty(hex)) return;

            string h = hex.StartsWith("#") ? hex : "#" + hex;
            if (!ColorUtility.TryParseHtmlString(h, out var color))
            {
                Debug.LogWarning("[Weapons.Hue] could not parse hex '" + hex + "' — skipping tint");
                return;
            }

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            var renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(_baseColorId,   color);
                _mpb.SetColor(_legacyColorId, color);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
