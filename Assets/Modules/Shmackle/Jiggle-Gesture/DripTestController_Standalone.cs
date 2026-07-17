using System.Collections.Generic;
using System.Linq;
using Shmackle.Data;
using Shmackle.Data.ScriptableObjects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System;

[Serializable]
public class DripPackUIMapping_Standalone
{
    public DripPack pack;
    public TMP_Dropdown dropdown;
}

public sealed class DripTestController_Standalone : MonoBehaviour
{
    // =============================================================================================================
    // ===== SERIALIZED FIELDS =====================================================================================
    // =============================================================================================================

    [Title("Core Dependencies")]
    [Required][SerializeField] private StandaloneDripController standaloneDripController;
    [Required][SerializeField] private DripDataContainer dripDataContainer;

    [Title("UI Mappings")]
    [SerializeField]
    [ListDrawerSettings(IsReadOnly = true)]
    private List<DripPackUIMapping_Standalone> uiMappings;

    [Title("UI Effect Controls")]
    [SerializeField] private Toggle gertToggle;
    [SerializeField] private Toggle bloodJmanToggle;
    [SerializeField] private Toggle freezeToggle;
    [SerializeField] private Toggle dissolveToggle;
    [SerializeField] private TMP_Dropdown outlineDropdown;
    [SerializeField] private TMP_Dropdown xrayDropdown;
    [SerializeField] private Toggle xrayToggle;

    [Title("Manual & Debug Controls")]
    [SerializeField] private TMP_InputField manualDripIdInput;

    // =============================================================================================================
    // ===== INSPECTOR ACTIONS & UI AUTOMATION =====================================================================
    // =============================================================================================================

#if UNITY_EDITOR
    [Title("Automation")]
    [InfoBox("Tự động tạo và map các UI dropdown cho DripPacks.")]
    [Required][SerializeField] private Transform dropdownsParent;
    [Required][AssetsOnly][SerializeField] private TMP_Dropdown dropdownPrefab;

    [Button("Generate & Map UI Dropdowns", ButtonSizes.Large), GUIColor(0.2f, 0.6f, 1.0f)]
    private void GenerateAndMapDropdowns()
    {
        if (dropdownsParent == null || dropdownPrefab == null) return;
        var uniquePacks = dripDataContainer.collection.Select(d => d.pack).Distinct().OrderBy(p => p.ToString()).ToList();
        uniquePacks.ForEach(pack =>
        {
            string name = $"{pack}_Dropdown";
            if (dropdownsParent.Find(name) == null) Instantiate(dropdownPrefab, dropdownsParent).name = name;
        });
        uiMappings.Clear();
        foreach (Transform child in dropdownsParent)
        {
            if (child.TryGetComponent<TMP_Dropdown>(out var dd) && Enum.TryParse<DripPack>(child.name.Replace("_Dropdown", ""), out var pack))
            {
                uiMappings.Add(new DripPackUIMapping_Standalone { pack = pack, dropdown = dd });
            }
        }
        uiMappings = uiMappings.OrderBy(m => m.pack.ToString()).ToList();
    }
#endif

