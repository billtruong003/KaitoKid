using UnityEngine;
using UnityEngine.Events;

namespace Stratton.Core.UI
{
    public class UISwitcherListener : MonoBehaviour
    {

        #region Serialized Fields

        [SerializeField]
        private GameObject _listenedWidget;
        [SerializeField, Tooltip("If true, the opposite state of the listened widget will be used on delegate invoke.")]
        private bool _invertValue = false;
        [SerializeField]
        private UnityEvent<bool> _selectedStatusChanged;
        [SerializeField, Tooltip("If unassigned, it will look at the parent component of the listened widget on runtime.")]
        private UISwitcher _widgetSwitcher;

        #endregion

        #region Private Fields

        private bool _isSelected = false;

        #endregion

        #region Private Methods

        private void Awake()
        {
            if (_listenedWidget != null)
            {
                if (_widgetSwitcher == null)
                {
                    _widgetSwitcher = _listenedWidget.GetComponentInParent<UISwitcher>();
                }
                if (_widgetSwitcher != null)
                {
                    if (_widgetSwitcher.DefaultActiveWidget)
                    {
                        _isSelected = _widgetSwitcher.DefaultActiveWidget == _listenedWidget;
                        InvokeSelectedStatusChangedEvent();
                    }
                    _widgetSwitcher.OnSelectWidget += OnSelectWidget;
                }
            }
        }

        private void OnSelectWidget(GameObject selectedWidget)
        {
            bool newSelected = selectedWidget == _listenedWidget;
            if (_isSelected != newSelected)
            {
                _isSelected = newSelected;
                InvokeSelectedStatusChangedEvent();
            }
        }

        private void InvokeSelectedStatusChangedEvent()
        {
            _selectedStatusChanged.Invoke(_invertValue ? !_isSelected : _isSelected);
        }

        #endregion

    }
}
