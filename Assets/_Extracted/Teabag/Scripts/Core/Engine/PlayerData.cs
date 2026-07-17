using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Teabag.Authentication;
using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Lightweight bridge exposing the local player's runtime identity.
    /// Populated by AuthenticationManager (Assembly-CSharp); consumed by named assemblies.
    /// </summary>
    public static class PlayerData
    {
        public static string displayName;
        public static string playFabId;
        public static List<CatalogItem> catalogItems = new List<CatalogItem>();
        
        // Auth bridges — wired by AuthenticationManager/AuthenticationUtils from Assembly-CSharp
        public static event Action OnLogin;
        public static Func<UniTask<string>> RequestFusionTokenAsync;
        public static Action<CosmeticSlot, string> SetCosmetic;
        public static List<InventoryItem> inventory = new List<InventoryItem>();

        // Phase 2 bridges — for Authentication/ and API/ folders moving to Core
        public static bool initialised;
        public static bool loggedIn;
        public static int currency;
        public static Dictionary<string, string> titleData = new Dictionary<string, string>();
        public static Dictionary<string, int> perks = new Dictionary<string, int>();
        public static List<string> perkEquip = new List<string>(3);

        /// <summary>
        /// Cached first-login timestamp fetched from PlayFab UserData.
        /// Null until <see cref="Teabag.Economy.BundleTimerService"/> resolves it.
        /// </summary>
        public static DateTime? firstLoginTimestamp;
        public static Func<string> GetClientSessionTicket;

        public static void NotifyLogin() => OnLogin?.Invoke();

        public static void ChangeCurrency(int changeValue)
        {
            currency = Mathf.Max(0, currency + changeValue);
        }

        public static void AddNewPerk(string id)
        {
            if (perks.ContainsKey(id))
            {
                return;
            }
            perks.Add(id, 1);
        }

        public static void EditPerk(string id, int level)
        {
            if (!perks.ContainsKey(id))
            {
                return;
            }
            perks[id] = Mathf.Max(0, level);
        }

        public static void EquipPerk(string id, int index)
        {
            if (index >= perkEquip.Count)
            {
                return;
            }

            if (perkEquip.Contains(id))
            {
                for (int i = 0; i < perkEquip.Count; i++)
                {
                    if (perkEquip[i] == id)
                    {
                        perkEquip[i] = "";
                        perkEquip[index] = id;
                        return;
                    }
                }
            }

            perkEquip[index] = id;
        }

        public static void UnEquipPerk(int index)
        {
            if (index >= perkEquip.Count)
            {
                return;
            }
            perkEquip[index] = "";
        }
    }

    public static class CatalogItemExtensions
    {
        /// <summary>
        /// Finds a catalog item by display name. Returns null if not found.
        /// Mirrors AuthenticationUtils.GetItem so Player.asmdef scripts don't need Assembly-CSharp.
        /// </summary>
        public static CatalogItem GetItem(this List<CatalogItem> items, string name)
        {
            if (items == null) return null;
            foreach (CatalogItem item in items)
                if (item.DisplayName == name) return item;
            return null;
        }
    }
}
