using System.Collections;
using Autohand;
using UnityEngine;
using UnityEngine.Splines;
using JankyAudio;
using NaughtyAttributes;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class PhysicsHand : MonoBehaviour
{
    public Hand hand;
    public Vector3 handVelocity;
    
    [HorizontalLine]
    [Header("PID")]
    [SerializeField] float frequency = 50f;
    [SerializeField] float damping = 1f;
    [SerializeField] float rotFrequency = 100f;
    [SerializeField] float rotDamping = 0.9f;
    public Rigidbody playerRigidbody;
    public CapsuleCollider bodyCollider;
    
    public Transform target;
    Transform savedTarget;
    [SerializeField] Vector3 targetOffset;


    public bool isClimb;
    //public bool isRotatingFast;
    public bool isLeftHand;
    public ShmacklePlayerController shmacklePlayerController;


    [HorizontalLine] 
    [Header("Springs")] 
    [SerializeField] public float climbForce = 1000f;
    [SerializeField] public float climbDrag = 500f;
    //public float maxForce = 10f; // Adjust this value as needed


    Vector3 _previousPosition;
    public Rigidbody _rigidbody;
    public bool _isColliding;


    [HorizontalLine]
    [Header("Climb")]
    public GameObject controller;
    public Transform climbPoint;


    [HorizontalLine]
    [Header("Sound")]
    [SerializeField] AudioSource audioSource; // Reference to the AudioSource

    [SerializeField] AudioClip[] movementSounds; // Sound to play
    [SerializeField] float movementThreshold = 0.1f; // Threshold to consider as "moving"
    [SerializeField] float minPitch = 0.5f; // Minimum pitch
    [SerializeField] float maxPitch = 2.0f; // Maximum pitch
    [SerializeField] float impactForceThreshold = 10f; // Minimum impact force to consider
    [SerializeField] float maxImpactForce = 100f; // Maximum force for pitch adjustment
    public JankyAudioSource jankyAudioSource;


    [HorizontalLine]
    [Header("Detect Ground")]
    public float groundCheckDistance = 0.1f; // Distance to check for ground
    public LayerMask groundLayer; // Set this to the layer your ground objects are on
    public bool isGrounded;

    public GameObject debugSphere;

    float startClimbForce;
    Vector3 force;
    [HideInInspector] public Vector3 displacementFromResting;

    public bool isHoldPhone;
    [HideInInspector] public bool isTouchJumpBox;

    private Vector3 lastHandPosition;
    
    
    void Start()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
        _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.maxAngularVelocity = float.PositiveInfinity;

        _previousPosition = transform.position;

        savedTarget = target;

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        startClimbForce = climbForce;
        
        lastHandPosition = target.position;

    }

    private void Update()
    {
        CheckGround();
        if (isClimb)
        {
            Climbing();
            //climbForce = startClimbForce;
            //return;
        }

        // Calculate velocity as (currentPosition - previousPosition) / deltaTime
        /*handVelocity = (target.position - _previousPosition) / Time.deltaTime;

        climbForce = startClimbForce * handVelocity.magnitude;
        climbForce = Mathf.Clamp(climbForce, 10, startClimbForce - 200);

        // Update the previous position for the next frame
        _previousPosition = target.position;*/

    }

    private void FixedUpdate()
    {
        // If not touching a surface, use PID to move hand towards target
        PIDMovement();
        PIDRotation();
        
        if (_isColliding ||
            isClimb)
        {
            HookesLaw();
        }
    }

    // void PIDMovement()
    // {
    //     float kp = (6f * frequency) * (6f * frequency) * 0.25f;
    //     float kd = 4.5f * frequency * damping;
    //     float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
    //     float ksg = kp * g;
    //     float kdg = (kd + kp * Time.fixedDeltaTime) * g;
    //     Vector3 force = (target.position - transform.position) * ksg + (playerRigidbody.velocity - _rigidbody.velocity) * kdg;
    //
    //     _rigidbody.AddForce(force, ForceMode.Acceleration);
    // }
    
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
        Vector3 distanceTraveled = (target.position - transform.position) + Vector3.down * 2f * 9.8f * Time.fixedDeltaTime * Time.fixedDeltaTime;

        // Apply PID-based force correction
        Vector3 force = distanceTraveled * ksg + (playerRigidbody.linearVelocity - _rigidbody.linearVelocity) * kdg;
    
        _rigidbody.AddForce(force, ForceMode.Acceleration);

    }
    
    void PIDRotation()
    {
        float kp = (6f * rotFrequency) * (6f * rotFrequency) * 0.25f;
        float kd = 4.5f * rotFrequency * rotDamping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Quaternion q = target.rotation * Quaternion.Inverse(transform.rotation);
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
        Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;
        _rigidbody.AddTorque(torque, ForceMode.Acceleration);

        
    }


    public void HookesLaw()
    {
        if(isTouchJumpBox)
            return;
        
        displacementFromResting = transform.position - target.position;
        force = displacementFromResting * climbForce;
        float drag = GetDrag();
        
        playerRigidbody.AddForce(force, ForceMode.Acceleration);
        playerRigidbody.AddForce(drag * -playerRigidbody.linearVelocity * climbDrag, ForceMode.Acceleration);
    }


    float GetDrag()
    {
        Vector3 handVelocity = (target.localPosition - _previousPosition) / Time.fixedDeltaTime;
        float drag = 1 / handVelocity.magnitude + 0.01f;
        drag = drag > 1 ? 1 : drag;
        drag = drag < 0.03f ? 0.03f : drag;
        _previousPosition = transform.position;
        return drag;
    }


    public void ReOrientHand()
    {
        if(isLeftHand)
        {
            hand.transform.localRotation = shmacklePlayerController.LeftController.transform.rotation;
        }
        else
        {
            hand.transform.localRotation = shmacklePlayerController.RightController.transform.rotation;
        }
        
    }


    void OnCollisionEnter(Collision collision)
    {
        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        
        if(isTouchJumpBox || isClimb)
            return;
        _isColliding = true;
        
        //Play sound and Haptic
        if (isLeftHand)
        {
            shmacklePlayerController.playLeftHandHaptic(0.5f, 0.07f);
            PlayMovementSound();
        }
        else
        {
            shmacklePlayerController.playRightHandHaptic(0.5f, 0.07f);
            PlayMovementSound();
        }
    }


    void OnCollisionExit(Collision other)
    {
        _isColliding = false;
    }

    public void Climbing()
    {
        transform.position = climbPoint.position;
            
        // if (isLeftHand)
        // {
        //     transform.rotation = playerClimbing.leftHand.transform.rotation;
        // }
        // else
        // {
        //     transform.rotation = playerClimbing.rightHand.transform.rotation;
        // }
    }


    #region Sound
    void PlayMovementSound()
    {
        if (isClimb)
            return;
        audioSource.PlayOneShot(movementSounds[Random.Range(0, movementSounds.Length)] , 0.5f);
    }
    #endregion

    bool CheckGround()
    {
        // Perform a raycast downwards
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // Debug log for visualization
        if (isGrounded)
        {
            shmacklePlayerController.isGrounded = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        // Visualize the ground check in the editor
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);

    }
    
    
}
