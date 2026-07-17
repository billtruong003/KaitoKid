using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArmDisplayBattleRoyale : MonoBehaviour
{
    private float localMaxShield { get; set; }
    private float localMaxHealth { get; set; }
    private float localHealthLeft { get; set; }
    private float localShieldLeft { get; set; }
    private float localFuelRatio { get; set; }
    private int localTimeLeft { get; set; }
    private int localPlayersLeft { get; set; }

    private PerkTreeDataObject[] localPerk { get; set; }

    [SerializeField] private TMP_Text playersLeftDisplay;
    [SerializeField] private TMP_Text timeLeftDisplay;
    [SerializeField] private Transform fuelArrow;
    [SerializeField] private Image healthBar;
    [SerializeField] private Image shieldBar;
    [SerializeField] private Image[] perkDisplayIcons;
    [SerializeField] private TMP_Text[] perkDisplayNames;
    [SerializeField] private TMP_Text currencyDisplay;
    [SerializeField] private Sprite perkEmptyIcon;

    private List<string> localEquipedPerks = new List<string>(3);
    private Gorilla _localGorilla { get => ServiceLocator.Get<IGorillaService>().LocalGorilla as Gorilla; }

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            var rigInfoService = ServiceLocator.Get<IRigInfoService>();
            return rigInfoService?.HardwareRig;
        }
    }

    private PerkService _perkService => ServiceLocator.Get<IPerkService>() as PerkService;
    private BattleRoyaleManager _gameInstance => BattleRoyaleManager.instance;
    private IZoneService _zoneService;

    private void OnEnable()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_localGorilla == null) return;

        _zoneService = ServiceLocator.Get<IZoneService>();

        if (_localGorilla?.health != null)
        {
            localHealthLeft = _localGorilla.health.TotalHealth;
            localMaxHealth = _localGorilla.health.MaxHealth;
            localShieldLeft = _localGorilla.health.CurrentShieldAmount;
            localMaxShield = _localGorilla.health.MaxShield;
            _localGorilla.health.onHealthChanged += OnHealthShieldChanged;
        }

        if (LocalHardwareRig != null)
        {
            LocalHardwareRig.LocomotionController.GetLocomotionModule<JetpackLocomotion>(out JetpackLocomotion jetpackLocomotion);
            if(jetpackLocomotion != null)
            {
                localFuelRatio = jetpackLocomotion.FuelRatio;
                jetpackLocomotion.OnFuelChange += OnFuelChanged;
            }
        }
        
        if (_gameInstance != null)
        {
            _gameInstance.OnPlayerAmountChanged += OnPlayersRemainingChanged;
        }

        if (_perkService != null)
        {
            _perkService.OnItemsChanged += UpdateEquipedPerks;
        }

        UpdatePerksUI();
        UpdateFuelUI();
        UpdateHealthShieldUI();
        UpdatePerksUI();
        UpdatePlayersLeft();
        UpdateTimeLeft();
    }

    private void OnDisable()
    {
        if (_localGorilla == null) return;

        if (_localGorilla.health != null)
        {
            _localGorilla.health.onHealthChanged -= OnHealthShieldChanged;
        }

        if (LocalHardwareRig != null)
        {
            LocalHardwareRig.LocomotionController.GetLocomotionModule<JetpackLocomotion>(out JetpackLocomotion jetpackLocomotion);
            if (jetpackLocomotion != null)
            {
                jetpackLocomotion.OnFuelChange -= OnFuelChanged;
            }
        }

        if (_gameInstance != null)
        {
            _gameInstance.OnPlayerAmountChanged -= OnPlayersRemainingChanged;
        }

        if (_perkService != null)
        {
            _perkService.OnItemsChanged -= UpdateEquipedPerks;
        }
    }

    private void OnFuelChanged(float ratio)
    {
        localFuelRatio = ratio;
        UpdateFuelUI();
    }

    private void OnHealthShieldChanged(byte health, byte shield)
    {
        localHealthLeft = health;
        localShieldLeft = shield;
        UpdateHealthShieldUI();
    }

    private void OnPlayersRemainingChanged(int amount)
    {
        localPlayersLeft = amount;
        UpdatePlayersLeft();
    }

    private void UpdateHealthShieldUI()
    {
        healthBar.fillAmount = localHealthLeft / localMaxHealth;
        shieldBar.fillAmount = localShieldLeft / localMaxShield;
    }
    private void UpdateFuelUI()
    {
        if(fuelArrow != null)
        {
            float angle = Mathf.Lerp(92, -92, localFuelRatio);
            fuelArrow.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void UpdatePlayersLeft()
    {
        playersLeftDisplay.text = localPlayersLeft.ToString();
    }

    private void FixedUpdate()
    {
        int timeLeft = Mathf.CeilToInt(_zoneService?.GetTotalZoneTimeSeconds() ?? 0f);
        if (timeLeft != localTimeLeft)
        {
            localTimeLeft = timeLeft;
            UpdateTimeLeft();
        }
    }

    private void UpdateTimeLeft()
    {
        int totalSeconds = Mathf.Max(0, localTimeLeft);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timeLeftDisplay.text = $"{minutes}:{seconds:D2}";
    }

    private void UpdateEquipedPerks()
    {
        if (_perkService == null)
            return;

        localEquipedPerks.Clear();
        localEquipedPerks = PlayerData.perkEquip;
        UpdatePerksUI();
    }

    private void UpdatePerksUI()
    {
        for (int i = 0; i < localEquipedPerks.Count; i++)
        {
            BasePerkDataObject obj = _perkService.GetPerkDataObject(localEquipedPerks[i]);
            if (i < perkDisplayIcons.Length)
            {
                perkDisplayIcons[i].sprite = obj != null? obj.Icon : perkEmptyIcon;
            }
            if (i < perkDisplayNames.Length)
            {
                perkDisplayNames[i].text = obj != null ? obj.NameDisplay : "";
            }
        }
    }
}
