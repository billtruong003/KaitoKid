using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Word 
{
    public string word;
    private int typeIndex;

    public Word (string w)
    {
        word = w;
        typeIndex = 0;
    }

    public char GetNextLetter()
    {
        return word[typeIndex];
    }

    public void TypedLetter()
    {
        typeIndex++;
    }

    public bool WordCompleted()
    {
        bool completed = (typeIndex >= word.Length);

        return completed;
    }

    public int GetIndex()
    {
        return typeIndex;
    }
}
