using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockFloating : MonoBehaviour
{
    [SerializeField] private BlockController blockController;
    public float FloatSpeed = 5f;
    public float PivotPose;
    public bool Right = false;
    
    public void SetPivotPose(float x)
    {
        PivotPose = x;
    }    
    void Start()
    {
        SetStartPose();
    }

    public void SetStartPose()
    {
        transform.position = new Vector3(PivotPose, 12.6f);
    }    
    void Update()
    {
        transform.Translate(Vector3.down * FloatSpeed * Time.deltaTime);
        if (transform.localPosition.y <= -7)
        {
            transform.localPosition = new Vector3(PivotPose, 12.6f);
            blockController.BlockReset();
            AddBackToList();
            gameObject.SetActive(false);
            
        }
    }
    private void AddBackToList()
    {
        if (Right)
        {
            TargetSpawner.Instance.AddRightBlock(gameObject);
            return;
        }
        TargetSpawner.Instance.AddLeftBlock(gameObject);
    }    
}
