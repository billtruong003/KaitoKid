using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimController : MonoBehaviour
{
    [SerializeField] private Animator charAnim;

    [Button]
    public void ThrowKunai()
    {
        charAnim.SetTrigger("Thrown");
    }
}
