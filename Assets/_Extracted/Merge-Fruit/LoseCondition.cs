using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoseCondition : MonoBehaviour
{
    private bool touch;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fruit"))
        {
            touch = true;
            StartCoroutine(sendLose());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Fruit"))
        {
            touch = false;
        }
    }
    private IEnumerator sendLose()
    {
        yield return new WaitForSeconds(0.5f);
        if (touch == true)
        {
            MainManager.Instance.GameOver = true;
            Debug.Log("Game Over");
            UIManager.Instance.AppearGameOverPanel();
        }
    }
}
