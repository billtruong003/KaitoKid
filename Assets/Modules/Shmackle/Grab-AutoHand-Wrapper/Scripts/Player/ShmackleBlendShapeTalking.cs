using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShmackleBlendShapeTalking : MonoBehaviour
{
    [Header("Blend Shape Settings")]
    [SerializeField] private int mouthBlendShapeIndex = 0;
    [SerializeField] private float minWeight = 0f;
    [SerializeField] private float maxWeight = 100f;

    [Header("Timing")]
    [Tooltip("Seconds to go from closed to open (or open to closed).")]
    [SerializeField] private float talkSpeed = 0.2f;  // half-cycle duration

    public SkinnedMeshRenderer skinnedMesh;
    private float currentWeight;
    private float targetWeight;

    private enum MouthState { Idle, Opening, Closing }
    private MouthState state = MouthState.Idle;

    void Awake()
    {
        currentWeight = minWeight;
        skinnedMesh.SetBlendShapeWeight(mouthBlendShapeIndex, currentWeight);
        state = MouthState.Idle;
    }

    private void OnDisable()
    {
        skinnedMesh.SetBlendShapeWeight(mouthBlendShapeIndex, 0);
    }

    void Update()
    {
        float velocity = (maxWeight - minWeight) / talkSpeed; 
        switch (state)
        {
            case MouthState.Idle:
                // start opening immediately
                targetWeight = Random.Range(minWeight, maxWeight);
                state = MouthState.Opening;
                break;

            case MouthState.Opening:
                // move toward open target at constant speed
                currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, velocity * Time.deltaTime);
                if (Mathf.Approximately(currentWeight, targetWeight))
                {
                    // reached open → start closing
                    targetWeight = minWeight;
                    state = MouthState.Closing;
                }
                break;

            case MouthState.Closing:
                // move toward closed at same speed
                currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, velocity * Time.deltaTime);
                if (Mathf.Approximately(currentWeight, minWeight))
                {
                    // fully closed → idle (next cycle begins)
                    state = MouthState.Idle;
                }
                break;
        }

        skinnedMesh.SetBlendShapeWeight(mouthBlendShapeIndex, currentWeight);
    }
}
