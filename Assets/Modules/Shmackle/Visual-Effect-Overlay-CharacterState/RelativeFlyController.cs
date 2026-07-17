using Sirenix.OdinInspector;
using UnityEngine;

public sealed class RelativeFlyController : MonoBehaviour
{
    // Enum định nghĩa rõ ràng các trạng thái bay có thể có.
    public enum FlightMode
    {
        Disabled,
        Full,
        Physics
    }

    [Title("Core Dependencies")]
    [SerializeField, Required] private PlayerInputListener playerInputListener;
    [SerializeField, Required] private Transform hmdTransform;
    [SerializeField, Required] private Rigidbody playerRigidbody;
    // Tham chiếu trực tiếp, giảm phụ thuộc vào Singleton
    [SerializeField, Required] private ShmackleRaycastLocomotion locomotionController;

    [Title("Movement Settings")]
    [InfoBox("Tốc độ bay tức thời, không sử dụng gia tốc.")]
    [SerializeField, Range(1f, 30f)] private float flySpeed = 10f;
    [SerializeField, Range(1f, 30f)] private float verticalSpeed = 7f;

#if UNITY_EDITOR
    [Title("Editor Debugging")]
    [SerializeField] private bool enableEditorControls = true;
    [BoxGroup("Editor Controls"), ShowIf("enableEditorControls")]
    [SerializeField, Range(50f, 500f)] private float mouseSensitivity = 200f;
    [BoxGroup("Editor Controls/Key Bindings"), ShowIf("enableEditorControls")]
    [SerializeField] private KeyCode forwardKey = KeyCode.W, backKey = KeyCode.S, leftKey = KeyCode.A, rightKey = KeyCode.D;
    [BoxGroup("Editor Controls/Key Bindings"), ShowIf("enableEditorControls")]
    [SerializeField] private KeyCode upKey = KeyCode.E, downKey = KeyCode.Q;
#endif

    [Title("State")]
    [SerializeField, ReadOnly]
    private FlightMode currentFlightMode = FlightMode.Disabled;

    private bool initialRigidbodyIsKinematic;
    private bool initialRigidbodyUseGravity;

    private float cameraPitch;
    private float playerYaw;
    private bool isFlyModeActive;

    private void Update()
    {
        if (currentFlightMode == FlightMode.Disabled)
        {
            return;
        }

        HandleFlyMovement();

#if UNITY_EDITOR
        HandleEditorMouseLook();
#endif
    }

    public void OnFullFlyButtonPressed()
    {
        SetFlightMode(FlightMode.Full);
    }

    public void OnPhysicsFlyButtonPressed()
    {
        SetFlightMode(FlightMode.Physics);
    }

    public void OnDisableFlyButtonPressed()
    {
        SetFlightMode(FlightMode.Disabled);
    }

    public void ToggleFlyMode(bool state)
    {
        if (isFlyModeActive == state)
        {
            return;
        }

        isFlyModeActive = state;
        if (isFlyModeActive)
        {
            SetFlightMode(FlightMode.Physics);
        }
        else
        {
            SetFlightMode(FlightMode.Disabled);
        }
    }

    public void OnFlyModeToggleButtonPressed()
    {
        var currentMode = currentFlightMode;
        var newMode = (currentMode == FlightMode.Full)
            ? FlightMode.Disabled
            : FlightMode.Full;
        SetFlightMode(newMode);
    }

    public void SetFlightMode(FlightMode newMode)
    {
        if (currentFlightMode == newMode)
        {
            return;
        }

        ExitCurrentFlightMode();
        currentFlightMode = newMode;
        EnterNewFlightMode();
    }

    private void EnterNewFlightMode()
    {
        if (currentFlightMode == FlightMode.Disabled)
        {
            return;
        }

        StoreInitialRigidbodyState();
        playerRigidbody.useGravity = false;
        playerRigidbody.isKinematic = true;

        if (currentFlightMode == FlightMode.Full)
        {
            locomotionController.ToggleNoCollideMode(true, ShmackleRaycastLocomotion.NoCollideType.Full);
        }
        else if (currentFlightMode == FlightMode.Physics)
        {
            locomotionController.ToggleNoCollideMode(true, ShmackleRaycastLocomotion.NoCollideType.Physics);
        }

#if UNITY_EDITOR
        InitializeEditorRotations();
#endif
    }

    private void ExitCurrentFlightMode()
    {
        if (currentFlightMode == FlightMode.Disabled)
        {
            return;
        }

        if (currentFlightMode == FlightMode.Full)
        {
            locomotionController.ToggleNoCollideMode(false, ShmackleRaycastLocomotion.NoCollideType.Full);
        }
        else if (currentFlightMode == FlightMode.Physics)
        {
            locomotionController.ToggleNoCollideMode(false, ShmackleRaycastLocomotion.NoCollideType.Physics);
        }

        RestoreInitialRigidbodyState();
        playerRigidbody.linearVelocity = Vector3.zero;

#if UNITY_EDITOR
        UnlockCursor();
#endif
    }

    private void StoreInitialRigidbodyState()
    {
        initialRigidbodyIsKinematic = playerRigidbody.isKinematic;
        initialRigidbodyUseGravity = playerRigidbody.useGravity;
    }

    private void RestoreInitialRigidbodyState()
    {
        playerRigidbody.isKinematic = initialRigidbodyIsKinematic;
        playerRigidbody.useGravity = initialRigidbodyUseGravity;
    }

    private void HandleFlyMovement()
    {
        playerRigidbody.linearVelocity = Vector3.zero;
        Vector3 movementInput = GetMovementInput();
        Vector3 forwardDirection = hmdTransform.forward;
        Vector3 rightDirection = hmdTransform.right;
        Vector3 upDirection = Vector3.up;

        Vector3 desiredMovement =
            (forwardDirection * movementInput.z * flySpeed) +
            (rightDirection * movementInput.x * flySpeed) +
            (upDirection * movementInput.y * verticalSpeed);

        if (desiredMovement.magnitude > 0.1f)
        {
            playerRigidbody.transform.position += desiredMovement * Time.deltaTime;
        }
        
        
    }

    private Vector3 GetMovementInput()
    {
        Vector3 input = Vector3.zero;
#if UNITY_EDITOR
        if (enableEditorControls)
        {
            if (Input.GetKey(forwardKey)) input.z += 1f;
            if (Input.GetKey(backKey)) input.z -= 1f;
            if (Input.GetKey(leftKey)) input.x -= 1f;
            if (Input.GetKey(rightKey)) input.x += 1f;
            if (Input.GetKey(upKey)) input.y += 1f;
            if (Input.GetKey(downKey)) input.y -= 1f;

            if (input.sqrMagnitude > 0.01f)
            {
                return input.normalized;
            }
        }
#endif
        Vector2 vrPlanarInput = playerInputListener.moveInput;
        float vrVerticalInput = playerInputListener.turnInput.y;

        return new Vector3(vrPlanarInput.x, vrVerticalInput, vrPlanarInput.y).normalized;
    }

#if UNITY_EDITOR
    private void InitializeEditorRotations()
    {
        playerYaw = playerRigidbody.transform.eulerAngles.y;
        cameraPitch = hmdTransform.localEulerAngles.x;
    }

    private void HandleEditorMouseLook()
    {
        if (!enableEditorControls || !Input.GetMouseButton(1))
        {
            UnlockCursor();
            return;
        }

        LockCursor();

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        playerYaw += mouseX;
        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -90f, 90f);

        playerRigidbody.transform.rotation = Quaternion.Euler(0f, playerYaw, 0f);
        hmdTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
#endif
}