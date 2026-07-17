using System;
using System.Collections;
using DG.Tweening;
using Fusion;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System.IO;
using _Shmackle.Minigames;
using _Shmackle.Minigames.BloodJman;
using _Shmackle.Minigames.BoxingRing;
using Liv.Lck;
using Liv.Lck.Tablet;
using Shmackle.Runtime.UI.Phone;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR;

public class PlayerAbilities : NetworkBehaviour
{
    public ShmacklePlayerController playerController;
    public ShmackleNetworkRig playerNetworkRig;
    public CollisionEventListener playerCollision;
    public PlayerInputListener playerInputListener;

    [Header("Breakable Vent")]
    [FoldoutGroup("Breakable Vent")]public GameObject           vfx; // Reference to visual effect GameObject
    [FoldoutGroup("Breakable Vent")]public GameObject           energyChargeLeft;
    [FoldoutGroup("Breakable Vent")]public GameObject           energyChargeRight;
    [FoldoutGroup("Breakable Vent")]public VelocityForceOnSphereCastHit handForceL;
    [FoldoutGroup("Breakable Vent")]public VelocityForceOnSphereCastHit handForceR;
    [FoldoutGroup("Breakable Vent")]public bool                 CanBreakObject = false;  // Flag indicating if player can break objects
    
    [Header("Double Jump")]
    [FoldoutGroup("Double Jump")]public bool                 isDoubleJump;
    [FoldoutGroup("Double Jump")]public bool                 isHardDoubleJump;
    [FoldoutGroup("Double Jump")]public float                hardDoubleJumpGroundCheckDistance = 2.0f;
    [FoldoutGroup("Double Jump")]public VisualEffect         doubleJumpVisualEffect;
    [FoldoutGroup("Double Jump")]public AudioClip            doubleJumpSound;
    [FoldoutGroup("Double Jump")]public UnityEvent           OnDoubleJump;
    [FoldoutGroup("Double Jump")]public UnityEvent           OnFinishDoubleJump;
    [FoldoutGroup("Double Jump")]public LayerMask groundLayer;
    [FoldoutGroup("Double Jump")] public bool dontAllowDoubleJump;
    [FoldoutGroup("Double Jump")] public bool dontAllowDoubleJumpInBloodJman;
    [FoldoutGroup("Double Jump")] public bool enableLandingEffect = true; // Toggle landing effect on/off
    [FoldoutGroup("Double Jump")] private bool hasPlayedHitGroundEffect; // Flag to prevent playing effect twice
    [FoldoutGroup("Double Jump")] public float highVelocityFallThreshold = 8f; // Minimum fall speed to trigger effect
    [FoldoutGroup("Double Jump")] private bool wasHighVelocityFall; // Track if player reached high velocity while falling
    [Header("PlayerPunch")]
    [FoldoutGroup("PlayerPunch")]public PlayerPunchCollider playerPunchColliderLeft;
    [FoldoutGroup("PlayerPunch")]public PlayerPunchCollider playerPunchColliderRight;
    [FoldoutGroup("PlayerPunch")]private float holdForPunchTimeRight;
    [FoldoutGroup("PlayerPunch")]public float holdForPunchTimeLeft;
    
    [Header("Fist Bump")]
    [FoldoutGroup("Fist Bump")] public FistBump fistBumpLeft;
    [FoldoutGroup("Fist Bump")] public FistBump fistBumpRight;
    
    [Header("High Five")]
    [FoldoutGroup("High Five")] public PlayerHighFiveController highFiveLeft;
    [FoldoutGroup("High Five")] public PlayerHighFiveController highFiveRight;

    private bool isMoving;
    private Tween dashTween;
    private bool isDashCooldown;
    
    [Header("TakePictureByHand")]
    [FoldoutGroup("TakePictureByHand")] public bool disableTakePictureByHand;
    [FoldoutGroup("TakePictureByHand")]public bool isTakePictureByHand;
    [FoldoutGroup("TakePictureByHand")]public GameObject flashEffect;
    [FoldoutGroup("TakePictureByHand")]public AudioClip photographySound;
    [FoldoutGroup("TakePictureByHand")]public RenderTexture renderTexture;
    [FoldoutGroup("TakePictureByHand")]public GameObject cameraScreenshot;
    [FoldoutGroup("TakePictureByHand")]public GameObject screenRender;
    [FoldoutGroup("TakePictureByHand")]public GameObject screenHolder;
    [FoldoutGroup("TakePictureByHand")]public TextMeshProUGUI countDownText;
    [FoldoutGroup("TakePictureByHand")]public Image countDownBar;
    float timer = 0;
    [FoldoutGroup("TakePictureByHand")]public Transform screenPoint;
    
