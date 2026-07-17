using System.Collections;
using System.Collections.Generic;
using FruitCasual;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance { get; private set; }

    [Header("Config Mergeable Object")]
    [SerializeField] private MergeableConfig fruitConfig;
    [SerializeField] private MapConfig mapConfig; // Temparature none

    [Header("Fruit Setup")]
    [SerializeField] private int RandomFruit;
    [SerializeField] private GameObject currentFruit;
    [SerializeField] private GameObject nextFruit;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform fruitContainer;
    [SerializeField] private Transform upgradeContainer;

    [Header("Pooling Object Setting")]
    [SerializeField] private int numPool;
    [SerializeField] private List<GameObject> poolingObjects;


    [Header("Particle System")]
    [SerializeField] private ParticleManager particleManager;

    private GameObject instantGO;
    private Rigidbody2D rb;
    private Vector3 initialMousePos;
    private Vector3 mousePosition;
    private static int mergeCount = 0;
    [HideInInspector] public bool GameOver = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitPooling();
        PickFruitRandom();
        StartCoroutine(SetupFruit());
    }

    private void Update()
    {
        if (GameOver)
            return;
        Controller();
    }
    private void InitPooling()
    {
        int fruitEach = numPool / fruitConfig.objectMerges.Length;
        for (int i = 0; i < fruitConfig.objectMerges.Length; i++)
        {
            for (int j = 0; j < fruitEach; j++)
            {
                instantGO = Instantiate(fruitConfig.objectMerges[i], fruitContainer);
                instantGO.SetActive(false);
                poolingObjects.Add(instantGO);
            }
        }
    }

    private void PickFruitRandom()
    {
        currentFruit = GetFruitRandom();
        nextFruit = GetFruitRandom();
    }

    private void PickNextFruitRandom()
    {
        currentFruit = nextFruit;
        nextFruit = GetFruitRandom();
        StartCoroutine(SetupFruit());
    }

    private IEnumerator SetupFruit()
    {
        yield return new WaitForSeconds(1f);
        currentFruit.transform.position = startPoint.transform.position;
        ResetHasMerged();
        TurnOffCurrentFruitGravity();
        currentFruit.SetActive(true);
    }

    private GameObject GetFruitRandom()
    {
        int randNum = Random.Range(0, poolingObjects.Count);
        GameObject randomFruit = poolingObjects[randNum];
        poolingObjects.RemoveAt(randNum);
        return randomFruit;
    }

    private void ResetHasMerged()
    {
        Level level = currentFruit.GetComponent<Level>();
        level.hasMerged = false;
    }

    private void Controller()
    {
        if (!currentFruit.activeSelf)
            return;
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetMouseButton(0))
        {

            mousePosition.z = 0f;

            currentFruit.transform.position = new Vector3(mousePosition.x, currentFruit.transform.position.y);

            Debug.Log("MousePosition: " + mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            TurnOnCurrentFruitGravity();
            PickNextFruitRandom();
        }
    }

    private void TurnOffCurrentFruitGravity()
    {
        rb = currentFruit.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
    }

    private void TurnOnCurrentFruitGravity()
    {
        rb = currentFruit.GetComponent<Rigidbody2D>();
        rb.gravityScale = 1;
    }

    public void UpgradeObject(GameObject fruitMerged1, GameObject fruitMerged2, GameObject upgradeObject)
    {
        mergeCount++;
        if (mergeCount == 2)
        {
            Vector3 middlePosition = Vector3.Lerp(fruitMerged1.transform.position, fruitMerged2.transform.position, 0.5f);


            GameObject newLevelObject = Instantiate(upgradeObject, middlePosition, Quaternion.identity, upgradeContainer).gameObject;
            newLevelObject.name = RemoveCloneSuffix(newLevelObject.name) + " (UpgradeObject)";
            // Check if the cast is successful
            if (newLevelObject != null)
            {
                mergeCount = 0;
                Fruit scoreFruit = fruitMerged1.GetComponent<Fruit>();
                ScoreManager.Instance.PlusScore(scoreFruit.score);
            }
            else
            {
                Debug.LogError("Failed to cast instantiated object to GameObject.");
            }
        }
        Debug.Log("Upgrade Object Type: " + upgradeObject.GetType());
    }

    string RemoveCloneSuffix(string originalName)
    {
        int cloneIndex = originalName.IndexOf("(Clone)");

        if (cloneIndex != -1)
        {
            return originalName.Substring(0, cloneIndex).Trim();
        }

        return originalName;
    }

    private IEnumerator resetMergeCount()
    {
        yield return new WaitForSeconds(0.01f);
        mergeCount = 0;
    }

    public void AddToPoolingObjects(GameObject fruit)
    {
        poolingObjects.Add(fruit);
    }
}
