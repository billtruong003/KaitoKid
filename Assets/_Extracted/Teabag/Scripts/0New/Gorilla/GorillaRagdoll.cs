using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using TMPro;
using UnityEngine;

namespace Teabag.Player
{
    public class GorillaRagdoll : MonoBehaviour
    {
        // ── Existing fields ──────────────────────────────────────────────────────
        public Transform root;
        public CosmeticSetter cosmeticSetter;
        public ColourSetter colourSetter;
        public TMP_Text playerName;

        [Header("Material")]
        [SerializeField] private List<RendererSlot> _rendererSlots = new List<RendererSlot>();
        [SerializeField] private int _normalMaterialIndex = 0;

        // ── Teabag logic (array for separated testicles) ─────────────────────────
        [Header("Teabag")]
        public RagdollTeabag[] teabags;

        [Header("Config")]
        public float despawnTime = 10.0f;
        public float forceDestroyTime = 20;

        /// <summary>Which player this ragdoll belongs to (set in Clone).</summary>
        [System.NonSerialized] public PlayerRef ownerPlayer;

        // ── Static ragdoll registry (for cross-client lookup via RPCs) ───────────
        public static readonly Dictionary<PlayerRef, GorillaRagdoll> activeRagdolls = new();

        private Rigidbody[] _rigidbodies;
        private Coroutine _despawnCoroutine;
        private Coroutine _forceDestroyCoroutine;
        private bool _isFalling = false;
        private IGorillaService _localGorillaService;
        private Gorilla _localGorrilla;
        private static readonly WaitForSeconds _waitSettle = new WaitForSeconds(0.2f);
        private WaitForSeconds _waitDespawn;
        private WaitForSeconds _waitForceDestroy;
        private static readonly WaitForFixedUpdate _waitFixedUpdate = new WaitForFixedUpdate();

        private MaterialPropertyBlock _mpb;
        private List<Material> _sharedMaterialsCache;


        public void Clone(Gorilla gorilla, Vector3 deathPos, Quaternion deathRot, Vector3 velocity)
        {
            // Set root position/rotation to synced death pose to avoid jitter on remotes
            transform.position = deathPos;
            transform.rotation = deathRot;

            _localGorillaService ??= ServiceLocator.Get<IGorillaService>();
            _localGorrilla = (Gorilla)_localGorillaService.LocalGorilla;

            if (gorilla.cosmetics != null)
                cosmeticSetter.SetCosmetics(gorilla.cosmetics.cosmeticSetter.GetCosmetics());

            if (gorilla.material != null)
                colourSetter.SetColour(gorilla.material.clampedColour);

            ApplyMaterial(gorilla.material);

            // Store owner for network lookup
            if (gorilla.Object != null && gorilla.Object.IsValid)
                ownerPlayer = gorilla.Object.StateAuthority;

            if (playerName != null)
            {
                playerName.text = gorilla.playerName;
                playerName.enabled = _localGorrilla == null || ownerPlayer != _localGorrilla.Object.StateAuthority;
            }

            // Register in static dictionary
            activeRagdolls[ownerPlayer] = this;

            Transform targetRoot = gorilla.rootBoneTransform;

            // Transform bones from local gorilla space to authority's death pose space
            Quaternion rotOffset = deathRot * Quaternion.Inverse(targetRoot.rotation);
            Loop(root, targetRoot, deathPos, targetRoot.position, rotOffset);

            // Wire teabag events using index
            if (teabags != null)
            {
                for (int i = 0; i < teabags.Length; i++)
                {
                    int index = i; // local copy for closure
                    RagdollTeabag tb = teabags[i];

                    if (tb != null)
                    {
                        tb.OnGrabLocal += (_) => _localGorrilla?.health?.RPCTeabagGrab(ownerPlayer, index);
                        tb.OnPullLocal += (_, progress, pos) => _localGorrilla?.health?.RPCTeabagPull(ownerPlayer, index, progress, pos);
                        tb.OnCancelLocal += (_) => _localGorrilla?.health?.RPCTeabagCancel(ownerPlayer, index);
                        tb.OnRipLocal += (_, midpoint) =>
                        {
                            // Immediately despawn on the ripper's own client — do not depend on RPC loopback.
                            // This ensures the ripper always sees the ragdoll disappear even if the RPC
                            // cannot be delivered (e.g. runner shutting down due to victim disconnect).
                            if (!_isFalling) OnDespawn(true);
                            HandleRipLocal(index, midpoint);
                        };
                        // OnRipVisual is still subscribed so remote clients (via ShowRip RPC) also trigger despawn.
                        tb.OnRipVisual += () => OnDespawn(true);
                    }
                }
            }

            _rigidbodies = GetComponentsInChildren<Rigidbody>();
            Vector3 finalVelocity = velocity;

            foreach (Rigidbody rb in _rigidbodies)
            {
                rb.isKinematic = false;
                rb.linearVelocity = finalVelocity;
            }

            OnDespawn(false);
            _waitDespawn ??= new WaitForSeconds(despawnTime);
            _waitForceDestroy ??= new WaitForSeconds(forceDestroyTime);
            _forceDestroyCoroutine = StartCoroutine(IEForceDestroy());
        }

