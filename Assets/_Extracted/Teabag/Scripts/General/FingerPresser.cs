using Teabag.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Player;
using Teabag.UI;
using Teabag.Core;

public class FingerPresser : MonoBehaviour
{
    public bool isLeftHand;

    //private void FixedUpdate() => Physics.SyncTransforms();

    private void OnTriggerEnter(Collider other)
    {
        if (Computer.loading)
            return;

        GorillaButton button = other.GetComponentInParent<GorillaButton>();
        if (button != null)
        {
            // Block normal buttons if a modal is open, but ALWAYS allow ModalButtons to be pressed
            if (ModalService.IsInputBlocked && !(button is ModalButton))
                return;

            if (button.Press())
                VRInputHandler.VibrateController(isLeftHand, 0.1f, 0.1f);
        }
    }
}
