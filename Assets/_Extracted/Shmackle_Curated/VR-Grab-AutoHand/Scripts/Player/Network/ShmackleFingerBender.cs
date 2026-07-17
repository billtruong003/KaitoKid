using System;
using Autohand;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Fusion;

public class ShmackleFingerBender : NetworkBehaviour
{
    public enum FingerType
    {
        Grip,
        Trigger,
        Primary,
        Undefine
    }

    [Serializable]
    public class Events
    {
        public UnityEvent<float[]> onBendAction = null;
        public UnityEvent<float[]> onUnbendAction = null;
    }
    
    #region ===== Fields =====

    [SerializeField]
    private FingerType type = FingerType.Undefine;

    [SerializeField]
    private Hand hand = null; // Your local hand reference

    [SerializeField]
    private InputActionProperty bendAction = default;

    [SerializeField]
    private InputActionProperty unbendAction = default;

    [SerializeField]
    private Events fingerEvents = new();
    
    [SerializeField]
    private bool isLocalOnly;
    
    [HideInInspector]public float[] bendOffsets;

    // We track whether it's currently pressed on this instance.
    // This will be updated locally and also synchronized via RPC for others.
    private bool pressed;

    #endregion
    
    #region ===== Properties =====

    public Events FingerEvents => fingerEvents;
    public FingerType Type => type;
    public Hand Hand => hand;

    #endregion
    
    #region ===== MonoBehaviour =====

    private void OnEnable()
    {
        //Debug.Log("S");
        if (isLocalOnly == true)
        {
            if (bendAction.action != null)
            {
                bendAction.action.Enable();
                bendAction.action.performed += BendActionLocal;
            }

            if (unbendAction.action != null)
            {
                unbendAction.action.Enable();
                unbendAction.action.performed += UnbendActionLocal;
            }
        }
    }

    private void OnDisable()
    {
        if (isLocalOnly == true)
        {
            if (bendAction.action != null)
                bendAction.action.performed -= BendActionLocal;

            if (unbendAction.action != null)
                unbendAction.action.performed -= UnbendActionLocal;
        }
    }
    #endregion
    
    #region ===== Fusion Lifecycle =====

    // Called after spawning. Good place to enable input for the owner.
    public override void Spawned()
    {
        if (isLocalOnly)
        {
            return;
        }
        
        // Only the client with input authority should enable input reading
        // or set up local input callbacks.
        if (Object.HasInputAuthority)
        {
            if (bendAction.action != null)
            {
                bendAction.action.Enable();
                bendAction.action.performed += BendActionLocal;
            }

            if (unbendAction.action != null)
            {
                unbendAction.action.Enable();
                unbendAction.action.performed += UnbendActionLocal;
            }
        }
    }

    // Called on shutdown or despawn. Good place to unsubscribe from events.
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (isLocalOnly)
        {
            return;
        }
        
        if (bendAction.action != null)
            bendAction.action.performed -= BendActionLocal;

        if (unbendAction.action != null)
            unbendAction.action.performed -= UnbendActionLocal;
    }

    #endregion
    
    #region ===== Local Input Handlers =====

    /// <summary>
    /// Called only on the client with input authority (owner).
    /// Invokes an RPC to bend for all clients.
    /// </summary>
    private void BendActionLocal(InputAction.CallbackContext context)
    {
        // We only do local logic if not already pressed
        if (!pressed)
        {
            pressed = true; // local set

            if (isLocalOnly)
            {
                BendExecuteLocal(bendOffsets);
            }
            else
            {
                // Trigger an RPC so all clients do this
                RPC_BendFingers(bendOffsets);
            }
        }
    }

    /// <summary>
    /// Called only on the client with input authority (owner).
    /// Invokes an RPC to unbend for all clients.
    /// </summary>
    private void UnbendActionLocal(InputAction.CallbackContext context)
    {
        // We only do local logic if currently pressed
        if (pressed)
        {
            pressed = false; // local set

            if (isLocalOnly)
            {
                UnbendExecuteLocal(bendOffsets);
            }
            else
            {
                // Trigger an RPC so all clients do this
                RPC_UnbendFingers(bendOffsets);
            }
        }
    }

    private void BendExecuteLocal(float[] offsets)
    {
        // On every client, we apply the offset and invoke local events
        if (hand != null && hand.fingers != null && offsets != null)
        {
            for (int i = 0; i < hand.fingers.Length; i++)
            {
                hand.fingers[i].bendOffset += offsets[i];
            }
        }
        fingerEvents?.onBendAction?.Invoke(offsets);
    }
    
    private void UnbendExecuteLocal(float[] offsets)
    {
        // On every client, we revert the offset and invoke local events
        if (hand != null && hand.fingers != null && offsets != null)
        {
            for (int i = 0; i < hand.fingers.Length; i++)
            {
                hand.fingers[i].bendOffset -= offsets[i];
            }
        }
        fingerEvents?.onUnbendAction?.Invoke(offsets);
    }

    #endregion
    
    #region ===== Networked Methods (RPCs) =====

    /// <summary>
    /// RPC that all clients receive. Bends the fingers on all clients.
    /// </summary>
    /// <param name="offsets">Offsets to add to the bend for each finger.</param>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_BendFingers(float[] offsets)
    {
        BendExecuteLocal(offsets);
    }

    /// <summary>
    /// RPC that all clients receive. Unbends the fingers on all clients.
    /// </summary>
    /// <param name="offsets">Offsets to remove from the bend for each finger.</param>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_UnbendFingers(float[] offsets)
    {
        UnbendExecuteLocal(offsets);
    }

    #endregion

}