        private IEnumerator IEStayOnBack()
        {
            Rigidbody rootRb = (_rigidbodies != null && _rigidbodies.Length > 0) ? _rigidbodies[0] : null;
            if (rootRb == null) yield break;

            float timeout = 2.0f;
            float elapsed = 0;

            // Phase 1: Torque until face up
            while (elapsed < timeout)
            {
                elapsed += Time.fixedDeltaTime;

                // If already mostly face up, stop applying torque and angular drift
                if (Vector3.Dot(rootRb.transform.forward, Vector3.up) > 0.8f)
                {
                    rootRb.angularVelocity = Vector3.zero; // "dont allow rotate"
                    break;
                }

                Vector3 torque = Vector3.Cross(rootRb.transform.forward, Vector3.up) * 100f;
                rootRb.AddTorque(torque, ForceMode.Acceleration);
                yield return _waitFixedUpdate;
            }

            // Phase 2: Settle
            float maxVelocity = 1.0f;
            while (maxVelocity > 0.05f)
            {
                if (_rigidbodies == null || _rigidbodies.Length == 0) break;
                maxVelocity = 0;
                foreach (Rigidbody rb in _rigidbodies)
                {
                    if (rb.linearVelocity.magnitude > maxVelocity)
                        maxVelocity = rb.linearVelocity.magnitude;
                }
                yield return _waitSettle;
            }

            // Phase 3: Final Sync (Authority Only)
            if (_localGorrilla != null && ownerPlayer == _localGorrilla.Object.StateAuthority)
            {
                // Send original root sync
                _localGorrilla.health?.RPCSyncFinalRagdollPosition(ownerPlayer, transform.position, transform.rotation);

                // Send new full bone sync (filtered)
                if (_rigidbodies != null && _rigidbodies.Length > 0)
                {
                    List<Vector3> positions = new List<Vector3>();
                    List<Quaternion> rotations = new List<Quaternion>();
                    List<int> validIndices = new List<int>();

                    for (int i = 0; i < _rigidbodies.Length; i++)
                    {
                        string boneName = _rigidbodies[i].gameObject.name.ToLower();
                        // Skip head, hands, and arms
                        if (boneName.Contains("head") ||
                            boneName.Contains("hand") ||
                            boneName.Contains("arm"))
                        {
                            continue;
                        }

                        positions.Add(_rigidbodies[i].position);
                        rotations.Add(_rigidbodies[i].rotation);
                        validIndices.Add(i);
                    }

                    _localGorrilla.health?.RPCSyncFinalRagdollBones(ownerPlayer, positions.ToArray(), rotations.ToArray(), validIndices.ToArray());
                }
            }

            // Phase 4: Lock state locally
            ApplyFinalSync(transform.position, transform.rotation);
            _settled = true;
        }

        private bool _settled = false;
        private bool _bonusGiven = false;

        private void HandleRipLocal(int index, Vector3 midpoint)
        {
            // Call ShowRip directly on this client's local ragdoll instance.
            // This plays VFX/SFX and fires OnRipVisual on the ripper without
            // depending on the RPC loopback — which may fail if the runner is
            // disrupted by the victim's disconnect.
            if (activeRagdolls.TryGetValue(ownerPlayer, out GorillaRagdoll localRag)
                && localRag != null
                && index >= 0
                && localRag.teabags != null
                && index < localRag.teabags.Length)
            {
                localRag.teabags[index].ShowRip(midpoint);
            }

            // Guard: if the runner is gone, remote clients cannot be notified via RPC.
            // The ripper's local visual is already handled above.
            if (_localGorrilla == null)
            {
                GameLogger.Warning("[GorillaRagdoll] HandleRipLocal: _localGorrilla is null — cannot broadcast rip RPC.");
                return;
            }

            if (_localGorrilla.Runner == null || !_localGorrilla.Runner.IsRunning)
            {
                GameLogger.Warning("[GorillaRagdoll] HandleRipLocal: Runner not running — rip RPC skipped. Ripper-local visual already triggered.");
                return;
            }

            GameLogger.Info($"[TeabagRip] Local rip for dead={ownerPlayer} index={index}. Sending RPC via {_localGorrilla.playerName}.");
            _localGorrilla.health?.RPCTeabagRip(ownerPlayer, index, midpoint);

            if (!_bonusGiven)
            {
                _bonusGiven = true;
                _localGorrilla.health?.RPCTeabagBonus();
            }
        }

