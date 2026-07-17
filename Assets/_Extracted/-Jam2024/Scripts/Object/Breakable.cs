using UnityEngine;

[SelectionBase]
public class Breakable : MonoBehaviour
{
    [SerializeField] GameObject intact;
    [SerializeField] GameObject broken;
    public bool allowBreak;

    Collider bc;

    private void Awake()
    {
        intact.SetActive(true);
        broken.SetActive(false);

        bc = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!allowBreak) return;
        if (collision.gameObject.CompareTag("Player")) // Assuming the player has the tag "Player"
        {
            Break(collision.relativeVelocity);
        }
    }

    public void Break(Vector3 playerVelocity)
    {
        Debug.Log("break");
        broken.SetActive(true);
        intact.SetActive(false);

        bc.enabled = false;

        // Apply force to all Rigidbody components in brokenMug
        Rigidbody[] rigidbodies = broken.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rigidbodies)
        {
            Vector3 randomDirection = Random.insideUnitSphere; // Add some randomness to the direction
            Vector3 force = playerVelocity + randomDirection * 400f; // Scale random force
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
