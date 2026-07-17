using Fusion;
using Teabag.Core;
using UnityEngine;

public class SpawnFromChest : NetworkBehaviour
{
    [Networked] private Vector3 StartPos { get; set; }
    [Networked] private Vector3 EndPos { get; set; }
    [Networked] private TickTimer Timer { get; set; }
    [Networked] private float ArcHeight { get; set; }
    [Networked] private float Duration { get; set; }


    private RoyaleNetworkRigidbody netRB;
    private Rigidbody rb;
    private Collider col;

    /// <summary>
    /// On non-authority clients, cache component references and suppress network position sync
    /// while the arc is in flight. This prevents RoyaleNetworkRigidbody.Recieve() from overwriting
    /// the locally-calculated arc position, which caused flickering.
    /// Timer.IsRunning is checked to handle late joiners who spawn after the arc has finished.
    /// </summary>
    public override void Spawned()
    {
        base.Spawned();

        if (!Object.HasStateAuthority)
        {
            TryGetComponent(out netRB);
            TryGetComponent(out rb);
            TryGetComponent(out col);

            if (Timer.IsRunning)
            {
                DisablePhysics();
                enabled = true; // Enable so Render() drives the arc animation on this client
            }
        }
    }

    public void Initialize(Vector3 start, Vector3 end, float height, float duration)
    {
        if (!Object.HasStateAuthority)
            return;

        StartPos = start;
        EndPos = end;
        ArcHeight = height;
        Duration = duration;

        Timer = TickTimer.CreateFromSeconds(Runner, duration);

        TryGetComponent(out netRB);
        TryGetComponent(out rb);
        TryGetComponent(out col);

        DisablePhysics();

        enabled = true;
    }

    public override void Render()
    {
        if (!Timer.IsRunning)
            return;

        float remaining = Timer.RemainingTime(Runner) ?? 0f;
        float t = Mathf.SmoothStep(0, 1, (Duration - remaining) / Duration);

        if (t >= 1f)
        {
            transform.position = EndPos;
            EnablePhysics();
            enabled = false;
            return;
        }

        Vector3 pos = Vector3.Lerp(StartPos, EndPos, t);

        float arc = ArcHeight * 4 * (t - t * t);
        pos.y += arc;

        transform.position = pos;
    }

    private void DisablePhysics()
    {
        if (netRB != null)
        {
            netRB.suppressSync = true; // Block Recieve() from overwriting position during arc
            netRB.kinematic = true;
            netRB.Transmit();
        }

        if (rb != null)
            rb.isKinematic = true;

        if (col != null)
            col.enabled = false;
    }

    private void EnablePhysics()
    {
        if (netRB != null)
        {
            netRB.suppressSync = false; // Restore normal network position sync after arc lands
            netRB.kinematic = false;
            netRB.Transmit();
        }

        if (rb != null)
            rb.isKinematic = false;

        if (col != null)
            col.enabled = true;
    }
}
