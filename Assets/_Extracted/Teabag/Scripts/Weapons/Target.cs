using Fusion;
using Teabag.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Teabag.Gameplay
{
    public class Target : MonoBehaviour, IHittable
    {
        public UnityEvent onHit;

        public void Hit()
        {
            onHit.Invoke();
        }

        public void OnHit(byte damage, float bulletSpeed, RaycastHit hit, Vector3 source, PlayerRef? killer = null)
        {
            Hit();
        }
    }
}
