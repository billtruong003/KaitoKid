#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Mythfall.Enemy;

namespace Mythfall.EditorTools
{
    /// <summary>
    /// Sprint 2 Day 1 setup — scaffold the enemy variety placeholder prefabs:
    ///   - Brute (capsule, purple, taller — root + visual + body collider)
    ///   - Shooter (capsule, yellow, slimmer — root + visual + body collider + muzzle child)
    ///   - EnemyProjectile (small orange sphere — pooled ammo for Shooter)
    /// Also creates Brute_Data + Shooter_Data EnemyDataSO assets in Resources/Enemies/.
    /// Idempotent — re-run replaces existing assets.
    ///
    /// Prerequisite: 'Sprint 2 — Build Placeholder Prefabs' must have been run at
    /// least once (this tool reuses the existing Enemy AnimatorController + folders).
    ///
    /// Run via: Tools → Mythfall → Sprint 2 — Build Enemy Variety Prefabs
    /// </summary>
    public static class Sprint2VarietySetup
    {
        const string ResourcesRoot = "Assets/Mythfall/Resources";
        const string AnimationsFolder = "Assets/Mythfall/Animations";
        const string MaterialsFolder = "Assets/Mythfall/Materials";

        const string EnemyPrefabsFolder = ResourcesRoot + "/Prefabs/Enemies";
        const string ProjectilePrefabsFolder = ResourcesRoot + "/Prefabs/Projectiles";
        const string EnemiesDataFolder = ResourcesRoot + "/Enemies";

        const string EnemyAnimCtrlPath = AnimationsFolder + "/EnemyAnimator.controller";

        const string BrutePrefabPath = EnemyPrefabsFolder + "/Brute.prefab";
        const string ShooterPrefabPath = EnemyPrefabsFolder + "/Shooter.prefab";
        const string EnemyProjectilePrefabPath = ProjectilePrefabsFolder + "/EnemyProjectile.prefab";

        const string BruteDataPath = EnemiesDataFolder + "/Brute_Data.asset";
        const string ShooterDataPath = EnemiesDataFolder + "/Shooter_Data.asset";

        const string BruteMatPath = MaterialsFolder + "/Brute_Mat.mat";
        const string ShooterMatPath = MaterialsFolder + "/Shooter_Mat.mat";
        const string EnemyProjectileMatPath = MaterialsFolder + "/EnemyProjectile_Mat.mat";

        const int EnemyLayer = 8;
        const int ProjectileLayer = 9;

        [MenuItem("Tools/Mythfall/Sprint 2 — Build Enemy Variety Prefabs")]
        public static void Build()
        {
            EnsureFolders();

            var enemyCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyAnimCtrlPath);
            if (enemyCtrl == null)
            {
                Debug.LogError($"[Sprint2VarietySetup] {EnemyAnimCtrlPath} missing — run 'Sprint 2 — Build Placeholder Prefabs' first.");
                return;
            }

            var bruteMat = CreateUrpLitMaterial(BruteMatPath, new Color(0.42f, 0.28f, 0.55f)); // dusky purple
            var shooterMat = CreateUrpLitMaterial(ShooterMatPath, new Color(0.85f, 0.78f, 0.32f)); // mustard yellow
            var projMat = CreateUrpLitMaterial(EnemyProjectileMatPath, new Color(0.95f, 0.45f, 0.18f)); // orange ember

            var brutePrefab = BuildBrutePrefab(BrutePrefabPath, bruteMat, enemyCtrl);
            var shooterPrefab = BuildShooterPrefab(ShooterPrefabPath, shooterMat, enemyCtrl);
            var projectilePrefab = BuildEnemyProjectilePrefab(EnemyProjectilePrefabPath, projMat);

            BuildBruteData(BruteDataPath, brutePrefab);
            BuildShooterData(ShooterDataPath, shooterPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Sprint2VarietySetup] DONE. 3 prefabs (Brute + Shooter + EnemyProjectile) + 3 materials + 2 EnemyDataSO assets configured.");
        }

        // -------------------------------------------------------------------
        // Folders
        // -------------------------------------------------------------------

