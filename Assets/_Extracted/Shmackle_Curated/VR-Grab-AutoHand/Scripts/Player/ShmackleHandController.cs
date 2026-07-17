using System;
using Autohand;
using DG.Tweening;
using UnityEngine;

public class ShmackleHandController : MonoBehaviour
{
    public bool isHoldPhone;
    public float handVelocity;
    public Vector3 linearVelocity;
    Quaternion lastRotation;
    public Vector3 angularVelocity;
    
    public Transform handTransform;
    public ShmackleRaycastLocomotion playerLocomotion;
    public ShmacklePlayerController playerController;
    public Hand autoHand;
    public bool isLeft;
    public Rigidbody playerRigidbody;
    public Rigidbody HandRigidbody;
    
    // Internal state: store last frame’s position
    private Vector3 _lastPosition;
    
    [SerializeField] public float climbForce = 1000f;
    [SerializeField] public float climbDrag = 500f;
    private Vector3 _previousPosition;
    public bool _isColliding;
    
    public bool isClimb;
    [SerializeField]private LayerMask climbMask;
    [SerializeField]private FixedJoint currentJoint;
    public ParticleSystem climbEffect;
    
    [Header("PID")]
    [SerializeField] float frequency = 50f;
    [SerializeField] float damping = 1f;
    [SerializeField] float rotFrequency = 100f;
    [SerializeField] float rotDamping = 0.9f;
    
    
    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip climbSound;

