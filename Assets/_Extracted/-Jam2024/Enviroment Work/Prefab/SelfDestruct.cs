using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public void Boom()
    {
        Destroy(this.gameObject);
    }
}
