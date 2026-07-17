using Fusion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using UnityEngine;
using Teabag.Player;

public class RocketHand : MonoBehaviour
{
    public GorillaHandAnimator animator;
    NetworkObject obj;
    public ParticleSystem particles;
    public ParticleSystem secondaryParticles;
    public AudioSource audioSource;
    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }
    private  INetworkManager _networkManager;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private void Awake()
    {
        obj = GetComponentInParent<NetworkObject>();
    }

    void Update()
    {
        if (!NetworkManager.CurrentRoom.IsModded)
            return;

        if (obj == null)
            return;

        if (!obj.IsValid)
            return;

        var emission = particles.emission;
        var secondaryEmission = secondaryParticles.emission;
        emission.enabled = animator.thumb;
        secondaryEmission.enabled = animator.thumb;
        if (animator.thumb && !audioSource.isPlaying)
            audioSource.Play();
        else if (!animator.thumb && audioSource.isPlaying)
            audioSource.Pause();

        if (animator.thumb && obj.HasStateAuthority)
        {
            var rig = LocalHardwareRig;
            rig?.LocomotionController?.PlayerRigidbody?.AddForce(transform.forward * Time.deltaTime * 500);
        }

    }

    /*
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
    */
}
