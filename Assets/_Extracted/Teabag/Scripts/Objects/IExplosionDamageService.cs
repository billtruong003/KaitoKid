using System;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;

namespace Teabag.Gameplay
{
    public interface IExplosionDamageService : IService
    {
        void ApplyExplosion(
            Grabbable source,
            PlayerRef attacker,
            Vector3 explosionPosition,
            Func<Vector3, byte> calculateCharacterDamage,
            Func<Vector3, byte> calculateShieldDamage,
            Func<Component, Vector3, bool> hitsComponent,
            Action onGorillaKilled = null);
    }
}
