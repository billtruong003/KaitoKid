using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Fusion;

using Teabag.Networking;
using Teabag.Player.Rig;

using Cysharp.Threading.Tasks;
using System;
using Teabag.Player;
using Teabag.Core;

namespace Teabag.Gameplay
{
    public class RoyaleObject : RoyaleNetworkBehaviour
    {
        [NonSerialized] public int prefabIndex;
        [NonSerialized] public bool followNetworkPosition = true;

        [Networked, OnChangedRender(nameof(OnNetworkPositionChanged))]
        public RoyalePosition networkPosition { get; set; }

        public virtual void Awake()
        {

        }

        public void OnDestroy()
        {
            NetworkObjectsManager.OnJoined -= OnPlayerJoined;
        }

        public override void SpawnedRoyale()
        {
            base.SpawnedRoyale();

            NetworkObjectsManager.OnJoined += OnPlayerJoined;
            ApplyNetworkPosition();
        }

        public override void DespawnedRoyale(NetworkRunner runner, bool hasState)
        {
            base.DespawnedRoyale(runner, hasState);
            NetworkObjectsManager.OnJoined -= OnPlayerJoined;
        }

        public virtual void OnPlayerJoined(PlayerRef player)
        {

        }

        public void OnNetworkPositionChanged()
        {
            ApplyNetworkPosition();
        }

        /// <summary>
        /// Applies the replicated transform immediately so proxy instances spawn at the synced position instead of the origin.
        /// </summary>
        private void ApplyNetworkPosition()
        {
            if (!followNetworkPosition)
                return;

            RoyalePosition position = networkPosition;
            transform.position = position.position;
            transform.rotation = position.rotation;
        }

    }

    public struct RoyalePosition : INetworkStruct
    {
        public Vector3 position;
        public Quaternion rotation;

        public RoyalePosition(Transform Transform)
        {
            position = Transform.position;
            rotation = Transform.rotation;
        }

        public RoyalePosition(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }
}
