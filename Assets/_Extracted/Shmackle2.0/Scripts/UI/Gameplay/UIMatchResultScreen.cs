using Shmackle.Gameplay;
using Stratton.Core;
using Stratton.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Shmackle.UI
{
    public class UIMatchResultScreen : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField]
        private TMP_Text _resultText;
        
        #endregion
        
        #region Private Methods
        
        private void OnEnable()
        {
            NetworkingSystem networkingSystem = GameSystemsManager.Instance.Get<NetworkingSystem>();
            GameplaySystem gameplaySystem = GameSystemsManager.Instance.Get<GameplaySystem>();
            GameModeBase gameModeBase = gameplaySystem.GetActiveGameMode<GameModeBase>();
            if (gameModeBase)
            {
                List<ShmacklePlayerState> playerStates = gameModeBase.GetAllPlayerStates<ShmacklePlayerState>();
                ShmacklePlayerState winnerPlayerState = playerStates.OrderByDescending(p => p.Score).First();
                if (winnerPlayerState)
                {
                    string result = networkingSystem.Runner.LocalPlayer == winnerPlayerState.Owner ? "You won!" : "You lose!";
                    _resultText.SetText(result);
                }
                else
                {
                    Stratton.Core.Log.Error(BaseLogChannel.UI, "Winner player state not found!");
                    gameObject.SetActive(false);
                }
            }
        }
        
        #endregion
    }
}