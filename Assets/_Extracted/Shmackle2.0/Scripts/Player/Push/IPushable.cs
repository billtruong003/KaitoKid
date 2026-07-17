using Fusion;
using UnityEngine;

namespace Shmackle.Player
{
    public interface IPushable
    {
        void ReceivePush(NetworkObject source, Vector3 direction);
        Rigidbody Rigidbody => null;
    }
}