        // ── Owner-disconnect watchdog ─────────────────────────────────────────────

        private void Update()
        {
            if (_isFalling || ownerPlayer == PlayerRef.None) return;

            // ── Case 1: victim's own client ─────────────────────────────────────────
            // When FallbackReturnToStation fires, the Fusion runner shuts down and
            // Player A's Gorilla NetworkObject is despawned, making _localGorrilla
            // Unity-null. No RPC can reach this client anymore, so despawn locally.
            if (_localGorrilla == null)
            {
                OnDespawn(true);
                return;
            }
        }

        // ── Visual methods (called by RPC handlers on ALL clients) ───────────────

        public void ShowGrab(int index)
        {
            if (teabags != null && index >= 0 && index < teabags.Length && teabags[index] != null)
                teabags[index].ShowGrab();
        }

        public void ShowPull(int index, float progress, Vector3 handPosition)
        {
            if (teabags != null && index >= 0 && index < teabags.Length && teabags[index] != null)
                teabags[index].ShowPull(progress, handPosition);
        }

        public void ShowRip(int index, Vector3 position)
        {
            if (teabags != null && index >= 0 && index < teabags.Length && teabags[index] != null)
                teabags[index].ShowRip(position);
        }

        public void ResetVisual(int index)
        {
            if (teabags != null && index >= 0 && index < teabags.Length && teabags[index] != null)
                teabags[index].ResetVisual();
        }

        // ── Material sync ────────────────────────────────────────────────────────

        private void ApplyMaterial(GorillaMaterial src)
        {
            if (_rendererSlots == null || _rendererSlots.Count == 0) return;

            _mpb ??= new MaterialPropertyBlock();
            _sharedMaterialsCache ??= new List<Material>(8);

            if (src.materials != null && src.materials.Count > 0 && src.material >= 0 && src.material < src.materials.Count)
            {
                GorillaMaterialType materialType = src.materials[_normalMaterialIndex];
                Material targetMaterial = materialType.material;

                foreach (RendererSlot slot in _rendererSlots)
                {
                    if (!slot.renderer) continue;

                    slot.renderer.GetSharedMaterials(_sharedMaterialsCache);

                    if (slot.materialIndex >= 0 && slot.materialIndex < _sharedMaterialsCache.Count)
                    {
                        if (_sharedMaterialsCache[slot.materialIndex] != targetMaterial)
                        {
                            _sharedMaterialsCache[slot.materialIndex] = targetMaterial;
                            slot.renderer.SetMaterials(_sharedMaterialsCache);
                        }
                    }

                    if (materialType.useColour)
                    {
                        slot.renderer.GetPropertyBlock(_mpb, slot.materialIndex);
                        _mpb.SetColor(GorillaMaterial.PropColorR, src.ClampColour(src.colourR));
                        _mpb.SetColor(GorillaMaterial.PropColorG, src.ClampColour(src.colourG));
                        _mpb.SetColor(GorillaMaterial.PropColorB, src.ClampColour(src.colourB));
                        _mpb.SetColor(GorillaMaterial.PropBaseColor, src.ClampColour(src.colour));
                        slot.renderer.SetPropertyBlock(_mpb, slot.materialIndex);
                    }
                    else
                    {
                        slot.renderer.SetPropertyBlock(null, slot.materialIndex);
                    }
                }
            }
        }

        // ── Existing methods ─────────────────────────────────────────────────────

        public void Loop(Transform parent, Transform targetRoot, Vector3 deathPos, Vector3 remoteRootPos, Quaternion rotOffset)
        {
            foreach (Transform t in parent)
            {
                if (t.GetComponent<HealthBar>() != null)
                    continue;

                Transform target = FindTransformByName(targetRoot, t.name);
                if (target != null)
                {
                    // Transform from remote gorilla space to authority death pose space
                    Vector3 relativePos = target.position - remoteRootPos;
                    Vector3 rotatedPos = rotOffset * relativePos;
                    t.transform.position = deathPos + rotatedPos;
                    t.transform.rotation = rotOffset * target.rotation;
                }
                Loop(t, targetRoot, deathPos, remoteRootPos, rotOffset);
            }
        }

