using System;
using Fusion;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;

namespace Teabag.Gameplay
{
    public sealed class ExplosionDamageService : IExplosionDamageService
    {
        private readonly IGorillaService _gorillaService;
        private readonly IDamageableRegistry _damageableRegistry;

        public ExplosionDamageService(IGorillaService gorillaService, IDamageableRegistry damageableRegistry)
        {
            _gorillaService = gorillaService;
            _damageableRegistry = damageableRegistry;
        }

        public void ApplyExplosion(
            Grabbable source,
            PlayerRef attacker,
            Vector3 explosionPosition,
            Func<Vector3, byte> calculateCharacterDamage,
            Func<Vector3, byte> calculateShieldDamage,
            Func<Component, Vector3, bool> hitsComponent,
            Action onGorillaKilled = null)
        {
            var gorillas = _gorillaService?.Gorillas;
            if (gorillas != null)
            {
                for (int i = 0; i < gorillas.Count; i++)
                {
                    if (gorillas[i] is not Gorilla gorilla)
                        continue;

                    byte damage = calculateCharacterDamage(gorilla.headTransform.position);
                    if (damage <= 0 || !hitsComponent(gorilla, gorilla.headTransform.position))
                        continue;

                    if (gorilla.health.Damage(damage, HitType.Normal, attacker, explosionPosition))
                        onGorillaKilled?.Invoke();
                }
            }

            for (int i = _damageableRegistry.Grenades.Count - 1; i >= 0; i--)
            {
                var grenade = _damageableRegistry.Grenades[i];
                if (grenade == null || grenade == source)
                    continue;

                if (hitsComponent(grenade, grenade.transform.position))
                    grenade.Explode();
            }

            for (int i = _damageableRegistry.C4s.Count - 1; i >= 0; i--)
            {
                var c4 = _damageableRegistry.C4s[i];
                if (c4 == null || c4 == source)
                    continue;

                if (hitsComponent(c4, c4.transform.position))
                    c4.Explode();
            }

            for (int i = _damageableRegistry.Shields.Count - 1; i >= 0; i--)
            {
                var shield = _damageableRegistry.Shields[i];
                if (shield == null)
                    continue;

                if (hitsComponent(shield, shield.transform.position))
                    shield.HitShield(calculateShieldDamage(shield.transform.position));
            }

            for (int i = _damageableRegistry.DummyTargets.Count - 1; i >= 0; i--)
            {
                var dummy = _damageableRegistry.DummyTargets[i];
                if (dummy == null || dummy.IsDead)
                    continue;

                byte damage = calculateCharacterDamage(dummy.transform.position);
                if (damage <= 0 || !hitsComponent(dummy, dummy.transform.position))
                    continue;

                dummy.Damage(damage, HitType.Normal);
            }
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }
    }
}
