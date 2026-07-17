using UnityEngine;

public class DoubleJumpController : MonoBehaviour
{
    [Header("References")]
    public ShmackleRaycastLocomotion locomotion;       // existing locomotion script
    public PlayerInputListener playerInputListener;
    public Rigidbody playerRb;

    [Header("Double Jump Settings")]
    //public LayerMask doubleJumpLayer;            // invisible mid-air surface
    public float doubleJumpForce = 12f;
    public float maxJumpForce = 18f;
    public float sphereRadius = 0.12f;           // same as hand radius
    public bool resetOnGround = true;

    private bool hasDoubleJumped = false;

    private Vector3 cachedJumpDirLeft;
    private Vector3 cachedJumpDirRight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    [Header("Invisible floor")]
    public GameObject invisibleFloor;
    
    void Update()
    {
        if (locomotion == null || playerRb == null)
            return;

        if (resetOnGround && locomotion.playerController.isGrounded)
        {
            hasDoubleJumped = false;
        }

        // Both grips must be held to enable double jump
        bool gripsHeld =
            playerInputListener.leftGripState == PlayerInputListener.ButtonState.Holding &&
            playerInputListener.rightGripState == PlayerInputListener.ButtonState.Holding;

        if (!hasDoubleJumped && gripsHeld)
        {
            CheckVirtualDoubleJump(
                locomotion.lastLeftHandPosition,
                locomotion.GetClampedLeftHandPosition(),
                ref cachedJumpDirLeft);

            CheckVirtualDoubleJump(
                locomotion.lastRightHandPosition,
                locomotion.GetClampedRightHandPosition(),
                ref cachedJumpDirRight);
        }
    }
    
    private void CheckVirtualDoubleJump(Vector3 lastPos, Vector3 currentPos, ref Vector3 cachedDir)
    {
        Vector3 handMovement = currentPos - lastPos;

        // Hand must be moving (avoid noise)
        if (handMovement.sqrMagnitude < 0.001f)
            return;

        // Must be in mid-air
        if (locomotion.playerController.isGrounded)
            return;

        // Detect downward / backward swing
        if (handMovement.y < -0.05f)  // downward direction
        {
            cachedDir = (lastPos - currentPos).normalized;

            DoDoubleJump(cachedDir);
        }
    }


    private void DoDoubleJump(Vector3 jumpDir)
    {
        if (hasDoubleJumped)
            return;

        hasDoubleJumped = true;

        Vector3 jumpVel = jumpDir * doubleJumpForce;
        if (jumpVel.magnitude > maxJumpForce)
            jumpVel = jumpVel.normalized * maxJumpForce;

        // override current velocity (clean double jump)
        playerRb.linearVelocity = jumpVel;
    }
}