        public Transform FindTransformByName(Transform parent, string name)
        {
            foreach (Transform t in parent)
            {
                if (t.name == name)
                    return t;

                Transform found = FindTransformByName(t, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void OnDespawn(bool immediate)
        {
            if (_despawnCoroutine != null) StopCoroutine(_despawnCoroutine);
            if (_forceDestroyCoroutine != null) StopCoroutine(_forceDestroyCoroutine);
            _despawnCoroutine = StartCoroutine(IEDespawn(immediate));
        }

        IEnumerator IEDespawn(bool immediate)
        {
            if (_isFalling) yield break;

            if (!immediate)
            {
                _settled = false;
                StartCoroutine(IEStayOnBack());

                // Wait until the physics have settled and synced
                while (!_settled)
                    yield return null;

                // Now wait for the actual despawn timer
                yield return _waitDespawn;
            }

            _despawnCoroutine = StartCoroutine(IEFalling());
        }

        public void ApplyFinalSync(Vector3 pos, Quaternion rot)
        {
            StartCoroutine(IEApplyFinalSync(pos, rot));
        }

        private IEnumerator IEApplyFinalSync(Vector3 targetPos, Quaternion targetRot)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (this == null || gameObject == null) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            if (this != null && gameObject != null)
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
            }
        }

        public void ApplyFinalBoneSync(Vector3[] positions, Quaternion[] rotations, int[] indices)
        {
            if (_rigidbodies == null || positions == null || rotations == null || indices == null) return;

            _settled = true;
            StartCoroutine(IEApplyFinalBoneSync(positions, rotations, indices));
        }

        private IEnumerator IEApplyFinalBoneSync(Vector3[] targetPositions, Quaternion[] targetRotations, int[] indices)
        {
            int count = Mathf.Min(Mathf.Min(indices.Length, targetPositions.Length), targetRotations.Length);

            Vector3[] startPositions = new Vector3[count];
            Quaternion[] startRotations = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                int boneIndex = indices[i];
                if (boneIndex >= 0 && boneIndex < _rigidbodies.Length)
                {
                    _rigidbodies[boneIndex].isKinematic = true;
                    startPositions[i] = _rigidbodies[boneIndex].position;
                    startRotations[i] = _rigidbodies[boneIndex].rotation;
                }
            }

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (this == null || gameObject == null) yield break;

                elapsed += Time.fixedDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);

                for (int i = 0; i < count; i++)
                {
                    int boneIndex = indices[i];
                    if (boneIndex >= 0 && boneIndex < _rigidbodies.Length)
                    {
                        Rigidbody rb = _rigidbodies[boneIndex];
                        if (rb != null)
                        {
                            rb.MovePosition(Vector3.Lerp(startPositions[i], targetPositions[i], t));
                            rb.MoveRotation(Quaternion.Slerp(startRotations[i], targetRotations[i], t));
                        }
                    }
                }

                yield return _waitFixedUpdate;
            }

            // Final snap to ensure exact values
            if (this == null || gameObject == null) yield break;

            for (int i = 0; i < count; i++)
            {
                int boneIndex = indices[i];
                if (boneIndex >= 0 && boneIndex < _rigidbodies.Length)
                {
                    Rigidbody rb = _rigidbodies[boneIndex];
                    if (rb != null)
                    {
                        rb.MovePosition(targetPositions[i]);
                        rb.MoveRotation(targetRotations[i]);
                    }
                }
            }
        }

        private IEnumerator IEFalling()
        {
            if (_isFalling) yield break;
            _isFalling = true;

            foreach (Rigidbody rigidbody in _rigidbodies)
                rigidbody.isKinematic = true;

            float t = 0;
            while (transform.position.y > -10 || t < 5)
            {
                t += Time.deltaTime;
                transform.position += Vector3.down * Time.deltaTime * 2;
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator IEForceDestroy()
        {
            yield return _waitForceDestroy;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Unregister from static dictionary
            if (activeRagdolls.ContainsKey(ownerPlayer) && activeRagdolls[ownerPlayer] == this)
                activeRagdolls.Remove(ownerPlayer);

            StopAllCoroutines();
        }
    }
}
