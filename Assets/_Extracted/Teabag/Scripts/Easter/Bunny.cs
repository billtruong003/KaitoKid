using Fusion;
using Teabag.Authentication;
using Teabag.Player;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

public class Bunny : Grabbable
{
    private static readonly List<Bunny> _bunnies = new List<Bunny>();
    public static IReadOnlyList<Bunny> Bunnies => _bunnies;

    public new Renderer renderer;
    public AdvancedAudioClip clip;
    public Animator animator;

    public GameObject spawnOnDeath;

    public List<Race> races = new List<Race>();


    private IAudioService _audioService;


    [Networked, OnChangedRender(nameof(OnRaceChanged))]
    public int raceIndex { get; set; }

    public Race race
    {
        get
        {
            return races[raceIndex];
        }
    }

    Health health;
    float lastJump;
    float secTarget;


    protected override void Awake()
    {
        base.Awake();
        _audioService = ServiceLocator.Get<IAudioService>();
    }

    public override void Spawned()
    {
        base.Spawned();
        health = GetComponent<Health>();

        if (health != null)
            health.onDeath += OnDie;


        if (HasStateAuthority)
        {
            while (raceIndex < races.Count - 1)
            {
                if (UnityEngine.Random.Range(0, 3) > 1) // 1 in 3 chance of continuing (Giving the rarest a 5.45% chance of spawning)
                    break;

                raceIndex++;
            }
        }

        OnRaceChanged();
    }

    private void OnEnable()
    {
        if (!_bunnies.Contains(this))
            _bunnies.Add(this);
    }

    private void OnDisable()
    {
        _bunnies.Remove(this);
    }

    public void OnDie()
    {
        if (!HasStateAuthority)
            return;

        Runner.Despawn(Object);
        Instantiate(spawnOnDeath, transform.position, Quaternion.identity);
    }

    public void OnRaceChanged()
    {
        renderer.sharedMaterial = race.material;

        CatalogItem item = AuthenticationUtils.catalogItems.GetItem(race.cosmetic);

        if (item == null)
        {
            Debug.LogError("Failed to get item: " + race.cosmetic);
            return;
        }

        var rarity = GetComponent<GrabbableRarity>();
        if (rarity != null)
        {
            var iItem = new InventoryItem(item);
            rarity.rarity = iItem.Rarity;
        }
    }

    public override void Render()
    {
        base.Render();
        if (GetPresser() != null && RarityAssignment.Contains(transform))
            RarityAssignment.DestroyRarity(transform);
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (!HasStateAuthority)
            return;

        if (grabber != null)
        {
            lastJump = Time.time;
            return;
        }

        if (GetPresser() != null)
        {
            lastJump = Time.time;
            return;
        }

        if (Time.time - lastJump < secTarget)
            return;

        Jump();
    }

    public void Jump()
    {
        Vector3 force = new Vector3(UnityEngine.Random.Range(-1f, 1f), 1.5f, UnityEngine.Random.Range(-1f, 1f));
        rigidbody.Rigidbody.AddForce(force * 3 * race.speedMultiplier, ForceMode.Impulse);
        transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(force).eulerAngles.y - 90, 0);

        RPCJump();
    }

    [Rpc(targets: RpcTargets.All, sources: RpcSources.StateAuthority)]
    public void RPCJump()
    {
        lastJump = Time.time;
        secTarget = UnityEngine.Random.Range(2, 4) * race.jumpDelayMultiplier;
        _audioService.Play(clip, transform.position);
        animator.SetTrigger("Jump");
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.StateAuthority)]
    public void RPCDestroy()
    {
        Runner.Despawn(Object);
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    public async void RPCConvertBegin()
    {
        canGrab = false;

        BunnyPresser presser = GetPresser();
        if (HasStateAuthority && presser != null)
        {
            Vector3 offset = transform.position - presser.animator.transform.position;
            while (transform != null)
            {
                transform.position = presser.animator.transform.position + offset;
                await UniTask.Yield();
            }
        }
    }

    BunnyPresser GetPresser()
    {
        foreach (BunnyPresser presser in BunnyPresser.Pressers)
        {
            if (presser.targetBunny == this)
            {
                return presser;
            }
        }

        return null;
    }

    [Serializable]
    public class Race
    {
        public string name = "White";
        public string cosmetic = "BUNNY WHITE";
        public Material material;

        [Header("Movement")]
        public float speedMultiplier = 1;
        public float jumpDelayMultiplier = 1;
    }
}
