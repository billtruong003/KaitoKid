using System;
using UnityEngine;
using Teabag.Player;
using Teabag.Core;
using Squido.JungleXRKit.Core;

public class KillUnder : MonoBehaviour
{
    private IGorillaService _gorillaService;

    private void Awake()
    {
        _gorillaService = ServiceLocator.Get<IGorillaService>();
    }

    public void FixedUpdate()
    {
        var localGorilla = _gorillaService?.LocalGorilla as Gorilla;

        // Null check the gorilla and the health component efficiently
        if (localGorilla != null && localGorilla.transform.position.y < transform.position.y)
        {
            Health health = localGorilla.health;
            if (health == null) return;

            // Optional: Re-enable game state check if needed
            /*
            if (GorillaGameManager.instance != null && GorillaGameManager.instance.gameState != GameState.Running)
                return;
            */

            health.Damage(health.CurrentHealthAmount);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1.0f, 0, 0, 0.3f);
        var center = new Vector3(transform.position.x, transform.position.y, transform.position.z);
        var size = new Vector3(20000f, 0.1f, 20000f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }
}
