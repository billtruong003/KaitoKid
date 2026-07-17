using Teabag.Networking;
using Teabag.Player;
using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
public class Map : Grabbable
{
    public static Map myMap;

    public MapPlayer player;
    public new Renderer renderer;
    public Renderer mapClosed;

    [Header("UI")]
    public DataViewer dataViewer;
    public GameObject canvas;
    public Transform panel;

    [Header("Puff")]
    public ParticleSystem system;
    public AdvancedAudioClip clip;

    Action<object> teamSwitchedWrapper;
    Action<Texture2D> mapUpdatedWrapper;
    Action<IGorilla> gorillaSpawnedWrapper;
    private IGorillaService _gorillaService;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }

    private INetworkManager _networkManager;

#if UNITY_EDITOR
    private bool m_EditorHeld;
#endif


    private IMapService _mapService;
    private readonly Dictionary<string, string> _renderData = new(2);

    protected override void Awake()

    {
        base.Awake();
        _mapService = ServiceLocator.Get<IMapService>();
        teamSwitchedWrapper = (obj) => MapPlayers(obj as Gorilla);
        mapUpdatedWrapper = OnMapUpdated;
        gorillaSpawnedWrapper = (ig) => MapPlayers(ig as Gorilla);
        GameServices.OnTeamSwitched += teamSwitchedWrapper;
        if (_mapService != null) _mapService.OnMapUpdated += mapUpdatedWrapper;
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        if (_gorillaService != null) _gorillaService.OnGorillaSpawned += gorillaSpawnedWrapper;
        GorillaHealth.OnGorillaDied += OnGorillaDied;
    }

    public new void Update()
    {
        base.Update();
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            m_EditorHeld = !m_EditorHeld;
            if (m_EditorHeld)
            {
                OnGrab(null);
                GameLogger.Debug("Map Editor Debug: Summoned map");
            }
            else
            {
                GameLogger.Debug("Map Editor Debug: Released map");
            }
        }
