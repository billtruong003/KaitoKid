#if STDB_BINDINGS
using System.Collections.Generic;
using UnityEngine;
using SpacetimeDB;
using SpacetimeDB.Types;
using LayerLab.ArtMaker;

namespace SpumOnline
{
    /// <summary>
    /// Quản lý hình ảnh nhân vật bằng hệ thống Layer Lab (Spine 2D).
    /// Áp dụng dữ liệu ngoại hình từ server, điều khiển animation và hướng quay mặt.
    /// </summary>
    public class CharacterVisualSync : MonoBehaviour
    {
        // -------------------------------------------------------
        // Tham chiếu
        // -------------------------------------------------------

        [Tooltip("Tham chiếu đến PartsManager trên nhân vật (Layer Lab).")]
        [SerializeField] private PartsManager _partsManager;

        [Tooltip("Transform để lật hướng quay mặt (thường là root của skeleton).")]
        [SerializeField] private Transform _spriteRoot;

        // -------------------------------------------------------
        // Trạng thái
        // -------------------------------------------------------

        private int _currentAnimState;
        private bool _facingRight = true;
        private bool _initialized;

        // Ánh xạ AnimState int từ server sang tên animation Spine
        private static readonly Dictionary<int, string> AnimStateMap = new()
        {
            { 0, "Idle" },
            { 1, "Run" },
            { 2, "Attack1" },
            { 3, "Die" },
            { 4, "Die" }
        };

        // -------------------------------------------------------
        // Vòng đời
        // -------------------------------------------------------

        private void Awake()
        {
            if (_partsManager == null)
                _partsManager = GetComponentInChildren<PartsManager>();

            if (_spriteRoot == null && _partsManager != null)
                _spriteRoot = _partsManager.transform;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized || _partsManager == null) return;

            _partsManager.Init();
            _initialized = true;
        }

        // -------------------------------------------------------
        // Ngoại hình
        // -------------------------------------------------------

        /// <summary>
        /// Áp dụng toàn bộ PlayerAppearance từ server lên nhân vật Layer Lab.
        /// </summary>
        public void ApplyAppearance(PlayerAppearance appearance)
        {
            if (_partsManager == null) return;
            if (!_initialized) Initialize();

            // Gán tất cả bộ phận qua PartsManager
            var parts = new Dictionary<PartsType, int>
            {
                { PartsType.Skin, appearance.Skin },
                { PartsType.Eyes, appearance.Eyes },
                { PartsType.Brow, appearance.Brow },
                { PartsType.Mouth, appearance.Mouth },
                { PartsType.Hair_Short, appearance.HairShort },
                { PartsType.Hair_Hat, appearance.HairHat },
                { PartsType.Beard, appearance.Beard },
                { PartsType.Top, appearance.Top },
                { PartsType.Bottom, appearance.Bottom },
                { PartsType.Boots, appearance.Boots },
                { PartsType.Gloves, appearance.Gloves },
                { PartsType.Helmet, appearance.Helmet },
                { PartsType.Eyewear, appearance.Eyewear },
                { PartsType.Gear_Left, appearance.GearLeft },
                { PartsType.Gear_Right, appearance.GearRight },
                { PartsType.Back, appearance.Back }
            };
            _partsManager.SetSkinActiveIndex(parts);

            // Áp dụng màu sắc
            ApplyColorFromPacked(appearance.SkinColor, c => _partsManager.ChangeSkinColor(c));
            ApplyColorFromPacked(appearance.HairColor, c => _partsManager.ChangeHairColor(c));
            ApplyColorFromPacked(appearance.BeardColor, c => _partsManager.ChangeBeardColor(c));
            ApplyColorFromPacked(appearance.BrowColor, c => _partsManager.ChangeBrowColor(c));

            Debug.Log("[CharacterVisualSync] Đã áp dụng ngoại hình Layer Lab.");
        }

        /// <summary>
        /// Áp dụng trang bị (vũ khí, giáp, mũ) lên nhân vật.
        /// </summary>
        public void ApplyEquipmentVisual(Equipment equipment, ItemDef weapon, ItemDef armor, ItemDef helmet)
        {
            if (_partsManager == null) return;

            if (weapon != null && weapon.SpriteIndex > 0)
                _partsManager.EquipParts(PartsType.Gear_Right, weapon.SpriteIndex);

            if (armor != null && armor.SpriteIndex > 0)
                _partsManager.EquipParts(PartsType.Top, armor.SpriteIndex);

            if (helmet != null && helmet.SpriteIndex > 0)
                _partsManager.EquipParts(PartsType.Helmet, helmet.SpriteIndex);
        }

        // -------------------------------------------------------
        // Animation
        // -------------------------------------------------------

        /// <summary>
        /// Đặt trạng thái animation theo giá trị int từ server.
        /// 0=Idle, 1=Run, 2=Attack, 3=Death
        /// </summary>
        public void SetAnimState(int state)
        {
            if (!_initialized) Initialize();
            if (_partsManager == null || !_initialized) return;

            _currentAnimState = state;

            if (AnimStateMap.TryGetValue(state, out string animName))
            {
                _partsManager.PlayAnimation(animName);
            }
            else
            {
                _partsManager.PlayAnimation("Idle");
            }
        }

        // -------------------------------------------------------
        // Hướng quay mặt
        // -------------------------------------------------------

        /// <summary>
        /// Đặt hướng quay mặt. Lật transform scale.x.
        /// </summary>
        public void SetFacing(bool facingRight)
        {
            _facingRight = facingRight;
            if (_spriteRoot != null)
            {
                Vector3 scale = _spriteRoot.localScale;
                scale.x = facingRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
                _spriteRoot.localScale = scale;
            }
        }

        // -------------------------------------------------------
        // Tiện ích
        // -------------------------------------------------------

        /// <summary>
        /// Giải mã packed RGBA int thành Color và gọi callback.
        /// Format: (R << 16) | (G << 8) | B
        /// </summary>
        private static void ApplyColorFromPacked(int packed, System.Action<Color> apply)
        {
            if (packed == 0) return; // 0 = mặc định, không đổi màu

            float r = ((packed >> 16) & 0xFF) / 255f;
            float g = ((packed >> 8) & 0xFF) / 255f;
            float b = (packed & 0xFF) / 255f;
            apply(new Color(r, g, b, 1f));
        }

        /// <summary>
        /// Đóng gói Color thành int để gửi lên server.
        /// </summary>
        public static int PackColor(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
            return (r << 16) | (g << 8) | b;
        }

        // -------------------------------------------------------
        // Truy cập
        // -------------------------------------------------------

        public int CurrentAnimState => _currentAnimState;
        public bool FacingRight => _facingRight;
        public PartsManager Parts => _partsManager;
    }
}

#endif // STDB_BINDINGS
