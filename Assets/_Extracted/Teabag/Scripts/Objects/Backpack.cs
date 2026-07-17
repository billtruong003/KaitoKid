using Teabag.Networking;
using Teabag.Player;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using Teabag.Core;

namespace Teabag.Gameplay
{
    public class Backpack : Grabbable
    {
        public static Backpack myBackpack;
        public bool infiniteAmmo;
        public Map mapPrefab;
        Map map;
        public Grabber mapGrabber;
        public Grabber modGrabber;

        [Header("Inventory")]
        public List<NonGrabbableItem> nonGrabbables = new List<NonGrabbableItem>();
        public List<NonGrabbableBackpackItem> items = new List<NonGrabbableBackpackItem>();

        private INetworkManager _networkManager;
        private IGorillaService _gorillaService;

        public delegate void OnNonGrabbableAdded(NonGrabbableBackpackItem item);
        public OnNonGrabbableAdded DelegateAmmoAdded;

        public override void Spawned()
        {
            base.Spawned();
            takeStateOnGrab = false;

            _networkManager = ServiceLocator.Get<INetworkManager>();
            _gorillaService = ServiceLocator.Get<IGorillaService>();

            if (!HasStateAuthority)
            {
                canGrab = string.IsNullOrEmpty(_networkManager.CurrentGameMode) || _networkManager.IsShop;
            }
            else
            {
                myBackpack = this;
                GameServices.AddBackpackAmmo = (typeName, amount) =>
                {
                    if (myBackpack is object)
                        myBackpack.AddNonGrabbable(new NonGrabbableBackpackItem { name = typeName, amount = amount });
                };
            }
        }

        public override bool CanGrab(Grabber holster)
        {
            if (holster.hand == null)
                return false;

            return base.CanGrab(holster);
        }

        public override bool CanHolsterPreview(Grabber holster)
        {
            return false;
        }

        async UniTaskVoid SpawnMap()
        {
            var result = Runner.Spawn(mapPrefab);
            map = result.GetComponent<Map>();
            await UniTask.Yield();
            mapGrabber.Grab(map);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (HasStateAuthority)
            {
                if (!Runner.IsSceneManagerBusy && _networkManager.IsBattleRoyale)
                {
                    var bpLocal = _gorillaService?.LocalGorilla as Gorilla;
                    if (bpLocal?.health != null)
                    {
                        if (!bpLocal.health.isDead)
                        {
                            if (map == null)
                                SpawnMap();
                        }
                        else if (map != null && map.Object != null && map.Object.IsValid)
                        {
                            // The map can already be despawned during end-of-round cleanup, so guard the direct despawn.
                            if (Runner != null && !Runner.IsShutdown && map.Object != null && map.Object.IsValid)
                                Runner.Despawn(map.Object);

                            map = null;
                        }
                    }
                }
            }
        }

        public override void Render()
        {
            base.Render();
            if (map == null)
                return;

            if (grabber != null)
            {
                map.canGrab = grabber.isMine;
            }
            else if (mapGrabber.grabbable == map)
                map.canGrab = false;
        }

        public void Die()
        {
            foreach (NonGrabbableBackpackItem item in items)
            {
                Spawn(item);
            }

            ResetBackpack();
        }

        public void ResetBackpack()
        {
            items = new List<NonGrabbableBackpackItem>();
        }

        public void Spawn(NonGrabbableBackpackItem item)
        {
            foreach (NonGrabbableItem i in nonGrabbables)
            {
                if (i.name == item.name)
                {
                    NonGrabbableItem obj = _networkManager.Runner.Spawn(i, transform.position + Vector3.up, Quaternion.identity);
                    obj.item = item;
                }
            }
        }

        public void AddNonGrabbable(NonGrabbableBackpackItem item)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].localName == item.localName)
                {
                    NonGrabbableBackpackItem it = items[i];
                    it.amount += item.amount;
                    items[i] = it;
                    DelegateAmmoAdded?.Invoke(item);
                    return;
                }
            }
            items.Add(item);
            DelegateAmmoAdded?.Invoke(item);
        }

        public NonGrabbableBackpackItem GetNonGrabbable(string name)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].name == name)
                {
                    return items[i];
                }
            }

            return new NonGrabbableBackpackItem()
            {
                name = name
            };
        }

        public int UseNonGrabbable(string name, int amount)
        {
            if (infiniteAmmo || (GameServices.IsModEnabled?.Invoke("Infinite Ammo") ?? false))
                return amount;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].name == name)
                {
                    if (items[i].amount - amount > 0)
                    {
                        NonGrabbableBackpackItem it = items[i];
                        it.amount -= amount;
                        items[i] = it;
                        return amount;
                    }
                    else
                    {
                        NonGrabbableBackpackItem it = items[i];
                        int originalAmount = it.amount;
                        it.amount -= amount;
                        it.amount = Mathf.Clamp(it.amount, 0, int.MaxValue);
                        items[i] = it;

                        return originalAmount;
                    }
                }
            }

            return 0;
        }
    }
}
