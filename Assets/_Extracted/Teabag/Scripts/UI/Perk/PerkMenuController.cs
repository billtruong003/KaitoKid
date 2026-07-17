using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.UI
{
    using GorillaLocomotion;
    public class PerkMenuController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _lbCash;
        [SerializeField] private PerkTreeController[] _perkTrees;

        IGorillaService _gorillaService;
        IPerkService _perkService;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private void Start()
        {
            _perkService = ServiceLocator.Get<IPerkService>();
            _perkService.LoadPerk();
            RefreshAllTree();
            UpdateCash();
        }

        private void Awake()
        {
            ServiceLocator.TryGet(out _gorillaService);
        }

        public void UpdateCash()
        {
            if (LocalHardwareRig == null)
            {
                return;
            }
            _lbCash.SetText(PlayerData.currency.ToString());
        }

        public void ChangePlayerCash(int cashChange)
        {
            PlayerData.currency = Mathf.Max(PlayerData.currency + cashChange, 0);
            UpdateCash();
        }

        public void RefreshAllTree()
        {
            for (int i = 0; i < _perkTrees.Length; i++)
            {
                _perkTrees[i].RefreshPerkTree();
            }
        }
    }
}