    [Header("SelfTurn")]
    [FoldoutGroup("SelfTurn")] public bool isActiveSnapTurn;
    [FoldoutGroup("SelfTurn")] public bool isActiveSmoothTurn;
    [FoldoutGroup("SelfTurn")] public float smoothTurnSpeed = 30;
    private float smoothTurnInput;

    [Header("PropHunt")] 
    [FoldoutGroup("PropHunt")] public PropHuntShootingController propHuntShootingNetwork;
    [FoldoutGroup("PropHunt")] public SnowGunController propHuntShootingOffline;
    [FoldoutGroup("PropHunt")] public GameObject armCamera;
    [FoldoutGroup("PropHunt")] public GameObject armDisplay;
    [FoldoutGroup("PropHunt")] public PropHunterControllerExtra hunterControllerExtra;

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;
    public GameObject arrowPrefab;
    
    private void Start()
    {
        playerController = GetComponent<ShmacklePlayerController>();
        
        playerCollision.onCollisionEnter.AddListener(StopDash);
        playerCollision.onCollisionEnter.AddListener(Landing);
        
        cameraScreenshot.SetActive(true);
        screenRender.SetActive(true);

        if (disableTakePictureByHand)
        {
            screenHolder.SetActive(false);
            cameraScreenshot.SetActive(false);
        }

        DOVirtual.DelayedCall(0.5f, () =>
        {
            screenHolder.SetActive(false);
            cameraScreenshot.SetActive(false);
        });


        if (PlayerPrefs.GetInt("selfTurn") == 1)
        {
            isActiveSnapTurn = true;
        }

        if (PlayerPrefs.HasKey("smoothTurnSpeed"))
        {
            smoothTurnSpeed = PlayerPrefs.GetFloat("smoothTurnSpeed");
        }

        if (playerController.playerModuleRef.shmackleNetworkRig.IsLocalNetworkRig)
        {
            doubleJumpVisualEffect.gameObject.SetActive(true);
            doubleJumpVisualEffect.Play();
        
            DOVirtual.DelayedCall(0.1f, () =>
            {
                doubleJumpVisualEffect.gameObject.SetActive(false);
            });
        }
    }
    
    void FixedUpdate()
    {
        if (isMoving)
        {
            playerController.playerRigidbody.linearVelocity =
                playerController.HeadCamera.transform.forward * 250 * Time.fixedDeltaTime;
        }
        
        
        // PlayerDoubleJumpVer2();
        // UpdateHandHistory();
        //
        // if (playerController.isGrounded)
        // {
        //     if (isDoubleJump)
        //     {
        //         isDoubleJump = false;
        //         playerController.playerRigidbody.linearVelocity = Vector3.zero;
        //         playerController.playerRigidbody.angularVelocity = Vector3.zero;
        //         OnFinishDoubleJump.Invoke();
        //     }
        // }
    }

    private bool IsSpectator()
    {
        return BloodJmanGameManager.Instance &&
               BloodJmanGameManager.Instance.IsPlayerSpectator(playerController.playerModuleRef.shmackleNetworkRig
                   .playerID);
    }
    
