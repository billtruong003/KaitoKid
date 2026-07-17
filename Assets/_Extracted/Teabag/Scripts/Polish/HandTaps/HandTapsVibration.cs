using System;
using System.Collections.Generic;
using Fusion;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Gameplay;
using Teabag.Networking;
using Teabag.Player;
using UnityEngine;

public sealed class HandTapsVibration : MonoBehaviour
{
    [Header("Hand")]
    [SerializeField]
    private bool _isLeftHand;

    [Header("Hand Tap")]
    [SerializeField]
    private HandTap _handTap;

    [SerializeField]
    private List<HandTapType> _handTaps = new List<HandTapType>();

    [SerializeField]
    private LayerMask _locomotionEnabledLayers;

    [SerializeField]
    private float _sphereRadius = 0.05f;

    [Header("Snow")]
    [SerializeField]
    private NetworkObject _snowballPrefab;

    private Vector3 _lastPosition;
    private bool _hitLast;
    private DateTime _lastHitTime;
    private Gorilla _gorilla;

    private IGorillaService _gorillaService;
    private INetworkManager _networkManager;
    private RaycastHit[] _sphereCastHits = new RaycastHit[16];

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    private INetworkManager NetworkManager
    {
        get
        {
            _networkManager ??= ServiceLocator.Get<INetworkManager>();
            return _networkManager;
        }
    }

    private void Awake()
    {
        _gorilla = GetComponentInParent<Gorilla>();
    }

    private void Update()
    {
        CheckHandTap();
    }

    public void CheckHandTap()
    {
        var localRig = LocalHardwareRig;
        if (localRig == null) return;

        if ((transform.position - localRig.Headset.Position).sqrMagnitude > 256f)
            return;

        if (_gorilla != null)
        {
            _gorillaService ??= ServiceLocator.Get<IGorillaService>();
            var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
            if (_gorilla.health != null && localGorilla?.health != null)
            {
                if (_gorilla.health.isDead && !localGorilla.health.isDead)
                    return;
            }
        }

        var movementVector = transform.position - _lastPosition;
        float moveSqr = movementVector.sqrMagnitude;

        if (moveSqr < 0.0001f)
            return;

        int hitCount = Physics.SphereCastNonAlloc(
                _lastPosition,
                _sphereRadius,
                movementVector.normalized,
                _sphereCastHits,
                Mathf.Sqrt(moveSqr) + _sphereRadius,
                _locomotionEnabledLayers);

        if (hitCount > 0)
        {
            RaycastHit hit = _sphereCastHits.GetClosestHit(hitCount);

            if (!_hitLast && (DateTime.UtcNow - _lastHitTime).TotalMilliseconds > 250)
            {
                ForceDebug.Log((DateTime.UtcNow - _lastHitTime).TotalMilliseconds);
                bool isMine = _gorilla != null && _gorilla.HasStateAuthority;
                PlayHandTap(hit, _isLeftHand, isMine);
                _lastHitTime = DateTime.UtcNow;
                _hitLast = true;
            }

            if (_gorilla != null && _gorilla.HasStateAuthority)
            {
                var handTapType = GetHandTapType(hit);
                if (handTapType != null && handTapType.name == "Snow")
                {
                    if (VRInputHandler.GetInputDown(_isLeftHand, InputType.Grip))
                    {
                        TrySpawnSnowball(_isLeftHand);
                    }
                }
            }
        }
        else
        {
            _hitLast = false;
        }

        _lastPosition = transform.position;
    }

    private void TrySpawnSnowball(bool isLeftHand)
    {
        if (_gorilla == null || !_gorilla.HasStateAuthority) return;
        if (_snowballPrefab == null) return;

        GorillaHand hand = isLeftHand ? _gorilla.leftHand : _gorilla.rightHand;
        if (hand == null || hand.grabber == null) return;
        if (hand.grabber.grabbable != null) return;
        if (_gorilla.health != null && _gorilla.health.isDead) return;

        _networkManager ??= ServiceLocator.Get<INetworkManager>();
        if (_networkManager?.Runner == null) return;

        NetworkObject snowballObj = _networkManager.Runner.Spawn(_snowballPrefab, hand.transform.position);
        var snowball = snowballObj.GetComponent<Snowball>();
        if (snowball != null)
            snowball.transform.position = hand.transform.position;

        hand.grabber.Grab(snowball);
    }

