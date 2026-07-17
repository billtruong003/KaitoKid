using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public List<GameObject> UImenu;
    public List<GameObject> GamePlayDif;
    public static GameController Instance;
    public int difficult;
    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        Instance = this;
    }
    private void OnDestroy()
    {
        Instance = null;
    }
    public void SetDif(int dif)
    {
        difficult = dif;
    }
    public void OpenAllMode()
    {
        foreach (GameObject UIO in UImenu)
        {
            UIO.SetActive(false);
        }
        foreach (GameObject UIO in GamePlayDif)
        {
            UIO.SetActive(true);
        }
    }
    public void LoadSceneTuto()
    {
        SceneManager.LoadScene("TutoScene");
    }
    public void LoadSceneGamePlay(int dif)
    {
        SetDif(dif);
        SceneManager.LoadScene("GamePlay");
    }
    public void LoadScenePractice()
    {
        SceneManager.LoadScene("TrainingScene");
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Reload()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
