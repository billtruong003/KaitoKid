using System.Collections;
using UnityEngine;

public class ShmackleInventoryController : MonoBehaviour
{
    public ShmackleInventorySlot LeftInventorySlot;
    public ShmackleInventorySlot RightInventorySlot;

    private ShmacklePlayerController PlayerController;

    public bool isRelease;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip putItemSound;
    public AudioClip getItemSound;
    
    // Start is called before the first frame update
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        PlayerController = GetComponent<ShmacklePlayerController>();
        LeftInventorySlot.playerController = PlayerController;
        RightInventorySlot.playerController = PlayerController;

        LeftInventorySlot.inventoryController = this;
        RightInventorySlot.inventoryController = this;

    }

    public void releaseItem()
    {
        if(isRelease)
            return;
        isRelease = true;
        StartCoroutine(release_Process());
    }

    IEnumerator release_Process()
    {
        yield return new WaitForSeconds(1);
        isRelease = false;
    }
    
    
    
}
