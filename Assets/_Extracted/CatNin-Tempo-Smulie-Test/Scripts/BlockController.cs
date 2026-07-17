using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class BlockController : MonoBehaviour
{
    [SerializeField] private Animator blockAnim;
    [SerializeField] private BlockFloating blockFloat;
    [SerializeField] private WordController wordController;
    public void Init()
    {
        blockFloat.SetStartPose();
        gameObject.SetActive(true);
        InitWord();
    }

    public void BlockReset()
    {
        BackToNormal();
    }
    public void InitWord()
    {
        wordController.WordGen();
    }
    public void RightBlock()
    {
        blockFloat.Right = true;
        blockFloat.SetPivotPose(2f);
        transform.eulerAngles = new Vector3(0, 0, 0);
        transform.localPosition = new Vector3(blockFloat.PivotPose, 0, 0);
        wordController.RightTrigger();
    }

    public void LeftBlock()
    {
        blockFloat.Right = false;
        blockFloat.SetPivotPose(-2f);
        transform.eulerAngles = new Vector3(0, 180, 0);
        transform.localPosition = new Vector3(blockFloat.PivotPose, 0, 0);
        wordController.LeftTrigger();
    }

    [Button]
    private void GoodActive()
    {
        blockAnim.SetTrigger("Good");
    }

    [Button]
    private void NiceActive()
    {
        blockAnim.SetTrigger("Nice");
    }

    [Button]
    private void PerfectActive()
    {
        blockAnim.SetTrigger("Perfect");
    }

    [Button]
    public void BackToNormal()
    {
        blockAnim.SetTrigger("Idle");
    }
    void OnTriggerEnter2D(Collider2D collide)
    {
        if (collide.gameObject.CompareTag("Shuriken"))
        {
            int randNum = Random.Range(0, 3);
            TriggerAnim(randNum);
            SoundManager.Instance.MashWood();
            Debug.Log("CollideShuriken");
        }
    }
    private void TriggerAnim(int randInt)
    {
        if (randInt == 0)
        {
            GoodActive();
        }
        else if (randInt == 1)
        {
            NiceActive();
        }
        else
        {
            PerfectActive();
        }
    }    

}
