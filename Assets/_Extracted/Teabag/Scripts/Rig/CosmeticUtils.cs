using System;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Player.Cosmetics;

namespace Teabag.Player
{
public class CosmeticUtils : MonoBehaviour
{
    // ── Cosmetic object cache ──
    // Key: "SlotName/CosmeticName" → cached inactive GameObject
    private static readonly Dictionary<string, GameObject> s_Cache = new();
    private static Transform s_CacheRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void WireBridges()
    {
        Cosmetic.ResolveCosmeticType = CosmeticType;
        s_Cache.Clear();
        s_CacheRoot = null;
    }

    private static Transform GetCacheRoot()
    {
        if (s_CacheRoot == null)
        {
            var go = new GameObject("[CosmeticCache]");
            DontDestroyOnLoad(go);
            go.SetActive(true);
            s_CacheRoot = go.transform;
        }
        return s_CacheRoot;
    }

    private static string CacheKey(string category, string cosmeticName)
    {
        return $"{category}/{cosmeticName}";
    }

    public static bool Exists(Cosmetic cosmetic)
    {
        return Load(cosmetic.category.ToString(), cosmetic.cosmetic) != null;
    }

    /// <summary>
    /// Caches a cosmetic GameObject instead of destroying it.
    /// The object is deactivated and re-parented to a persistent cache root.
    /// </summary>
    public static void ReturnToCache(GameObject cosmetic)
    {
        if (cosmetic == null) return;

        string key = CacheKey(cosmetic.transform.parent != null
            ? cosmetic.transform.parent.name
            : "Unknown", cosmetic.name);

        cosmetic.SetActive(false);
        cosmetic.transform.SetParent(GetCacheRoot(), false);

        // Only cache the first instance per key; destroy duplicates
        if (!s_Cache.ContainsKey(key))
            s_Cache[key] = cosmetic;
        else
            Destroy(cosmetic);
    }

    /// <summary>
    /// Legacy destroy method — now delegates to caching.
    /// </summary>
    public static void DestroyCosmetic(GameObject cosmetic)
    {
        ReturnToCache(cosmetic);
    }

    /// <summary>
    /// Tries to retrieve a cached instance. Returns null if not cached.
    /// </summary>
    private static GameObject GetFromCache(string category, string cosmeticName)
    {
        string key = CacheKey(category, cosmeticName);
        if (s_Cache.TryGetValue(key, out GameObject cached) && cached != null)
        {
            s_Cache.Remove(key);
            cached.SetActive(true);
            return cached;
        }

        s_Cache.Remove(key); // clean up null entries
        return null;
    }

    public static GameObject InstantiateCosmetic(Cosmetic cosmetic, Transform parent, string fallback = "DEFAULT")
    {
        // Clear existing children by caching them
        foreach (Transform t in parent)
            ReturnToCache(t.gameObject);

        string category = cosmetic.category.ToString();
        GameObject prefab = Load(category, cosmetic.cosmetic);
        bool spawnDefault = cosmetic.category == CosmeticSlot.Head;

        if (prefab != null)
        {
            InteractiveCosmetic interactive = prefab.GetComponentInChildren<InteractiveCosmetic>();
            if (interactive != null)
            {
                spawnDefault = !interactive.overrideDefault;
            }
        }
        else
            spawnDefault = true;

        if (spawnDefault)
        {
            // Try cache first for default
            GameObject defaultObj = GetFromCache(category, fallback);
            if (defaultObj != null)
            {
                defaultObj.transform.SetParent(parent, false);
                defaultObj.transform.localPosition = Vector3.zero;
                defaultObj.transform.localRotation = Quaternion.identity;
                defaultObj.transform.localScale = Vector3.one *  0.007f;
                defaultObj.name = fallback;
            }
            else
            {
                GameObject defaultPrefab = Load(category, fallback);
                if (defaultPrefab != null)
                {
                    GameObject obj = Instantiate(defaultPrefab, parent);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = Vector3.one *  0.007f;
                    obj.name = fallback;
                }
            }
        }

        if (prefab != null)
        {
            // Try cache first
            GameObject cached = GetFromCache(category, cosmetic.cosmetic);
            if (cached != null)
            {
                cached.transform.SetParent(parent, false);
                cached.transform.localPosition = Vector3.zero;
                cached.transform.localRotation = Quaternion.identity;
                cached.transform.localScale = Vector3.one *  0.007f;
                cached.name = cosmetic.cosmetic;
                return cached;
            }

            // Instantiate new
            GameObject obj = Instantiate(prefab, parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one *  0.007f;
            obj.name = cosmetic.cosmetic;
            return obj;
        }

        return null;
    }

    public static GameObject Load(string category, string cosmetic)
    {
        return Resources.Load<GameObject>($"Cosmetics/{category}/{cosmetic}");
    }

    public static CosmeticSlot CosmeticType(string cosmeticName)
    {
        foreach (int i in Enum.GetValues(typeof(CosmeticSlot)))
        {
            CosmeticSlot slot = (CosmeticSlot)i;
            if (Load(slot.ToString(), cosmeticName) != null)
                return slot;
        }

        return CosmeticSlot.Head;
    }

    /// <summary>
    /// Returns all cosmetic prefab names found under Resources/Cosmetics/{slotName}/.
    /// Used by the unlock-all testing bypass.
    /// </summary>
    public static List<string> GetAllCosmeticNamesForSlot(CosmeticSlot slot)
    {
        List<string> names = new List<string>();
        string path = $"Cosmetics/{slot}";
        GameObject[] prefabs = Resources.LoadAll<GameObject>(path);
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.name == "DEFAULT") continue;
            names.Add(prefab.name);
        }
        return names;
    }
}
}
