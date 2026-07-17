#if STDB_BINDINGS
// Requires module_bindings (auto-generated SpacetimeDB bindings)
using UnityEngine;
using SpacetimeDB;
using SpacetimeDB.Types;

namespace SpumOnline
{
    /// <summary>
    /// Controller for remote player characters.
    /// Receives position updates from the SpacetimeDB subscription and smoothly
    /// interpolates the visual to the target position. Drives Layer Lab animations
    /// based on server-provided AnimState and FacingRight fields.
    /// </summary>
    [RequireComponent(typeof(CharacterVisualSync))]
    public class RemotePlayerController : MonoBehaviour
    {
        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private CharacterVisualSync _visualSync;

        /// <summary>The SpacetimeDB identity of the player who owns this character.</summary>
        public Identity OwnerIdentity { get; private set; }

        /// <summary>Display name of this remote player.</summary>
        public string PlayerName { get; private set; }

        /// <summary>Current HP (synced from stats table for targeting display).</summary>
        public int CurrentHp { get; set; }

        /// <summary>Max HP (synced from stats table for targeting display).</summary>
        public int MaxHp { get; set; }

        // Interpolation
        private Vector2 _targetPosition;
        private Vector2 _currentVelocity;
        private bool _hasTarget;
        private float _lerpSpeed;

        // Animation
        private int _currentAnimState;
        private bool _facingRight = true;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            _visualSync = GetComponent<CharacterVisualSync>();
            _lerpSpeed = NetworkConfig.REMOTE_LERP_SPEED;
        }

        private void Update()
        {
            if (!_hasTarget) return;

            // Smoothly interpolate toward the target position
            Vector2 currentPos = (Vector2)transform.position;
            float dist = Vector2.Distance(currentPos, _targetPosition);

            if (dist > 5f)
            {
                // Large discrepancy (teleport), snap immediately
                transform.position = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
            }
            else if (dist > 0.01f)
            {
                Vector2 newPos = Vector2.Lerp(currentPos, _targetPosition, _lerpSpeed * Time.deltaTime);
                transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
            }
        }

        // -------------------------------------------------------
        // Public API (called by PlayerSpawner)
        // -------------------------------------------------------

        /// <summary>
        /// Initialize this remote player with an identity and starting position.
        /// </summary>
        public void Initialize(Identity identity, string playerName, float x, float y, bool facingRight, int animState)
        {
            OwnerIdentity = identity;
            PlayerName = playerName;
            _targetPosition = new Vector2(x, y);
            _facingRight = facingRight;
            _currentAnimState = animState;
            _hasTarget = true;

            transform.position = new Vector3(x, y, transform.position.z);
            _visualSync.SetFacing(_facingRight);
            _visualSync.SetAnimState(_currentAnimState);
        }

        /// <summary>
        /// Update the target position from a server-side table update.
        /// Called by PlayerSpawner when player_position OnUpdate fires.
        /// </summary>
        public void OnServerPositionUpdate(float x, float y, bool facingRight, int animState)
        {
            _targetPosition = new Vector2(x, y);
            _hasTarget = true;

            // Update facing
            if (facingRight != _facingRight)
            {
                _facingRight = facingRight;
                _visualSync.SetFacing(_facingRight);
            }

            // Update animation state
            if (animState != _currentAnimState)
            {
                _currentAnimState = animState;
                _visualSync.SetAnimState(_currentAnimState);
            }
        }

        /// <summary>
        /// Update stats for targeting display.
        /// </summary>
        public void UpdateStats(int currentHp, int maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        /// <summary>
        /// Set the displayed name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            PlayerName = name;

            // Update nameplate if one exists
            var nameplate = GetComponentInChildren<TMPro.TMP_Text>();
            if (nameplate != null)
            {
                nameplate.text = name;
            }
        }
    }
}

#endif // STDB_BINDINGS
