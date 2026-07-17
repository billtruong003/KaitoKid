using Sirenix.OdinInspector;
using UnityEngine;

namespace YourProject.Player.Movement
{
    public sealed class SuperiorModeController : MonoBehaviour
    {
        [Title("Core Dependencies")]
        [SerializeField, Required] private PlayerInputListener playerInputListener;
        [SerializeField, Required] private GameObject physicsRig;
        [SerializeField, Required] private Rigidbody playerRigidbody;

        [Title("Movement Settings")]
        [InfoBox("Các giá trị này điều khiển tốc độ di chuyển tức thời, không có gia tốc.")]
        [SerializeField, Range(1f, 20f)] private float moveSpeed = 5f;
        [SerializeField, Range(1f, 20f)] private float verticalSpeed = 3f;
        [SerializeField, Range(10f, 1000f)] private float rotationSpeed = 300f;

        // --- EDITOR TESTING SECTION ---
        [Title("Editor Testing & Debugging")]
        [InfoBox("Bật tính năng này để điều khiển bằng bàn phím ngay trong Editor. Sẽ không ảnh hưởng đến bản build cuối cùng.")]
        [SerializeField] private bool enableEditorKeyboardControls;

        [BoxGroup("EditorInputsLeft/Left Hand Simulation (Movement)")]
        [ShowIf("enableEditorKeyboardControls")]
        [HorizontalGroup("EditorInputsLeft", LabelWidth = 140)]
        [SerializeField] private KeyCode moveForward = KeyCode.I;
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsLeft/Left Hand Simulation (Movement)")]
        [SerializeField] private KeyCode moveBack = KeyCode.K;
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsLeft/Left Hand Simulation (Movement)")]
        [SerializeField] private KeyCode moveLeft = KeyCode.J;
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsLeft/Left Hand Simulation (Movement)")]
        [SerializeField] private KeyCode moveRight = KeyCode.L;


        [HorizontalGroup("EditorInputsRight", LabelWidth = 140)]
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsRight/Right Hand Simulation (Look Vertical)")]
        [SerializeField] private KeyCode turnLeft = KeyCode.LeftArrow;

        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsRight/Right Hand Simulation (Look Vertical)")]
        [SerializeField] private KeyCode turnRight = KeyCode.RightArrow;
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsRight/Right Hand Simulation (Look Vertical)")]
        [SerializeField] private KeyCode moveUp = KeyCode.UpArrow;
        [ShowIf("enableEditorKeyboardControls")]
        [BoxGroup("EditorInputsRight/Right Hand Simulation (Look Vertical)")]
        [SerializeField] private KeyCode moveDown = KeyCode.DownArrow;
        // --- END EDITOR TESTING SECTION ---

        [SerializeField, ReadOnly, Title("State")]
        private bool isSuperiorModeActive;

        private bool initialRigidbodyIsKinematic;

        private void Awake()
        {
            StoreInitialRigidbodyState();
        }

        private void Update()
        {
            if (!isSuperiorModeActive)
            {
                return;
            }
            HandleSuperiorModeMovement();
        }

        public void ToggleSuperiorMode(bool state)
        {
            isSuperiorModeActive = state;
            if (isSuperiorModeActive)
            {
                EnterSuperiorMode();
            }
            else
            {
                ExitSuperiorMode();
            }
        }

        private void StoreInitialRigidbodyState()
        {
            initialRigidbodyIsKinematic = playerRigidbody.isKinematic;
        }

        private void EnterSuperiorMode()
        {
            playerRigidbody.isKinematic = true;
            physicsRig.SetActive(false);

            Vector3 currentEuler = playerRigidbody.rotation.eulerAngles;
            playerRigidbody.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        }

        private void ExitSuperiorMode()
        {
            playerRigidbody.isKinematic = initialRigidbodyIsKinematic;
            physicsRig.SetActive(true);
        }

        private void HandleSuperiorModeMovement()
        {
            HandleRotation();
            HandleTranslation();
        }

        private void HandleRotation()
        {
            float yawInput = playerInputListener.turnInput.x;

#if UNITY_EDITOR
            if (enableEditorKeyboardControls)
            {
                float editorYaw = 0f;
                if (Input.GetKey(turnLeft)) editorYaw = -1f;
                if (Input.GetKey(turnRight)) editorYaw = 1f;

                if (Mathf.Abs(editorYaw) > 0.01f)
                {
                    yawInput = editorYaw;
                }
            }
#endif
            float yawChange = yawInput * rotationSpeed * Time.deltaTime;
            Vector3 currentEulerAngles = playerRigidbody.rotation.eulerAngles;
            Quaternion newRotation = Quaternion.Euler(0f, currentEulerAngles.y + yawChange, 0f);

            playerRigidbody.MoveRotation(newRotation);
        }

        private void HandleTranslation()
        {
            Vector2 planarInput = playerInputListener.moveInput;
            float verticalInput = playerInputListener.turnInput.y;

#if UNITY_EDITOR
            if (enableEditorKeyboardControls)
            {
                Vector2 editorPlanar = Vector2.zero;
                if (Input.GetKey(moveForward)) editorPlanar.y = 1f;
                if (Input.GetKey(moveBack)) editorPlanar.y = -1f;
                if (Input.GetKey(moveLeft)) editorPlanar.x = -1f;
                if (Input.GetKey(moveRight)) editorPlanar.x = 1f;

                float editorVertical = 0f;
                if (Input.GetKey(moveUp)) editorVertical = 1f;
                if (Input.GetKey(moveDown)) editorVertical = -1f;

                if (editorPlanar.sqrMagnitude > 0.01f)
                {
                    planarInput = editorPlanar.normalized;
                }
                if (Mathf.Abs(editorVertical) > 0.01f)
                {
                    verticalInput = editorVertical;
                }
            }
#endif
            Vector3 worldDirection = new Vector3(planarInput.x, 0, planarInput.y);
            Vector3 targetPlanarVelocity = transform.TransformDirection(worldDirection) * moveSpeed;
            Vector3 verticalVelocity = Vector3.up * (verticalInput * verticalSpeed);

            Vector3 targetVelocity = targetPlanarVelocity + verticalVelocity;
            Vector3 frameMovement = targetVelocity * Time.deltaTime;

            playerRigidbody.MovePosition(playerRigidbody.position + frameMovement);
        }
    }
}