    private void Update()
    {
        if(!HasStateAuthority || 
           playerController.playerHealth.CurrentHealth <= 0 ||
           playerController.playerHealth.IsDead)
            return;

        // DRIP TRANSFORMATION
        if (playerNetworkRig.CanDripTransformation)
        {
            playerNetworkRig.CheckDripTransformationInput(playerInputListener);
        }
        
        //FIST BUMP
        if (IsSpectator())
        {
            fistBumpLeft.isActive = false;
            fistBumpRight.isActive = false;
        }
        else
        {
            if (playerInputListener.leftGripState          == PlayerInputListener.ButtonState.Holding
                && playerInputListener.leftTriggerState    == PlayerInputListener.ButtonState.Holding
                && playerInputListener.leftPrimaryButtonState == PlayerInputListener.ButtonState.Holding)
            {
                fistBumpLeft.isActive = true;
            }
            else
            {
                fistBumpLeft.isActive = false;
            }
        
            if (playerInputListener.rightGripState          == PlayerInputListener.ButtonState.Holding
                && playerInputListener.rightTriggerState    == PlayerInputListener.ButtonState.Holding
                && playerInputListener.rightPrimaryButtonState == PlayerInputListener.ButtonState.Holding)
            {
                fistBumpRight.isActive = true;
            }
            else
            {
                fistBumpRight.isActive = false;
            }
        }
        
        
        //PUNCH FUNCTION
        if (playerInputListener.leftTriggerState == PlayerInputListener.ButtonState.Holding && 
            playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding)
        {
            if (fistBumpLeft.isActive == false || (BoxingRingGameManager.Instance != null && BoxingRingGameManager.Instance.IsLocalPlayerInArenaZone()))
            {
                playerPunchColliderLeft.isActive = true;
            }
            else
            {
                playerPunchColliderLeft.isActive = false;
            }
            
            // if (playerController.physicsHandLeft.HandRigidbody.linearVelocity.magnitude > 1 && 
            //     playerController.isGrounded)
            // {
            //     PlayerDash();
            // }

            CanBreakObject = true;
            //wheelHandEffectL.SetActive(true);

        }
        else
        {
            if (fistBumpLeft.isActive             == false && ShmackleConnectionManager.Instance.IsBloodJmanMinigame() && 
                playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding)
            {
                playerPunchColliderLeft.isActive = true;
            }
            else
            {
                playerPunchColliderLeft.isActive = false;
            }
            
            holdForPunchTimeLeft = 0;
        }

        if (playerInputListener.rightTriggerState == PlayerInputListener.ButtonState.Holding &&
            playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding)
        {

            if (fistBumpRight.isActive == false || (BoxingRingGameManager.Instance != null && BoxingRingGameManager.Instance.IsLocalPlayerInArenaZone()))
            {
                playerPunchColliderRight.isActive = true;
            }
            else
            {
                playerPunchColliderRight.isActive = false;
            }
            
            // if (playerController.physicsHandRight.HandRigidbody.linearVelocity.magnitude > 1 && 
            //     playerController.isGrounded)
            // {
            //     PlayerDash();
            // }
            CanBreakObject = true;
                
        }
        else
        {
            if (fistBumpRight.isActive             == false && ShmackleConnectionManager.Instance.IsBloodJmanMinigame() && 
                playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding)
            {
                
                playerPunchColliderRight.isActive = true;
            }
            else
            {
                playerPunchColliderRight.isActive = false;
            }
            
            holdForPunchTimeRight = 0;
        }
        
        //DOUBLE JUMP FUNCTION
        if(playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
           playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding &&
           isDoubleJump == false &&
           playerController.isGrounded == false &&
           playerController.isHeadCollision == false &&
           playerController.physicsRig.wasLeftHandTouching == false &&
           playerController.physicsRig.wasRightHandTouching == false)
        {
            if (playerController.physicsHandLeft.isClimb ||
                playerController.physicsHandRight.isClimb ||
                dontAllowDoubleJump || 
                dontAllowDoubleJumpInBloodJman)
                return;


            if (isHardDoubleJump && playerController.ManuallyCheckGrounded(hardDoubleJumpGroundCheckDistance))
            {
                return;
            }
            
            if(playerController.autoHandLeft.IsHolding() && playerController.autoHandRight.IsHolding())
                return;
            
            isDoubleJump = true;
            hasPlayedHitGroundEffect = false; // Reset flag when starting double jump
            doubleJumpVisualEffect.gameObject.SetActive(true);
            doubleJumpVisualEffect.Play();
        
            playerController.audioSourceAlwaysOn.PlayOneShot(doubleJumpSound ,0.25f);
            
            //call event
            OnDoubleJump.Invoke();
            PlayerDoubleJump();
            playerController.audioSourceAlwaysOn.PlayOneShot(doubleJumpSound, 0.4f);
            RPC_SetActiveDoubleJump();
        }
        
        // Track high velocity falls (even without double jump)
        float currentFallSpeed = -playerController.playerRigidbody.linearVelocity.y;
        if (!playerController.isGrounded && currentFallSpeed >= highVelocityFallThreshold)
        {
            wasHighVelocityFall = true;
        }
        
        // Predictive ground detection - play effect slightly before landing
        // Works for both double jumps AND high velocity falls
        bool shouldCheckForLanding = (isDoubleJump || wasHighVelocityFall) && !hasPlayedHitGroundEffect && !playerController.isGrounded;
        
        if (shouldCheckForLanding)
        {
            // Only check when falling (negative Y velocity)
            float fallSpeed = currentFallSpeed;
            if (fallSpeed > 0.5f) // Only when actually falling
            {
                // Calculate how far to look ahead based on velocity
                // We want the effect to play ~0.05-0.1 seconds before landing
                float lookAheadTime = 0.02f; // seconds before landing to trigger effect
                float rayDistance = fallSpeed * lookAheadTime + 0.005f; // Add small buffer
                
                Vector3 rayOrigin = playerController.bodyCollider.transform.position;
                
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
                {
                    // Ground detected! Play effect early
                    hasPlayedHitGroundEffect = true;
                    PlayHitGroundEffect();
                    RPC_HitGround();
                }
            }
        }
        
        if (playerController.isGrounded)
        {
            // Handle double jump landing
            if (isDoubleJump)
            {
                isDoubleJump = false;
                playerController.playerRigidbody.linearVelocity = Vector3.zero;
                playerController.playerRigidbody.angularVelocity = Vector3.zero;
                
                // Only play effect if not already played by predictive detection
                if (!hasPlayedHitGroundEffect)
                {
                    PlayHitGroundEffect();
                    RPC_HitGround();
                }
                
                OnFinishDoubleJump.Invoke();
            }
            // Handle high velocity fall landing (without double jump)
            else if (wasHighVelocityFall && !hasPlayedHitGroundEffect)
            {
                PlayHitGroundEffect();
                RPC_HitGround();
            }
            
            // Reset flags when grounded
            hasPlayedHitGroundEffect = false;
            wasHighVelocityFall = false;
        }

        //SelfTurn
        if (isActiveSnapTurn)
        {
            // Detect left press (just pressed)
            if (playerInputListener.turnInput.x < -threshold && previousTurnInput.x >= -threshold)
            {
                Debug.Log("Joystick pressed LEFT");
                playerController.playerRigidbody.linearVelocity = new Vector3(0, playerController.playerRigidbody.linearVelocity.y , 0);
                RotateLeft();
            }

            // Detect right press (just pressed)
            if (playerInputListener.turnInput.x > threshold && previousTurnInput.x <= threshold)
            {
                Debug.Log("Joystick pressed RIGHT");
                playerController.playerRigidbody.linearVelocity = new Vector3(0, playerController.playerRigidbody.linearVelocity.y , 0);
                RotateRight();
            }

            // Store current for next frame
            previousTurnInput = playerInputListener.turnInput;
        }

        if (isActiveSmoothTurn)
        {
            float yaw = playerInputListener.turnInput.x * smoothTurnSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(yaw) > Mathf.Epsilon)
            {
                if (BoxingRingGameManager.Instance == null || !BoxingRingGameManager.Instance.IsLocalPlayerInPlayingZone())
                {
                    playerController.playerRigidbody.linearVelocity = Vector3.zero;
                }
               
                Quaternion turnRot = Quaternion.Euler(0f, yaw, 0f);
                playerController.playerRigidbody.MoveRotation(playerController.playerRigidbody.rotation * turnRot);
            }
        }


