using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordController : MonoBehaviour
{
    [SerializeField] private TextMeshPro Text;
    [SerializeField] private WordSO words;
    public string word;
    
    
    // Start is called before the first frame update
    public void WordGen()
    {
        UIManager UIM = UIManager.Instance;
        if (UIM != null )
        {
            word = GetWord();
            if (UIM.TimeGen == 4)
            {
                word = UIM.CurrentWord;
            }
            if (word == UIM.CurrentWord)
            {
                UIM.TimeGen = 0;
            }
            Text.text = word;
        }
            
        
    }
    public string GetWord()
    {
        return words.words[Random.Range(0, 3)].word;
    }
    public void RightTrigger()
    {
        Text.transform.localScale = new Vector3(1, 1, 1);
    }
    public void LeftTrigger()
    {
        Text.transform.localScale = new Vector3(-1, 1, 1);
    }
}
