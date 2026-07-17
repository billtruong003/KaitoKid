using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KunaiThrow : MonoBehaviour
{
    [SerializeField] private float throwSpeed = 30f;
    public Vector3 characterPosition;
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = (target.position - characterPosition).normalized;
        transform.Translate(direction * throwSpeed * Time.deltaTime);
    }
    public void SetTransform(Transform target, Vector3 characterPosition)
    {
        this.target = target;
        this.characterPosition = characterPosition;
    }    
}
