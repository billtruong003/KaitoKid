#nullable enable
using UnityEngine;

namespace RadiantArena.Arena
{
    /// <summary>
    /// Singleton MonoBehaviour spawned by ArenaBootstrap. Configures Main Camera
    /// (top-down orthographic) and creates ground + 4 walls + 2 player capsules
    /// at runtime. PlayerVisual components attach to capsules.
    ///
    /// DDOL — lives for app lifetime. Bill.IsReady-guard on OnDestroy per
    /// [[bill-ondestroy-guard]] precedent (not strictly needed — we don't touch
    /// Bill services in OnDestroy — but the singleton-clear is harmless).
    /// </summary>
    public class ArenaSceneBuilder : MonoBehaviour
    {
        public static ArenaSceneBuilder? Instance { get; private set; }

        public PlayerVisual? MyVisual { get; private set; }
        public PlayerVisual? OpponentVisual { get; private set; }

        // Layout constants.
        const float MapHalf         = 5.0f;   // world units per side (10×10 total)
        const float WallThickness   = 0.4f;
        const float WallHeight      = 1.0f;
        const float CapsuleY        = 0.5f;
        const float SlotMeX         = -3.0f;
        const float SlotOppX        = 3.0f;
        const float CameraY         = 10.0f;
        const float CameraOrthoSize = 6.0f;

        static readonly Color GroundColor = new Color(0.12f, 0.14f, 0.18f);
        static readonly Color WallColor   = new Color(0.22f, 0.26f, 0.32f);
        static readonly Color MeColor     = new Color(0.30f, 0.86f, 0.55f);
        static readonly Color OppColor    = new Color(0.94f, 0.65f, 0.32f);
        static readonly Color BgColor     = new Color(0.06f, 0.07f, 0.10f);

        Material? _groundMat;
        Material? _wallMat;
        Material? _meMat;
        Material? _oppMat;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildMaterials();
            ConfigureCamera();
            BuildGround();
            BuildWalls();
            BuildPlayers();

            Debug.Log("[Arena.Scene] ArenaSceneBuilder ready — ground, 4 walls, 2 capsules, top-down ortho camera");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────

        void BuildMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _groundMat = new Material(shader); _groundMat.color = GroundColor;
            _wallMat   = new Material(shader); _wallMat.color   = WallColor;
            _meMat     = new Material(shader); _meMat.color     = MeColor;
            _oppMat    = new Material(shader); _oppMat.color    = OppColor;
        }

        void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Arena.Scene] Camera.main null — skip configure");
                return;
            }
            cam.orthographic       = true;
            cam.orthographicSize   = CameraOrthoSize;
            cam.transform.position = new Vector3(0f, CameraY, 0f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.clearFlags         = CameraClearFlags.SolidColor;
            cam.backgroundColor    = BgColor;
        }

        void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ArenaGround";
            ground.transform.SetParent(transform, worldPositionStays: false);
            // Plane default mesh = 10×10 world units (vertex extent ±5). Scale 1 = matches map.
            ground.transform.localScale = Vector3.one;
            var col = ground.GetComponent<Collider>();
            if (col != null) Destroy(col); // server-authoritative, visual-only
            var r = ground.GetComponent<MeshRenderer>();
            if (r != null && _groundMat != null) r.sharedMaterial = _groundMat;
        }

        void BuildWalls()
        {
            BuildWall("WallNorth", new Vector3(0f, WallHeight * 0.5f,  MapHalf),  new Vector3(MapHalf * 2f + WallThickness, WallHeight, WallThickness));
            BuildWall("WallSouth", new Vector3(0f, WallHeight * 0.5f, -MapHalf),  new Vector3(MapHalf * 2f + WallThickness, WallHeight, WallThickness));
            BuildWall("WallEast",  new Vector3( MapHalf, WallHeight * 0.5f, 0f),  new Vector3(WallThickness, WallHeight, MapHalf * 2f));
            BuildWall("WallWest",  new Vector3(-MapHalf, WallHeight * 0.5f, 0f),  new Vector3(WallThickness, WallHeight, MapHalf * 2f));
        }

        void BuildWall(string n, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = n;
            w.transform.SetParent(transform, worldPositionStays: false);
            w.transform.position = pos;
            w.transform.localScale = scale;
            var col = w.GetComponent<Collider>();
            if (col != null) Destroy(col); // visual-only
            var r = w.GetComponent<MeshRenderer>();
            if (r != null && _wallMat != null) r.sharedMaterial = _wallMat;
        }

        void BuildPlayers()
        {
            MyVisual       = BuildPlayer("MyPlayerVisual",       new Vector3(SlotMeX,  CapsuleY, 0f), _meMat,  isMine: true);
            OpponentVisual = BuildPlayer("OpponentPlayerVisual", new Vector3(SlotOppX, CapsuleY, 0f), _oppMat, isMine: false);
        }

        PlayerVisual BuildPlayer(string n, Vector3 pos, Material? mat, bool isMine)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = n;
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.position = pos;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            var pv = go.AddComponent<PlayerVisual>();
            pv.IsMine = isMine;
            pv.SlotAnchor = pos;
            return pv;
        }
    }
}
