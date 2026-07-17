using System;
using UnityEngine;
using UnityEngine.Events;

namespace Stratton.Core.UI
{
    /// <summary>
    /// Allows one child gameobject to be active, enabling a simple way to handle panel changing.
    /// </summary>
    public class UISwitcher : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject[] _children;
        [SerializeField]
        private GameObject _defaultActiveWidget;
        [SerializeField]
        private bool _activateDefaultWidgetOnStart = true;

        #endregion

        #region Public Fields

        public Action<GameObject> OnSelectWidget;

        #endregion

        #region Properties

        public GameObject ActiveWidget
        {
            get;
            private set;
        }

        public GameObject DefaultActiveWidget => _defaultActiveWidget;

        #endregion

        #region Private Methods

        private void Start()
        {
            UpdateChildren();
            if (_activateDefaultWidgetOnStart)
            {
                ActivateDefaultWidget();
            }
        }

        private void OnValidate()
        {
            UpdateChildren();
            ActivateDefaultWidget();
        }

        private void UpdateChildren()
        {
            int count = transform.childCount;
            _children = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                _children[i] = transform.GetChild(i).gameObject;
            }
        }

        #endregion

        #region Public Methods

        public void ActivateDefaultWidget()
        {
            if (_defaultActiveWidget == null)
            {
                if (_children.Length > 0)
                {
                    _defaultActiveWidget = _children[0];
                }
            }
            SetActiveWidgetByRef(_defaultActiveWidget);
        }

        public void SetActiveWidgetByRef(GameObject toActivateWidget)
        {
            if(ActiveWidget == toActivateWidget)
            {
                return;
            }
            ActiveWidget = null;
            for(int i = 0; i <  _children.Length; i++)
            {
                bool isActive = toActivateWidget == _children[i];
                _children[i].SetActive(isActive);
                if(isActive)
                {
                    ActiveWidget = toActivateWidget;
                }
            }
            if(!ActiveWidget)
            {
                Log.Error(BaseLogChannel.UI, $"Invalid widget to activate. Default widget is activated");
                if(_defaultActiveWidget == toActivateWidget)
                {
                    _defaultActiveWidget = null; // if default active widget assigned is invalid, reset the reference
                }
                ActivateDefaultWidget();
            }
            OnSelectWidget?.Invoke(ActiveWidget);
        }

        public void SetActiveWidgetByIndex(int toActivateChildIndex)
        {
            if (toActivateChildIndex >= 0 && toActivateChildIndex < _children.Length)
            {
                SetActiveWidgetByRef(_children[toActivateChildIndex]);
            }
            else
            {
                Log.Error(BaseLogChannel.UI, $"Invalid child index [{toActivateChildIndex}] to activate. Default widget is activated");
                ActivateDefaultWidget();
            }
        }

        #endregion
    }
}