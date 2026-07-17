using System;
using Fusion;
using Teabag.Networking;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using Teabag.Player;

namespace Teabag.Game
{
    public enum ZoneState : byte
    {
        Idle,      // before game starts
        Waiting,   // waiting before next shrink
        Shrinking, // actively shrinking
        Finished   // all phases done
    }

    public class DamageZone : NetworkBehaviour
    {
        private static readonly int s_ActivityHashCode = Shader.PropertyToID("_Activity");
        private static readonly int s_THashCode = Shader.PropertyToID("_T");

        [Header("Config")]
        public ZoneConfig config;

        [Header("Music")]
        public AudioSource zoneMusic;

        [Header("Warning")]
        public AudioSource warningAudio;

        [Header("Renderers")]
        public MeshRenderer stormCloudRenderer;
        public MeshRenderer zoneRenderer;
        public MeshRenderer previewRenderer;

        [Header("Zone")]
        public bool inZone;
        public bool inWarningZone;
        [Range(0, 1)]
        public float maxSize;

        [Header("Info")]
        public float offset = 371.15f;
        public float sizeMultiplier = 1;

        // ── Networked state ──────────────────────────────────────────
        [Networked, OnChangedRender(nameof(OnZoneChanged))]
        public float zoneT { get; set; }

        [Networked, OnChangedRender(nameof(OnZoneChanged))]
        public Vector2 zonePosition { get; set; }

        [Networked, OnChangedRender(nameof(OnPhaseStateChanged))]
        public ZoneState zoneState { get; set; }

        [Networked, OnChangedRender(nameof(OnPhaseStateChanged))]
        public int currentPhase { get; set; }

        [Networked]
        public int phaseStartTick { get; set; }

        [Networked, OnChangedRender(nameof(OnPreviewChanged))]
        public Vector2 nextZoneCenter { get; set; }

        [Networked, OnChangedRender(nameof(OnPreviewChanged))]
        public float nextZoneRadius { get; set; }

        // ── Private ──────────────────────────────────────────────────
        private Vector3 m_CloudScale;
        private bool m_WasInZone;
        private int m_LastDamageTick;
        private float m_ShrinkStartT;
        private Vector2 m_ShrinkStartPos;
        private bool m_WarningSoundPlayed;

        private IGorillaService _gorillaService;
        private Func<(float zoneT, Vector2 zonePosition, float worldRadius)> _stormDataProvider;
        private Func<float> _countdownProvider;
        private Func<int> _currentPhaseProvider;
        private Func<int> _totalPhasesProvider;
        private Func<(Vector2 center, float worldRadius)> _previewProvider;
        private Func<bool> _isShrinkingProvider;
        private Func<bool> _isFinishedProvider;
        private Func<float> _totalZoneTimeProvider;

        private IHardwareRig LocalHardwareRig
        {
            get
            {
                if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                    return rigInfo.HardwareRig;
                return null;
            }
        }

        private IZoneService _zoneService;

        private bool HasValidNetworkState
        {
            get
            {
                return Object != null && Object.IsValid && Runner != null && !Runner.IsShutdown;
            }
        }

        // ── Spawned ──────────────────────────────────────────────────
        public override void Spawned()
        {
            base.Spawned();

            _zoneService = ServiceLocator.Get<IZoneService>();
            if (_zoneService != null)
            {
                _stormDataProvider = () =>
                {
                    if (!HasValidNetworkState)
                        return (0f, Vector2.zero, 0f);

                    return (zoneT, zonePosition, offset * sizeMultiplier * zoneT);
                };
                _countdownProvider = GetCountdownSeconds;
                _currentPhaseProvider = () => HasValidNetworkState ? currentPhase : 0;
                _totalPhasesProvider = () => config ? config.PhaseCount : 0;
                _previewProvider = () => HasValidNetworkState ? (zonePosition, config ? config.finalZoneRadius * offset * sizeMultiplier : 0f) : (Vector2.zero, 0f);
                _isShrinkingProvider = () => HasValidNetworkState && zoneState == ZoneState.Shrinking;
                _isFinishedProvider = () => HasValidNetworkState && zoneState == ZoneState.Finished;
                _totalZoneTimeProvider = GetTotalRemainingSeconds;

                _zoneService.OnGetStormData = _stormDataProvider;
                _zoneService.OnDamageZoneGameEnded = OnGamemodeEnded;
                _zoneService.OnStartZonePhases = StartZonePhases;
                _zoneService.OnSubtractDamageZoneT = SubtractT;


                _zoneService.SetHUDProviders(
                    _countdownProvider,
                    _currentPhaseProvider,
                    _totalPhasesProvider,
                    _previewProvider,
                    _isShrinkingProvider,
                    _isFinishedProvider,
                    _totalZoneTimeProvider
                );
            }

            var networkManager = ServiceLocator.Get<INetworkManager>();
            if (HasStateAuthority && networkManager.IsBattleRoyale)
            {
                zoneT = 1;
                zonePosition = Vector2.zero;    // placeholder; StartZonePhases() picks the random centre
                zoneState = ZoneState.Idle;
                currentPhase = 0;
                CalculateNextZone();
            }

            m_WarningSoundPlayed = false;
            OnZoneChanged();
            OnPreviewChanged();
        }

