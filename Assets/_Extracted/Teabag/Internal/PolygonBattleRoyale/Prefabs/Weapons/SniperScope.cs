using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Gameplay;

public class SniperScope : MonoBehaviour
{
    public Firearm sniper;
    public GameObject scope;
    public GameObject aimSign;
    public Camera cam;
    public Renderer scopeDisplay;
    public float maxDistance = 0.25f;
    [Range(-1, 1)] public float facing = 0.7f;
    public float minFOV = 5f;
    public float maxFOV = 15f;
    public float minSign = 0.01f;
    public float maxSign = 0.05f;
    RenderTexture texture;

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
        texture = new RenderTexture(cam.targetTexture);
        cam.targetTexture = texture;
        scopeDisplay.material.mainTexture = texture;
    }

    private void Update()
    {
        if (Time.frameCount % 2 == 0)
            return;

        if (sniper.grabber == null)
            scope.SetActive(false);
        else
        {
            var rig = LocalHardwareRig;
            if (rig == null) { scope.SetActive(false); return; }
            Vector3 playerPosition = rig.Headset.Position;
            float dot = Vector3.Dot(transform.forward, (transform.position - playerPosition).normalized);
            float distance = Vector3.Distance(transform.position, playerPosition);
            bool inFront = dot >= facing;
            bool isActive = sniper.grabber.isMine && sniper.grabber.hand != null && distance < maxDistance && inFront;

            if (scope.activeSelf != isActive)
            {
                scope.SetActive(isActive);
            }

            if (cam.gameObject.activeSelf != isActive)
            {
                cam.gameObject.SetActive(isActive);
            }

            if(aimSign.gameObject.activeSelf != isActive)
            {
                aimSign.SetActive(isActive);
            }

            if (isActive)
            {
                float amount = distance / maxDistance;
                cam.fieldOfView = Mathf.Clamp(amount * maxFOV, minFOV, maxFOV);
                aimSign.transform.localScale = Vector3.one * Mathf.Clamp(amount * maxSign, minSign, maxSign);
            }
        }
    }

    private void OnDestroy()
    {
        texture.Release();
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
