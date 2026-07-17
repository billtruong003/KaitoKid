using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockOnTouch : MonoBehaviour
{
    [SerializeField] private WordController wordController;
    private void OnMouseDown()
    {
        Debug.Log("Click Click");
        ThrownKunai.Instance.kunaiThrown(transform);
        StartCoroutine(ChangeWord());
    }
    public IEnumerator ChangeWord()
    {
        yield return new WaitForSeconds(0.4f);
        UIManager UIM = UIManager.Instance;
        if (wordController.word == UIM.CurrentWord)
        {
            UIM.InitWord();
            UIM.PlusScore();
        }
        else
        {
            if (UIM.Life > 1) 
            { 
                UIM.MinusLife(); 
            }
            else
            {
                if (UIM.Life <= 0)
                {
                    yield return null;
                }
                UIM.GameOver();
                UIM.MinusLife();
            } 
                
            
        }    
    }
}
