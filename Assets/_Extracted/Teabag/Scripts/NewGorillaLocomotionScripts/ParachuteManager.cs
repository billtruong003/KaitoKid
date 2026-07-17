using System;
using Cysharp.Threading.Tasks;
using Teabag.Player.Rig;
using UnityEngine;
using Teabag.Core;
using Squido.JungleXRKit.Core;
using Teabag.Player;

namespace GorillaLocomotion
{
public class ParachuteManager : MonoBehaviour
{
    public Transform head;
    //public AdvancedAudioClip jumpFanfare;
    public bool forceJump;

    private IGorillaService _gorillaService;

    new Rigidbody rigidbody
    {
        get
        {
            return GetComponent<Rigidbody>();
        }
    }

    private void Start()
    {
        InitializeNetworkAsync();
    }

    private async UniTask InitializeNetworkAsync()
    {
        _gorillaService = await ServiceLocator.WaitForServiceAsync<IGorillaService>();
    }

    private void Update()
    {
        if (_gorillaService == null)
        {
            return;
        }

        var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
        if (localGorilla == null)
            return;

        if ((GameServices.IsModEnabled?.Invoke("Rockets") ?? false))
        {
            localGorilla.parachute.isParachuting = false;
            rigidbody.useGravity = !Swimming.IsInWater;
            return;
        }

        if (!localGorilla.parachute.isParachuting)
        {
            if ((VRInputHandler.GetInputDown(false, InputType.Primary) || VRInputHandler.GetInputDown(true, InputType.Primary) || forceJump) && rigidbody.linearVelocity.y < -6)
            {
                rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z);
                localGorilla.parachute.isParachuting = true;
            }
        }

        Vector3 headDir = head.forward;
        headDir.y = 0;
        Debug.DrawRay(head.position, headDir);

        rigidbody.useGravity = !localGorilla.parachute.isParachuting && !Swimming.IsInWater;

        if (localGorilla.parachute.isParachuting)
        {
            rigidbody.AddForce(headDir * 250 * Time.deltaTime);
            rigidbody.AddForce(Vector3.down * 20 * Time.deltaTime);
            rigidbody.linearVelocity = Vector3.ClampMagnitude(rigidbody.linearVelocity, 10);

            if (VRInputHandler.GetInputDown(false, InputType.Primary) || VRInputHandler.GetInputDown(true, InputType.Primary))
            {
                forceJump = false;
            }

            if (!(VRInputHandler.GetInputDown(false, InputType.Primary) || VRInputHandler.GetInputDown(true, InputType.Primary) || forceJump) || rigidbody.linearVelocity.y > 0)
            {
                localGorilla.parachute.isParachuting = false;
            }
        }
    }
}
}
