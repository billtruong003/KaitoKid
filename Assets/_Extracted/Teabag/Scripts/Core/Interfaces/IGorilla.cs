using UnityEngine;

namespace Teabag.Core
{
    /// <summary>
    /// Read-only interface for a Gorilla entity.
    /// Provides common properties needed by consumers across assemblies.
    /// For mutations (health, team, material), cast to the concrete Gorilla type.
    /// </summary>
    public interface IGorilla
    {
        /// <summary>True if this gorilla is the local (state authority) player.</summary>
        bool HasStateAuthority { get; }

        /// <summary>The player's display name.</summary>
        string PlayerName { get; }

        /// <summary>The player's unique identifier (PlayFab ID).</summary>
        string PlayerId { get; }

        /// <summary>True if this gorilla's health is depleted.</summary>
        bool IsDead { get; }

        /// <summary>Head tracking transform.</summary>
        Transform HeadTransform { get; }

        /// <summary>Body tracking transform.</summary>
        Transform BodyTransform { get; }

        /// <summary>Left hand tracking transform.</summary>
        Transform LeftHandTransform { get; }

        /// <summary>Right hand tracking transform.</summary>
        Transform RightHandTransform { get; }

        /// <summary>Root transform of the gorilla entity.</summary>
        Transform Transform { get; }
    }
}
