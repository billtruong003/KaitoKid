#nullable enable
using UnityEngine;

namespace RadiantArena.Weapons
{
    /// <summary>
    /// Runtime composite primitive factory. Slug → GameObject built from Unity
    /// primitives parented to a "WeaponRoot" empty. No .prefab assets — keeps
    /// version control clean while we're in placeholder phase. Real FBX models
    /// + materials replace per-method bodies when D.U12 / future asset pass ships.
    ///
    /// All composite primitives have Colliders dropped (visual-only, server is
    /// authoritative on collisions).
    /// </summary>
    public static class WeaponPrefabRegistry
    {
        static Material? _baseMat;
        static System.Collections.Generic.HashSet<string>? _warnedSlugs;

        /// <summary>
        /// Spawn a weapon prefab parented to the given transform. Returns the
        /// root GameObject. Unknown slugs return the placeholder (grey sphere)
        /// and log a one-time warning.
        /// </summary>
        public static GameObject Spawn(string slug, Transform parent)
        {
            EnsureBaseMaterial();
            GameObject root;
            switch (slug)
            {
                case "weapon_thiet_con_01":   root = BuildThietCon();   break;
                case "weapon_chuy_01":        root = BuildChuy();       break;
                case "weapon_kiem_01":        root = BuildKiem();       break;
                case "weapon_thiet_phien_01": root = BuildThietPhien(); break;
                case "weapon_di_hoa_01":      root = BuildDiHoa();      break;
                case "weapon_le_bang_01":     root = BuildLeBang();     break;
                default:
                    WarnUnknown(slug);
                    root = BuildPlaceholder();
                    break;
            }
            root.name = "Weapon_" + slug;
            if (parent != null) root.transform.SetParent(parent, worldPositionStays: false);
            return root;
        }

        static void EnsureBaseMaterial()
        {
            if (_baseMat != null) return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _baseMat = new Material(shader);
            _baseMat.color = new Color(0.85f, 0.85f, 0.90f);
        }

        static void WarnUnknown(string slug)
        {
            if (_warnedSlugs == null) _warnedSlugs = new System.Collections.Generic.HashSet<string>();
            if (_warnedSlugs.Contains(slug)) return;
            _warnedSlugs.Add(slug);
            Debug.LogWarning("[Weapons] unknown slug '" + slug + "' — spawning placeholder");
        }

        static GameObject NewRoot()
        {
            return new GameObject("WeaponRoot");
        }

        static GameObject AddPrim(GameObject root, PrimitiveType type, Vector3 localPos, Vector3 localScale, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(root.transform, worldPositionStays: false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (rot.HasValue) go.transform.localRotation = rot.Value;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && _baseMat != null) r.sharedMaterial = _baseMat;
            return go;
        }

        // Sizes tuned for top-down ortho cam (size 6, ~12u visible). Each weapon's
        // XZ footprint targets ~0.8-1.5u so the silhouette reads against the
        // 1u-wide capsule. Cylinders are Z-rotated 90deg to lay horizontal along X.

        static GameObject BuildThietCon()
        {
            // Iron staff — long horizontal cylinder, X-extent 1.5u.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Cylinder,
                Vector3.zero,
                new Vector3(0.12f, 0.75f, 0.12f),
                Quaternion.Euler(0f, 0f, 90f));
            return root;
        }

        static GameObject BuildChuy()
        {
            // Mace — handle + ball head. Total X-extent ~1.5u.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Cylinder,
                new Vector3(-0.30f, 0f, 0f),
                new Vector3(0.10f, 0.40f, 0.10f),
                Quaternion.Euler(0f, 0f, 90f));
            AddPrim(root, PrimitiveType.Sphere,
                new Vector3(0.45f, 0f, 0f),
                Vector3.one * 0.40f);
            return root;
        }

        static GameObject BuildKiem()
        {
            // Sword — blade + crossguard hilt. Total X-extent ~1.6u.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Cube,
                new Vector3(0.30f, 0f, 0f),
                new Vector3(1.20f, 0.08f, 0.20f));
            AddPrim(root, PrimitiveType.Cube,
                new Vector3(-0.40f, 0f, 0f),
                new Vector3(0.20f, 0.10f, 0.40f));
            return root;
        }

        static GameObject BuildThietPhien()
        {
            // Iron fan — wide flat plate, XZ footprint 1.0 x 0.7u.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Cube,
                Vector3.zero,
                new Vector3(1.00f, 0.08f, 0.70f));
            return root;
        }

        static GameObject BuildDiHoa()
        {
            // Exotic flower — 3 spheres clustered, ~0.8 x 0.6u cluster.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Sphere, new Vector3( 0.00f, 0f,  0.00f), Vector3.one * 0.40f);
            AddPrim(root, PrimitiveType.Sphere, new Vector3( 0.32f, 0f,  0.20f), Vector3.one * 0.30f);
            AddPrim(root, PrimitiveType.Sphere, new Vector3(-0.25f, 0f, -0.22f), Vector3.one * 0.32f);
            return root;
        }

        static GameObject BuildLeBang()
        {
            // Frost icicles — 3 cylinders fanning out, ~0.9 x 0.5u spread.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Cylinder, new Vector3( 0.00f, 0f,  0.00f), new Vector3(0.10f, 0.45f, 0.10f), Quaternion.Euler(0f,    0f, 90f));
            AddPrim(root, PrimitiveType.Cylinder, new Vector3(-0.10f, 0f,  0.22f), new Vector3(0.08f, 0.35f, 0.08f), Quaternion.Euler(0f,   30f, 90f));
            AddPrim(root, PrimitiveType.Cylinder, new Vector3(-0.10f, 0f, -0.22f), new Vector3(0.08f, 0.35f, 0.08f), Quaternion.Euler(0f,  -30f, 90f));
            return root;
        }

        static GameObject BuildPlaceholder()
        {
            // Grey sphere fallback, diameter 0.4u.
            var root = NewRoot();
            AddPrim(root, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.40f);
            return root;
        }
    }
}
