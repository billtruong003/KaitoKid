using System;
using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using Shmackle.Data.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DripPackUIMapping
{
    public DripPack pack;
    public TMP_Dropdown dropdown;
}

public sealed class DripTestController : MonoBehaviour
{
    // ============================================================================================
    // ===== DEPENDENCIES (ASSIGN IN INSPECTOR) ===================================================
    // ============================================================================================

    [Header("Core Dependencies")]
    [SerializeField]
    private ShmackleNetworkRig playerNetworkRig;

    [SerializeField]
    private DripDataContainer dripDataContainer;

    [Header("UI Mappings")]
    [Tooltip("Map each DripPack to its corresponding UI Dropdown here.")]
    [SerializeField]
    private List<DripPackUIMapping> uiMappings;

    [Header("UI Effect Controls")]
    [SerializeField]
    private Toggle freezeToggle;

    [SerializeField]
    private Toggle bloodJmanToggle;

    [SerializeField]
    private Toggle gertToggle;

    // ============================================================================================
    // ===== PRIVATE STATE ========================================================================
    // ============================================================================================

    private DripManager _dripManager;
    private Dictionary<DripPack, DripData> _currentlyEquipped = new();
    private Dictionary<DripPack, TMP_Dropdown> _dropdownMap = new();

    // ============================================================================================
    // ===== UNITY LIFECYCLE ======================================================================
    // ============================================================================================

    private void Start()
    {
        InitializeDependencies();
        BuildDropdownMap();
        PopulateAllDropdowns();
        SetupEventListeners();
    }

    // ============================================================================================
    // ===== INITIALIZATION =======================================================================
    // ============================================================================================

    private void InitializeDependencies()
    {
        if (playerNetworkRig == null || dripDataContainer == null)
        {
            Debug.LogError("Critical dependencies are not assigned in the inspector.");
            enabled = false;
            return;
        }
        _dripManager = playerNetworkRig.dripManager;
    }

    private void BuildDropdownMap()
    {
        _dropdownMap = uiMappings.Where(mapping => mapping.dropdown != null)
                                 .ToDictionary(mapping => mapping.pack, mapping => mapping.dropdown);
    }

    private void PopulateAllDropdowns()
    {
        foreach (var kvp in _dropdownMap)
        {
            PopulateDropdown(kvp.Value, kvp.Key);
        }
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, DripPack pack)
    {
        dropdown.ClearOptions();

        var options = new List<TMP_Dropdown.OptionData> { new("None") };

        var filteredDrips = dripDataContainer.collection
            .Where(drip => drip.pack == pack)
            .Select(drip => new TMP_Dropdown.OptionData(drip.name)) // Sửa lỗi CS7036
            .ToList();

        options.AddRange(filteredDrips);
        dropdown.AddOptions(options);
    }

    private void SetupEventListeners()
    {
        foreach (var kvp in _dropdownMap)
        {
            DripPack pack = kvp.Key;
            kvp.Value.onValueChanged.AddListener(index => OnDripSelectionChanged(pack, index));
        }

        freezeToggle.onValueChanged.AddListener(SetFreezeState);
        bloodJmanToggle.onValueChanged.AddListener(SetBloodJmanState);
        gertToggle.onValueChanged.AddListener(SetGertState);
    }

    // ============================================================================================
    // ===== UI EVENT HANDLERS ====================================================================
    // ============================================================================================

    private void OnDripSelectionChanged(DripPack pack, int selectionIndex)
    {
        UnequipSlot(pack);

        if (selectionIndex == 0) return;

        string selectedDripName = _dropdownMap[pack].options[selectionIndex].text;
        DripData dripToEquip = dripDataContainer.collection
            .FirstOrDefault(d => d.pack == pack && d.name == selectedDripName);

        if (dripToEquip != null)
        {
            EquipDrip(dripToEquip);
        }
    }

    public void EquipRandomDrip()
    {
        _dripManager.DebugSetRandomDrip();
        UpdateAllDropdownsToReflectManagerState();
    }

    public void UnequipAll()
    {
        var equippedPacks = _currentlyEquipped.Keys.ToList();
        foreach (var pack in equippedPacks)
        {
            UnequipSlot(pack);
            if (_dropdownMap.TryGetValue(pack, out var dropdown))
            {
                dropdown.SetValueWithoutNotify(0);
            }
        }
    }

    // ============================================================================================
    // ===== CORE LOGIC ===========================================================================
    // ============================================================================================

    private void EquipDrip(DripData data)
    {
        if (data == null) return;

        bool success = _dripManager.EquipDrip(data);
        if (success)
        {
            _currentlyEquipped[data.pack] = data;
        }

        UpdateAllDropdownsToReflectManagerState();
    }

    private void UnequipSlot(DripPack pack)
    {
        if (_currentlyEquipped.TryGetValue(pack, out DripData equippedDrip) && equippedDrip != null)
        {
            _dripManager.UnequipDrip(equippedDrip);
            _currentlyEquipped[pack] = null;
        }
    }

    private void UpdateAllDropdownsToReflectManagerState()
    {
        var equippedDripsFromManager = _dripManager.DripPackMap
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var kvp in _dropdownMap)
        {
            if (equippedDripsFromManager.TryGetValue(kvp.Key, out var equippedDripData))
            {
                int index = kvp.Value.options.FindIndex(opt => opt.text == equippedDripData.name);
                kvp.Value.SetValueWithoutNotify(Mathf.Max(0, index));
            }
            else
            {
                kvp.Value.SetValueWithoutNotify(0);
            }
        }

        _currentlyEquipped = new Dictionary<DripPack, DripData>(equippedDripsFromManager);
    }

    // ============================================================================================
    // ===== EFFECT CONTROLLERS ===================================================================
    // ============================================================================================

    private void SetFreezeState(bool isFrozen)
    {
        playerNetworkRig.EffectHub.ApplyLocalFreezeEffect(isFrozen, 2f);
    }

    private void SetBloodJmanState(bool isMonster)
    {
        _dripManager.SetVisibleAndMonster(true, isMonster, true);
    }

    private void SetGertState(bool isGert)
    {
        _dripManager.SetGertMaterialStatus(isGert);
        if (isGert)
        {
            _dripManager.SetGertMaterialProperty(1f);
        }
    }
}