        static void EnsureFolders()
        {
            EnsureFolder(EnemyPrefabsFolder);
            EnsureFolder(ProjectilePrefabsFolder);
            EnsureFolder(EnemiesDataFolder);
            EnsureFolder(MaterialsFolder);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // -------------------------------------------------------------------
        // Materials
        // -------------------------------------------------------------------

        static Material CreateUrpLitMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // -------------------------------------------------------------------
        // Brute prefab — taller, beefier capsule
        // -------------------------------------------------------------------

        static GameObject BuildBrutePrefab(string path, Material mat, AnimatorController ctrl)
        {
            var root = new GameObject("Brute");
            root.layer = EnemyLayer;

            // Body collider (taller + wider than swarmer)
            var body = root.AddComponent<CapsuleCollider>();
            body.height = 2.2f;
            body.radius = 0.55f;
            body.center = new Vector3(0f, 1.1f, 0f);
            body.isTrigger = false;

            root.AddComponent<BruteEnemy>();
            root.AddComponent<DynamicAnimationEventHub>();

            // Kinematic Rigidbody — same fix as Swarmer (Day 2 ARCHITECTURE_DECISIONS).
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Visual child — larger than Swarmer
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            visual.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            var brute = root.GetComponent<BruteEnemy>();
            SetObjectRef(brute, "animator", animator);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // -------------------------------------------------------------------
        // Shooter prefab — slimmer capsule + muzzle child
        // -------------------------------------------------------------------

        static GameObject BuildShooterPrefab(string path, Material mat, AnimatorController ctrl)
        {
            var root = new GameObject("Shooter");
            root.layer = EnemyLayer;

            var body = root.AddComponent<CapsuleCollider>();
            body.height = 1.5f;
            body.radius = 0.35f;
            body.center = new Vector3(0f, 0.75f, 0f);
            body.isTrigger = false;

            root.AddComponent<ShooterEnemy>();
            root.AddComponent<DynamicAnimationEventHub>();

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Visual child (slimmer than swarmer)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            visual.transform.localScale = new Vector3(0.7f, 0.75f, 0.7f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // Muzzle for projectile spawn (chest height, slightly forward)
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(root.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);

            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            var shooter = root.GetComponent<ShooterEnemy>();
            SetObjectRef(shooter, "animator", animator);
            SetObjectRef(shooter, "muzzle", muzzle.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // -------------------------------------------------------------------
        // EnemyProjectile prefab — pooled ammo for Shooter
        // -------------------------------------------------------------------

        static GameObject BuildEnemyProjectilePrefab(string path, Material mat)
        {
            var root = new GameObject("EnemyProjectile");
            root.layer = ProjectileLayer;

            var rb = root.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.2f;

            root.AddComponent<EnemyProjectile>();

            // Visual — small glowing sphere
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<SphereCollider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // -------------------------------------------------------------------
        // EnemyDataSO builders
        // -------------------------------------------------------------------

        static void BuildBruteData(string path, GameObject prefab)
        {
            var so = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EnemyDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.enemyId = "brute";
            so.nameKey = "enemy.brute.name";
            so.descKey = "enemy.brute.desc";
            so.maxHP = 80f;
            so.attackPower = 15f;
            so.moveSpeed = 2f;
            so.attackRange = 1.8f;       // Brute engages at 1.8m so telegraph fires before player kisses it
            so.attackCooldown = 2.5f;
            so.xpReward = 3f;
            so.prefab = prefab;
            so.poolKey = "Enemy_Brute";
            EditorUtility.SetDirty(so);

            if (prefab != null)
            {
                var brute = prefab.GetComponent<BruteEnemy>();
                if (brute != null)
                {
                    SetObjectRef(brute, "data", so);
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        static void BuildShooterData(string path, GameObject prefab)
        {
            var so = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<EnemyDataSO>();
                AssetDatabase.CreateAsset(so, path);
            }
            so.enemyId = "shooter";
            so.nameKey = "enemy.shooter.name";
            so.descKey = "enemy.shooter.desc";
            so.maxHP = 40f;
            so.attackPower = 8f;
            so.moveSpeed = 3f;
            so.attackRange = 10f;        // Shooter's max engage; ShooterEnemy.maxEngageDistance default also 10
            so.attackCooldown = 2f;
            so.xpReward = 2f;
            so.prefab = prefab;
            so.poolKey = "Enemy_Shooter";
            EditorUtility.SetDirty(so);

            if (prefab != null)
            {
                var shooter = prefab.GetComponent<ShooterEnemy>();
                if (shooter != null)
                {
                    SetObjectRef(shooter, "data", so);
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        // -------------------------------------------------------------------
        // SerializedObject field setter (mirror of Sprint2Setup helper)
        // -------------------------------------------------------------------

        static void SetObjectRef(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[Sprint2VarietySetup] '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
#endif
