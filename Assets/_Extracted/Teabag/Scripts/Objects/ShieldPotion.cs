using Fusion;
using Teabag.Networking;
using UnityEngine;

using Teabag.Core;
using Squido.JungleXRKit.Core;
using Teabag.Player;
using Cysharp.Threading.Tasks;
using Teabag.Gameplay;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
// ReSharper disable once CheckNamespace
public sealed class ShieldPotion : Grabbable
{
    private const float DISTANCE_TO_HEAL = 0.25f;

    [Header("Potion Settings")]
    [SerializeField, Range(0, 50)] private byte m_GiveShieldAmount;
    [SerializeField] private ParticleSystem m_ParticleSystem;
    [SerializeField] private AdvancedAudioClip m_PrepareSound;
    [SerializeField] private AdvancedAudioClip m_ConsumeSound;
    [SerializeField] private GameObject m_CorkRigidbody;

    [Header("Objects")]
    [SerializeField] private GameObject m_Cork;
    [SerializeField] private GameObject m_Liquid;

    private IAudioService _audioService;

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public State CurrentState { get; set; }

    private IGorillaService _gorillaService;

    protected override void Awake()
    {
        base.Awake();
        _audioService = ServiceLocator.Get<IAudioService>();
    }

    public override void Spawned()
    {
        base.Spawned();
        OnStateChanged();
    }

    public void OnStateChanged()
    {
        switch (CurrentState)
        {
            case State.Preparing:
                _audioService.Play(m_PrepareSound, transform.position);
                m_Cork.SetActive(false);
                GameObject newObj = Instantiate(m_CorkRigidbody, transform);
                newObj.transform.parent = null;

                Rigidbody newRigidbody = newObj.GetComponent<Rigidbody>();
                newRigidbody.AddForce(transform.up * 2, ForceMode.Impulse);
                newRigidbody.AddForce(grabber.hand.tracker.velocity);
                newRigidbody.AddTorque(grabber.hand.tracker.angularVelocity);
                break;
            case State.Slurped:
                _audioService.Play(m_ConsumeSound, transform.position);
                if (m_ParticleSystem && !m_ParticleSystem.isPlaying)
                    m_ParticleSystem.Play();

                m_Cork.SetActive(false);
                m_Liquid.SetActive(false);
                break;
        }
    }

    public new void Update()
    {
        base.Update();
        if (!Object) return;

        if (CurrentState == State.Slurped)
        {
            canGrab = false;
            if (grabber?.isMine ?? false)
                grabber.Release();
            return;
        }

        _gorillaService ??= ServiceLocator.Get<IGorillaService>();
        var spLocal = _gorillaService?.LocalGorilla as Gorilla;
        if (interacting && HasStateAuthority && spLocal && spLocal.health)
        {
            if (VRInputHandler.GetInputDown(grabber.hand.isLeftHand, InputType.Trigger) && CurrentState == State.Default)
            {
                CurrentState = State.Preparing;
                VRInputHandler.VibrateController(grabber.hand.isLeftHand, 0.3f, 0.2f);
            }

            if (CurrentState != State.Preparing)
                return;

            var gorillas = _gorillaService?.Gorillas;
            if (gorillas != null)
            {
                foreach (var gorillaEntry in gorillas)
                {
                    var gorilla = (Gorilla)gorillaEntry;
                    if (!gorilla.health || gorilla.health.isDead) continue;

                    if (Vector3.Distance(gorilla.headTransform.position, grabber.transform.position) < DISTANCE_TO_HEAL && gorilla.health.CanHeal(true))
                    {
                        Give(gorilla);
                        return;
                    }
                }
            }
        }
        else if (interacting)
            Object.RequestStateAuthority();
    }

    private void Give(Gorilla gorilla)
    {
        if (m_GiveShieldAmount > byte.MinValue)
        {
            gorilla.health.Heal(m_GiveShieldAmount, true);
            //PopupManager.Display("+" + m_GiveShieldAmount, transform.position, Color.cyan, 0.1f);
            CurrentState = State.Slurped;
            AwaitDespawn();
        }
    }

    public async UniTaskVoid AwaitDespawn()
    {
        await UniTask.Delay(3000); // 3 seconds
        if (Object != null)
            Runner.Despawn(Object);
    }

    public enum State : byte
    {
        Default,
        Preparing,
        Slurped
    }
}
}
