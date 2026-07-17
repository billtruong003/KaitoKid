using System.Collections;
using UnityEngine;

namespace Shmackle.Utilities
{
    public class AutoDeactivate : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private float _delay = 2.0f;
        [SerializeField, Tooltip("If unassigned, target will be the attached game object")]
        private GameObject _target;
        [SerializeField, Tooltip("If true, it will reactivate the target if the attached gameobject is also activated")]
        private bool _autoEnableCustomTarget = true;

        #endregion

        #region Private Fields

        private WaitForSeconds _hideDelayWaiter;
        private Coroutine _hideCoroutine;

        #endregion

       #region Private Methods

        private void Awake()
        {
            if (_target == null)
            {
                _target = gameObject;
            }
            if (_delay > 0)
            {
                _hideDelayWaiter = new WaitForSeconds(_delay);
            }
        }

        private void OnEnable()
        {
            if (_autoEnableCustomTarget && _target != gameObject)
            {
                _target.SetActive(true);
            }
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
            }
            _hideCoroutine = StartCoroutine(Disable());
        }

        private IEnumerator Disable()
        {
            if (_hideDelayWaiter != null)
            {
                yield return _hideDelayWaiter;
            }
            _target.SetActive(false);
        }

        #endregion
    }
}
