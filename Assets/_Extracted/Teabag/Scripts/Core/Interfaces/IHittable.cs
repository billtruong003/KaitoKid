using Fusion;
using UnityEngine;

namespace Teabag.Core
{
    public interface IHittable
    {
        void OnHit(byte damage, float bulletSpeed, RaycastHit hit, Vector3 source, PlayerRef? killer = null);
    }
}
