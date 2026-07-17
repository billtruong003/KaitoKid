using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using Teabag.Player;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
public class Grenade : Weapon, IHittable
{
    [Header("Weapon Data")]
    [SerializeField] private ThrowableData weaponData;

    [Header("References")]
    public AudioSource audioSource;

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public int state { get; set; }

    [Header("Model")]
    public GameObject pin;
    public Transform safety;

    bool sentExplosion;
    private IGorillaService _gorillaService;
    private IAudioService _audioService;
    private static readonly WaitForSeconds _waitExplosionCountdown = new WaitForSeconds(7.5f);

    protected override void Awake()
    {
        base.Awake();
        WaitForInitialize();
    }

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private async UniTaskVoid WaitForInitialize()
    {
        _audioService = await ServiceLocator.WaitForServiceAsync<IAudioService>();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (interacting)
        {
            float f = VRInputHandler.GetInputDownAmount(hand.isLeftHand, InputType.Trigger);
            safety.localRotation = Quaternion.Euler(f * -4, 0, 0);
            if (f > 0.1f)
            {
                if (state < 1)
                {
                    state = 1;
                }
            }
            if (f >= 1)
            {
                if (state < 2)
                {
                    VRInputHandler.VibrateController(hand.isLeftHand, 0.2f, 0.1f);
                    state = 2;
                }
            }
        }

        // TODO: Bridge BananaBlimp fan physics (wind zone force on grenade rigidbody)
        // Original: if in blimp, apply fan wind force to grenade rigidbody
    }

    void OnStateChanged()
    {
        switch (state)
        {
            case 0:
                break;
            case 1:
                //Debug.Log("Throwing pin");
                pin.SetActive(false);
                _audioService.Play(weaponData.armClip, transform.position);
                break;
            case 2:
                //Debug.Log("Throwing safety! This thing is gonna explode!");
                pin.SetActive(false);

                audioSource.Play();
                safety.gameObject.SetActive(false);
                StartCoroutine(ExplosionCountdown());
                break;
            case 3:
                if (Object.HasStateAuthority)
                {
                    Runner.Despawn(Object);
                }
                break;
            default:
                break;
        }
    }

    public override void OnRelease(Grabber holster)
    {
        base.OnRelease(holster);
        rigidbody.Rigidbody.linearVelocity *= 2;
        _audioService.Play(weaponData.throwClip, transform.position);
    }


    IEnumerator ExplosionCountdown()
    {
        yield return _waitExplosionCountdown;

        if (Object.HasStateAuthority)
        {
            Explode();
        }
    }

    int scores = 0;

    public void OnHit(byte damage, float bulletSpeed, RaycastHit hit, Vector3 source, PlayerRef? killer = null)
    {
        Explode();
    }


    public void Explode()
    {
        if (sentExplosion)
            return;

        sentExplosion = true;

        if (weaponData.doDamage)
        {
            Damage();
        }

        RPCDestroy();
    }

    public void Damage()
    {
        if (!ServiceLocator.TryGet<IExplosionDamageService>(out var explosionDamageService))
            return;

        explosionDamageService.ApplyExplosion(
            this,
            Object.StateAuthority,
            transform.position,
            position => (byte)(Grabbable.CalculateDamage(transform.position, position) * 1.5f),
            position => Grabbable.CalculateDamage(transform.position, position),
            (component, position) => Grabbable.HitsComponent(component, transform.position, position, 7.5f),
            () => scores++);

        ScoreScores();
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPCDestroy()
    {
        state = 3;
    }

    public override void DespawnedRoyale(NetworkRunner runner, bool hasState)
    {
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.UnregisterGrenade(this);

        base.DespawnedRoyale(runner, hasState);
        if (state < 2)
            return;

        GameObject effect = PoolObject.Get(weaponData.explosionPrefab, transform.position, Quaternion.identity);
        if (!effect.TryGetComponent<PoolAutoReturn>(out _))
        {
            effect.AddComponent<PoolAutoReturn>().delay = 4f;
        }

        var grenadeLocal = _gorillaService?.LocalGorilla as Gorilla;
        if (grenadeLocal == null)
            return;

        if (HitsComponent(grenadeLocal, transform.position, grenadeLocal.headTransform.position) && weaponData.doDamage)
        {
            LocalHardwareRig.LocomotionController.PlayerRigidbody.AddExplosionForce(250, transform.position - Vector3.down, 50);
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.RegisterGrenade(this);

        // Reset state when spawned from pool
        state = 0;
        sentExplosion = false;
        scores = 0;

        // Reset visual state
        if (pin != null) pin.SetActive(true);
        if (safety != null) safety.gameObject.SetActive(true);
    }

    public async UniTaskVoid ScoreScores()
    {
        while (scores > 0)
        {
            if (GameServices.ScoreChallengeAsync != null)
                await GameServices.ScoreChallengeAsync(5); // 5 = KillGrenade
            scores--;
        }
    }
}
}
