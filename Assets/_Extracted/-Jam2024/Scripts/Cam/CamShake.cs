using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//Make a camera Holder and hook this shiet on that
public class CamShake : MonoBehaviour
{
    #region Variables
    public bool camShakeAcive = true; //on or off
    public bool Shaking; //on or off
    [Range(0, 1)]
    [SerializeField] float trauma;
    [SerializeField] float traumaMult = 16; //the power of the shake
    [SerializeField] float traumaMag = 0.8f; //the range of movement
    [SerializeField] float traumaRotMag = 17f; //the rotational power
    [SerializeField] float traumaDepthMag = 0.6f; //the depth multiplier
    [SerializeField] float traumaDecay = 1.3f; //how quickly the shake falls off
    [SerializeField] float extraAdd = 0f; //extra when stack

    float timeCounter = 0; //counter stored for smooth transition
    public static CamShake Instance { get; private set; }
    #endregion
    
    #region Accessors
    public float Trauma //accessor is used to keep trauma within 0 to 1 range
    {
        get
        {
            return trauma;
        }
        set
        {
            trauma = Mathf.Clamp01(value);
        }
    }
    
    //Get a perlin float between -1 & 1, based off the time counter.
    float GetFloat(float seed)
    {
        return (Mathf.PerlinNoise(seed, timeCounter) - 0.5f) * 2f;
    }
    
    //use the above function to generate a Vector3, different seeds are used to ensure different numbers
    Vector3 GetVec3()
    {
        return new Vector3(
            GetFloat(1),
            GetFloat(10),
            //deapth modifier applied here
            GetFloat(100) * traumaDepthMag
        );
    }
    #endregion

    #region Unity Method
    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (camShakeAcive && Trauma > 0)
        {
            //increase the time counter (how fast the position changes) based off the traumaMult and some root of the Trauma
            timeCounter += Time.deltaTime * Mathf.Pow(Trauma, 0.3f) * traumaMult;
            //Bind the movement to the desired range
            Vector3 newPos = GetVec3() * (traumaMag * Trauma);
            transform.localPosition = newPos;
            //rotation modifier applied here
            transform.localRotation = Quaternion.Euler(newPos * traumaRotMag);
            //decay faster at higher values
            Trauma -= Time.deltaTime * traumaDecay * (Trauma + 0.3f);
            
            Shaking = true;
        }
        else
        {
            //lerp back towards default position and rotation once shake is done
            Vector3 newPos = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime);
            transform.localPosition = newPos;
            transform.localRotation = Quaternion.Euler(newPos * traumaRotMag);
            
            Shaking = false;
        }
    }
    #endregion
    
    #region Base And Ults Methods
    float scaledValue;
    float scaledAdd;
    float traumaBuffer;
    public void AddTrauma(float add, bool allowBreakLimit, float extraAddBuffer)
    {
        if (!Shaking)
        {
            traumaBuffer = add;
            trauma += add;
            return;
        }
        
        if(!allowBreakLimit) trauma = traumaBuffer;
        else
        {
            traumaBuffer += extraAddBuffer;
            trauma = traumaBuffer;
        }
    }
    
    //maxDis is for explosion range
    //distance is distance input (take base on the distance get from player and the explosion)
    //minAdd and maxAdd is the min/max value will add 
    //default min is 0.2, max is 0.6
    public void AddTrauma_ScaleByDistance(float maxDis, float distance, float minAdd, float maxAdd, bool AllowBreakLimit, float extraAddBuffer)
    {
        scaledValue = Mathf.Clamp01(distance / maxDis);
        scaledAdd = Mathf.Lerp(minAdd, maxAdd, scaledValue);
        AddTrauma(scaledValue, AllowBreakLimit, extraAddBuffer);
    }

    [Space(10)] [Header("Explosion Debug Settings")]
    [SerializeField] float debug_maxDis = 4f;
    [SerializeField] float debug_inputDis = 2f;
    [SerializeField] float debug_minAdd = 0.2f;
    [SerializeField] float debug_maxAdd = 0.6f;
    [SerializeField] float debug_extraAdd = 0.05f;
    
    [Button("Explosion Shake - Extend")]
    public void ActivateShake()
    {
        AddTrauma_ScaleByDistance(debug_maxDis, debug_inputDis, debug_minAdd, debug_maxAdd, false, debug_extraAdd);
    }
    
    [Button("Explosion Shake - Add Extra")]
    public void ActivateShake_BreakLimit()
    {
        AddTrauma_ScaleByDistance(debug_maxDis, debug_inputDis, debug_minAdd, debug_maxAdd, true, debug_extraAdd);
    }
    #endregion
}

