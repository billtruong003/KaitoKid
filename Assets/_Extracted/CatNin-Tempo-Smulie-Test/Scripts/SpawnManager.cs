using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private Transform platformContainer;
    [SerializeField] private List<GameObject> platforms;
    private int laneNum = 1;
    private void Awake()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        Instance = null;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        InitPlatform();
        ActivePlatform();
    }
    private void InitPlatform()
    { 
        for (int i = 0; i < 50; i++)
        {
            GameObject newPlatform = Instantiate(platformPrefab, platformContainer);
            
            platforms.Add(newPlatform);
            newPlatform.SetActive(false);
        }
    }
    private IEnumerator Cor_ActivePlatform()
    {
        yield return new WaitForSeconds(2);
        while (true) { 
            int randNum = Random.Range(0, platforms.Count);
            int numPlatform = Random.Range(4, 11);

            PlatformBuild platformBuild = platforms[randNum].GetComponent<PlatformBuild>();
            if (platformBuild != null)
            {
                platformBuild.ActivePlatform(numPlatform);
            }
            ChangeLanePlatform(platformBuild);
            platforms[randNum].SetActive(true);
            platforms.RemoveAt(randNum);
            yield return new WaitForSeconds((numPlatform / 5));
        }
    }
    private void ChangeLanePlatform(PlatformBuild platformBuild)
    {
        if (laneNum == 1)
        {
            platformBuild.transform.position = Vector3.zero;
            laneNum = Random.Range(0, 2) * 2;
            return;
        }
        else if (laneNum == 2)
        {
            platformBuild.transform.localPosition = new Vector2(1, 0);
            laneNum = Random.Range(0, 2);
            return;
        }
        else
        {
            platformBuild.transform.localPosition = new Vector2(-1, 0);
            laneNum = Random.Range(1, 3);
            return;
        }
    }
    public void AddPlatform(GameObject platform)
    {
        if (platform == null)
        {
            return;
        }
        else
        {
            platforms.Add(platform);
        }
    }
    private void ActivePlatform()
    {
        StartCoroutine(Cor_ActivePlatform());
    }
    // Update is called once per frame
    void Update()
    {

    }
}
