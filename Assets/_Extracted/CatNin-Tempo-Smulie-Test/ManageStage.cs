using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageStage : MonoBehaviour
{
    [SerializeField] private Animator anim;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            anim.SetTrigger("NextTuto");
        }
    }
    public void BackToMenu()
    {
        SceneLoad.Instance.LoadMenuScene();
    }
}
