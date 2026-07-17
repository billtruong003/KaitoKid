using GorillaLocomotion;
using Squido.JungleXRKit.Core;
using System;
using System.Collections.Generic;
using Teabag.Core;
using UnityEngine;

namespace Teabag.UI
{
    public sealed class PerkEquipMenuController : MonoBehaviour
    {
        public static int EquippedPerkIndex { private set; get; } = -1;
        public static Action OnUpdateEquipPerk;

        [SerializeField] PerkEquipController[] perkSlots;
        IPerkService _perkService;

        private void OnEnable()
        {
            EquippedPerkIndex = -1;
            UpdateAllPerkEquipped();
            for (int i = 0; i < perkSlots.Length; i++)
            {
                perkSlots[i].SetSelect(i == EquippedPerkIndex);
            }
        }

        private void Awake()
        {
            _perkService = ServiceLocator.Get<IPerkService>();
            _perkService.LoadPerkEquipped();
            UpdateAllPerkEquipped();
            if (PlayerData.perkEquip.Count == 0)
            {
                for (int i = 0; i < perkSlots.Length; i++)
                {
                    PlayerData.perkEquip.Add("");
                }
            }
        }

        private void Start()
        {
            OnUpdateEquipPerk += UpdateAllPerkEquipped;
        }

        private void OnDestroy()
        {
            OnUpdateEquipPerk -= UpdateAllPerkEquipped;
        }

        public void SetShowHidePanel(bool isShow)
        {
            gameObject.SetActive(isShow);
        }

        public void HandleOnClick_SelectEquipSlot(int slotIndex)
        {
            EquippedPerkIndex = slotIndex;
            for (int i = 0; i < perkSlots.Length; i++)
            {
                perkSlots[i].SetSelect(i == slotIndex);
            }
        }

        public void UpdateAllPerkEquipped()
        {
            int index = -1;
            foreach (var id in PlayerData.perkEquip)
            {
                index += 1;
                if (id == "")
                {
                    perkSlots[index].UpdatePerk("-");
                    continue;
                }

                BasePerkDataObject data = ServiceLocator.Get<IPerkService>().GetPerkDataObject(id);
                if (data == null)
                {
                    perkSlots[index].UpdatePerk("-");
                    continue;
                }

                perkSlots[index].UpdatePerk(data.NameDisplay);
            }
        }
    }
}
