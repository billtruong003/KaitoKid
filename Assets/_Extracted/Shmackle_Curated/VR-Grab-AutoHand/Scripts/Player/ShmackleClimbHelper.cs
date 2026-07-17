using UnityEngine;
using Autohand;

public class ShmackleClimbHelper : MonoBehaviour
{
    public Grabbable grabbable;
    public Autohand.Hand hand;
    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        grabbable = other.GetComponent<Grabbable>();
        if(grabbable && !hand.IsGrabbing())
        {
            onGrab();
        }
    }


    void onGrab()
    {
        Debug.Log("grab to climb");
        hand.ForceGrab(grabbable);
    }
}
