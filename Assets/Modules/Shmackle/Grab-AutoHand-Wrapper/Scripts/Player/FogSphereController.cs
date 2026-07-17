using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class FogSphereController : MonoBehaviour
{
    public MeshRenderer fogRenderer;
    public float blending = 0;

    [HorizontalLine]
    public bool isFadeOnStart;
    public float startValue = -0.03f;
    public float endValue = 1;
    public float fadeDuration = 20;

    [HorizontalLine] 
    [Tooltip("update camera view base on fog fading progress")]
    public bool isUpdateCameraView;
    public Camera mainCamera;
    public float startViewDistance;
    public float endViewDistance;
    
    private void Start()
    {
        if (isFadeOnStart)
        {
            blending = startValue;
            fogRenderer.material.SetFloat("_Blending", blending);
            StartCoroutine(FogFading(startValue, endValue ,fadeDuration));
        }

        mainCamera = Camera.main;
    }

    private IEnumerator FogFading(float _startValue, float _endValue, float duration)
    {
        blending = _startValue;
        float elapsedTime = 0f;

        if (isUpdateCameraView)
            StartCoroutine(UpdateCameraViewDistance(startViewDistance , endViewDistance, duration));
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            blending = Mathf.Lerp(startValue, _endValue, elapsedTime / duration);
            fogRenderer.material.SetFloat("_Blending", blending);
            yield return null;
        }

        blending = endValue;
        fogRenderer.material.SetFloat("_Blending", blending);
        Destroy(gameObject);
    }
    
    private IEnumerator UpdateCameraViewDistance(float startViewDistance, float endViewDistance , float duration)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            mainCamera.farClipPlane = Mathf.Lerp(startViewDistance, endViewDistance, progress);
            yield return null;
        }

        mainCamera.farClipPlane = endViewDistance;
    }
}
