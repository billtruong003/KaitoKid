#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mythfall.Characters;

namespace Mythfall.EditorTools
{
    /// <summary>
    /// One-shot Sprint 1 setup. Generates pre-filled CharacterDataSO assets for Kai + Lyra
    /// in Resources/Characters/. Idempotent — overrides existing assets with canonical values.
    ///
    /// Run via: Tools → Mythfall → Sprint 1 — Create Character Data
    /// </summary>
    public static class Sprint1Setup
    {
        const string CharactersFolder = "Assets/Mythfall/Resources/Characters";
        const string KaiAssetPath = CharactersFolder + "/Kai_Data.asset";
        const string LyraAssetPath = CharactersFolder + "/Lyra_Data.asset";

        [MenuItem("Tools/Mythfall/Sprint 1 — Create Character Data")]
        public static void CreateCharacterData()
        {
            EnsureFolder(CharactersFolder);

            var kai = LoadOrCreate<CharacterDataSO>(KaiAssetPath);
            ConfigureKai(kai);
            EditorUtility.SetDirty(kai);

            var lyra = LoadOrCreate<CharacterDataSO>(LyraAssetPath);
            ConfigureLyra(lyra);
            EditorUtility.SetDirty(lyra);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Sprint1Setup] Kai_Data.asset + Lyra_Data.asset written to Resources/Characters/. " +
                      "Verify in Inspector. Assign portrait/icon/prefab manually when art is ready.");
        }

        static void ConfigureKai(CharacterDataSO so)
        {
            so.characterId = "kai";
            so.nameKey = "character.kai.name";
            so.titleKey = "character.kai.title";
            so.loreKey = "character.kai.lore";
            so.starTier = 4;
            so.role = CombatRole.Melee;

            so.baseStats = new CharacterBaseStats
            {
                maxHP = 120f,
                attackPower = 15f,
                defense = 8f,
                moveSpeed = 5f,
                attackRange = 1.8f,
                attackInterval = 0.6f,
                critRate = 15f,
                critDamage = 180f,
                cooldownReduction = 0f,
                lifesteal = 0f,
                aoeRadius = 0f,
            };
        }

        static void ConfigureLyra(CharacterDataSO so)
        {
            so.characterId = "lyra";
            so.nameKey = "character.lyra.name";
            so.titleKey = "character.lyra.title";
            so.loreKey = "character.lyra.lore";
            so.starTier = 4;
            so.role = CombatRole.Ranged;

            so.baseStats = new CharacterBaseStats
            {
                maxHP = 80f,
                attackPower = 20f,
                defense = 4f,
                moveSpeed = 5f,
                attackRange = 10f,
                attackInterval = 1.0f,
                critRate = 20f,
                critDamage = 200f,
                cooldownReduction = 0f,
                lifesteal = 0f,
                aoeRadius = 0f,
            };
        }

        // -------------------------------------------------------------------

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
