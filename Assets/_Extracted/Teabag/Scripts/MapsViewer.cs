using Teabag.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Teabag.Player;
using Teabag.UI;

public class MapsViewer : MonoBehaviour
{
    public List<MapView> views = new List<MapView>();
    public GameObject youAreHere;

    [Header("Game Mode Changing")]
    public List<GorillaButton> buttons = new List<GorillaButton>();
    public GorillaButton bootcamp;
    public TMP_Text text;

    string currentMap = "";

    private void Update()
    {
        text.enabled = false;

        foreach (Scene s in SceneManager.GetAllScenes())
        {
            if (s.buildIndex != 0)
            {
                currentMap = s.name;
                break;
            }
        }
        
        foreach (MapView view in views)
        {
            if (view.mapName == currentMap)
            {
                youAreHere.gameObject.SetActive(true);
                youAreHere.transform.position = view.map.position;
                return;
            }
        }

        youAreHere.gameObject.SetActive(false);
    }

    [System.Serializable]
    public class MapView
    {
        public string mapName;
        public Transform map;
    }
}
