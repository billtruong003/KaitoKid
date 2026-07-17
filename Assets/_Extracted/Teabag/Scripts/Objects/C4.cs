using Fusion;
using Teabag.Networking;
using Teabag.Player;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using Teabag.Core;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Gameplay;

namespace Teabag.Gameplay
{
public class C4 : Grabbable
{
    public TMP_Text text;
    public AudioSource explosionCountdown;
    public GameObject explosion;
    PlayerRef exploder;
    private IGorillaService _gorillaService;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    bool exploding;
    bool canExplode
    {
        get
        {
            if (grabber != null || rigidbody.Rigidbody.linearVelocity.magnitude > 0.05f)
                return false;

            return true;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
    }
    public bool Explode()
    {
        GameLogger.Debug("Click");

        if (!canExplode)
        {
            GameLogger.Debug("C4 cannot explode right now");
            return false;
        }

        RPCExplode();
        return true;
    }

    public override void Render()
    {
        base.Render();
        if (!exploding)
            text.text = canExplode ? "<color=orange>ARMED" : "<color=green>STABLE";
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public async void RPCExplode(RpcInfo info = default)
    {
        exploding = true;
        text.text = "<color=red>UH OH";
        exploder = info.Source;
        explosionCountdown.Play();

        await UniTask.Delay(Mathf.RoundToInt(explosionCountdown.clip.length * 1000));
        if (HasStateAuthority) Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.UnregisterC4(this);

        base.Despawned(runner, hasState);
        GameObject fx = PoolObject.Get(explosion, transform.position, Quaternion.identity);
        if (!fx.TryGetComponent<PoolAutoReturn>(out _))
        {
            fx.AddComponent<PoolAutoReturn>().delay = 4f;
        }

        if (Runner.LocalPlayer == exploder) HandleExplosion();
        var c4Local = _gorillaService?.LocalGorilla as Gorilla;
        if (c4Local == null)
            return;

        if (HitsComponent(c4Local, c4Local.headTransform.position))
        {
            var rig = LocalHardwareRig;
            if (rig != null)
                rig.LocomotionController.PlayerRigidbody.AddExplosionForce(250, transform.position - Vector3.down, 50);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.RegisterC4(this);

        // Reset state when spawned from pool
        exploding = false;
        exploder = PlayerRef.None;
    }

    public void HandleExplosion()
    {
        if (!ServiceLocator.TryGet<IExplosionDamageService>(out var explosionDamageService))
            return;

        explosionDamageService.ApplyExplosion(
            this,
            exploder,
            transform.position,
            position => (byte)(CalculateDamage(position) * 1.5f),
            position => CalculateDamage(position),
            (component, position) => HitsComponent(component, position, 7.5f));
    }
    public byte CalculateDamage(Vector3 position, float maxDistance = 15)
    {
        float distance = Vector3.Distance(position, transform.position);
        distance = maxDistance - Mathf.Clamp(distance, 0, maxDistance);
        return (byte)(distance * 10);
    }
}
}
