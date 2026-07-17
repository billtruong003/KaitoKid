using UnityEngine;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Networking;
using Teabag.UI;

namespace Teabag.Player
{
    public class LivCameraSpawner : MonoBehaviour
    {
        [Header("LIV Settings")]
        [SerializeField] private GameObject livCameraPrefab;
        [SerializeField] private HoldHintUI holdHintPrefab;

        [Header("Timers & Position")]
        [SerializeField] private float spawnHoldTime = 1.0f;
        [SerializeField] private float despawnHoldTime = 1.0f;
        [SerializeField] private float spawnDistance = 1.0f;
        [SerializeField] private float spawnHeightOffset = -0.2f;

        private GameObject spawnedCamera;
        private HoldHintUI activeHint;
        private float holdTimer = 0f;
        private bool isHoldingB = false;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private INetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = ServiceLocator.Get<INetworkManager>();
        }

        private void Update()
        {
            bool bButtonHeld = VRInputHandler.GetInputDown(false, InputType.Secondary);

#if UNITY_EDITOR
            if (Input.GetKey(KeyCode.B))
            {
                bButtonHeld = true;
            }
#endif

            if (bButtonHeld)
            {
                holdTimer += Time.deltaTime;
                isHoldingB = true;

                UpdateHintUI();

                if (spawnedCamera == null)
                {
                    if (holdTimer >= spawnHoldTime)
                    {
                        SpawnCamera();
                        holdTimer = -1000f;
                        HideHintUI();
                    }
                }
                else
                {
                    if (holdTimer >= despawnHoldTime)
                    {
                        DespawnCamera();
                        holdTimer = -1000f;
                        HideHintUI();
                    }
                }
            }
            else
            {
                if (isHoldingB)
                {
                    isHoldingB = false;
                    holdTimer = 0f;
                    HideHintUI();
                }
            }
        }

        private void UpdateHintUI()
        {
            if (holdTimer < 0) return;

            if (activeHint == null)
            {
                if (holdHintPrefab != null)
                {
                    activeHint = Instantiate(holdHintPrefab);
                }
                else
                {
                    GameObject go = new GameObject("HoldHintUI_Auto");
                    activeHint = go.AddComponent<HoldHintUI>();
                    GameLogger.Warning("[LivCameraSpawner] holdHintPrefab is not assigned! Please assign a prefab for better visuals.");
                }
            }

            activeHint.Show(true);

            string actionText = spawnedCamera == null ? "spawn cam" : "despawn cam";
            activeHint.SetText($"Hold B to {actionText}");

            float targetTotalTime = spawnedCamera == null ? spawnHoldTime : despawnHoldTime;
            activeHint.SetProgress(holdTimer / targetTotalTime);

            Transform headTransform = null;
            var rig = LocalHardwareRig;
            if (rig != null && rig.Headset != null) headTransform = rig.Headset.HeadsetTransform;
            else if (Camera.main != null) headTransform = Camera.main.transform;

            if (headTransform != null)
            {
                activeHint.UpdatePosition(headTransform, 0.8f, new Vector3(0, -0.1f, 0));
            }
        }

        private void HideHintUI()
        {
            if (activeHint != null)
            {
                activeHint.Show(false);
            }
        }

        private void SpawnCamera()
        {
            if (livCameraPrefab == null)
            {
                GameLogger.Warning("[LivCameraSpawner] livCameraPrefab variable is not assigned! Please drag and drop the prefab in the Inspector.");
                return;
            }

            Transform headTransform = null;
            var rig = LocalHardwareRig;
            if (rig != null && rig.Headset != null)
            {
                headTransform = rig.Headset.HeadsetTransform;
            }
            else if (Camera.main != null)
            {
                headTransform = Camera.main.transform;
            }

            if (headTransform != null)
            {
                // Position: 1m in front of player
                Vector3 spawnPosition = headTransform.position + headTransform.forward * spawnDistance;
                spawnPosition.y += spawnHeightOffset;

                // Rotate camera to face player
                // Only rotate around Y axis to prevent tilt
                Vector3 directionToPlayer = headTransform.position - spawnPosition;
                directionToPlayer.y = 0; // Keep camera upright

                Quaternion spawnRotation = Quaternion.LookRotation(directionToPlayer);

                if (_networkManager != null && _networkManager.HasRunner)
                {
                    var no = livCameraPrefab.GetComponent<Fusion.NetworkObject>();
                    if (no != null)
                    {
                        var spawnedNO = _networkManager.Runner.Spawn(no, spawnPosition, spawnRotation, _networkManager.Runner.LocalPlayer);
                        spawnedCamera = spawnedNO.gameObject;
                    }
                    else
                    {
                        spawnedCamera = Instantiate(livCameraPrefab, spawnPosition, spawnRotation);
                    }
                }
                else
                {
                    spawnedCamera = Instantiate(livCameraPrefab, spawnPosition, spawnRotation);
                }

                // Vibrate right controller to signify spawn
                VRInputHandler.VibrateController(false, 0.5f, 0.2f);
                GameLogger.Info("[LivCameraSpawner] Successfully spawned LIV Camera.");
            }
            else
            {
                GameLogger.Warning("[LivCameraSpawner] Could not find player head transform to get spawn position.");
            }
        }

        private void DespawnCamera()
        {
            if (spawnedCamera != null)
            {
                if (_networkManager != null && _networkManager.HasRunner)
                {
                    var no = spawnedCamera.GetComponent<Fusion.NetworkObject>();
                    if (no != null)
                    {
                        if (no.HasStateAuthority) _networkManager.Runner.Despawn(no);
                    }
                    else
                    {
                        Destroy(spawnedCamera);
                    }
                }
                else
                {
                    Destroy(spawnedCamera);
                }

                spawnedCamera = null;

                // Vibrate right controller for longer to signify despawn
                VRInputHandler.VibrateController(false, 0.6f, 0.4f);
                GameLogger.Info("[LivCameraSpawner] Despawned LIV Camera.");
            }
        }
    }
}
