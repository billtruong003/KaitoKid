using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBehaviour : MonoBehaviour
{
    private Animator ani;
    private Animator explosionAni;

    public GameObject explosion;
    public GameObject opponent;

    private int damageValue;
    private readonly static int[] damageLvl = { 5, 10, 20, 30 };

    private int currHealth;
    public HealthBar hBar;

    // Start is called before the first frame update
    void Start()
    {
        ani = GetComponent<Animator>();
        explosionAni = explosion.GetComponent<Animator>();
        RestartHealthBar();
        RestartAnimation();

        damageValue = damageLvl[PlayerPrefs.GetInt("Enemy DMG")];
    }

    public void RestartAnimation()
    {
        ani.Rebind();
        ani.Update(0f);
    }

    public void RestartHealthBar()
    {
        currHealth = 100;
        hBar.SetMaxHealth(currHealth);
    }

    public void Animate(string action)
    {
        ani.SetTrigger(action);
    }

    public void TakeDamage()
    {

        explosionAni.SetTrigger("explode");
        currHealth -= damageValue;
        hBar.SetHealth(currHealth);

        // Check if Health is at zero
        if (currHealth <= 0)
        {
            FindObjectOfType<WordsManager>().StopWordsSpawning(opponent.tag);
            Animate("die");
            opponent.GetComponent<CharacterBehaviour>().Animate("win");
        }
        else
        {
            Animate("damage");
        }
    }
}
