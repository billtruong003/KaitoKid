using Teabag.Core;
using TMPro;
using UnityEngine;
using Squido.JungleXRKit.Core;

namespace Teabag.UI
{
    public sealed class PlayerPerkController : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _textMeshPro;
        [SerializeField] private int _slotIndex;

        IPerkService _perkService;

        private void Start()
        {
            _perkService = ServiceLocator.Get<IPerkService>();
            GameServices.OnUpdatePerkEquipEvent += UpdatePerk;
            UpdatePerk();
        }

        private void OnDestroy()
        {
            GameServices.OnUpdatePerkEquipEvent -= UpdatePerk;
        }

        private void UpdatePerk()
        {
            if (PlayerData.perkEquip.Count <= _slotIndex || _perkService == null)
            {
                _textMeshPro.SetText("-");
                return;
            }

            var perkId = _perkService.GetPerkDataObject(PlayerData.perkEquip[_slotIndex]);
            if (perkId == null)
            {
                _textMeshPro.SetText("-");
                return;
            }
            _textMeshPro.SetText(perkId.NameDisplay);
        }
    }
}
