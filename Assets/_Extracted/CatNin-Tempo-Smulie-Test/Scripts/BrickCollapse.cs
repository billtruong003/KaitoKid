using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrickCollapse : MonoBehaviour
{
    [SerializeField] private Animator platformAnim;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void Collapse()
    {
        if (transform.position.y < -5)
        {
            platformAnim.SetTrigger("Collapse");
        }
    }
}
