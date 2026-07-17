using System.Collections.Generic;

namespace Mythfall.Inventory
{
    /// <summary>
    /// Persistable player profile. Round-tripped via Bill.Save (JsonUtility).
    /// Pure POCO — all manipulation goes through InventoryService.
    ///
    /// Schema versioned: future field additions bump CURRENT_VERSION and
    /// InventoryService.MigrateData rewrites old saves before any caller reads them.
    ///
    /// JsonUtility limitation: Dictionary&lt;,&gt; not supported. Wrap in a serializable
    /// list-of-pairs when characterStarLevels lands in Sprint 5+.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public const int CURRENT_VERSION = 1;

        public int schemaVersion = CURRENT_VERSION;

        public List<string> ownedCharacterIds = new() { "kai", "lyra" };
        public string currentCharacterId = "kai";
        public string preferredLanguage = "vi";

        // Reserved schema slots — populated in future sprints. Keep commented so old
        // saves round-trip cleanly without unrecognized fields once these go live.
        // public int crystal = 0;                 // Sprint 6+: gacha currency
        // public int gold = 0;                    // Sprint 6+: shop currency
        // public List<CharacterStarEntry> stars;  // Sprint 5+: ascension (wrap dict in serializable list)
    }
}
