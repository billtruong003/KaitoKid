using PlayFab;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;

namespace Teabag.Services
{
    /// <summary>
    /// Provides authentication status, player identity, and the auth sequence entry point.
    /// Replaces direct static access to AuthenticationManager.
    /// Resolved via <see cref="ServiceLocator"/>.Get&lt;IAuthenticationManager&gt;().
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>True after Awake / bootstrap has started the manager.</summary>
        bool Initialised { get; }

        /// <summary>True after PlayFab login succeeds.</summary>
        bool LoggedIn { get; }

        /// <summary>True when the full authentication sequence has completed successfully.</summary>
        bool FullyLoggedIn { get; }

        /// <summary>True when both login and full initialization have completed without error.</summary>
        bool IsConnected { get; }

        /// <summary>True if an error occurred during authentication.</summary>
        bool IsError { get; }

        /// <summary>The PlayFab player identifier.</summary>
        string PlayFabId { get; set; }

        /// <summary>The PlayFab session ticket used for API authorization.</summary>
        string Token { get; set; }

        /// <summary>The PlayFab authentication context.</summary>
        PlayFabAuthenticationContext Authentication { get; set; }

        /// <summary>The platform user identifier (e.g. Oculus user ID).</summary>
        ulong PlatformId { get; set; }

        /// <summary>The platform user name.</summary>
        string PlatformName { get; set; }

        /// <summary>The platform org-scoped user identifier.</summary>
        ulong OrgScopedId { get; set; }

        /// <summary>The application version string.</summary>
        string ApplicationVersion { get; }

        /// <summary>The current runtime platform.</summary>
        UnityEngine.RuntimePlatform Platform { get; }

        /// <summary>
        /// Raised when authentication completes successfully.
        /// Subscribers include tutorial flow, telemetry, and post-login data loading.
        /// </summary>
        event Action OnLogin;

        /// <summary>
        /// Raised when title data is retrieved from PlayFab.
        /// Used by progression, moderation, and configuration systems.
        /// </summary>
        event Action<Dictionary<string, string>> OnTitleData;

        /// <summary>
        /// Runs the authentication sequence using platform service identity.
        /// Called by PlayFabAuthService during XRKit bootstrap.
        /// </summary>
        UniTask RunAuthSequenceAsync(IOculusPlatformService platformService);
    }
}
