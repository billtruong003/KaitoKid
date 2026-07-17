using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.UI
{
    public class PerkNodeController : MonoBehaviour
    {
        [SerializeField] private string perkID;
        [SerializeField] private TextMeshProUGUI lbPerkName;
        [SerializeField] private TextMeshProUGUI lbPerkCost;
        [SerializeField] private PerkNodeController[] requests;
        [SerializeField] private PerkNodeController[] nexts;
        [SerializeField] private LineRenderer[] lines;
        [SerializeField] private PerkMenuController perkMenu;

        public string PerkID => perkID;
        public PerkNodeController[] Requests => requests;
        public PerkNodeController[] Nexts => nexts;

        [SerializeField] MeshRenderer techForm;
        [SerializeField] Material unlockedMat;
        [SerializeField] Material availableMat;
        [SerializeField] Material lackMat;
        [SerializeField] Material lockMat;

        public PerkStatus CurrentPerkStatus { private set; get; }
        IPerkService _perkService;

        private void Awake()
        {
            _perkService = ServiceLocator.Get<IPerkService>();
        }

        private void Start()
        {
            BasePerkDataObject dataBase = _perkService.GetPerkDataObject(perkID);

            if (dataBase == null)
            {
                lbPerkName.SetText("-");
                lbPerkCost.SetText("-");
                return;
            }

            lbPerkName.SetText(dataBase.NameDisplay);
            if(CurrentPerkStatus == PerkStatus.Owned)
            {
                lbPerkCost.SetText("-");
                return;
            }
            lbPerkCost.SetText(dataBase.Cost.ToString());
        }

        public void SetStatus(PerkStatus perkStatus)
        {
            CurrentPerkStatus = perkStatus;
            switch (CurrentPerkStatus)
            {
                case PerkStatus.Locked:
                    {
                        techForm.sharedMaterial = lockMat;
                        break;
                    }
                case PerkStatus.Not_Enough_Cash:
                    {
                        techForm.sharedMaterial = lackMat;
                        break;
                    }
                case PerkStatus.Unlockable:
                    {
                        techForm.sharedMaterial = availableMat;
                        break;
                    }
                case PerkStatus.Owned:
                    {
                        techForm.sharedMaterial = unlockedMat;
                        lbPerkCost.SetText("-");
                        break;
                    }
            }
        }

        public void SetUnlockPerk()
        {
            if (CurrentPerkStatus != PerkStatus.Unlockable)
            {
                return;
            }

            SetStatus(PerkStatus.Owned);
            perkMenu.RefreshAllTree();
        }

        public void CheckPerkStatus()
        {
            if (CurrentPerkStatus == PerkStatus.Owned || _perkService == null)
            {
                return;
            }

            for (int i = 0; i < Requests.Length; i++)
            {
                if (Requests[i].CurrentPerkStatus != PerkStatus.Owned)
                {
                    SetStatus(PerkStatus.Locked);
                    return;
                }
            }

            BasePerkDataObject dataBase = _perkService.GetPerkDataObject(perkID);
            if (PlayerData.currency < dataBase.Cost)
            {
                SetStatus(PerkStatus.Not_Enough_Cash);
                return;
            }
            SetStatus(PerkStatus.Unlockable);
        }

        public void HandleOnClick_UnlockOrEquipPerk()
        {
            if (CurrentPerkStatus != PerkStatus.Owned)
            {
                UnlockPerk();
            }
            else
            {
                EquipPerk();
            }
        }

        private void UnlockPerk()
        {
            if (CurrentPerkStatus != PerkStatus.Unlockable)
            {
                return;
            }

            BasePerkDataObject dataBase = _perkService.GetPerkDataObject(perkID);
            if (PlayerData.currency < dataBase.Cost)
            {
                return;
            }
            PlayerData.ChangeCurrency(-dataBase.Cost);
            PlayerData.AddNewPerk(perkID);
            SetUnlockPerk();
            _perkService.SavePerk();
        }

        private void EquipPerk()
        {
            if (PerkEquipMenuController.EquippedPerkIndex < 0)
            {
                return;
            }
            PlayerData.EquipPerk(perkID, PerkEquipMenuController.EquippedPerkIndex);
            PerkEquipMenuController.OnUpdateEquipPerk?.Invoke();
            GameServices.OnUpdatePerkEquipEvent?.Invoke();
            _perkService.SavePerkEquipped();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (requests == null || lines == null)
            {
                return;
            }

            if (requests.Length == 0 || lines.Length == 0)
            {
                return;
            }

            for (int i = 0; i < requests.Length; i++)
            {
                if (i >= lines.Length)
                {
                    continue;
                }

                Vector3 _pointA = lines[i].transform.InverseTransformPoint(transform.position);
                Vector3 _pointB = lines[i].transform.InverseTransformPoint(requests[i].transform.position);

                lines[i].SetPosition(0, _pointA);
                lines[i].SetPosition(1, _pointB);
            }
        }
#endif
    }
}
