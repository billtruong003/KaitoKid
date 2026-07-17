using PlayFab.ClientModels;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.Economy
{
    public sealed class ConsumableMachine : MonoBehaviour, IConsumableContainer
    {
        [Header("References")]
        [SerializeField] private ConsumableMachineItem _itemPrefab;
        [SerializeField] private Transform _previewContainer;

        [Header("UI Feedback")]
        [SerializeField] private GameObject _loadingObject;
        [SerializeField] private TMP_Text _statusText;

        [Header("Machine Settings")]
        [Tooltip("If empty, loads all consumables in the catalog. Otherwise, only loads items with these Display Names.")]
        [SerializeField] private List<string> _targetConsumableNames = new List<string>();

        private List<ConsumableMachineItem> _cachedPreviews = new List<ConsumableMachineItem>();
        private bool _isBuying = false;

        private const int PURCHASE_FEEDBACK_DELAY_MS = 2000;

        private void Start()
        {
            if (_loadingObject != null) _loadingObject.SetActive(false);
            if (_statusText != null) _statusText.text = "";
            Render();
        }

        public void LoadConsumables()
        {
            List<string> itemsToDisplay = new List<string>();

            // Filter catalog items by name (if targetConsumableNames is configured)
            foreach (var item in ConsumablesManager.catalogItems)
            {
                if (_targetConsumableNames.Count > 0 && !_targetConsumableNames.Contains(item.DisplayName))
                    continue;

                if (!itemsToDisplay.Contains(item.DisplayName))
                {
                    itemsToDisplay.Add(item.DisplayName);
                }
            }

            // Basic Object Pooling for UI reuse
            while (_cachedPreviews.Count < itemsToDisplay.Count)
            {
                Transform parent = _previewContainer != null ? _previewContainer : transform;
                ConsumableMachineItem p = Instantiate(_itemPrefab, parent);
                _cachedPreviews.Add(p);
            }

            for (int i = 0; i < _cachedPreviews.Count; i++)
            {
                if (i < itemsToDisplay.Count)
                {
                    _cachedPreviews[i].gameObject.SetActive(true);
                    _cachedPreviews[i].Initialise(itemsToDisplay[i], this);
                    _cachedPreviews[i].transform.SetAsLastSibling();
                }
                else
                {
                    _cachedPreviews[i].gameObject.SetActive(false);
                }
            }
        }

        public void Render()
        {
            LoadConsumables();
        }

        public async UniTask BuyConsumable(string name)
        {
            if (_isBuying) return;
            _isBuying = true;

            try
            {
                if (_loadingObject != null) _loadingObject.SetActive(true);
                if (_statusText != null) _statusText.text = $"BUYING {name}...";

                await UniTask.Delay(PURCHASE_FEEDBACK_DELAY_MS);

                var result = await ConsumablesManager.BuyConsumable(name);

                if (_statusText != null)
                {
                    _statusText.text = result.Message;
                }

                await UniTask.Delay(PURCHASE_FEEDBACK_DELAY_MS);
            }
            finally
            {
                _isBuying = false;
                if (_loadingObject != null) _loadingObject.SetActive(false);
                if (_statusText != null) _statusText.text = "";
            }
        }

        public CatalogItem GetCatalogItem(string name)
        {
            return Teabag.Authentication.AuthenticationUtils.catalogItems.GetItem(name);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Camera.main == null)
                {
                    GameLogger.Error("No Main Camera found for debug raycast!");
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                // Use RaycastAll with a very long distance and hitting triggers
                RaycastHit[] hits = Physics.RaycastAll(ray, 100f, Physics.AllLayers, QueryTriggerInteraction.Collide);

                if (hits.Length == 0)
                {
                    GameLogger.Info("Debug Raycast: No objects hit.");
                }

                foreach (var hit in hits)
                {
                    GameLogger.Info($"Debug Raycast hit: {hit.collider.name} on layer {hit.collider.gameObject.layer}");

                    // Look for DefaultButton on the hit object or its parents
                    var button = hit.collider.GetComponentInParent<DefaultButton>();
                    if (button != null)
                    {
                        GameLogger.Info($"Success! Pressing button: {button.name}");
                        button.Press();
                        break;
                    }
                }
            }
        }
#endif
    }
}