        //TAKE PICTURE
            if(disableTakePictureByHand)
                return;
            
            if (playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
                playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding &&
                playerInputListener.leftTriggerState != PlayerInputListener.ButtonState.Holding &&
                playerInputListener.rightTriggerState != PlayerInputListener.ButtonState.Holding &&
                playerInputListener.leftPrimaryButtonState != PlayerInputListener.ButtonState.Holding &&
                playerInputListener.rightPrimaryButtonState != PlayerInputListener.ButtonState.Holding)
            {
                Transform leftPos = playerController.LeftController.transform;
                Transform rightPos = playerController.RightController.transform;
                float distance = Vector3.Distance(leftPos.position, rightPos.position);
                
                
                
                Vector3 leftPalmNormal = playerController.autoHandLeft.transform.forward;
                Vector3 rightPalmNormal = playerController.autoHandRight.transform.forward;

                Vector3 leftToRight = (playerController.autoHandRight.transform.position - playerController.autoHandLeft.transform.position).normalized;
                Vector3 rightToLeft = -leftToRight; // same as (leftPos - rightPos).normalized

                float dotNormals = Vector3.Dot(leftPalmNormal, rightPalmNormal);

                // Check if opposite
                bool facingOpposite = dotNormals < -0.7f; // ~135° or more apart

                // Check if not facing each other
                bool leftFacingRight = Vector3.Dot(leftPalmNormal, leftToRight) > 0.5f;   // left palm points toward right hand
                bool rightFacingLeft = Vector3.Dot(rightPalmNormal, rightToLeft) > 0.5f; // right palm points toward left hand
                bool facingEachOther = leftFacingRight && rightFacingLeft;
                
                if (distance < 0.4f && 
                    facingOpposite && 
                    !facingEachOther && 
                    isTakePictureByHand == false)
                {
                    cameraScreenshot.SetActive(true);
                    screenHolder.SetActive(true);
                    
                    // Midpoint position
                    Vector3 midPoint = (leftPos.position + rightPos.position) / 2f;
                    screenHolder.transform.position = midPoint;
                    screenPoint.transform.position = midPoint;
                    
                    screenHolder.transform.parent = screenPoint;
                    screenHolder.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                    // Average rotation
                    /*Quaternion avgRotation = Quaternion.Slerp(
                        playerController.autoHandLeft.transform.rotation,
                        playerController.autoHandRight.transform.rotation,
                        0.5f
                    );

                    // Apply offset (e.g. rotate 90° around Y)
                    Quaternion offset = Quaternion.Euler(0f, 90f, 0f);
                    Quaternion targetRotation = avgRotation * offset;

                    // Smooth factor (adjust for slower/faster rotation)
                    float rotationSpeed = 5f;  // Lower = slower, Higher = faster

                    // Interpolate rotation over time for smooth movement
                    screenHolder.transform.rotation = Quaternion.Slerp(
                        screenHolder.transform.rotation,
                        targetRotation,
                        rotationSpeed * Time.deltaTime
                    );*/
                    
                    
                    timer += Time.deltaTime;
                    int displayNumber = Mathf.CeilToInt(timer);
                    displayNumber = Mathf.Clamp(displayNumber, 1, 3);
                    countDownText.text = displayNumber.ToString();
                    countDownBar.fillAmount = timer / 2.5f;

                    if (timer >= 2.5f)
                    {
                        //TO DO SOMETHING
                        isTakePictureByHand = true;
                        flashEffect.SetActive(true);
                        playerController.audioSourceAlwaysOn.PlayOneShot(photographySound, 1);
                        
                        //screenRender.SetActive(true);

                        Invoke(nameof(CaptureAndSaveScreenshot),0.1f);
                    
                        DOVirtual.DelayedCall(0.35f, () =>
                        {
                            flashEffect.SetActive(false);
                            cameraScreenshot.SetActive(false);
                            screenHolder.SetActive(false);
                            screenHolder.transform.parent = null;
                        });

                    
                        DOVirtual.DelayedCall(2.5f, () =>
                        {
                            isTakePictureByHand = false;
                            timer = 0;
                        });
                    }
                }
                else
                {
                    #if !UNITY_EDITOR
                    screenHolder.SetActive(false);
                    cameraScreenshot.SetActive(false);
                    screenHolder.transform.parent = null;
                    timer = 0;
                    #endif
                }
            }
            else
            {
                #if !UNITY_EDITOR
                screenHolder.SetActive(false);
                cameraScreenshot.SetActive(false);
                screenHolder.transform.parent = null;
                timer = 0;
                #endif
            }
           
    }
    

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_SetActiveDoubleJump()
    {
        doubleJumpVisualEffect.gameObject.SetActive(true);
        doubleJumpVisualEffect.Play();
        DOVirtual.DelayedCall(0.5f, () =>
        {
            doubleJumpVisualEffect.gameObject.SetActive(false); 
            
        });
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_HitGround()
    {
        // Skip for local player since we already played it immediately
        if (HasStateAuthority)
            return;
            
        PlayHitGroundEffect();
    }
    
    private void PlayHitGroundEffect()
    {
        if (!enableLandingEffect)
            return;
            
        var effect = playerController.playerModuleRef.shmackleNetworkRig.EffectHub.impactGroundEffect;
        Instantiate(effect, playerController.bodyCollider.transform.position - new Vector3(0 , playerController.bodyCollider.height/2, 0), Quaternion.identity);
    }
    

    [ContextMenu("PlayerDoubleJump")]
    void PlayerDoubleJump()
    {
        playerController.physicsRig.disableMovement = false;
        playerController.physicsRig.isTeleporting = false;
        playerController.playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        isDoubleJump = true;

        var head = playerController.HeadCamera.transform;
        var rb = playerController.playerRigidbody;

        // --- ✅ Always push UPWARD ---
        rb.AddForce(head.up * 10f, ForceMode.Impulse);

        // --- ✅ Always push FORWARD (ignore pushback) ---
        // Get the horizontal forward direction (no vertical component for natural movement)
        Vector3 forwardDir = head.forward;
        forwardDir.y = 0f; // Flatten to horizontal plane
        
        if (forwardDir.sqrMagnitude > 0.001f) // Check if we have a valid direction
        {
            forwardDir = forwardDir.normalized;
            rb.AddForce(forwardDir * 4f, ForceMode.Impulse); // Push forward
        }
        else
        {
            // Fallback: if somehow head forward is pointing straight up/down
            rb.AddForce(head.forward * 4f, ForceMode.Impulse);
        }
    }

    
    private void UpdateHandHistory()
    {
        if (!_initializedHandHistory)
        {
            _lastLeftHandPos  = playerController.autoHandLeft.palmTransform.position;
            _lastRightHandPos = playerController.autoHandRight.palmTransform.position;
            _initializedHandHistory = true;
            return;
        }

        _lastLeftHandPos  = playerController.LeftController.transform.position;
        _lastRightHandPos = playerController.RightController.transform.position;
    }
    
    private Vector3 _lastLeftHandPos;
    private Vector3 _lastRightHandPos;
    private bool _initializedHandHistory = false;
    void PlayerDoubleJumpVer2()
    {
        // --- Early exit conditions ---
        if (isDoubleJump || playerController.isGrounded || playerController.isHeadCollision)
            return;

        if (playerController.physicsHandLeft.isClimb || playerController.physicsHandRight.isClimb)
            return;

        // If both hands are grabbing props, don't double jump
        if (playerController.autoHandLeft.IsHolding() && playerController.autoHandRight.IsHolding())
            return;

        // Only trigger if both grips are held
        if (playerController.playerInputListener.leftGripState != PlayerInputListener.ButtonState.Holding ||
            playerController.playerInputListener.rightGripState != PlayerInputListener.ButtonState.Holding)
            return;

        // --- Tunable constants ---
        const float MinSwingSpeed      = 0.15f;   // more sensitive than 0.7
        const float SwingToStrength    = 0.22f;  // how much swing adds to strength
        const float MaxSpeedStrength   = 12f;    // clamp from swing
        const float MaxTotalStrength   = 24f;    // safety cap
        const float UpBlend            = 2f;     // upward bias

        // ==========================
        // 1) Swing-based sensitivity
        // ==========================
        Vector3 leftPosNow  = playerController.LeftController.transform.position;
        Vector3 rightPosNow = playerController.RightController.transform.position;

        Vector3 leftSwing  = leftPosNow  - _lastLeftHandPos;
        Vector3 rightSwing = rightPosNow - _lastRightHandPos;

        float leftSpeed  = leftSwing.magnitude / Time.deltaTime;
        float rightSpeed = rightSwing.magnitude / Time.deltaTime;

        // Average swing speed of both hands
        float avgSpeed = (leftSpeed + rightSpeed) * 0.5f;

        // Not swinging hard enough → no double jump
        if (avgSpeed < MinSwingSpeed)
            return;

        // Combined swing direction (for “are you pushing back?” check)
        Vector3 combinedSwing = leftSwing + rightSwing;
        float combinedMag = combinedSwing.magnitude;
        Vector3 combinedSwingDir = combinedMag > 0.0001f ? combinedSwing / combinedMag : Vector3.zero;

        // ==========================
        // 2) Arm direction logic (forward is priority, but FLAT on XZ)
        // ==========================
        #region calculate arm direction

        // --- References ---
        var leftHand  = playerController.LeftController.transform;
        var rightHand = playerController.RightController.transform;
        var head      = playerController.HeadCamera.transform;
        var rb        = playerController.playerRigidbody;

        Vector3 toLeftHand  = leftHand.position  - head.position;
        Vector3 toRightHand = rightHand.position - head.position;

        // --- Calculate direction first ---
        Vector3 avgHandDir = ((toLeftHand.normalized + toRightHand.normalized) * 0.5f).normalized;

        // FLATTENED head axes (ignore pitch) → safe for VR
        Vector3 flatForward = head.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.001f)
            flatForward = new Vector3(head.transform.forward.x, 0f, head.transform.forward.z);
        flatForward = flatForward.normalized;

        Vector3 flatRight = head.right;
        flatRight.y = 0f;
        if (flatRight.sqrMagnitude < 0.001f)
            flatRight = new Vector3(head.transform.right.x, 0f, head.transform.right.z);
        flatRight = flatRight.normalized;

        // Also flatten hand direction when doing dot tests
        Vector3 avgHandDirFlat = avgHandDir;
        avgHandDirFlat.y = 0f;
        if (avgHandDirFlat.sqrMagnitude > 0.0001f)
            avgHandDirFlat = avgHandDirFlat.normalized;

        // Local space projection (relative to FLAT head axes)
        float forwardDot = Vector3.Dot(flatForward, avgHandDirFlat);
        float rightDot   = Vector3.Dot(flatRight,   avgHandDirFlat);
        // float upDot   = Vector3.Dot(Vector3.up,  avgHandDir); // currently not needed

        // --- Determine push direction and base strength ---
        // Default: always forward (priority, flat)
        Vector3 pushDir = flatForward;
        float baseStrength = 11f; // default forward boost

        bool bothHandsForward     = forwardDot > 0.45f;          // clearly in front horizontally
        bool centeredHorizontally = Mathf.Abs(rightDot) < 0.25f; // both hands near center

        // Forward-centered hands → strong forward dash
        if (bothHandsForward && centeredHorizontally)
        {
            pushDir = flatForward;
            baseStrength = 14f;
        }
        else
        {
            // Side push: still has forward, but with more lateral
            if (Mathf.Abs(rightDot) > 0.3f)
            {
                Vector3 side = (rightDot > 0) ? -flatRight : flatRight; // opposite to side
                // Blend: forward has priority but side is stronger here
                pushDir = (flatForward * 0.5f + side * 1.0f).normalized;
                baseStrength = 15f;
            }
            // Hands behind → weaker forward push (still forward, no backward)
            else if (forwardDot < -0.3f)
            {
                pushDir = flatForward;
                baseStrength = 11f;
            }
        }

        // If we somehow lost direction, bail
        if (pushDir.sqrMagnitude < 0.0001f)
            return;

        // ==========================
        // 2.5) Check swing matches push direction (accuracy)
        // ==========================

        // Require that hands moved roughly opposite to pushDir (like a real push)
        if (combinedSwingDir == Vector3.zero || Vector3.Dot(combinedSwingDir, -pushDir) < 0.15f)
            return;

        // Blend a small upward component to feel like a jump
        Vector3 finalPushDir = (pushDir + Vector3.up * UpBlend).normalized;

        #endregion

        // ==========================
        // 3) Combine pose + swing for strength
        // ==========================

        // Scale with swing speed so harder swings give stronger jump
        float speedFactor = Mathf.Clamp(avgSpeed * SwingToStrength, 0f, MaxSpeedStrength);
        float finalStrength = baseStrength + speedFactor;

        // Safety clamp so it never explodes
        finalStrength = Mathf.Min(finalStrength, MaxTotalStrength);

        Vector3 jumpForce = finalPushDir * finalStrength;

        // Apply instant velocity change
        rb.AddForce(jumpForce, ForceMode.VelocityChange);

        isDoubleJump = true;
        playerController.audioSourceAlwaysOn.PlayOneShot(doubleJumpSound, 0.4f);
        RPC_SetActiveDoubleJump();
    }

    
    [ContextMenu("PlayerDash")]
    void PlayerDash()
    {
        if(isDashCooldown)
            return;
        isDashCooldown = true;
        DOVirtual.DelayedCall(1, () =>
        {
            isDashCooldown = false;
        });
    }

    public void WasKicked(Vector3 direction)
    {
        RPC_WasKicked(direction);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_WasKicked(Vector3 direction)
    {
        if (!HasStateAuthority)
        {
            return;   
        }

        // For Jman Test
        playerController = GetComponent<ShmacklePlayerController>(); // Remove Later
        playerController.playerRigidbody.isKinematic = false; // Remove Later
        playerController.playerRigidbody.useGravity = true; // Remove later
        
        if (playerController)
        {
            playerController.playerRigidbody.AddForce(direction, ForceMode.VelocityChange);
            playerPunchColliderLeft.PlaySlapButtSound();
        }
    }


    private void StopDash()
    {
        dashTween.Kill();
    }

    void Landing()
    {
        playerController.physicsRig.disableMovement = false;
        playerController.physicsRig.isTeleporting = false;
        
        playerController.playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
    }
    
    
    [ContextMenu("Capture Screenshot")]
    void CaptureAndSaveScreenshot()
    {
        // Set active render texture
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        // Create a Texture2D from the RenderTexture
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        
        Color[] pixels = screenshot.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = pixels[i].gamma; // convert from linear to sRGB
        }
        screenshot.SetPixels(pixels);
        
        screenshot.Apply();

        // Rotate the screenshot by 90 degrees clockwise
        //Texture2D rotatedScreenshot = RotateTexture(screenshot, 90);

        // Save to persistent data path for internal reference
        string screenshotName = "Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(Application.persistentDataPath, screenshotName);
        File.WriteAllBytes(filePath, screenshot.EncodeToPNG());

        // Save to gallery using NativeGallery
        NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(screenshot, "MyGallery", screenshotName);

        if (permission == NativeGallery.Permission.Granted)
        {
            Debug.Log("Screenshot saved to gallery: " + screenshotName);
        }
        else
        {
            Debug.Log("Failed to save screenshot to gallery.");
        }

        // Reset render texture
        RenderTexture.active = currentRT;

        // Optionally clean up textures from memory
        Destroy(screenshot);
    }


    [ContextMenu("Debug Show Screen")]
    public void DebugShowScreen()
    {
        screenHolder.transform.parent = null;
        screenHolder.SetActive(true);
        cameraScreenshot.SetActive(true);
        Transform leftPos = playerController.LeftController.transform;
        Transform rightPos = playerController.RightController.transform;
        
        // Midpoint between hands
        Vector3 midPoint = (leftPos.position + rightPos.position) / 2f;

        // Place screen holder there
        screenHolder.transform.position = midPoint;
        screenPoint.transform.position = midPoint;
                
        screenHolder.transform.parent = screenPoint;
        screenHolder.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        
        
        /*Quaternion avgRotation = Quaternion.Slerp(
            playerController.autoHandLeft.transform.rotation,
            playerController.autoHandRight.transform.rotation,
            0.5f
        );
        
        
        // Apply offset (e.g. rotate 90° around Y)
        Quaternion offset = Quaternion.Euler(0f, 90f, 0f);

        // Final rotation
        screenHolder.transform.rotation = avgRotation * offset;*/
        
       
    }


    private Vector2 previousTurnInput;
    private bool isRotate;
    private float threshold = 0.5f;
    
    // Rotate character 30 degrees on Y-axis over 0.5 seconds
    [ContextMenu("Snap Rotate Right")]
    public void RotateRight()
    {
        isRotate = true;
        //playerController.physicsRig.disableMovement = true;
        //playerController.physicsRig.ToggleNoCollideMode(true);
        transform.DOLocalRotate(
            new Vector3(0, transform.eulerAngles.y + 90f, 0), 
            0.1f, 
            RotateMode.Fast
        ).OnComplete(() =>
        {
            //playerController.physicsRig.disableMovement = false;
            //playerController.physicsRig.ToggleNoCollideMode(false);
        });
    }

    // Rotate character 30 degrees on Y-axis to the left
    [ContextMenu("Snap Rotate Left")]
    public void RotateLeft()
    {
        isRotate = true;
        //playerController.physicsRig.disableMovement = true;
        //playerController.physicsRig.ToggleNoCollideMode(true);
        transform.DOLocalRotate(
            new Vector3(0, transform.eulerAngles.y - 90f, 0), 
            0.1f, 
            RotateMode.Fast
        ).OnComplete(() =>
        {
            //playerController.physicsRig.disableMovement = false;
            //playerController.physicsRig.ToggleNoCollideMode(false);
            
        });
    }
    
    public void ActiveHunterControllerExtra()
    {
        hunterControllerExtra.ChangeAbilityContainerState(true);
    }
}
