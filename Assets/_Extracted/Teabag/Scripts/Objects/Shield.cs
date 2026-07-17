using Teabag.Player;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
public class Shield : Grabbable
{
    [Header("Shield")]
    public Health health;

    [Header("Shield Visual")]
    public ParticleSystem breakParticles;
    //public Renderer shieldMaterial;

    [Header("ShieldAudio")]
    public List<AdvancedAudioClip> shieldHitSound;
    //public AdvancedAudioClip shieldBreakSound;

    private IAudioService _audioService;

    protected override void Awake()
    {
        base.Awake();
        _audioService = ServiceLocator.Get<IAudioService>();
    }

    private void OnEnable()
    {
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.RegisterShield(this);

        health.onHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (ServiceLocator.TryGet<IDamageableRegistry>(out var damageableRegistry))
            damageableRegistry.UnregisterShield(this);

        health.onHealthChanged -= OnHealthChanged;
    }

    public void OnHealthChanged(byte health, byte shield)
    {
        if (health > 0)
            _audioService.Play(shieldHitSound, transform.position);
    }

    public void HitShield(byte bulletDamage)
    {
        health.Damage(bulletDamage);
    }

    public override void FixedUpdateRoyale()
    {
        base.FixedUpdateRoyale();

        if (health.CurrentHealthAmount < 1)
        {
            if (HasStateAuthority)
                Runner.Despawn(Object);
            else if (Object.StateAuthority == null && Runner.IsSharedModeMasterClient)
            {
                RequestStateAuthority();
            }
        }
    }

    public override void DespawnedRoyale(NetworkRunner runner, bool hasState)
    {
        base.DespawnedRoyale(runner, hasState);
        //AudioManager.Play(shieldBreakSound, transform.position);
        CarryVelocity(breakParticles);
        breakParticles.transform.parent = null;
        breakParticles.gameObject.SetActive(true);
    }

    public void CarryVelocity(ParticleSystem particleSystem)
    {
        var velocity = particleSystem.velocityOverLifetime;
        velocity.xMultiplier = rigidbody.Rigidbody.linearVelocity.x;
        velocity.yMultiplier = rigidbody.Rigidbody.linearVelocity.y;
        velocity.zMultiplier = rigidbody.Rigidbody.linearVelocity.z;
    }
}
}
