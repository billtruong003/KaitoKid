using System.Collections.Generic;
using UnityEngine;
using Squido.JungleXRKit.Core;
using Teabag.Authentication;
using Teabag.Core;
using TMPro;
using UnityEngine.UI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class ArmDisplayLobby : MonoBehaviour
{
    private float localCurrency { get; set; }
    private static readonly ILogger _logger = JungleXRLogger.GetLogger();
    private PerkService _perkService => ServiceLocator.Get<IPerkService>() as PerkService;
    private List<string> localEquipedPerks = new List<string>(3);
    [SerializeField] private Image[] perkDisplayIcons;
    [SerializeField] private TMP_Text[] perkDisplayNames;
    [SerializeField] private TMP_Text currencyDisplay;
    [SerializeField] private Sprite perkEmptyIcon;

    private void OnEnable()
    {
        AuthenticationUtils.CurrencyChanged += UpdateCurrencyUI;
        if(_perkService != null)
        {
            _perkService.OnItemsChanged += UpdateEquipedPerks;
        }
        UpdateCurrencyUI();
        UpdatePerksUI();
    }
    private void OnDisable()
    {
        AuthenticationUtils.CurrencyChanged -= UpdateCurrencyUI;
        if (_perkService != null)
        {
            _perkService.OnItemsChanged -= UpdateEquipedPerks;
        }
    }
    private void Update()
    {
        //temp until economy refactor
        if(localCurrency != AuthenticationUtils.currency)
        {
            localCurrency = AuthenticationUtils.currency;
            UpdateCurrencyUI();
        }
    }
    private void UpdatePerksUI()
    {
        for (int i = 0; i < localEquipedPerks.Count; i++)
        {
            BasePerkDataObject obj = _perkService.GetPerkDataObject(localEquipedPerks[i]);
            if (i < perkDisplayIcons.Length)
            {
                perkDisplayIcons[i].sprite = obj != null ? obj.Icon : perkEmptyIcon;
            }
            if (i < perkDisplayNames.Length)
            {
                perkDisplayNames[i].text = obj != null ? obj.NameDisplay : "";
            }
        }
    }

    private void UpdateEquipedPerks()
    {
        if (_perkService == null)
            return;

        localEquipedPerks.Clear();
        localEquipedPerks = PlayerData.perkEquip;
        UpdatePerksUI();
    }

    private void UpdateCurrencyUI()
    {
        //_logger.LogInformation("Currency: " + localCurrency);
        currencyDisplay.text = localCurrency.ToString();
    }
}
