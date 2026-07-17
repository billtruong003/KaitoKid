#if UNITY_ANDROID && !UNITY_EDITOR && !DISABLE_ENTITLEMENT_CHECK
#define ANDROID_PLAYER
#endif

using Squido.JungleXRKit.Core;
using Teabag.Services;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Teabag.Core
{
    /// <summary>
    /// Terminal bootstrap state: the player is authenticated and in the default room.
    ///
    /// This state has no NextStateType — it is the final state in the bootstrap chain.
    /// DisposeContainerOnExit is false because its services must remain alive
    /// for the duration of the game session.
    ///
    /// After this state is entered:
    /// - Gorilla spawning happens via GorillaRunner.OnSceneLoadDone (existing flow)
    /// - Game mode transitions are handled by NetworkManager (existing flow)
    /// - The FSM remains in this state for the rest of the session
    /// </summary>
    public class LobbyReadyState : LifetimeStateMachine
    {
        public override bool DisposeContainerOnExit => false;

        private LobbyReadyState(LifetimeScope lifetimeScope) : base(lifetimeScope) { }

        protected override void OnLifetimeScopeReady(IObjectResolver container)
        {
            base.OnLifetimeScopeReady(container);
            ValidateServiceLocator();

#if ANDROID_PLAYER
            // Reconcile pending IAP purchases once fully authenticated and networked
            ServiceLocator.Get<IIAPManager>()?.ConsumePurchasesAsync();
#endif
            
            // Report Daily Login quest progress when entering lobby
            ServiceLocator.Get<IQuestService>()?.ReportProgressAsync("daily_login");
        }


        /// <summary>
        /// Validates that core services are resolvable via ServiceLocator.
        /// This proves the DI container and FSM pipeline are wired correctly.
        /// </summary>
        private static void ValidateServiceLocator()
        {
            var playfab = ServiceLocator.Get<IPlayFabAuthService>();
            var auth = ServiceLocator.Get<IAuthenticationService>();
            var network = ServiceLocator.Get<IFusionNetworkService>();
            var voice = ServiceLocator.Get<IPlayerVoiceService>();
            var gorillaService = ServiceLocator.Get<IGorillaService>();

            Debug.Log($"[Bootstrap] LobbyReady — ServiceLocator validation:\n" +
                      $"  IPlayFabAuthService: {(playfab != null ? $"OK (LoggedIn={playfab.IsFullyLoggedIn})" : "MISSING")}\n" +
                      $"  IAuthenticationService: {(auth != null ? $"OK (LoggedIn={auth.FullyLoggedIn})" : "MISSING")}\n" +
                      $"  IFusionNetworkService: {(network != null ? $"OK (InRoom={network.IsInRoom})" : "MISSING")}\n" +
                      $"  IPlayerVoiceService: {(voice != null ? $"OK (Ready={voice.IsReady})" : "MISSING")}\n" +
                      $"  IGorillaService: {(gorillaService != null ? $"OK (HasLocal={gorillaService.HasLocalGorilla})" : "MISSING")}");
        }
    }
}
