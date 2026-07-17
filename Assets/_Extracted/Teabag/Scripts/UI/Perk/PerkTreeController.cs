using System;
using Teabag.Core;
using UnityEngine;

namespace Teabag.UI
{
    public class PerkTreeController : MonoBehaviour
    {
        [SerializeField] PerkNodeController[] allPerkNode;
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        public void CheckAllPerkIsOwned()
        {
            foreach (var perk in PlayerData.perks)
            {
                for (int i = 0; i < allPerkNode.Length; i++)
                {
                    if (perk.Key != allPerkNode[i].PerkID)
                    {
                        continue;
                    }
                    allPerkNode[i].SetStatus(PerkStatus.Owned);
                }
            }
        }

        public void RefreshPerkTree()
        {
            for (int i = 0; i < allPerkNode.Length; i++)
            {
                allPerkNode[i].SetStatus(PerkStatus.Locked);
            }

            CheckAllPerkIsOwned();
            for (int i = 0; i < allPerkNode.Length; i++)
            {
                allPerkNode[i].CheckPerkStatus();
            }
        }
    }
}
