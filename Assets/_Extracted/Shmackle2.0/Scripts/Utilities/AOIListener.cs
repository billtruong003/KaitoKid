using Fusion.XR.Shared.Core;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace Shmackle.Utilities
{
    public class AOIListener : NetworkBehaviour, IInterestEnter, IInterestExit
    {
        #region Serialize Field

        [SerializeField]
        private UnityEvent _interestEnter;
        [SerializeField]
        private UnityEvent _interestExit;

        #endregion

        #region IInterestEnter

        public void InterestEnter(PlayerRef player)
        {
            _interestEnter.Invoke();
        }

        #endregion

        #region IInterestExit

        public void InterestExit(PlayerRef player)
        {
            _interestExit.Invoke();
        }

        #endregion
    }
}
