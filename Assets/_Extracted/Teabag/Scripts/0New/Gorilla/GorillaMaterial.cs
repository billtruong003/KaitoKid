using Fusion;
using System;
using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;

namespace Teabag.Player
{
    public class GorillaMaterial : NetworkBehaviour
    {

        [Networked, OnChangedRender(nameof(OnMaterialChanged))]
        public Color32 colourR { get; set; }
        [Networked, OnChangedRender(nameof(OnMaterialChanged))]
        public Color32 colourG { get; set; }
        [Networked, OnChangedRender(nameof(OnMaterialChanged))]
        public Color32 colourB { get; set; }

        [Networked, OnChangedRender(nameof(OnMaterialChanged))]
        public Color32 colour { get; set; }

        [Networked, OnChangedRender(nameof(OnOutlineChanged))]
        public Color32 friendlyColour { get; set; }

        [Networked, OnChangedRender(nameof(OnOutlineChanged))]
        public Color32 enemyColour { get; set; }

        [Networked, OnChangedRender(nameof(OnOutlineChanged))]
        public TeamOutlineMode teamOutlineMode { get; set; }
        public Color clampedColour => ClampColour(colour);

        public Color ActiveOutlineColour
        {
            get
            {
                return teamOutlineMode switch
                {
                    TeamOutlineMode.Friendly => (Color)friendlyColour,
                    TeamOutlineMode.Enemy => (Color)enemyColour,
                    _ => Color.clear // None — no team outline
                };
            }
        }

        private static IGorillaService _gorillaService; // kept static per existing convention (not our code)

        private int _material;

        public int material
        {
            get => _material;
            set
            {
                if (_material == value) return;
                _material = value;
                OnMaterialChanged();
            }
        }

        [Header("Options")]
        public List<RendererSlot> rendererSlots = new List<RendererSlot>();
        public List<GorillaMaterialType> materials = new List<GorillaMaterialType>();

        internal static readonly int PropColorR = Shader.PropertyToID("_ColorR");
        internal static readonly int PropColorG = Shader.PropertyToID("_ColorG");
        internal static readonly int PropColorB = Shader.PropertyToID("_ColorB");
        internal static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");

        private static readonly int PropOutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropOutlineWidth = Shader.PropertyToID("_OutlineWidth");

        private MaterialPropertyBlock _mpb;
        private List<Material> _sharedMaterialsCache;
        private Color32 white = new Color32(255, 255, 255, 255);

        private static readonly Color DefaultFriendlyColour = new Color(0.2f, 0.6f, 1f, 1f);  // blue-ish
        private static readonly Color DefaultEnemyColour = new Color(1f, 0.25f, 0.2f, 1f);  // red-ish
        private const float DefaultTeamOutlineWidth = 1.5f;

        public Color ClampColour(Color32 raw)
        {
            Color c = raw;
            c.r = Mathf.Clamp(c.r, 0f, 0.9f);
            c.g = Mathf.Clamp(c.g, 0f, 0.9f);
            c.b = Mathf.Clamp(c.b, 0f, 0.9f);
            c.a = 1f;
            return c;
        }

        private void Awake()
        {
            _gorillaService = ServiceLocator.Get<IGorillaService>();
            _mpb = new MaterialPropertyBlock();
            _sharedMaterialsCache = new List<Material>(8);
        }

        public override void Spawned()
        {
            base.Spawned();
            Load();
            OnMaterialChanged();
            OnOutlineChanged();
        }

