using System;
using System.Collections;
using System.Collections.Generic;
using Micosmo.SensorToolkit;
using UnityEngine;

public class CullingPlayer : MonoBehaviour
{
    public RangeSensor rangeSensor;
    public ShmackleNetworkRig localNetworkRig;
    
    private void Start()
    {
        rangeSensor.OnDetected.AddListener(onPlayerDetected);
        rangeSensor.OnLostDetection.AddListener(onPlayerLostDetection);
    }


    private void onPlayerDetected(GameObject detectedObject, Micosmo.SensorToolkit.Sensor sensor)
    {
        Debug.Log($"Player mod detected: {detectedObject.name} by sensor: {sensor.name}");
        ShmackleNetworkRig _player = detectedObject.GetComponentInParent<ShmackleNetworkRig>();
        if (_player && _player != localNetworkRig)
        {
            _player.characterIK.gameObject.SetActive(true);
        }
    }
    
    
    private void onPlayerLostDetection(GameObject detectedObject, Micosmo.SensorToolkit.Sensor sensor)
    {
        Debug.Log($"Player mod Lost Detection: {detectedObject.name} by sensor: {sensor.name}");
        ShmackleNetworkRig _player = detectedObject.GetComponentInParent<ShmackleNetworkRig>();
        if (_player && _player != localNetworkRig)
        {
            _player.characterIK.gameObject.SetActive(false);
        }
    }
}