        public void SubtractT(float t)
        {
            if (HasStateAuthority && config && config.PhaseCount > 0)
            {
                // To "subtract T" in continuous mode, we must skip time forward.
                // zoneT = Lerp(1, finalRadius, progress)
                // progress = elapsedSec / duration
                float duration = config.phases[0].shrinkDuration + config.phases[0].waitDuration;
                float deltaProgress = t / (1f - config.finalZoneRadius);
                int ticksToSkip = Mathf.RoundToInt((deltaProgress * duration) / Runner.DeltaTime);

                phaseStartTick -= ticksToSkip;
            }
        }

        // ── Authority: called when BR game starts ────────────────────
        public void StartZonePhases()
        {
            if (!HasStateAuthority) return;

            zonePosition = PickRandomZoneCenter();

            zoneState = ZoneState.Waiting;
            phaseStartTick = Runner.Tick;
            m_WarningSoundPlayed = false;
            GameLogger.Debug($"Zone phases started at {zonePosition}. Phase 0 waiting.");
        }

        private Vector2 PickRandomZoneCenter()
        {
            if (MapInfo.currentInfo == null)
            {
                GameLogger.Warning("[DamageZone] MapInfo.currentInfo is null at StartZonePhases — falling back to Vector2.zero.");
                return Vector2.zero;
            }

            var mi = MapInfo.currentInfo;
            float finalWorldRadius = config != null ? config.finalZoneRadius * offset * sizeMultiplier : 0f;

            float xMin = mi.xMin + finalWorldRadius;
            float xMax = mi.xMax - finalWorldRadius;
            float zMin = mi.zMin + finalWorldRadius;
            float zMax = mi.zMax - finalWorldRadius;

            if (xMax <= xMin || zMax <= zMin)
            {
                GameLogger.Warning($"[DamageZone] MapInfo bounds too small for final zone radius ({finalWorldRadius}) — using bounds centre.");
                return new Vector2((mi.xMin + mi.xMax) * 0.5f, (mi.zMin + mi.zMax) * 0.5f);
            }

            return new Vector2(
                UnityEngine.Random.Range(xMin, xMax),
                UnityEngine.Random.Range(zMin, zMax)
            );
        }

        // ── FixedUpdateNetwork — drives state machine (authority) ────
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (!HasStateAuthority || !config || config.PhaseCount == 0)
                return;
            if (zoneState == ZoneState.Idle || zoneState == ZoneState.Finished)
                return;

            int elapsed = Runner.Tick - phaseStartTick;
            float elapsedSec = elapsed * Runner.DeltaTime;
            var phase = config.phases[currentPhase];

            switch (zoneState)
            {
                case ZoneState.Waiting:
                    // In continuous mode, we skip waiting and go straight to shrinking
                    zoneState = ZoneState.Shrinking;
                    phaseStartTick = Runner.Tick;
                    m_ShrinkStartT = zoneT;
                    m_ShrinkStartPos = zonePosition;
                    break;
                case ZoneState.Shrinking:
                    //HandleShrinking(elapsedSec, phase);
                    HandleContinuousShrinking(elapsedSec, phase);
                    break;
            }
        }