        private void OnMaterialChanged()
        {
            if (materials == null || materials.Count == 0 || _material < 0 || _material >= materials.Count) return;

            GorillaMaterialType materialType = materials[_material];
            Material targetMaterial = materialType.material;

            foreach (RendererSlot slot in rendererSlots)
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

                    _mpb.SetColor(PropColorR, ClampColour(colourR));
                    _mpb.SetColor(PropColorG, ClampColour(colourG));
                    _mpb.SetColor(PropColorB, ClampColour(colourB));
                    _mpb.SetColor(PropBaseColor, ClampColour(colour));

                    slot.renderer.SetPropertyBlock(_mpb, slot.materialIndex);
                }
                else
                {
                    slot.renderer.SetPropertyBlock(null, slot.materialIndex);
                }
            }
        }

        private void OnOutlineChanged()
        {
            Color outlineCol;
            float outlineWidth;

            if (teamOutlineMode == TeamOutlineMode.None)
            {
                outlineCol = Color.black;
                outlineWidth = 0f;
            }
            else
            {
                outlineCol = ActiveOutlineColour;
                outlineWidth = DefaultTeamOutlineWidth;
            }

            foreach (RendererSlot slot in rendererSlots)
            {
                if (!slot.renderer) continue;

                slot.renderer.GetPropertyBlock(_mpb, slot.materialIndex);
                _mpb.SetColor(PropOutlineColor, outlineCol);
                _mpb.SetFloat(PropOutlineWidth, outlineWidth);
                slot.renderer.SetPropertyBlock(_mpb, slot.materialIndex);
            }
        }

        public void Load()
        {
            if (!HasStateAuthority) return;

            var persistence = ServiceLocator.Get<IDataPersistenceService>();
            if (persistence == null) return;

            colourR = LoadPartColour(persistence, BodyPart.PartR);
            colourG = LoadPartColour(persistence, BodyPart.PartG);
            colourB = LoadPartColour(persistence, BodyPart.PartB);

            colour = white;
        }

        public void SetPartColour(BodyPart part, Color rgb)
        {
            if (!HasStateAuthority) return;

            switch (part)
            {
                case BodyPart.PartR:
                    colourR = rgb;
                    break;
                case BodyPart.PartG:
                    colourG = rgb;
                    break;
                case BodyPart.PartB:
                    colourB = rgb;
                    break;
            }

            OnMaterialChanged();
        }

        // ═══════════════════════════════════════════════════════════════
        //  PUBLIC API — TEAM / OUTLINE COLOURS  (new)
        // ═══════════════════════════════════════════════════════════════
        //
        //  Usage example from a future team system:
        //
        //    // On the server or state authority:
        //    gorilla.material.SetFriendlyColour(Color.cyan);
        //    gorilla.material.SetEnemyColour(Color.red);
        //    gorilla.material.SetTeamOutlineMode(TeamOutlineMode.Enemy);
        //
        //  All three are [Networked] so they auto-sync. The OnOutlineChanged
        //  callback fires on every client to update the shader.

        public void SetFriendlyColour(Color rgb)
        {
            if (!HasStateAuthority) return;
            friendlyColour = rgb;
        }

        public void SetEnemyColour(Color rgb)
        {
            if (!HasStateAuthority) return;
            enemyColour = rgb;
        }

        public void SetTeamOutlineMode(TeamOutlineMode mode)
        {
            if (!HasStateAuthority) return;
            teamOutlineMode = mode;
        }

        public void SetTeamColours(Color friendly, Color enemy)
        {
            if (!HasStateAuthority) return;
            friendlyColour = friendly;
            enemyColour = enemy;
        }

        public void SetFullTeamOutline(Color friendly, Color enemy, TeamOutlineMode mode)
        {
            if (!HasStateAuthority) return;
            friendlyColour = friendly;
            enemyColour = enemy;
            teamOutlineMode = mode;
        }

        public void ClearTeamOutline()
        {
            if (!HasStateAuthority) return;
            friendlyColour = Color.clear;
            enemyColour = Color.clear;
            teamOutlineMode = TeamOutlineMode.None;
        }

        public void ApplyDefaultTeamColours()
        {
            if (!HasStateAuthority) return;
            friendlyColour = DefaultFriendlyColour;
            enemyColour = DefaultEnemyColour;
        }

        public static void RandomiseColour()
        {
            var persistence = ServiceLocator.Get<IDataPersistenceService>();
            if (persistence == null) return;

            for (int i = 0; i < 3; i++)
            {
                var part = (BodyPart)i;
                float hue = UnityEngine.Random.value;
                float tone = UnityEngine.Random.Range(0.3f, 0.7f);

                persistence.TrySaveData($"Colour_{part}_Hue", hue);
                persistence.TrySaveData($"Colour_{part}_Tone", tone);
            }

            if (_gorillaService?.LocalGorilla is Gorilla localGorilla && localGorilla.material != null)
            {
                localGorilla.material.Load();
            }
        }

        private Color LoadPartColour(IDataPersistenceService persistence, BodyPart part)
        {
            float hue = persistence.LoadData<float>($"Colour_{part}_Hue");
            float tone = persistence.LoadData<float>($"Colour_{part}_Tone");

            if (Mathf.Approximately(hue, 0f) && Mathf.Approximately(tone, 0f))
            {
                return Color.white;
            }

            SmartToneCurve(tone, out float s, out float v);
            return Color.HSVToRGB(hue, s, v);
        }

        private static void SmartToneCurve(float t, out float saturation, out float value, float pastelSat = 0.70f)
        {
            float tLower = Mathf.Clamp01(t * 2f);
            float tUpper = Mathf.Clamp01((t - 0.5f) * 2f);

            value = tLower;
            saturation = Mathf.Lerp(pastelSat, 0f, tUpper);
        }
    }

    public enum BodyPart
    {
        PartR = 0,
        PartG = 1,
        PartB = 2
    }

    public enum TeamOutlineMode
    {
        None = 0,
        Friendly = 1,
        Enemy = 2
    }

    [Serializable]
    public struct RendererSlot
    {
        public Renderer renderer;
        public int materialIndex;
    }

    [Serializable]
    public struct GorillaMaterialType
    {
        public Material material;
        public bool useColour;
    }
}
