using Shmackle.Player.Grab;
using UnityEngine;

namespace Shmackle.Pad
{
    public class ShmacklePadDeactivator : MonoBehaviour
    {
        #region Private Methods

        private void OnTriggerEnter(Collider other)
        {
            ShmackleGrabber grabber = other.GetComponentInParent<ShmackleGrabber>();
            if (grabber != null)
            {
                if (grabber.GrabbedObject != null)
                {
                    ShmacklePad pad = grabber.GrabbedObject.GetComponent<ShmacklePad>();
                    if (pad != null)
                    {
                        pad.Deactivate();
                    }
                }
            }
        }

        #endregion
    }
}