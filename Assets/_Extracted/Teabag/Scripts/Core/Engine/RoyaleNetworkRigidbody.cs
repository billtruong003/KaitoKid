using System;
using UnityEngine;
using Fusion;

namespace Teabag.Core
{
[RequireComponent(typeof(Rigidbody))]
public class RoyaleNetworkRigidbody : NetworkTRSP
{
    // Settings
    public const int DECIMALS = 4;
    [NonSerialized]
    public bool transmitting = true;
    // When true, Receive() skips applying replicated position/rotation.
    // Used by SpawnFromChest to prevent network sync from overwriting the local arc animation on proxy clients.
    [NonSerialized]
    public bool suppressSync = false;

    // Fields required for AOI positioning
    [Networked, OnChangedRender(nameof(Receive))]
    public Vector3 ReplicatedPosition { get; set; }
    [Networked, OnChangedRender(nameof(Receive))]
    public Quaternion ReplicatedRotation { get; set; }

    // Velocity
    [Networked, OnChangedRender(nameof(Receive))]
    public Vector3 velocity { get; set; }
    [Networked, OnChangedRender(nameof(Receive))]
    public Vector3 angularVelocity { get; set; }

    bool _kinematic;
    public bool kinematic
    {
        get
        {
            return Rigidbody.isKinematic;
        }
        set
        {
            if (_kinematic)
            {
                Rigidbody.isKinematic = true;
                return;
            }

            Rigidbody.isKinematic = value;
        }
    }

    //public override int PositionWordOffset => throw new NotImplementedException();

    [NonSerialized] public Rigidbody Rigidbody;
    bool hasRecieved = false;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        _kinematic = Rigidbody.isKinematic;
        Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public override void Spawned()
    {
        base.Spawned();
        UpdatePosition();
    }

    /*
    public override void Spawned()
    {
        base.Spawned();
        Transmit();
    }
    */

    /*
    public override void Render()
    {
        base.Render();
        called = false;
    }

    private void LateUpdate()
    {
        Transmit();
    }
    */

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        UpdatePosition();
    }

    public void UpdatePosition()
    {
        if (HasStateAuthority)
            Transmit();
        else
            Receive();
    }

    public void Transmit()
    {
        if (!transmitting)
            return;

        if (!HasStateAuthority)
            return;

        if (IsNan(transform.position))
            Physics.SyncTransforms();

        // Replicate transform
        ReplicatedPosition = Round(transform.position, DECIMALS);
        Vector3 euler = transform.eulerAngles;
        euler = Round(euler, DECIMALS);
        ReplicatedRotation = Quaternion.Euler(euler);

        if (!kinematic)
        {
            velocity = Round(Rigidbody.linearVelocity, DECIMALS);
            angularVelocity = Round(Rigidbody.angularVelocity, DECIMALS);
            //GameLogger.Info("Angular Velocity: " + angularVelocity);
        }
        else
        {
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;
        }
    }

    public void Receive()
    {
        if (!HasStateAuthority)
        {
            // Skip applying replicated position while another system (e.g. arc animation) drives the transform.
            if (suppressSync)
                return;

            if (IsNan(ReplicatedPosition))
            {
                Debug.LogError("Is NaN: " + ReplicatedPosition);
                return;
            }
            transform.position = ReplicatedPosition;
            transform.rotation = ReplicatedRotation;
            Rigidbody.linearVelocity = velocity;
            Rigidbody.angularVelocity = angularVelocity;
        }

        hasRecieved = true;
    }

    public static Vector3 Round(Vector3 vector3, int decimals = 2)
    {
        float multiplier = 1;
        for (int i = 0; i < decimals; i++)
            multiplier *= 10;
        float x = MathF.Round(vector3.x * multiplier) / multiplier;
        float y = MathF.Round(vector3.y * multiplier) / multiplier;
        float z = MathF.Round(vector3.z * multiplier) / multiplier;
        return new Vector3(x, y, z);
    }

    public static bool IsNan(Vector3 vector)
    {
        return float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z);
    }

}
}