        private void HandleContinuousShrinking(float elapsedSec, ZoneConfig.ZonePhase phase)
        {
            // Calculate total duration from first phase or a default if none
            float duration = config.phases[0].shrinkDuration + config.phases[0].waitDuration;
            float progress = Mathf.Clamp01(elapsedSec / duration);

            // Continuous shrink: zoneT goes from 1 to finalZoneRadius
            zoneT = Mathf.Lerp(1f, config.finalZoneRadius, progress);

            // Center stays static in v1 continuous mode
            // (zonePosition already set in Spawned)

            if (progress >= 1f)
            {
                zoneState = ZoneState.Finished;
                GameLogger.Debug("Continuous zone shrink completed.");
            }
        }

        private void HandleShrinking(float elapsedSec, ZoneConfig.ZonePhase phase)
        {
            float progress = Mathf.Clamp01(elapsedSec / phase.shrinkDuration);

            // Lerp zone size
            zoneT = Mathf.Lerp(m_ShrinkStartT, phase.endRadiusPercent, progress);

            // Lerp zone center toward next zone center
            zonePosition = Vector2.Lerp(m_ShrinkStartPos, nextZoneCenter, progress);

            if (progress >= 1f)
            {
                // Shrink complete — advance phase
                GameLogger.Debug($"Phase {currentPhase}: Shrink complete. zoneT={zoneT}");

                currentPhase++;
                if (currentPhase >= config.PhaseCount)
                {
                    // All phases done — stay at final size
                    zoneState = ZoneState.Finished;
                    currentPhase = config.PhaseCount - 1; // clamp to last
                    phaseStartTick = Runner.Tick; // reset for countdown return 0
                    GameLogger.Debug("All zone phases completed.");
                    return;
                }

                // Start next waiting phase
                zoneState = ZoneState.Waiting;
                phaseStartTick = Runner.Tick;
                m_WarningSoundPlayed = false;
                CalculateNextZone();
                GameLogger.Debug($"Phase {currentPhase}: Waiting started.");
            }
        }

        private void CalculateNextZone()
        {
            // v1 continuous mode: no multi-phase target; preview = current zone at final radius.
            nextZoneCenter = zonePosition;
            nextZoneRadius = config.finalZoneRadius;
        }

        // ── Warning RPC (all clients play audio/visual) ──────────────
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        private void RPC_PlayWarning()
        {
            if (warningAudio && !warningAudio.isPlaying)
                warningAudio.Play();

            if (ZoneEffect.instance)
                ZoneEffect.instance.SetEffect(FogState.Zone);
        }

        // ── Visual updates (all clients) ─────────────────────────────
        public void OnZoneChanged()
        {
            transform.position = new Vector3(zonePosition.x, 27, zonePosition.y);

            zoneT = Mathf.Clamp01(zoneT);
            float value = zoneT * sizeMultiplier;
            zoneRenderer.transform.localScale = new Vector3(value, 1, value);

            if (stormCloudRenderer)
            {
                stormCloudRenderer.sharedMaterial.SetFloat(s_THashCode, 1 - zoneT);
                stormCloudRenderer.sharedMaterial.SetFloat(s_ActivityHashCode, Mathf.Clamp01((1 - zoneT) * 5));

                Vector3 stormPos = stormCloudRenderer.transform.position;
                stormPos.x = transform.position.x;
                stormPos.z = transform.position.z;
                stormCloudRenderer.transform.position = stormPos;
            }
        }

        public void OnPreviewChanged()
        {
            if (!previewRenderer) return;

            float previewScale = nextZoneRadius * sizeMultiplier;
            previewRenderer.transform.localScale = new Vector3(previewScale, 1, previewScale);
            previewRenderer.transform.position = new Vector3(nextZoneCenter.x, 27, nextZoneCenter.y);
            previewRenderer.enabled = zoneState != ZoneState.Idle;
        }

        public void OnPhaseStateChanged()
        {
            // Preview visibility
            OnPreviewChanged();
        }

