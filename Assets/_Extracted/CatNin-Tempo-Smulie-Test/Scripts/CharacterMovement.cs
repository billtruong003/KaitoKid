using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharacterMovement : MonoBehaviour
{
    private int poseNum = 1;
    [SerializeField] private Transform FirstPose;
    [SerializeField] private Transform MiddlePose;
    [SerializeField] private Transform SecondPose;
    // Start is called before the first frame update
    void Start()
    {
        poseNum = 1;
        ChangePose();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) && poseNum != 0)
        {
            poseNum -= 1;
            ChangePose();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && poseNum  != 2)
        {
            poseNum += 1;
            ChangePose();
        }
    }
    private void ChangePose()
    {
        if (poseNum == 1)
        {
            transform.position = MiddlePose.position;
        }
        else if (poseNum == 0)
        {
            transform.position = SecondPose.position;
        }
        else if (poseNum == 2)
        {
            transform.position = FirstPose.position;
        }
    }
}