    public bool canClimb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _lastPosition = transform.position;
        _previousPosition = transform.position;
        lastRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        //ANGULAR VELOCITY
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);

        // Convert deltaRotation to angle-axis representation
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        // Convert degrees to radians
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;

        // Calculate angular velocity in radians per second
        angularVelocity = (rotationAxis * angleInRadians) / Time.deltaTime;

        lastRotation = transform.rotation;
    }
    
    private void FixedUpdate()
    {
        PIDMovement();
        PIDRotation();
        
        // HAND VELOCITY
        // 1. Get current position
        Vector3 currentPosition = transform.position;

        // 2. Compute displacement since last frame
        Vector3 deltaPos = currentPosition - _lastPosition;

        // 3. Divide by deltaTime to get velocity vector (in units/sec)
        Vector3 velocityVec = deltaPos / Time.deltaTime;

        // 4. Store the full velocity vector and magnitude (speed)
        linearVelocity = velocityVec;         // Vector3
        handVelocity = velocityVec.magnitude;     // float

        // 5. Update _lastPosition for the next frame
        _lastPosition = currentPosition;
    }
    
    void PIDMovement()
    {
        // PID Gains
        float kp = (6f * frequency) * (6f * frequency) * 0.25f;
        float kd = 4.5f * frequency * damping;

        // Discrete Time Approximation Factors
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;

        // Compute Movement Delta
        Vector3 distanceTraveled = (handTransform.position - transform.position) + Vector3.down * 2f * 9.8f * Time.fixedDeltaTime * Time.fixedDeltaTime;

        // Apply PID-based force correction
        Vector3 force = distanceTraveled * ksg + (playerRigidbody.linearVelocity - HandRigidbody.linearVelocity) * kdg;
    
        HandRigidbody.AddForce(force, ForceMode.Force);

    }
    
    void PIDRotation()
    {
        float kp = (6f * rotFrequency) * (6f * rotFrequency) * 0.25f;
        float kd = 4.5f * rotFrequency * rotDamping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Quaternion q = handTransform.rotation * Quaternion.Inverse(transform.rotation);
        if (q.w < 0)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
        }
        q.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();
        axis *= Mathf.Deg2Rad;
        Vector3 torque = ksg * axis * angle + - HandRigidbody.angularVelocity * kdg;
        HandRigidbody.AddTorque(torque, ForceMode.Acceleration);

        
    }
    
    void AddForce()
    {
        Vector3 displacementFromResting = transform.position - handTransform.position;
        Vector3 force = displacementFromResting * climbForce;
        float drag = GetDrag();
        
        playerRigidbody.AddForce(force, ForceMode.Acceleration);
        playerRigidbody.AddForce(drag * -playerRigidbody.linearVelocity * climbDrag, ForceMode.Acceleration);
    }
    
    float GetDrag()
    {
        Vector3 handVelocity = (handTransform.localPosition - _previousPosition) / Time.fixedDeltaTime;
        float drag = 1 / handVelocity.magnitude + 0.01f;
        drag = drag > 1 ? 1 : drag;
        drag = drag < 0.03f ? 0.03f : drag;
        _previousPosition = transform.position;
        return drag;
    }


    private Vector3 climbPoint;
    [HideInInspector]public FixedJoint joint;
    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbMask) != 0)
        {
            
            Debug.Log("climb hit " + other.gameObject.name);
            if (isLeft)
            {
                #if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    if (isClimb == false)
                    {
                        isClimb = true;
                        joint = gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = other.gameObject.GetComponent<Rigidbody>();
                        audioSource.clip = climbSound;
                        audioSource.Play();
                        playerController.physicsHandRight.isClimb = false;
                        Destroy(playerController.physicsHandRight.joint);
                        
                        climbEffect.Play();
                            
                    }
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    isClimb = false;
                    Destroy(joint);
                    if (playerController.physicsHandLeft.isClimb == false &&
                        playerController.physicsHandRight.isClimb == false)
                    {
                        playerLocomotion.disableMovement = false;
                    }
                    AddForce();
                    climbEffect.Stop();
                }
                #endif

                #if !UNITY_EDITOR
                if (playerController.playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding)
                {
                    if (isClimb == false)
                    {
                        canClimb = false; //because already climb
                        isClimb = true;
                        joint = gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = other.gameObject.GetComponent<Rigidbody>();
                        audioSource.clip = climbSound;
                        audioSource.Play();
                        playerController.physicsHandRight.isClimb = false;
                        Destroy(playerController.physicsHandRight.joint);
                        climbEffect.Play();
                            
                    }
                }
                else
                {
                    isClimb = false;
                    Destroy(joint);
                    if (playerController.physicsHandLeft.isClimb == false &&
                        playerController.physicsHandRight.isClimb == false)
                    {
                        playerLocomotion.disableMovement = false;
                        playerLocomotion.isTeleporting = false;
                    }
                    AddForce();
                    climbEffect.Stop();
                }
                #endif
                if (isClimb)
                {
                    playerRigidbody.interpolation = RigidbodyInterpolation.None;
                    
                    playerLocomotion.disableMovement = true;
                    playerLocomotion.isTeleporting = true;
                    playerLocomotion.rigidBodyMovement = playerController.gameObject.transform.position;
                    AddForce();
                }
                
            }
            else
            {
                #if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    if (isClimb == false)
                    {
                        isClimb = true;
                        joint = gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = other.gameObject.GetComponent<Rigidbody>();
                        audioSource.clip = climbSound;
                        audioSource.Play();
                        playerController.physicsHandLeft.isClimb = false;
                        Destroy(playerController.physicsHandLeft.joint);
                        climbEffect.Play();
                            
                    }
                }

                else if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    isClimb = false;
                    Destroy(joint);
                    if (playerController.physicsHandLeft.isClimb == false &&
                        playerController.physicsHandRight.isClimb == false)
                    {
                        playerLocomotion.disableMovement = false;
                    }
                    AddForce();
                    climbEffect.Stop();
                }
                #endif
                
                #if !UNITY_EDITOR
                if (playerController.playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding)
                {
                    if (isClimb == false)
                    {
                        isClimb = true;
                        joint = gameObject.AddComponent<FixedJoint>();
                        joint.connectedBody = other.gameObject.GetComponent<Rigidbody>();
                        audioSource.clip = climbSound;
                        audioSource.Play();
                        playerController.physicsHandLeft.isClimb = false;
                        Destroy(playerController.physicsHandLeft.joint);
                        climbEffect.Play();
                            
                    }
                }
                else
                {
                    isClimb = false;
                    Destroy(joint);
                    if (playerController.physicsHandLeft.isClimb == false &&
                        playerController.physicsHandRight.isClimb == false)
                    {
                        playerLocomotion.disableMovement = false;
                        playerLocomotion.isTeleporting = false;
                    }
                    AddForce();
                    climbEffect.Stop();
                }
                #endif
                
                if (isClimb)
                {
                    playerRigidbody.interpolation = RigidbodyInterpolation.None;
                    
                    playerLocomotion.disableMovement = true;
                    playerLocomotion.isTeleporting = true;
                    playerLocomotion.rigidBodyMovement = playerController.gameObject.transform.position;
                    AddForce();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbMask) != 0)
        {
            canClimb = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & climbMask) != 0)
        {
            canClimb = false;
            Destroy(joint);
            if (playerController.physicsHandLeft.isClimb == false &&
                playerController.physicsHandRight.isClimb == false)
            {
                playerLocomotion.disableMovement = false;
                playerLocomotion.isTeleporting = false;
                playerController.playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }

    [ContextMenu("Start Climbing")]
    public void StartClimbing()
    {
        if(isClimb)
            return;
        
        isClimb = true;
        _isColliding = true;
        HandRigidbody.isKinematic = false;
        if(audioSource)
            audioSource.PlayOneShot(climbSound);
    }


    public void StopClimbing()
    {
        _isColliding = false;
        HandRigidbody.isKinematic = true;
        Destroy(currentJoint);
        
        DOVirtual.DelayedCall(0.25f, () =>
        {
            isClimb = false;
            HandRigidbody.isKinematic = false;
        });
    }
}
