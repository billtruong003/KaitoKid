using Fusion;
using Teabag.Networking;
using Teabag.Player;
using Teabag.Player.Rig;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using Squido.JungleXRKit.Core;
using Teabag.Gameplay;

namespace Teabag.Gameplay
{
public class Healing : Grabbable
{
    public byte healing;
    [Networked]
    public byte healingRemaining { get; set; }
    public byte healingPerSecond;
    public new ParticleSystem particleSystem;
    public AudioSource audioSource;

    private IGorillaService _gorillaService;
    float distanceToHeal = 0.3f;
    DateTime lastHeal;

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority)
            healingRemaining = healing;

        lastHeal = DateTime.UtcNow;
        takeStateOnGrab = true;
    }

    public new void Update()
    {
        base.Update();
        if (!Object) return;

        if (interacting && HasStateAuthority)
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var gorillas = _gorillaService?.Gorillas;
            if (gorillas != null)
            {
                foreach (var gorillaEntry in gorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (!gorilla.health || gorilla.health.isDead) continue;

                    Transform target = gorilla.GetComponentInChildren<HealthBar>(true).transform;
                    if (Vector3.Distance(target.position, grabber.transform.position) < distanceToHeal && gorilla.health.CanHeal())
                    {
                        if (!particleSystem.isPlaying) particleSystem.Play();
                        if (!audioSource.isPlaying) audioSource.Play();
                        if (!((DateTime.UtcNow - lastHeal).TotalSeconds > 1)) return;

                        Give(gorilla);
                        lastHeal = DateTime.UtcNow;
                        return;
                    }
                }
            }
        }
        else if (interacting)
            Object.RequestStateAuthority();

        particleSystem.Stop();
        audioSource.Pause();
    }

    private void Give(Gorilla gorilla)
    {
        if (healingRemaining > 0)
        {
            gorilla.health.Heal(healingPerSecond, false);
            healingRemaining -= healingPerSecond;
            GameServices.DisplayPopupColored?.Invoke("+" + healingPerSecond, transform.position, Color.green, 0.3f);
        }
        else Runner.Despawn(Object);
    }
}
}