    private HandTapType PlayHandTap(RaycastHit hit, bool isLeftHand, bool isMine)
    {
        if (isMine)
            VRInputHandler.VibrateController(isLeftHand, 0.1f, 0.1f);

        return PlayHandTap(hit, isLeftHand);
    }

    private HandTapType PlayHandTap(RaycastHit hit, bool isLeftHand)
    {
        HandTapType type = GetHandTapType(hit);
        if (type == null)
            return null;

        Transform t = SpawnHandTap(hit.point, hit.normal, isLeftHand, type);
        t.parent = hit.collider.transform;
        return type;
    }

    private Transform SpawnHandTap(Vector3 position, Vector3 normal, bool isLeftHand, HandTapType type)
    {
        HandTap tap = UnityEngine.Object.Instantiate(_handTap);
        tap.transform.position = position + normal * 0.01f;
        tap.transform.up = normal;
        tap.transform.localScale = isLeftHand ? Vector3.one : new Vector3(-1, 1, 1);
        tap.Tap(type);
        return tap.transform;
    }

    private HandTapType GetHandTapType(RaycastHit hit)
    {
        GameObject surface = hit.collider.gameObject;
        HandTapType type = null;

        if (surface.TryGetComponent(out SurfaceHitSound hitSound))
        {
            if (_handTaps.Count > (int)hitSound.index)
            {
                type = _handTaps[(int)hitSound.index];
            }
        }
        else if (hit.collider.TryGetComponent(out Renderer renderer))
        {
            hit.collider.TryGetComponent(out MeshFilter filter);
            Texture texture = null;

            if (filter != null)
            {
                int submesh = GetSubmeshFromTriangle(hit.triangleIndex, filter.sharedMesh);
                if (renderer.materials[submesh].HasProperty("_MainTexture"))
                    texture = renderer.materials[submesh].mainTexture;
            }

            if (texture == null && renderer.material.HasProperty("_Sides"))
                texture = renderer.material.GetTexture("_Sides");

            if (texture == null && renderer.material.HasProperty("_MainTexture"))
                texture = renderer.material.mainTexture;

            if (texture != null)
                type = GetHandTap(texture);
        }
        else if (surface.TryGetComponent(out Terrain terrain))
        {
            Vector3 terrainPosition = hit.point - hit.collider.transform.position;
            Vector3 splatMapPosition = new Vector3(
                terrainPosition.x / terrain.terrainData.size.x,
                0,
                terrainPosition.z / terrain.terrainData.size.z);

            int x = Mathf.FloorToInt(splatMapPosition.x * terrain.terrainData.alphamapWidth);
            int z = Mathf.FloorToInt(splatMapPosition.z * terrain.terrainData.alphamapHeight);

            float[,,] alphaMap = terrain.terrainData.GetAlphamaps(x, z, 1, 1);
            int index = 0;
            for (int i = 0; i < alphaMap.Length; i++)
            {
                if (alphaMap[0, 0, i] > alphaMap[0, 0, index])
                    index = i;
            }

            Texture terrainTexture = terrain.terrainData.terrainLayers[index].diffuseTexture;
            type = GetHandTap(terrainTexture);
        }

        return type;
    }

    private HandTapType GetHandTap(Texture texture)
    {
        foreach (HandTapType type in _handTaps)
        {
            foreach (Texture activationTexture in type.activationTextures)
            {
                if (activationTexture == texture)
                {
                    ForceDebug.Log("Hit: " + type.name);
                    return type;
                }
            }
        }

        return _handTaps.Count > 0 ? _handTaps[0] : null;
    }

    private int GetSubmeshFromTriangle(int triangleIndex, Mesh mesh)
    {
        try
        {
            if (!mesh.isReadable || mesh.subMeshCount < 1)
                return 0;

            int[] hitTriangles =
            {
                mesh.triangles[triangleIndex * 3],
                mesh.triangles[triangleIndex * 3 + 1],
                mesh.triangles[triangleIndex * 3 + 2]
            };

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                int[] tris = mesh.GetTriangles(i);
                for (int j = 0; j < tris.Length; j += 3)
                {
                    if (tris[j] == hitTriangles[0] & tris[j + 1] == hitTriangles[1] & tris[j + 2] == hitTriangles[2])
                        return i;
                }
            }
        }
        catch (Exception)
        {
            // Surface detection is best-effort
        }

        return 0;
    }
}
