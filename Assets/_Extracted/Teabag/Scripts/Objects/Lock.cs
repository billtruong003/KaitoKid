using System;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using Teabag.Player;
using UnityEngine;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Gameplay
{
public class Lock : DistanceTouch
{
    public AdvancedAudioClip finish;
    public AdvancedAudioClip start;
    public AudioSource source;
    public Animator animator;
    public LockState state;
    public Action<LockState, string> onStateUpdated;
    private IGorillaService _gorillaService;
    private IAudioService _audioService;
    private Chest OwningChest;

    protected override void Awake()
    {
        base.Awake();
        _gorillaService = ServiceLocator.Get<IGorillaService>();
        _audioService = ServiceLocator.Get<IAudioService>();
    }

    private void Start()
    {
        OwningChest = GetComponentInParent<Chest>();
    }

    public async UniTask DetachLock()
    {
        Rigidbody rb = transform.GetComponent<Rigidbody>();
        Collider col = transform.GetComponent<Collider>();

        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        await UniTask.Delay(500);

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (col != null)
        {
            col.enabled = false;
        }

        await UniTask.Delay(500);

        gameObject.SetActive(false);
        enabled = false;
    }

    public async UniTask Unlock()
    {
        if (!OwningChest.canOpen && OwningChest.currentState != ChestState.Opened)
            return;

        if (state == LockState.StartedUnlocking || state == LockState.Unlocking || state == LockState.Unlocked)
            return;

        foreach (Lock l in transform.parent.GetComponentsInChildren<Lock>())
        {
            if (l.state == LockState.StartedUnlocking)
            {
                return;
            }
        }

        _audioService.Play(start, transform.position);

        state = LockState.Unlocking;
        if (onStateUpdated != null)
            onStateUpdated.Invoke(state, _gorillaService.LocalGorilla.PlayerId);

        animator.Play("LockUnlock");
        source.Play();
        await UniTask.Delay(500);
        state = LockState.StartedUnlocking;
        await UniTask.Delay(1000);
        source.Stop();
        _audioService.Play(finish, transform.position);
        state = LockState.Unlocked;

        if (onStateUpdated != null)
            onStateUpdated.Invoke(state, _gorillaService.LocalGorilla.PlayerId);
    }

    private void Update()
    {
        CheckHands();
    }

    public void CheckHands()
    {
        var localGorilla = _gorillaService?.LocalGorilla as Gorilla;
        if (localGorilla == null)
            return;

        if (localGorilla.health == null)
            return;

        if (localGorilla.health.isDead)
            return;

            if (state != LockState.Unlocking && state != LockState.Unlocked)
            {
                if (CheckDistance(true))
                {
                    Unlock();
                    VRInputHandler.VibrateController(true, 0.2f, 0.1f);
                }
                else if (CheckDistance(false))
                {
                    Unlock();
                    VRInputHandler.VibrateController(false, 0.2f, 0.1f);
                }
            }
        }

        public enum LockState
        {
            Locked,
            StartedUnlocking,
            Unlocking,
            Unlocked
        }
}
}
