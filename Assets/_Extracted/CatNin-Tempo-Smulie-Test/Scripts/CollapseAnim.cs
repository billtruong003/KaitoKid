using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

public class CollapseAnim : MonoBehaviour
{
    public List<Transform> columns; // Danh sách các cột

    public float duration = 1f; // Thời gian của hiệu ứng
    public float delayBetweenColumns = 1f; // Độ trễ giữa các cột

    void Start()
    {
        // Gọi hàm Collapse khi bạn muốn bắt đầu hiệu ứng
        Collapse();
    }

    [Button]
    void Collapse()
    {
        foreach (Transform column in columns)
        {
            LeanTween.alpha(column.gameObject, 0f, duration)
                .setEaseOutQuad()
                .setDelay(delayBetweenColumns);

            LeanTween.scale(column.gameObject, Vector3.zero, duration)
                .setEaseOutQuad()
                .setDelay(delayBetweenColumns);

            delayBetweenColumns += 0.1f;
        }
    }
}
