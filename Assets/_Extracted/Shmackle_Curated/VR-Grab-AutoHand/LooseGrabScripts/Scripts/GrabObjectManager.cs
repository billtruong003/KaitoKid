using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GrabObjectManager : MonoBehaviour
{
    private void Awake()
    {
        var items = GetComponentsInChildren<XRGrabInteractable>();
        foreach (var item in items)
        {
            item.AddComponent<GrabObjectMulti>();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
