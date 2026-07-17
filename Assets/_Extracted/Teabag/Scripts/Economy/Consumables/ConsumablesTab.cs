using Teabag.Authentication;
using Teabag.Economy;
using Teabag.Networking;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Player;

public class ConsumablesTab : MonoBehaviour
{
    public GameObject isInRoom;
    public ConsumablePreview preview;
    public float offset = -40;
    public float defaultOffset = 0;

    public INetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = ServiceLocator.Get<INetworkManager>();
            }
            return _networkManager;
        }
    }
    private INetworkManager _networkManager;

    void OnEnable()
    {
        Render();
    }

    private void Update()
    {
        if (!NetworkManager.Runner)
        {
            Render();
        }
    }

    public void Render()
    {
        if (!NetworkManager.Runner)
        {
            isInRoom.SetActive(true);
            return;
        }

        isInRoom.SetActive(false);
        foreach (ConsumablePreview preview in GetComponentsInChildren<ConsumablePreview>())
        {
            Destroy(preview.gameObject);
        }

        List<string> added = new List<string>();
        foreach (InventoryItem item in ConsumablesManager.inventory)
        {
            if (!added.Contains(item.Name))
            {
                ConsumablePreview p = Instantiate(preview, transform);
                p.transform.localPosition = new Vector3(0, defaultOffset + added.Count * offset, 0);
                p.Initialise(item.Name);
                added.Add(item.Name);
            }
        }
    }
}
