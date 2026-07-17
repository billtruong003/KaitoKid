using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class PlatformBuild : MonoBehaviour
{
    [SerializeField] private int platformNum;
    [SerializeField] private List<Sprite> platformSprites;
    [SerializeField] private GameObject Brick;
    [SerializeField] private GameObject SeparateBlock;
    [SerializeField] private GameObject ContainerBlock;
    [SerializeField] private List<GameObject> standingBlock;
    private float spacing = 0.7f;

    public void ActivePlatform(int numBlockCreate)
    {
        DeactivePlatform();
        for (int i = 0; i < numBlockCreate; i++)
        {
            standingBlock[i].SetActive(true);
        }
    }
    public void DeactivePlatform()
    {
        for (int i = 0;i < standingBlock.Count;i++)
        {
            standingBlock[i].SetActive(false);
        }
    }
    [Button]
    private void BuildPlatform()
    {
        Init(platformNum);
    }
    private void Init(int numBrick)
    {
        GameObject newBrick = Instantiate(Brick);
        for (int i = 0; i < numBrick; i++)
        {
            int counting = i + 1;

            GameObject newBlock = Instantiate(SeparateBlock, ContainerBlock.transform);
            newBlock.transform.position = new Vector2(0, -(counting * spacing));

            SpriteRenderer blockSprite = newBlock.GetComponent<SpriteRenderer>();
            blockSprite.sprite = RandomSprite();
            blockSprite.sortingOrder = counting;
            standingBlock.Add(newBlock);
        }
    }
    private Sprite RandomSprite()
    {
        int randNum = Random.Range(0, platformSprites.Count);
        return platformSprites[randNum];
    }
    [Button]
    private void ClearPlatform()
    {
        for (int i = 0; i < standingBlock.Count; i++)
        {
            DestroyImmediate(standingBlock[i].gameObject);
        }
        standingBlock.Clear();
    }
        
}
