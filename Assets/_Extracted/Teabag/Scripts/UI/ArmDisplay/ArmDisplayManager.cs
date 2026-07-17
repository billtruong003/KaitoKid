using Microsoft.Extensions.Logging;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

public class ArmDisplayManager : MonoBehaviour
{
    [SerializeField] private ArmDisplayBattleRoyale _armDisplayBattleRoyale;
    [SerializeField] private ArmDisplayLobby _armDisplayLobby;
    GameLoopManager gameLoop;
    private static readonly ILogger _logger = JungleXRLogger.GetLogger();

    public void OnEnable()
    {
        GetReferences();
        var gameLoopService = ServiceLocator.Get<IGameLoopService>() as GameLoopService;
        if (gameLoopService != null)
        {
            gameLoopService.OnManagerChanged += ChangePanel;
        }
        ChangePanel();
    }

    private void OnDisable()
    {
        var gameLoopService = ServiceLocator.Get<IGameLoopService>() as GameLoopService;
        if (gameLoopService != null)
        {
            gameLoopService.OnManagerChanged -= ChangePanel;
        }
    }
    private void GetReferences()
    {
        if(_armDisplayBattleRoyale == null)
        {
            _armDisplayBattleRoyale = GetComponentInChildren<ArmDisplayBattleRoyale>();
        }

        if (_armDisplayBattleRoyale == null)
        {
            _armDisplayLobby = GetComponentInChildren<ArmDisplayLobby>();
        }
    }

    private void ChangePanel()
    {
        bool inLobby = true;
        var gameLoopService = ServiceLocator.Get<IGameLoopService>() as GameLoopService;
        if (gameLoopService != null && gameLoopService.HasManager)
        {
            inLobby = false;
        }

        if (_armDisplayBattleRoyale != null)
        {
            _armDisplayBattleRoyale.enabled = !inLobby;
            _armDisplayBattleRoyale.gameObject.SetActive(!inLobby);
        }

        if (_armDisplayLobby != null)
        {
            _armDisplayLobby.enabled = inLobby;
            _armDisplayLobby.gameObject.SetActive(inLobby);
        }
    }
}
