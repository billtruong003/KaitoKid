using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("Performance/Frustum Culling System")]
public sealed class FrustumCullingSystem : MonoBehaviour
{
    private class CullableObject
    {
        public GameObject TargetGameObject { get; }
        public Renderer ObjectRenderer { get; }
        public bool IsVisible { get; set; }

        public CullableObject(GameObject target)
        {
            TargetGameObject = target;
            ObjectRenderer = target.GetComponentInChildren<Renderer>();
            IsVisible = target.activeSelf;
        }
    }

    [Header("Configuration")]
    [Tooltip("Danh sách các đối tượng sẽ được quản lý bởi hệ thống culling khi bắt đầu.")]
    [SerializeField] private List<GameObject> initialManagedObjects = new List<GameObject>();

    [Tooltip("Camera được sử dụng để tính toán tầm nhìn. Nếu để trống, hệ thống sẽ tự động tìm camera phù hợp.")]
    [SerializeField] private Camera cullingCamera;

    [Tooltip("Khoảng thời gian (giây) giữa mỗi lần kiểm tra. Giá trị nhỏ hơn sẽ phản ứng nhanh hơn nhưng tốn hiệu năng hơn.")]
    [Range(0.05f, 1.0f)]
    [SerializeField] private float updateInterval = 0.1f;

    private readonly List<CullableObject> managedCullableObjects = new List<CullableObject>();
    private readonly Plane[] cameraPlanes = new Plane[6];

    private void OnEnable()
    {
        InitializeSystem();
        StartCullingLoop();
    }

    private void OnDisable()
    {
        StopCullingLoop();
    }

    public void RegisterObject(GameObject objectToRegister)
    {
        if (objectToRegister == null || HasObject(objectToRegister))
        {
            return;
        }

        var cullableObject = new CullableObject(objectToRegister);
        if (cullableObject.ObjectRenderer != null)
        {
            managedCullableObjects.Add(cullableObject);
        }
    }

    public void UnregisterObject(GameObject objectToUnregister)
    {
        if (objectToUnregister == null)
        {
            return;
        }

        managedCullableObjects.RemoveAll(c => c.TargetGameObject == objectToUnregister);
    }

    private void InitializeSystem()
    {
        TryInitializeCamera();
        RegisterInitialObjects();
    }

    private void RegisterInitialObjects()
    {
        managedCullableObjects.Clear();
        foreach (var obj in initialManagedObjects)
        {
            RegisterObject(obj);
        }
    }

    private bool HasObject(GameObject target)
    {
        foreach (var cullable in managedCullableObjects)
        {
            if (cullable.TargetGameObject == target)
            {
                return true;
            }
        }
        return false;
    }

    private void TryInitializeCamera()
    {
        if (cullingCamera != null)
        {
            return;
        }

        if (cullingCamera == null)
        {
            cullingCamera = ShmackleGameManager.Instance.shmackleLocalPlayer.HeadCamera;
        }
    }

    private void StartCullingLoop()
    {
        InvokeRepeating(nameof(UpdateAllObjectVisibility), 0f, updateInterval);
    }

    private void StopCullingLoop()
    {
        CancelInvoke(nameof(UpdateAllObjectVisibility));
    }

    private void UpdateAllObjectVisibility()
    {
        if (!IsSystemReady())
        {
            // Nếu camera chưa sẵn sàng, thử tìm lại
            TryInitializeCamera();
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(cullingCamera, cameraPlanes);

        for (int i = 0; i < managedCullableObjects.Count; i++)
        {
            CullableObject currentObject = managedCullableObjects[i];

            if (currentObject.TargetGameObject == null)
            {
                // Đối tượng có thể đã bị hủy, loại bỏ nó khỏi danh sách
                managedCullableObjects.RemoveAt(i);
                i--; // Giảm chỉ số để không bỏ sót phần tử tiếp theo
                continue;
            }

            bool isVisible = GeometryUtility.TestPlanesAABB(cameraPlanes, currentObject.ObjectRenderer.bounds);

            if (currentObject.IsVisible != isVisible)
            {
                currentObject.TargetGameObject.SetActive(isVisible);
                currentObject.IsVisible = isVisible;
            }
        }
    }

    private bool IsSystemReady()
    {
        return cullingCamera != null && managedCullableObjects.Count > 0;
    }
}