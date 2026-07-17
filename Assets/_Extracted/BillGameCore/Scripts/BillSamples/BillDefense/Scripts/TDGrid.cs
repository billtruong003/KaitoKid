using UnityEngine;
using System.Collections.Generic;
using BillGameCore;

namespace BillSamples.TowerDefense
{
    public enum TileType { Blocked, Path, Buildable }

    /// <summary>
    /// Grid manager. Holds the map, handles tower placement, visual highlights.
    /// </summary>
    public class TDGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int width = 20;
        public int height = 12;
        public float tileSize = 1f;
        public Vector3 gridOrigin = Vector3.zero;

        [Header("Visuals")]
        public Material pathMaterial;
        public Material buildableMaterial;
        public Material blockedMaterial;
        public Material highlightValidMaterial;
        public Material highlightInvalidMaterial;

        // Internal
        private TileType[,] _tiles;
        private GameObject[,] _tileObjects;
        private TDTower[,] _towers;
        private List<Vector3> _pathWaypoints = new List<Vector3>();
        private Vector2Int _spawnTile;
        private Vector2Int _exitTile;

        // Prefab references for towers (set by Setup)
        [HideInInspector] public GameObject towerBasePrefab;

        // ─── Init ────────────────────────────────

        public void Init(int w, int h)
        {
            width = w;
            height = h;
            _tiles = new TileType[width, height];
            _tileObjects = new GameObject[width, height];
            _towers = new TDTower[width, height];

            // Default all to blocked
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _tiles[x, y] = TileType.Blocked;
        }

