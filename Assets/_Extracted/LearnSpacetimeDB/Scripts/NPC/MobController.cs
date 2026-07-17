#if STDB_BINDINGS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;
using LayerLab.ArtMaker;

namespace SpumOnline.NPC
{
    /// <summary>
    /// Controller cho mob (quai NPC) dung Layer Lab (Spine 2D).
    /// Nhan cap nhat tu bang mob_instance, noi suy vi tri,
    /// hien thanh mau va xu ly animation chet.
    /// </summary>
    public class MobController : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------

        [Header("Visual (Layer Lab)")]
        [SerializeField] private PartsManager _partsManager;
        [SerializeField] private Transform spriteRoot;

        [Header("HP Bar")]
        [SerializeField] private Slider hpBarSlider;
        [SerializeField] private GameObject hpBarRoot;

        [Header("Settings")]
        [SerializeField] private float lerpSpeed = 10f;
        [SerializeField] private float deathDestroyDelay = 2f;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        public uint MobId { get; private set; }
        public string MobName { get; private set; }
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }

        private Vector2 _targetPosition;
        private bool _hasTarget;
        private bool _isDead;
        private bool _facingRight;
        private int _currentAnimState;

        // Anh xa AnimState int tu server sang ten animation Spine
        private static readonly Dictionary<int, string> AnimStateMap = new()
        {
            { 0, "Idle" },      // Idle
            { 1, "Run" },       // Patrol
            { 2, "Run" },       // Chase
            { 3, "Attack1" },   // Attack
            { 4, "Run" },       // Return
            { 5, "Die" }        // Dead
        };

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void Awake()
        {
            if (_partsManager == null)
                _partsManager = GetComponentInChildren<PartsManager>();

            if (spriteRoot == null && _partsManager != null)
                spriteRoot = _partsManager.transform;
        }

        private void Start()
        {
            if (_partsManager != null)
                _partsManager.Init();

            if (hpBarRoot != null)
                hpBarRoot.SetActive(true);
        }

        private void Update()
        {
            if (_isDead || !_hasTarget) return;

            Vector2 currentPos = (Vector2)transform.position;
            float dist = Vector2.Distance(currentPos, _targetPosition);

            if (dist > 5f)
            {
                transform.position = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
            }
            else if (dist > 0.01f)
            {
                Vector2 newPos = Vector2.Lerp(currentPos, _targetPosition, lerpSpeed * Time.deltaTime);
                transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
            }

            // Z sorting
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y,
                transform.localPosition.y * 0.01f
            );
        }

        // -------------------------------------------------------
        // Initialization
        // -------------------------------------------------------

        /// <summary>
        /// Khoi tao mob tu du lieu MobInstance cua server.
        /// Goi boi mob spawner khi mob moi xuat hien.
        /// </summary>
        public void Initialize(MobInstance mobData)
        {
            MobId = mobData.MobId;
            CurrentHp = mobData.CurrentHp;
            _facingRight = mobData.FacingRight;
            _targetPosition = new Vector2(mobData.PosX, mobData.PosY);
            _hasTarget = true;
            _isDead = false;

            var gm = GameManager.Instance;
            if (gm != null && gm.Connection != null)
            {
                var mobDef = gm.Connection.Db.MobDef.MobDefId.Find(mobData.MobDefId);
                if (mobDef != null)
                {
                    MobName = mobDef.Name;
                    MaxHp = mobDef.MaxHp;
                }
                else
                {
                    MobName = $"Mob_{mobData.MobDefId}";
                    MaxHp = mobData.CurrentHp;
                }
            }
            else
            {
                MobName = $"Mob_{mobData.MobDefId}";
                MaxHp = mobData.CurrentHp;
            }

            transform.position = new Vector3(mobData.PosX, mobData.PosY, 0f);
            UpdateHpBar();
            SetFacing(_facingRight);
            SetAnimState(mobData.AnimState);
        }

        // -------------------------------------------------------
        // Server Update
        // -------------------------------------------------------

        public void OnServerUpdate(MobInstance newData)
        {
            if (_isDead) return;

            _targetPosition = new Vector2(newData.PosX, newData.PosY);
            _hasTarget = true;

            int previousHp = CurrentHp;
            CurrentHp = newData.CurrentHp;
            UpdateHpBar();

            if (newData.FacingRight != _facingRight)
            {
                _facingRight = newData.FacingRight;
                SetFacing(_facingRight);
            }

            if (newData.AnimState != _currentAnimState)
            {
                SetAnimState(newData.AnimState);
            }

            if (CurrentHp <= 0 && previousHp > 0)
            {
                OnDeath();
            }
        }

        // -------------------------------------------------------
        // HP Bar
        // -------------------------------------------------------

        private void UpdateHpBar()
        {
            if (hpBarSlider == null) return;

            float ratio = MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

            if (Bill.IsReady)
            {
                BillTween.Float(hpBarSlider.value, ratio, 0.3f, v => { if (hpBarSlider != null) hpBarSlider.value = v; })
                    ?.SetEase(EaseType.OutQuad)
                    .SetTarget(hpBarSlider);
            }
            else
            {
                hpBarSlider.value = ratio;
            }
        }

        // -------------------------------------------------------
        // Visual
        // -------------------------------------------------------

        private void SetFacing(bool facingRight)
        {
            _facingRight = facingRight;
            if (spriteRoot != null)
            {
                Vector3 scale = spriteRoot.localScale;
                scale.x = facingRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                spriteRoot.localScale = scale;
            }
        }

        private void SetAnimState(int state)
        {
            _currentAnimState = state;
            if (_partsManager == null) return;

            if (AnimStateMap.TryGetValue(state, out string animName))
                _partsManager.PlayAnimation(animName);
            else
                _partsManager.PlayAnimation("Idle");
        }

        // -------------------------------------------------------
        // Death
        // -------------------------------------------------------

        private void OnDeath()
        {
            _isDead = true;

            SetAnimState(5); // Death

            if (hpBarRoot != null)
                hpBarRoot.SetActive(false);

            if (Bill.IsReady)
            {
                // Fade skeleton alpha qua SkeletonAnimation
                var skeletonAnim = _partsManager != null ? _partsManager.GetSkeletonAnimation() : null;
                if (skeletonAnim != null)
                {
                    BillTween.Float(1f, 0f, deathDestroyDelay * 0.8f, alpha =>
                    {
                        if (skeletonAnim != null && skeletonAnim.skeleton != null)
                            skeletonAnim.skeleton.A = alpha;
                    })?.SetEase(EaseType.InQuad)
                      .SetTarget(skeletonAnim);
                }

                Bill.Timer.Delay(deathDestroyDelay, () =>
                {
                    if (gameObject != null)
                        Destroy(gameObject);
                });
            }
            else
            {
                Destroy(gameObject, deathDestroyDelay);
            }
        }
    }
}

#endif // STDB_BINDINGS
