using Shmackle.Player;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    public class UIPlayerPush : MonoBehaviour
    {
        #region Serialized Field

        [SerializeField]
        private PushController _pushController;

        [SerializeField]
        private Transform _chargeRootTransform;
        [SerializeField]
        private Transform _cooldownRootTransform;
        
        [SerializeField]
        private Slider _chargeSlider;

        [SerializeField]
        private Image _cooldownFillImage;

        #endregion
        
        #region Private Fields

        private bool _lastCooldownState = false;
        
        #endregion
        
        #region Private Methods

        private void Awake()
        {
            if (!_pushController)
            {
                _pushController = transform.root.GetComponentInChildren<PushController>();
            }

            _pushController.ChargeChanged += OnChargeChanged;
            if (!_chargeRootTransform)
            {
                _chargeRootTransform = _chargeSlider.transform;
            }
            if (!_cooldownRootTransform)
            {
                _cooldownRootTransform = _cooldownFillImage.transform;
            }
            
            _chargeRootTransform.gameObject.SetActive(false);
            _cooldownRootTransform.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_pushController)
            {
                _pushController.ChargeChanged -= OnChargeChanged;
            }
        }

        private void OnChargeChanged(int charge)
        {
            _chargeRootTransform.gameObject.SetActive(charge >= 0);
        }

        private void Update()
        {
            if (_pushController)
            {
                if (_pushController.Charge >= 0)
                {
                    _chargeSlider.value = _pushController.ChargePercent;
                }

                if (_pushController.CooldownPercent < 1.0f)
                {
                    if (!_lastCooldownState)
                    {
                        _cooldownRootTransform.gameObject.SetActive(true);
                    }
                    _lastCooldownState = true;
                    _cooldownFillImage.fillAmount = _pushController.CooldownPercent;
                }
                else
                {
                    if (_lastCooldownState)
                    {
                        _cooldownRootTransform.gameObject.SetActive(false);
                    }
                    _lastCooldownState = false;
                }
            }
        }

        #endregion
    }
}
