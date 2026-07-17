using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrownKunai : MonoBehaviour
{
    public static ThrownKunai Instance;
    [SerializeField] private GameObject character;
    [SerializeField] private CharacterAnimController charAnim;
    [SerializeField] private GameObject kunaiPrefab;
    [SerializeField] private Transform kunaiContainer;
    [SerializeField] private List<GameObject> kunaiList;
    

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
        Init();
    }
    public void kunaiThrown(Transform targetPosition)
    {

        GameObject kunai = kunaiList[0];
        kunai.transform.position = character.transform.position;
        charAnim.ThrowKunai();
        SoundManager.Instance.ShurikenThrow();
        KunaiThrow kunaiThrow = kunai.GetComponent<KunaiThrow>();
        kunaiThrow.SetTransform(targetPosition, character.transform.position);

        kunai.SetActive(true);
    }
    private void Init()
    {
        for (int i = 0; i < 10; i ++) { 
            GameObject kunaiSpawn = Instantiate(kunaiPrefab, kunaiContainer);
            kunaiList.Add(kunaiSpawn);
            kunaiSpawn.SetActive(false);
        }
    }
}
