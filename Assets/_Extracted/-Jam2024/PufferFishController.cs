using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Sirenix.OdinInspector; // Thêm namespace Odin Inspector
using UnityEngine;
using UnityEngine.UI;

public class PufferFishController : SerializedMonoBehaviour
{
    #region Serialized Fields

    [TitleGroup("Character Settings")]
    [SerializeField] PufferParticleController _particleController;
    [FoldoutGroup("Character Settings/Movement Settings")]
    [SerializeField] public Rigidbody rb;
    [FoldoutGroup("Character Settings/Movement Settings")]
    [SerializeField] private List<MagnetHover> magnetHover;

    [FoldoutGroup("Character Settings/Rotation Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [FoldoutGroup("Character Settings/Rotation Settings")]
    [SerializeField] private float rotationSpeed = 300f;
    [FoldoutGroup("Character Settings/Rotation Settings")]
    [SerializeField] private Quaternion originalRotation;
    private Quaternion targetRotation;

    [FoldoutGroup("Character Settings/Animation Settings")]
    [SerializeField] private Animator anim;
    [FoldoutGroup("Character Settings/Animation Settings")]
    [SerializeField] private Transform pufferChar;
    [FoldoutGroup("Character Settings/Animation Settings")]
    [SerializeField] public Transform ScaleParent;

    [TitleGroup("Camera Settings")]
    [SerializeField] private Camera mainCam;
    [TitleGroup("Camera Settings")]
    [SerializeField] private CinemachineFreeLook freeLookCamera;

    [FoldoutGroup("Camera Settings/Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [FoldoutGroup("Camera Settings/Zoom Settings")]
    [SerializeField] private float minZoom = 15f;
    [FoldoutGroup("Camera Settings/Zoom Settings")]
    [SerializeField] private float maxZoom = 90f;
    
    
    [FoldoutGroup("Stats/Buff")]
    public float MoveMultiplier = 0.75f, JumpMultiplier = 0.75f;
    [FoldoutGroup("Stats/Skill")]
    public bool JumpSkill, BoostSkill;
    [FoldoutGroup("Stats")]
    public float groundCheckDis = 2.5f;
    [FoldoutGroup("Stats")]
    public float popScale = 1.5f;
    [FoldoutGroup("Stats")]
    [SerializeField] public float speed = 15f;
    [FoldoutGroup("Stats")]
    [SerializeField] public float boostForce = 20f;
    [FoldoutGroup("Stats/Jump")]
    [SerializeField] private float jumpForce = 5f;
    [FoldoutGroup("Stats/Drag")]
    [SerializeField, ReadOnly] private float defaultDrag, defaultAngularDrag;
    [FoldoutGroup("Stats/Drag")]
    [SerializeField] private float PopDrag = 0.1f, PopAngularDrag = 0.05f;
    [FoldoutGroup("Stats/Slope")]
    public float maxSlopeAngle = 70f, SlopeAngle;
    [FoldoutGroup("Stats/Slope")] 
    public bool debugSlope, movingUpSlope;


    [FoldoutGroup("Debug")]
    public bool popping, isGrounded, allowFootPlacement;
    [FoldoutGroup("Debug")]
    public bool debugDownKey;
    [FoldoutGroup("Debug")]
    public float valueX, valueY;
    [FoldoutGroup("Debug")]
    [ReadOnly] public float currentSpeed;
    [FoldoutGroup("Debug")]
    [SerializeField] bool previousSpeedState, isSpeeding; 
    
    private RaycastHit hit;
    private Vector3 defaultScale;
    private Vector3 PlayerVector;
    private CamShake camShaker;
    
    //Slope
    private RaycastHit slopeHit;
    private bool exitingSlope;

    #endregion

    #region Calculate Var

    //Calculate var
    bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, 3.5f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            SlopeAngle = angle;
            movingUpSlope = (Mathf.Round(rb.velocity.y) > 0f);
            return angle < maxSlopeAngle && angle != 0;
        }

        movingUpSlope = false;
        return false;
    }
    
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(PlayerVector, slopeHit.normal).normalized;
    }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        defaultScale = anim.transform.localScale;

        defaultDrag = rb.drag;
        defaultAngularDrag = rb.angularDrag;
    }

    void Start()
    {
        camShaker = CamShake.Instance;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (rb == null) 
            rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        currentSpeed = rb.velocity.magnitude;
        isSpeeding = (currentSpeed > (speed * MoveMultiplier) - 0.5f && (speed * MoveMultiplier) > 20); // Determine the current speed state

        if (isSpeeding != previousSpeedState)
        {
            _particleController.ToggleSpeedParticles(isSpeeding);
            previousSpeedState = isSpeeding;
        }
        
        HandleInput();
        HandleZoom();

        GroundChecker();
        if (popping) return;

        Slope();
        MoveCharacter();
        GravityExtra();
        RotateCharacter();
    }

