using System.Collections.Generic;
using Teabag.Core;
using TMPro;
using UnityEngine;
using Teabag.Economy;

namespace Teabag.Economy
{
    public sealed class ConsumableMachineItem : MonoBehaviour
    {
        [SerializeField] private DataViewer _dataViewer;
        [SerializeField] private TMP_Text _priceLabel;

        private string _consumableName;
        private IConsumableContainer _container;

        public void Initialise(string name, IConsumableContainer container)
        {
            _consumableName = name;
            _container = container;
            Render();
        }

        public void Buy()
        {
            if (_container != null)
            {
                _ = _container.BuyConsumable(_consumableName);
            }
        }

        public void Render()
        {
            if (_dataViewer == null || _container == null) return;

            // Fetch information from catalog via container to get price
            var catalogItem = _container.GetCatalogItem(_consumableName);
            string priceText = "";
            if (catalogItem != null && catalogItem.VirtualCurrencyPrices.ContainsKey("BA"))
            {
                priceText = catalogItem.VirtualCurrencyPrices["BA"].ToString();
            }

            if (_priceLabel != null)
            {
                _priceLabel.text = priceText;
            }

            _dataViewer.Show(new Dictionary<string, string>()
            {
                { "CONSUMABLE", _consumableName },
                { "AMOUNT", "1" }
            });
        }
    }
}
