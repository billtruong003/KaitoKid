using NaughtyAttributes;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "WordGen", menuName = "WordSO")]
public class WordSO : ScriptableObject
{
    public TextAsset wordsData;
    public List<Word> words;
    [Serializable]
    public class Word
    {
        public string word;
        public string definition;
    }
    [Button]
    public void LoadWordsFromJSON()
    {
        if (wordsData == null)
        {
            Debug.LogError("Words Data is not assigned.");
            return;
        }

        // Read the JSON data from TextAsset
        string jsonText = wordsData.text;

        // Deserialize JSON data into a WordList object
        WordList wordList = JsonUtility.FromJson<WordList>(jsonText);

        // Assign the word list to the WordSO
        words = wordList.words;
    }

    [Serializable]
    private class WordList
    {
        public List<Word> words;
    }
}
