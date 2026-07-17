using UnityEngine;
using UnityEngine.UI;

namespace Shmackle.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Toggle))]
    public class UIToggleSpriteOverride : MonoBehaviour
    {
        #region Serialized Field

        [SerializeField]
        private Image _targetGraphic;
        [SerializeField]
        private Sprite _activeSprite;
        [SerializeField]
        private Sprite _inactiveSprite;

        #endregion

        #region Private Field

        private Toggle _toggle;

        #endregion

        #region Private Field

        private void Awake()
        {
            Setup();
        }

        private void OnEnable()
        {
            Setup();
            UpdateSprite(_toggle.isOn);
            _toggle.onValueChanged.AddListener(UpdateSprite);
        }

        private void OnDisable()
        {
            if (_toggle != null)
                _toggle.onValueChanged.RemoveListener(UpdateSprite);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Setup();
            if (_toggle != null)
                UpdateSprite(_toggle.isOn);
        }
#endif

        private void Setup()
        {
            if (_toggle == null)
                _toggle = GetComponent<Toggle>();

            // Remove default graphic functionality
            if (_toggle != null)
                _toggle.graphic = null;
        }

        private void UpdateSprite(bool isOn)
        {
            if (_targetGraphic == null) return;

            _targetGraphic.sprite = isOn ? _activeSprite : _inactiveSprite;
        }

        #endregion
    }
}