#endif
    }

    private void OnDestroy()
    {
        GameServices.OnTeamSwitched -= teamSwitchedWrapper;
        if (_mapService != null) _mapService.OnMapUpdated -= mapUpdatedWrapper;
        if (_gorillaService != null) _gorillaService.OnGorillaSpawned -= gorillaSpawnedWrapper;
        GorillaHealth.OnGorillaDied -= OnGorillaDied;

        if (myMap == this)
            myMap = null;
    }

    public void OnGorillaDied(GorillaHealth gorillaHealth)
    {
        MapPlayers();
    }

    public override void OnGrab(Grabber holster)
    {
        base.OnGrab(holster);
        if (_mapService != null && _mapService.MapTexture == null)
        {
            _mapService.TakeMapPicture?.Invoke();
        }

        if (_mapService != null && _mapService.MapTexture != null)
        {
            OnMapUpdated(_mapService.MapTexture);
        }
    }

    public void OnMapUpdated(Texture2D texture)
    {
        renderer.sharedMaterials[1].mainTexture = texture;
    }

    public void MapPlayers(Gorilla g = default)
    {
        if (_mapService != null && _mapService.MapTexture != null)
            renderer.sharedMaterials[1].mainTexture = _mapService.MapTexture;
        foreach (MapPlayer mapPlayer in panel.GetComponentsInChildren<MapPlayer>())
        {
            var poolObj = mapPlayer.GetComponent<PoolObject>();
            if (poolObj != null)
            {
                poolObj.Return();
            }
            else if (_mapService != null && ServiceLocator.TryGet<IPoolService>(out var poolService))
            {
                poolService.Release(mapPlayer.gameObject);
            }
            else
            {
                Destroy(mapPlayer.gameObject);
            }
        }

        var gorillas = _gorillaService?.Gorillas;
        if (gorillas != null)
        {
            foreach (var gorillaEntry in gorillas)
            {
                var gorilla = (Gorilla)gorillaEntry;
                if (GameServices.SharesTeam?.Invoke(gorilla) ?? true)
                {
                    MapPlayer mapPlayer = PoolObject.Get(player.gameObject, panel.position, panel.rotation).GetComponent<MapPlayer>();
                    mapPlayer.transform.SetParent(panel, false);
                    mapPlayer.gorilla = gorilla;
                }
            }
        }
    }

    public override void SpawnedRoyale()
    {
        base.SpawnedRoyale();

        if (Object.HasStateAuthority)
        {
            if (myMap != null && myMap != this && myMap.Object != null)
            {
                NetworkManager.Runner.Despawn(myMap.Object);
            }

            myMap = this;
        }
        else
            canGrab = false;
    }

    public override bool CanGrab(Grabber holster)
    {
        if (holster.hand == null)
        {
            if (holster.GetComponentInParent<Backpack>() == null)
                return false;
        }

        return base.CanGrab(holster);
    }

    public override bool CanHolsterPreview(Grabber holster)
    {
        if (holster.hand == null)
        {
            if (holster.GetComponentInParent<Backpack>() == null)
                return false;
        }

        return base.CanHolsterPreview(holster);
    }

    public override void Render()
    {
        base.Render();
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;

        lastInteractTime = DateTime.UtcNow;

        var renderRig = LocalHardwareRig;
        if (renderRig == null) return;
        Vector3 headPos = renderRig.Headset.Position;

        if (grabber != null)
        {
            int aliveCount = 0;
            var renderGorillas = _gorillaService?.Gorillas;
            if (renderGorillas != null)
            {
                foreach (var gorillaEntry in renderGorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (gorilla.health != null)
                    {
                        if (!gorilla.health.isDead)
                            aliveCount++;
                    }
                }
            }

            if (dataViewer != null)
            {
                _renderData["LEFT"] = aliveCount.ToString();
                _renderData["KILLS"] = (GameServices.GetKillCount?.Invoke() ?? 0).ToString();
                dataViewer.Show(_renderData);
            }

            var mapGrabber = Backpack.myBackpack != null ? Backpack.myBackpack.mapGrabber : null;
            if (grabber.hand == null && mapGrabber != null && mapGrabber.Object != null && mapGrabber.Object.IsValid && mapGrabber.grabbable != this && Object.HasStateAuthority)
            {
                Store();
                grabber.Release();
            }
        }
        else
        {
#if UNITY_EDITOR
            if (m_EditorHeld) return; // Don't auto-store if we're debugging in Editor
#endif
            if ((transform.position - headPos).sqrMagnitude > 4f && Object.HasStateAuthority)
            {
                Store();
            }
        }
    }

    public override void UpdatePosition()
    {
        base.UpdatePosition();
        bool showMap = true;

#if UNITY_EDITOR
        if (m_EditorHeld)
        {
            IHardwareRig editorRig = null;
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
            {
                editorRig = rigInfo.HardwareRig;
            }

            if (editorRig == null)
            {
                return;
            }

            Transform cam = GameServices.GetPlayerCamera?.Invoke() ?? editorRig.Headset.HeadsetTransform;
            if (cam)
            {
                // Place map 0.1m in front of camera
                transform.position = cam.position + cam.forward * 0.1f;
                transform.rotation = cam.rotation * Quaternion.Euler(0, 180, 0);
            }

            renderer.enabled = true;
            canvas.SetActive(true);
            mapClosed.enabled = false;
            return;
        }
#endif

        if (grabber != null)
        {
            showMap = grabber.hand != null;
        }
        renderer.enabled = showMap;
        canvas.SetActive(showMap);
        if (showMap)
        {
            var updateRig = LocalHardwareRig;
            if (updateRig != null)
            {
                Vector3 headPos = updateRig.Headset.Position;
                canvas.SetActive(Vector3.Distance(transform.position, headPos) < 10);
            }
        }
        mapClosed.enabled = !showMap;
        /*
        if (isInBackpack && wasInBackpack)
        {
            transform.position = Vector3.down * 10;
            rigidbody.Rigidbody.isKinematic = true;
        }

        if (isInBackpack != wasInBackpack)
            wasInBackpack = isInBackpack;

        renderer.enabled = !isInBackpack;
        canvas.SetActive(!isInBackpack);
        */
    }

    public void Store()
    {
        if (Backpack.myBackpack.mapGrabber.grabbable != this)
        {
            // Puff();
            Backpack.myBackpack.mapGrabber.Grab(this);
        }
        /*
        if (!isInBackpack)
        {
            Puff();
            isInBackpack = true;
        }
        */
    }

    public void Puff()
    {
        // system.Play();
        // AudioService.Play(clip, transform.position);
    }

}
}
