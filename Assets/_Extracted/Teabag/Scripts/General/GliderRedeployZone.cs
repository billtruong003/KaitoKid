using System.Collections;
using System.Collections.Generic;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;

public class GliderRedeployZone : MonoBehaviour
{
    public AudioSource source;
    public ParticleSystem particles;
    public float radius = 0.5f;
    public float height = 10;
    bool once = false;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Update()
    {
        var rig = LocalHardwareRig;
        if (rig == null) return;
        Transform head = rig.Headset.HeadsetTransform;
        float minY = transform.position.y;
        float maxY = transform.position.y + height;
        float y = Mathf.Clamp(head.position.y, minY, maxY);
        Vector3 vector = new Vector3(transform.position.x, y, transform.position.z);
        if (Vector3.Distance(head.position, vector) < radius)
        {
            if (!once)
            {
                if (rig.LocomotionController != null)
                {
                    rig.LocomotionController.GetLocomotionModule<ParachuteLocomotion>(out var module);
                    if (module != null)
                    {
                        module.forceJump = true;
                    }

                    // Apply redeploy forces
                    var rigidbody = rig.LocomotionController.PlayerRigidbody;
                    rigidbody.AddForce(rigidbody.linearVelocity * 10);
                    rigidbody.AddForce(Vector3.up * 2000);
                }

                Debug.Log("Redeploy");

                VRInputHandler.VibrateController(true, 0.4f, 0.2f);
                VRInputHandler.VibrateController(false, 0.4f, 0.2f);

                if (source != null)
                    source.Play();

                if (particles != null)
                    particles.Play();

                once = true;
            }
        }
        else
            once = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        //Gizmos.DrawWireSphere(transform.position, radius);
        //Gizmos.DrawWireSphere(transform.position + Vector3.up * height, radius);
        for (int i = 0; i <= height; i++)
        {
            Gizmos.DrawWireSphere(transform.position + Vector3.up * height * (i / 10f), radius);
        }
        //Gizmos.DrawWireCube(transform.position + Vector3.up * (height / 2), new Vector3(radius, height, radius));
    }
}
