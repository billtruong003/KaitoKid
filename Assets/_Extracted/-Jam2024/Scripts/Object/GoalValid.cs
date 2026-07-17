using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class GoalValid : MonoBehaviour
{
    [ReadOnly] public PufferFishController pufferTarget;
    public Breakable GlassScript;

    public float speedRequire = 20f;
    public float boostRequire = 20f;
    
    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("hi");
        pufferTarget = other.GetComponentInChildren<PufferFishController>();

        if(pufferTarget == null) return;
        CheckCondition();
    }

    void CheckCondition()
    {
        bool failflag = false;
        if(pufferTarget.currentSpeed * pufferTarget.MoveMultiplier < speedRequire) failflag = true;
        if(!pufferTarget.BoostSkill || pufferTarget.boostForce < boostRequire) failflag = true;
        if(!pufferTarget.JumpSkill) failflag = true;

        if (failflag)
        {
            //Debug.Log("failed");
            pufferTarget = null;
            return;
        } 
        
        Goal();
    }

    void Goal()
    {
        //Show Title "Thanks for playing the demo"
        //Debug.Log("success");
        GlassScript.allowBreak = true;

        GlassScript.Break(pufferTarget.rb.velocity);
    }
}
