using System;
using System.Collections.Generic;

/// <summary>
/// A Priority Queue class in which each item is associated with a priority value.
/// Dequeue and Peek functions return the item with the highest priority (lowest value).
/// </summary>
/// <typeparam name="T">Type of items to be stored in the queue.</typeparam>
/// <typeparam name="TPriority">Type of the priority value. Must implement IComparable.</typeparam>
/// 
[Serializable]
public class PriorityQueue<T, TPriority> where TPriority : IComparable<TPriority>
{   

    private readonly List<Tuple<T, TPriority>> elements;

    private readonly IComparer<TPriority> comparer;

    public PriorityQueue(IComparer<TPriority> comparer = null)
    {
        elements = new List<Tuple<T, TPriority>>();
        this.comparer = comparer ?? Comparer<TPriority>.Default;
    }

    public int Count => elements.Count;

    public void Enqueue(T item, TPriority priorityValue)
    {
        var newElement = Tuple.Create(item, priorityValue);
        elements.Add(newElement);
        BubbleUp(elements.Count - 1);
    }

    public T Dequeue()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("The queue is empty.");

        T bestItem = elements[0].Item1;
        Swap(0, elements.Count - 1);
        elements.RemoveAt(elements.Count - 1);
        BubbleDown(0);
        return bestItem;
    }

    public T Peek()
    {
        if (elements.Count == 0)
            throw new InvalidOperationException("The queue is empty.");

        return elements[0].Item1;
    }

    public void AdjustPriority(T item, TPriority newPriority)
    {
        int index = FindIndex(item);
        if (index == -1)
            throw new ArgumentException("Item not found in the queue.");

        TPriority oldPriority = elements[index].Item2;
        elements[index] = Tuple.Create(item, newPriority);

        if (comparer.Compare(newPriority, oldPriority) < 0)
        {
            BubbleUp(index);
        }
        else
        {
            BubbleDown(index);
        }
    }

    private int FindIndex(T item)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(elements[i].Item1, item))
            {
                return i;
            }
        }
        return -1;
    }

    private void BubbleUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (comparer.Compare(elements[index].Item2, elements[parentIndex].Item2) >= 0)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void BubbleDown(int index)
    {
        int lastIndex = elements.Count - 1;
        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallestIndex = index;

            if (leftChildIndex <= lastIndex &&
                comparer.Compare(elements[leftChildIndex].Item2, elements[smallestIndex].Item2) < 0)
            {
                smallestIndex = leftChildIndex;
            }

            if (rightChildIndex <= lastIndex &&
                comparer.Compare(elements[rightChildIndex].Item2, elements[smallestIndex].Item2) < 0)
            {
                smallestIndex = rightChildIndex;
            }

            if (smallestIndex == index)
                break;

            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    private void Swap(int index1, int index2)
    {
        var temp = elements[index1];
        elements[index1] = elements[index2];
        elements[index2] = temp;
    }
}