        public void SetTile(int x, int y, TileType type)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            _tiles[x, y] = type;
        }

        public void SetSpawn(int x, int y) { _spawnTile = new Vector2Int(x, y); }
        public void SetExit(int x, int y) { _exitTile = new Vector2Int(x, y); }

        public void AddWaypoint(Vector3 worldPos) { _pathWaypoints.Add(worldPos); }

        public Vector3[] GetWaypoints() => _pathWaypoints.ToArray();

        public Vector3 GetSpawnWorldPos() => GridToWorld(_spawnTile.x, _spawnTile.y);
        public Vector3 GetExitWorldPos() => GridToWorld(_exitTile.x, _exitTile.y);

        /// <summary>Build visual tiles after all SetTile calls.</summary>
        public void BuildVisuals()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"Tile_{x}_{y}";
                    go.transform.SetParent(transform);
                    go.transform.position = GridToWorld(x, y);
                    go.transform.localScale = new Vector3(tileSize * 0.95f, 0.1f, tileSize * 0.95f);

                    var renderer = go.GetComponent<Renderer>();
                    switch (_tiles[x, y])
                    {
                        case TileType.Path:
                            renderer.material = pathMaterial ?? CreateMat(new Color(0.75f, 0.7f, 0.55f));
                            break;
                        case TileType.Buildable:
                            renderer.material = buildableMaterial ?? CreateMat(new Color(0.4f, 0.65f, 0.3f));
                            break;
                        default:
                            renderer.material = blockedMaterial ?? CreateMat(new Color(0.35f, 0.3f, 0.25f));
                            break;
                    }

                    // Collider for raycast
                    var col = go.GetComponent<BoxCollider>();
                    col.isTrigger = false;

                    _tileObjects[x, y] = go;
                }
            }

            Debug.Log($"[TDGrid] Built {width}x{height} grid with {_pathWaypoints.Count} waypoints");
        }

        // ─── Tower Placement ─────────────────────

        public bool IsBuildable(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;
            return _tiles[pos.x, pos.y] == TileType.Buildable && _towers[pos.x, pos.y] == null;
        }

        public GameObject PlaceTower(TowerType type, Vector2Int pos)
        {
            if (!IsBuildable(pos)) return null;

            var worldPos = GridToWorld(pos.x, pos.y) + Vector3.up * 0.1f;

            GameObject towerGO;
            if (towerBasePrefab != null)
            {
                towerGO = Instantiate(towerBasePrefab, worldPos, Quaternion.identity);
            }
            else
            {
                towerGO = CreatePlaceholderTower(type);
                towerGO.transform.position = worldPos;
            }

            towerGO.name = $"Tower_{type}_{pos.x}_{pos.y}";
            towerGO.transform.SetParent(transform);

            var tower = towerGO.GetComponent<TDTower>();
            if (tower == null) tower = towerGO.AddComponent<TDTower>();
            tower.Setup(type, pos);

            // Add collider for enemy detection
            var sphereCol = towerGO.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.1f; // Tiny, tower uses OverlapSphere for range

            _towers[pos.x, pos.y] = tower;
            _tiles[pos.x, pos.y] = TileType.Buildable; // Still "buildable" tile, tower reference prevents double-place

            return towerGO;
        }

        public void ClearTile(Vector2Int pos)
        {
            if (pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height)
                _towers[pos.x, pos.y] = null;
        }

        public TDTower GetTowerAt(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return null;
            return _towers[pos.x, pos.y];
        }

        public void ClearAllTowers()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_towers[x, y] != null)
                    {
                        Destroy(_towers[x, y].gameObject);
                        _towers[x, y] = null;
                    }
                }
            }
        }

        // ─── Raycast ─────────────────────────────

        /// <summary>Raycast from screen to grid, return tile coords.</summary>
        public bool ScreenToTile(Vector3 screenPos, out Vector2Int tile)
        {
            tile = Vector2Int.zero;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 local = hit.point - gridOrigin;
                int x = Mathf.RoundToInt(local.x / tileSize);
                int y = Mathf.RoundToInt(local.z / tileSize);
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    tile = new Vector2Int(x, y);
                    return true;
                }
            }
            return false;
        }

        public void HighlightTile(Vector2Int pos, bool valid)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return;
            var go = _tileObjects[pos.x, pos.y];
            if (go == null) return;
            var r = go.GetComponent<Renderer>();

            if (valid)
                r.material = highlightValidMaterial ?? CreateMat(new Color(0.3f, 0.9f, 0.3f, 0.7f));
            else
                r.material = highlightInvalidMaterial ?? CreateMat(new Color(0.9f, 0.3f, 0.3f, 0.7f));
        }

        public void ResetTileHighlight(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return;
            var go = _tileObjects[pos.x, pos.y];
            if (go == null) return;
            var r = go.GetComponent<Renderer>();

            switch (_tiles[pos.x, pos.y])
            {
                case TileType.Path: r.material = pathMaterial ?? CreateMat(new Color(0.75f, 0.7f, 0.55f)); break;
                case TileType.Buildable: r.material = buildableMaterial ?? CreateMat(new Color(0.4f, 0.65f, 0.3f)); break;
                default: r.material = blockedMaterial ?? CreateMat(new Color(0.35f, 0.3f, 0.25f)); break;
            }
        }

        // ─── Helpers ─────────────────────────────

        public Vector3 GridToWorld(int x, int y)
        {
            return gridOrigin + new Vector3(x * tileSize, 0, y * tileSize);
        }

        Material CreateMat(Color c)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = c;
            return m;
        }

        GameObject CreatePlaceholderTower(TowerType type)
        {
            var go = new GameObject("TowerBase");

            // Base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(go.transform);
            baseObj.transform.localPosition = new Vector3(0, 0.25f, 0);
            baseObj.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
            var col = TDDatabase.GetTower(type).color;
            baseObj.GetComponent<Renderer>().material = CreateMat(col);
            Destroy(baseObj.GetComponent<Collider>());

            // Turret
            var turret = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turret.name = "Turret";
            turret.transform.SetParent(go.transform);
            turret.transform.localPosition = new Vector3(0, 0.6f, 0);
            turret.transform.localScale = new Vector3(0.3f, 0.3f, 0.6f);
            turret.GetComponent<Renderer>().material = CreateMat(col * 0.8f);
            Destroy(turret.GetComponent<Collider>());

            // Set tower component refs
            var tower = go.AddComponent<TDTower>();
            tower.modelRoot = go.transform;
            tower.turretPivot = turret.transform;
            tower.firePoint = turret.transform;

            // Range indicator (hidden by default)
            var rangeInd = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rangeInd.name = "RangeIndicator";
            rangeInd.transform.SetParent(go.transform);
            rangeInd.transform.localPosition = new Vector3(0, 0.02f, 0);
            rangeInd.transform.localScale = new Vector3(7, 0.01f, 7);
            var rangeMat = CreateMat(new Color(1, 1, 1, 0.15f));
            rangeMat.SetFloat("_Mode", 3); // Transparent
            rangeMat.SetInt("_SrcBlend", 5);
            rangeMat.SetInt("_DstBlend", 10);
            rangeMat.SetInt("_ZWrite", 0);
            rangeMat.renderQueue = 3000;
            rangeInd.GetComponent<Renderer>().material = rangeMat;
            Destroy(rangeInd.GetComponent<Collider>());
            rangeInd.SetActive(false);
            tower.rangeIndicator = rangeInd;

            return go;
        }
    }
}
