using System;
using UnityEngine;

namespace Shmackle.Utilities
{
    public class AutoFollowXZ : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private bool _lockLocalYOffset = true;

        #endregion
        
        #region Private Fields

        private float _localYOffset;
        
        #endregion

        private void Awake()
        {
            if (_target != null)
            {
                _localYOffset = transform.position.y -  _target.position.y;
            }
        }

        #region Private Methods

        private void LateUpdate()
        {
            if (_target != null)
            {
                Vector3 position = _target.position;
                if (_lockLocalYOffset)
                {
                    position.y += _localYOffset;
                }
                else
                {
                    position.y = transform.position.y;
                }
                transform.position = position;
            }
        }

        #endregion
    }
}