    [Title("Actions")]
    [Button(ButtonSizes.Large), GUIColor(0.9f, 0.7f, 0.2f)]
    public void EquipFromInputField()
    {
        if (manualDripIdInput != null && !string.IsNullOrWhiteSpace(manualDripIdInput.text))
        {
            if (standaloneDripController.EquipDrip(manualDripIdInput.text)) UpdateAllDropdownsToReflectState();
        }
    }

    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.9f, 0.4f)]
    public void EquipRandomDrips()
    {
        standaloneDripController.UnequipAll();
        dripDataContainer.collection.GroupBy(d => d.pack).ToList().ForEach(group =>
        {
            var items = group.ToList();
            if (items.Any()) standaloneDripController.EquipDrip(items[UnityEngine.Random.Range(0, items.Count)].id);
        });
        UpdateAllDropdownsToReflectState();
    }

    [Button(ButtonSizes.Large), GUIColor(1.0f, 0.5f, 0.5f)]
    public void UnequipAll()
    {
        standaloneDripController.UnequipAll();
        _dropdownMap.Values.ToList().ForEach(dd => dd.SetValueWithoutNotify(0));
    }

    // =============================================================================================================
    // ===== UNITY LIFECYCLE & SETUP ===============================================================================
    // =============================================================================================================

    private Dictionary<DripPack, TMP_Dropdown> _dropdownMap = new();

    private void Start()
    {
        InitializeDependencies();
        BuildDropdownMap();
        PopulateAllDropdowns();
        SetupEventListeners();
    }

    private void InitializeDependencies()
    {
        if (standaloneDripController == null || dripDataContainer == null) enabled = false;
    }

    private void BuildDropdownMap()
    {
        _dropdownMap = uiMappings.Where(m => m.dropdown != null).ToDictionary(m => m.pack, m => m.dropdown);
    }

    private void PopulateAllDropdowns()
    {
        foreach (var kvp in _dropdownMap)
        {
            kvp.Value.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData> { new("None") };
            options.AddRange(dripDataContainer.collection.Where(d => d.pack == kvp.Key).Select(d => new TMP_Dropdown.OptionData(d.name)));
            kvp.Value.AddOptions(options);
        }
        PopulateEffectDropdown(outlineDropdown, typeof(StandaloneEffectsController.OutlineType));
        PopulateEffectDropdown(xrayDropdown, typeof(StandaloneEffectsController.XRayType));
    }

    private void PopulateEffectDropdown(TMP_Dropdown dropdown, Type enumType)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        dropdown.AddOptions(Enum.GetNames(enumType).Select(name => new TMP_Dropdown.OptionData(name)).ToList());
    }

    private void SetupEventListeners()
    {
        foreach (var kvp in _dropdownMap)
        {
            kvp.Value.onValueChanged.AddListener(index => OnDripSelectionChanged(kvp.Key, index));
        }

        if (gertToggle != null) gertToggle.onValueChanged.AddListener(SetGertState);
        if (bloodJmanToggle != null) bloodJmanToggle.onValueChanged.AddListener(SetBloodJmanState);
        if (dissolveToggle != null) dissolveToggle.onValueChanged.AddListener(SetDissolveState);

        if (freezeToggle != null) freezeToggle.onValueChanged.AddListener(SetFreezeState);
        if (outlineDropdown != null) outlineDropdown.onValueChanged.AddListener(OnOutlineSelectionChanged);
        if (xrayDropdown != null) xrayDropdown.onValueChanged.AddListener(OnXRaySettingsChanged);
        if (xrayToggle != null) xrayToggle.onValueChanged.AddListener(OnXRaySettingsChanged);
    }

    // =============================================================================================================
    // ===== EVENT HANDLERS & UI LOGIC =============================================================================
    // =============================================================================================================

    private void OnDripSelectionChanged(DripPack pack, int selectionIndex)
    {
        string currentId = standaloneDripController.GetEquippedDripIdForPack(pack);
        if (!string.IsNullOrEmpty(currentId)) standaloneDripController.UnequipDrip(currentId);

        if (selectionIndex > 0)
        {
            string selectedName = _dropdownMap[pack].options[selectionIndex].text;
            var drip = dripDataContainer.collection.FirstOrDefault(d => d.pack == pack && d.name == selectedName);
            if (drip != null) standaloneDripController.EquipDrip(drip.id);
        }
        UpdateAllDropdownsToReflectState();
    }

    private void UpdateAllDropdownsToReflectState()
    {
        foreach (var mapping in uiMappings)
        {
            var equippedId = standaloneDripController.GetEquippedDripIdForPack(mapping.pack);
            int index = 0;
            if (!string.IsNullOrEmpty(equippedId))
            {
                var drip = dripDataContainer.collection.First(d => d.id == equippedId);
                index = mapping.dropdown.options.FindIndex(opt => opt.text == drip.name);
                if (index == -1) index = 0;
            }
            mapping.dropdown.SetValueWithoutNotify(index);
        }
    }

    private void SetGertState(bool isGert) => standaloneDripController.SetGertState(isGert);
    private void SetBloodJmanState(bool isBloodJman) => standaloneDripController.SetBloodJmanState(isBloodJman);
    private void SetFreezeState(bool isFrozen) => standaloneDripController.SetFreezeState(isFrozen);

    // Thay đổi ở đây: không cần truyền duration nữa, vì nó đã được cấu hình trong settings.
    private void SetDissolveState(bool isDissolving) => standaloneDripController.SetGenericDissolve(isDissolving);

    private void OnOutlineSelectionChanged(int index) => standaloneDripController.SetOutlineState((StandaloneEffectsController.OutlineType)index);
    private void OnXRaySettingsChanged(int ignored) => OnXRaySettingsChanged(xrayToggle.isOn); // For dropdown
    private void OnXRaySettingsChanged(bool ignored) // For toggle
    {
        bool isEnabled = xrayToggle != null && xrayToggle.isOn;
        var type = xrayDropdown != null ? (StandaloneEffectsController.XRayType)xrayDropdown.value : default;
        standaloneDripController.SetXRayState(isEnabled, type);
    }
}