        // ── Render (local client — damage + audio) ───────────────────
        public override void Render()
        {
            base.Render();
            var rig = LocalHardwareRig;
            if (rig == null || rig.Headset == null)
                return;

            Vector3 playerPos = rig.Headset.Position;
            Vector3 pos = transform.position;
            pos.y = playerPos.y;
            inZone = Vector3.Distance(pos, playerPos) < offset * sizeMultiplier * zoneT;

            if (zoneMusic)
            {
                zoneMusic.spatialBlend = inZone ? 1 : 0;
                zoneMusic.transform.position = GetClosestPerimeterPoint(playerPos);
            }

            if (inZone != m_WasInZone)
            {
                if (ZoneEffect.instance)
                    ZoneEffect.instance.SetZoneEffects(!inZone);

                m_WasInZone = inZone;
            }

            // Proximity warning SFX for players inside the zone
            if (zoneState == ZoneState.Shrinking && warningAudio != null)
            {
                float currentRadius = offset * sizeMultiplier * zoneT;
                float playerDist = Vector3.Distance(pos, playerPos);
                float distToEdge = currentRadius - playerDist;
                float threshold = config ? config.proximityWarningDistance : 20f;

                bool isCurrentlyInWarning = inZone && distToEdge > 0 && distToEdge < threshold;
                inWarningZone = isCurrentlyInWarning;

                if (isCurrentlyInWarning)
                {
                    if (!m_WarningSoundPlayed)
                    {
                        warningAudio.Play();
                        m_WarningSoundPlayed = true;
                    }
                }
                else
                {
                    // Reset the "played" flag ONLY if player is in the zone but outside the warning area
                    if (inZone && distToEdge >= threshold)
                    {
                        m_WarningSoundPlayed = false;
                    }

                    if (m_WarningSoundPlayed && warningAudio.isPlaying && !isCurrentlyInWarning)
                    {
                        warningAudio.Stop();
                    }
                }
            }
            else
            {
                inWarningZone = false;
                m_WarningSoundPlayed = false; // Reset if not shrinking or no audio
                if (warningAudio != null && warningAudio.isPlaying)
                {
                    warningAudio.Stop();
                }
            }

            // Damage tick — Fusion tick-based (roughly every tickRate ticks ≈ 1 second)
            if (!inZone && zoneState != ZoneState.Idle && Runner != null)
            {
                _gorillaService ??= ServiceLocator.Get<IGorillaService>();
                var dmgLocal = _gorillaService?.LocalGorilla as Gorilla;
                if (dmgLocal && dmgLocal.health)
                {
                    int tickRate = Mathf.RoundToInt(1f / Runner.DeltaTime);
                    int currentTick = Runner.Tick;

                    if (currentTick - m_LastDamageTick >= tickRate)
                    {
                        m_LastDamageTick = currentTick;
                        byte dmg = GetCurrentDamagePerSecond();
                        dmgLocal.health.Damage(dmg, HitType.Normal);
                    }
                }
            }

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.K) && HasStateAuthority)
            {
                if (zoneState == ZoneState.Waiting)
                {
                    phaseStartTick = Runner.Tick - Mathf.RoundToInt(config.phases[currentPhase].waitDuration / Runner.DeltaTime) - 1;
                }
                else if (zoneState == ZoneState.Shrinking)
                {
                    phaseStartTick = Runner.Tick - Mathf.RoundToInt(config.phases[currentPhase].shrinkDuration / Runner.DeltaTime) - 1;
                }
            }
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────
        private byte GetCurrentDamagePerSecond()
        {
            if (!config || config.PhaseCount == 0) return 5;
            int idx = Mathf.Clamp(currentPhase, 0, config.PhaseCount - 1);
            return config.phases[idx].damagePerSecond;
        }

        public float GetCountdownSeconds()
        {
            if (!HasValidNetworkState || !config || config.PhaseCount == 0)
                return 0f;

            int elapsed = Runner.Tick - phaseStartTick;
            float elapsedSec = elapsed * Runner.DeltaTime;

            if (zoneState == ZoneState.Waiting)
            {
                int idx = Mathf.Clamp(currentPhase, 0, config.PhaseCount - 1);
                return Mathf.Max(0f, config.phases[idx].waitDuration - elapsedSec);
            }
            else if (zoneState == ZoneState.Shrinking)
            {
                int idx = Mathf.Clamp(currentPhase, 0, config.PhaseCount - 1);
                return Mathf.Max(0f, config.phases[idx].shrinkDuration - elapsedSec);
            }

            return 0f;
        }

        public float GetTotalRemainingSeconds()
        {
            if (!HasValidNetworkState || !config || config.PhaseCount == 0)
                return 0f;

            if (zoneState == ZoneState.Idle || zoneState == ZoneState.Finished)
                return 0f;

            float duration = config.phases[0].shrinkDuration + config.phases[0].waitDuration;
            int elapsed = Runner.Tick - phaseStartTick;
            float elapsedSec = elapsed * Runner.DeltaTime;
            return Mathf.Max(0f, duration - elapsedSec);
        }

        public Vector3 GetClosestPerimeterPoint(Vector3 position)
        {
            Vector3 point = transform.position + new Vector3(offset * sizeMultiplier * zoneT, 0, 0);
            Vector3 difference = transform.position - position;
            Quaternion look = Quaternion.LookRotation(difference);

            look = Quaternion.Euler(0, look.eulerAngles.y - 90, 0);
            point = RotateAroundPivot(transform.position, point, look);
            point.y = position.y;

            return point;
        }

        public Vector3 RotateAroundPivot(Vector3 pivot, Vector3 point, Quaternion rotation)
        {
            Vector3 difference = pivot - point;
            Vector3 dir = rotation * difference;
            return dir + pivot;
        }

        private void OnDrawGizmos()
        {
            if (LocalHardwareRig != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(GetClosestPerimeterPoint(LocalHardwareRig.Headset.Position), 1);

                // Draw preview circle gizmo
                if (nextZoneRadius > 0)
                {
                    Gizmos.color = Color.yellow;
                    Vector3 previewPos = new Vector3(nextZoneCenter.x, 27, nextZoneCenter.y);
                    float radius = nextZoneRadius * sizeMultiplier * offset;
                    DrawCircleGizmo(previewPos, radius, 32);
                }

                // Draw warning distance gizmo
                if (zoneState == ZoneState.Shrinking && config != null)
                {
                    Gizmos.color = Color.red;
                    float currentRadius = offset * sizeMultiplier * zoneT;
                    float warningRadius = Mathf.Max(0, currentRadius - config.proximityWarningDistance);
                    DrawCircleGizmo(transform.position, warningRadius, 32);
                }
            }
        }

        private void DrawCircleGizmo(Vector3 center, float radius, int segments)
        {
            float step = 2f * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = step * i;
                float angle2 = step * (i + 1);
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                Gizmos.DrawLine(p1, p2);
            }
        }

        public void OnGamemodeEnded()
        {
            if (HasStateAuthority)
            {
                zoneT = 1f;
                zonePosition = Vector2.zero;
                zoneState = ZoneState.Idle;
                currentPhase = 0;
            }

            m_WarningSoundPlayed = false;
            ResetVisuals();
        }

        private void ResetVisuals()
        {
            // Reset fog/rain/skybox on the persistent Gorilla Rig ZoneEffect
            if (ZoneEffect.instance)
                ZoneEffect.instance.SetEffect(FogState.Default);

            // Reset storm cloud material (sharedMaterial writes persist across scenes)
            if (stormCloudRenderer && stormCloudRenderer.sharedMaterial)
            {
                stormCloudRenderer.sharedMaterial.SetFloat(s_THashCode, 0f);
                stormCloudRenderer.sharedMaterial.SetFloat(s_ActivityHashCode, 0f);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ClearZoneServiceProviders();
            base.Despawned(runner, hasState);
        }

        private void ClearZoneServiceProviders()
        {
            _zoneService ??= ServiceLocator.Get<IZoneService>();
            if (_zoneService == null)
                return;

            if (_zoneService.OnDamageZoneGameEnded == OnGamemodeEnded)
                _zoneService.OnDamageZoneGameEnded = null;

            if (_zoneService.OnStartZonePhases == StartZonePhases)
                _zoneService.OnStartZonePhases = null;

            if (_zoneService.OnSubtractDamageZoneT == SubtractT)
                _zoneService.OnSubtractDamageZoneT = null;

            if (_zoneService.OnGetStormData == _stormDataProvider)
                _zoneService.OnGetStormData = null;

            _zoneService.ClearHUDProviders(
                _countdownProvider,
                _currentPhaseProvider,
                _totalPhasesProvider,
                _previewProvider,
                _isShrinkingProvider,
                _isFinishedProvider,
                _totalZoneTimeProvider
            );

            _countdownProvider = null;
            _currentPhaseProvider = null;
            _totalPhasesProvider = null;
            _previewProvider = null;
            _isShrinkingProvider = null;
            _isFinishedProvider = null;
            _totalZoneTimeProvider = null;
            _stormDataProvider = null;
        }

        private void OnDestroy()
        {
            ClearZoneServiceProviders();
            ResetVisuals();
        }
    }
}
