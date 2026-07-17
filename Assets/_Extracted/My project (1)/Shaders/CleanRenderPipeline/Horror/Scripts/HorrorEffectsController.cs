using UnityEngine;

/// <summary>
/// Drives the FullscreenHorror material from gameplay systems.
/// Attach to any persistent GameObject. Set the Horror Material reference in inspector.
/// Call static methods from Wakefulness, Portal, Jumpscare, DoorKnock systems.
/// </summary>
public class HorrorEffectsController : MonoBehaviour
{
    public static HorrorEffectsController Instance { get; private set; }

    [Header("Material")]
    public Material horrorMaterial;

    [Header("Drowsiness")]
    [Range(0, 1)] public float drowsinessTarget = 0f;
    public float drowsinessLerpSpeed = 2f;

    [Header("Blink")]
    public float blinkInterval = 8f;
    public float blinkDuration = 0.3f;

    [Header("Runtime (Read Only)")]
    [SerializeField] float _drowsyCurrent;
    [SerializeField] float _blinkCurrent;
    [SerializeField] float _warpCurrent;
    [SerializeField] float _chromaCurrent;
    [SerializeField] float _damageCurrent;
    [SerializeField] float _peepholeCurrent;

    float _nextBlinkTime;
    float _blinkTimer;
    bool _isBlinking;

    static readonly int ID_DrowsyAmount  = Shader.PropertyToID("_DrowsyAmount");
    static readonly int ID_BlinkAmount   = Shader.PropertyToID("_BlinkAmount");
    static readonly int ID_WarpAmount    = Shader.PropertyToID("_WarpAmount");
    static readonly int ID_WarpCenter    = Shader.PropertyToID("_WarpCenter");
    static readonly int ID_ChromaBurst   = Shader.PropertyToID("_ChromaBurst");
    static readonly int ID_DamageFlash   = Shader.PropertyToID("_DamageFlash");
    static readonly int ID_PeepholeAmt   = Shader.PropertyToID("_PeepholeAmount");

    void Awake()
    {
        Instance = this;
        _nextBlinkTime = Time.time + blinkInterval;
    }

    void Update()
    {
        if (horrorMaterial == null) return;

        float dt = Time.deltaTime;

        // ── Drowsiness smooth lerp ──
        _drowsyCurrent = Mathf.Lerp(_drowsyCurrent, drowsinessTarget, dt * drowsinessLerpSpeed);
        horrorMaterial.SetFloat(ID_DrowsyAmount, _drowsyCurrent);

        // ── Auto blink when drowsy ──
        if (_drowsyCurrent > 0.25f)
        {
            float adjustedInterval = Mathf.Lerp(blinkInterval, blinkInterval * 0.3f, _drowsyCurrent);
            if (!_isBlinking && Time.time >= _nextBlinkTime)
            {
                _isBlinking = true;
                _blinkTimer = 0f;
            }
            if (_isBlinking)
            {
                _blinkTimer += dt;
                float halfDur = blinkDuration * 0.5f;
                _blinkCurrent = _blinkTimer < halfDur
                    ? Mathf.Lerp(0f, 1f, _blinkTimer / halfDur)
                    : Mathf.Lerp(1f, 0f, (_blinkTimer - halfDur) / halfDur);
                if (_blinkTimer >= blinkDuration)
                {
                    _isBlinking = false;
                    _blinkCurrent = 0f;
                    _nextBlinkTime = Time.time + adjustedInterval;
                }
            }
            else _blinkCurrent = Mathf.Lerp(_blinkCurrent, 0f, dt * 10f);
        }
        else _blinkCurrent = Mathf.Lerp(_blinkCurrent, 0f, dt * 10f);
        horrorMaterial.SetFloat(ID_BlinkAmount, _blinkCurrent);

        // ── Portal warp decay ──
        _warpCurrent = Mathf.Lerp(_warpCurrent, 0f, dt * 3f);
        horrorMaterial.SetFloat(ID_WarpAmount, _warpCurrent);

        // ── Chroma burst decay ──
        _chromaCurrent = Mathf.Lerp(_chromaCurrent, 0f, dt * 8f);
        horrorMaterial.SetFloat(ID_ChromaBurst, _chromaCurrent);

        // ── Damage flash decay ──
        _damageCurrent = Mathf.Lerp(_damageCurrent, 0f, dt * 5f);
        horrorMaterial.SetFloat(ID_DamageFlash, _damageCurrent);

        // ── Peephole lerp ──
        horrorMaterial.SetFloat(ID_PeepholeAmt, _peepholeCurrent);
    }

    // ══════════════════════════════════════
    // PUBLIC API — Call from gameplay systems
    // ══════════════════════════════════════

    /// <summary>Set drowsiness from Wakefulness system (0=awake, 1=asleep)</summary>
    public static void SetDrowsiness(float amount)
    {
        if (Instance) Instance.drowsinessTarget = Mathf.Clamp01(amount);
    }

    /// <summary>Trigger portal warp effect. Call when entering CRT portal.</summary>
    public static void TriggerPortalWarp(Vector2 screenCenter, float strength = 1f)
    {
        if (!Instance) return;
        Instance._warpCurrent = strength;
        Instance.horrorMaterial.SetVector(ID_WarpCenter, new Vector4(screenCenter.x, screenCenter.y, 0, 0));
    }

    /// <summary>Trigger chromatic aberration burst (jumpscare, damage).</summary>
    public static void TriggerChromaBurst(float strength = 0.03f)
    {
        if (Instance) Instance._chromaCurrent = strength;
    }

    /// <summary>Trigger damage flash (entity hit, failed event).</summary>
    public static void TriggerDamageFlash(float strength = 0.8f)
    {
        if (Instance) Instance._damageCurrent = strength;
    }

    /// <summary>Enable/disable peephole mode for door knock system.</summary>
    public static void SetPeephole(bool active)
    {
        if (Instance) Instance._peepholeCurrent = active ? 1f : 0f;
    }

    /// <summary>Force blink (e.g. The Sleeper trigger).</summary>
    public static void ForceBlink()
    {
        if (!Instance) return;
        Instance._isBlinking = true;
        Instance._blinkTimer = 0f;
    }

    void OnDestroy()
    {
        // Reset material to defaults
        if (horrorMaterial != null)
        {
            horrorMaterial.SetFloat(ID_DrowsyAmount, 0f);
            horrorMaterial.SetFloat(ID_BlinkAmount, 0f);
            horrorMaterial.SetFloat(ID_WarpAmount, 0f);
            horrorMaterial.SetFloat(ID_ChromaBurst, 0f);
            horrorMaterial.SetFloat(ID_DamageFlash, 0f);
            horrorMaterial.SetFloat(ID_PeepholeAmt, 0f);
        }
    }
}
