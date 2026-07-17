using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public static TargetSpawner Instance;
    [SerializeField] private GameObject target;
    [SerializeField] private Transform targetContainer;
    [SerializeField] private List<GameObject> targetLeft;
    [SerializeField] private List<GameObject> targetRight;

    private void Awake()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        Instance = null;
    }
    void Start()
    {
        Init();
        SetupRound();
    }

    private void Init()
    {
        for (int i = 0; i < 40; i++)
        {
            GameObject targetSpawn = Instantiate(target, targetContainer);
            BlockController blockController = targetSpawn.GetComponent<BlockController>();
            if (i < 20)
            {
                blockController.LeftBlock();
                targetLeft.Add(targetSpawn);
                targetSpawn.SetActive(false);
                continue;
            }
            blockController.RightBlock();
            targetRight.Add(targetSpawn);
            targetSpawn.SetActive(false);

        }    
    }
    private void SetupRound()
    {
        StartCoroutine(Cor_LeftRound());
        StartCoroutine(Cor_RightRound());
    }
    private IEnumerator Cor_LeftRound()
    {
        while (true)
        {
            BlockController leftBlockController = LeftPick();
            leftBlockController.Init();
            yield return new WaitForSeconds(Random.Range(1, 4));
        }
    }
    private IEnumerator Cor_RightRound()
    {
        while (true)
        {
            BlockController rightBlockController = RightPick();
            rightBlockController.Init();
            yield return new WaitForSeconds(Random.Range(1, 4));
        }
    }
    private BlockController LeftPick()
    {
        int randInt = Random.Range(0, targetLeft.Count);
        BlockController blockController = targetLeft[randInt].GetComponent<BlockController>();
        targetLeft.RemoveAt(randInt);
        return blockController;
    }
    private BlockController RightPick()
    {
        int randInt = Random.Range(0, targetRight.Count);
        BlockController blockController = targetRight[randInt].GetComponent<BlockController>();
        targetRight.RemoveAt(randInt);
        return blockController;
    }
    public void AddRightBlock(GameObject rightBlock)
    {
        targetRight.Add(rightBlock);
    }
    public void AddLeftBlock(GameObject leftBlock)
    {
        targetLeft.Add(leftBlock);
    }
}
