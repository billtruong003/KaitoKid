using System;
using Autohand;
using Autohand.Demo;
using UnityEngine;

namespace Shmackle.Runtime
{
    public class AutoHandFingerRaiseEvents : MonoBehaviour
    {
        #region ===== Fields =====

        [SerializeField]
        private HandType handType = HandType.none;
        private PlayerRIG playerRig = null;

        #endregion

        #region ===== Methods =====
        
        private void Awake()
        {
            var fingerBenders = GetComponents<OpenXRAutoHandFingerBender>();
            foreach (var fingerBender in fingerBenders)
            {
                var input = fingerBender.Type switch
                {
                    OpenXRAutoHandFingerBender.FingerType.Grip => QuestControllerInput.Grip,
                    OpenXRAutoHandFingerBender.FingerType.Trigger => QuestControllerInput.Trigger,
                    OpenXRAutoHandFingerBender.FingerType.Primary => QuestControllerInput.Primary,

                    _ => throw new NotImplementedException($"{fingerBender.Type}")
                };

                fingerBender.FingerEvents.onBendAction.AddListener(bendOffsets => OnPress(input, bendOffsets));
                fingerBender.FingerEvents.onUnbendAction.AddListener(bendOffsets => OnRelease(input, bendOffsets));
            }
        }

        private void OnPress(QuestControllerInput input, float[] bendOffsets)
        {
            if (playerRig == null)
                playerRig = GetComponentInParent<PlayerRIG>();
            if (playerRig == null)
                Debug.LogWarning("Missing Rig.");
            else
                playerRig.PlayerMod?.HandPress(handType, (int)input, bendOffsets);
        }
        private void OnRelease(QuestControllerInput input, float[] bendOffsets)
        {
            if (playerRig == null)
                playerRig = GetComponentInParent<PlayerRIG>();
            if (playerRig == null)
                Debug.LogWarning("Missing Rig.");
            else
                playerRig.PlayerMod?.HandRelease(handType, (int)input, bendOffsets);
        }

        #endregion
    }
    
}