    #endregion

    #region Private Methods

    private void GravityExtra()
    {
        if(!OnSlope())
        rb.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
    }
    private void HandleInput()
    {
        debugDownKey = Input.GetKey(KeyCode.LeftShift);
        if (Input.GetButtonDown("Jump"))
        {
            if (!popping)
                StartPopping();
            else
                StopPopping();
        }
    }

    private void StartPopping()
    {
        foreach (var magnet in magnetHover)
        {
            magnet.Activated = false;
        }
        
        popping = true;
        
        if(isSpeeding) setPopDrag();
        
        anim.SetBool("Popping", true);
    }

    public void ProcessBouncyTransform()
    {
        anim.transform.localScale = defaultScale * popScale;

        if (debugDownKey) Jump(jumpForce);
        
        if ((valueX != 0 || valueY != 0) && isGrounded && BoostSkill)
        {
            Debug.Log("Boost");
            Vector3 direction = rb.velocity.normalized;
            rb.AddForce(direction * (boostForce * MoveMultiplier), ForceMode.Impulse);
        }
    }

    private void StopPopping()
    {
        anim.transform.localScale = defaultScale;
        
        foreach (var magnet in magnetHover)
        {
            magnet.Activated = true;
        }
        
        Jump(jumpForce);
        StartCoroutine(Balance());
        popping = false;

        rb.drag = defaultDrag;
        rb.angularDrag = defaultAngularDrag;
        
        anim.SetBool("Popping", false);
    }

    private void RotateCharacter()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pufferChar.Rotate(Vector3.up * mouseX);
    }

    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        float newFOV = freeLookCamera.m_Lens.FieldOfView - scrollInput * zoomSpeed;
        freeLookCamera.m_Lens.FieldOfView = Mathf.Clamp(newFOV, minZoom, maxZoom);
    }

    private void MoveCharacter()
    {
        valueX = Input.GetAxis("Horizontal");
        valueY = Input.GetAxis("Vertical");

        Vector3 movementInput = new Vector3(valueX, 0f, valueY).normalized;
        Vector3 moveDirection = mainCam.transform.forward.normalized * movementInput.z + mainCam.transform.right.normalized * movementInput.x;
        moveDirection.y = 0f;
        
        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            pufferChar.localRotation = Quaternion.Slerp(pufferChar.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        Vector3 moveVelocity = moveDirection * (speed * MoveMultiplier);
        PlayerVector = new Vector3(moveVelocity.x, 0, moveVelocity.z);
        
        if (popping)
            PlayerVector = new Vector3(moveVelocity.x, 0, moveVelocity.z);
        
        rb.AddForce(PlayerVector, ForceMode.Acceleration);
    }

    public void Jump(float jumpForceParam)
    {
        rb.AddTorque((transform.right + transform.up) * 2f, ForceMode.Impulse);
        if(!isGrounded || !JumpSkill) return;
        rb.AddForce(Vector3.up * jumpForceParam, ForceMode.Impulse);
    }

    private IEnumerator Balance()
    {
        float elapsedTime = 0f;
        float duration = 0.5f;
        Quaternion startRotation = rb.rotation;
        startRotation = Quaternion.Normalize(startRotation);
        Quaternion targetRotation = Quaternion.Normalize(Quaternion.identity);

        while (elapsedTime < duration)
        {
            anim.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            rb.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rb.rotation = targetRotation;
        //rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void GroundChecker()
    {
        isGrounded = (Physics.SphereCast(transform.position, 0.1f,
                Vector3.down, out hit, groundCheckDis,
                        LayerMask.GetMask("Ground")));

        if (isGrounded && !popping)
        {
            allowFootPlacement = (Physics.SphereCast(transform.position, 0.1f,
                -transform.up, out hit, groundCheckDis,
                LayerMask.GetMask("Ground")));
        }
        else 
            allowFootPlacement = OnSlope();
    }
    
    void Slope()
    {
        debugSlope = OnSlope();
        if (debugSlope && !exitingSlope)
        {
            rb.AddForce((speed * 1.75f * MoveMultiplier) * GetSlopeMoveDirection(), ForceMode.Acceleration);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * (speed * MoveMultiplier), ForceMode.Acceleration);
        }

        if (movingUpSlope)
        {
            foreach (var magnet in magnetHover)
            {
                magnet.Activated = false;
            }
        }
    }

    void setPopDrag()
    {
        rb.drag = PopDrag;
        rb.angularDrag = PopAngularDrag;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (currentSpeed > 10f && !isGrounded && popping)
        {
            camShaker.ActivateShake();
            //Debug.Log("shake");
        }
    }

    #endregion
}
