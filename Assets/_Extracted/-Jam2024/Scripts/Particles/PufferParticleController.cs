using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PufferParticleController : MonoBehaviour
{
    public List<ParticleSystem> speedParticles;
    public float timeDelay = 0.5f;
    
    public async UniTaskVoid ToggleSpeedParticles(bool toggle)
    {
        //Debug.Log("toggle: " + toggle);
        if (!toggle)
        {
            UniTask.Delay(TimeSpan.FromSeconds(timeDelay));
            foreach (var particle in speedParticles)
            {
                particle.Stop();
            }    
        }
        else
        {
            foreach (var particle in speedParticles)
            {
                particle.Play();
            }    
        }
    }
}
