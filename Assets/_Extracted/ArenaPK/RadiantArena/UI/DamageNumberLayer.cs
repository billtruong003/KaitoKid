#nullable enable
using BillGameCore;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.UI
{
    /// <summary>
    /// Transient damage-number overlay. Spawn(worldPos, dmg, isCrit) attaches a
    /// Label child, animates pop → settle → drift up + fade, then detaches.
    /// Persistent BasePanel — open at CountdownState.Enter, close at EndState /
    /// LobbyState defensive.
    /// </summary>
    public class DamageNumberLayer : BasePanel
    {
        VisualElement? _root;

        protected override void Build(VisualElement root)
        {
            _root = root;

            var tree = Resources.Load<VisualTreeAsset>("DamageNumberLayer");
            if (tree == null)
            {
                Debug.LogError("[Arena.Dmg] DamageNumberLayer.uxml not found in Resources/");
                return;
            }
            tree.CloneTree(root);

            var uss = Resources.Load<StyleSheet>("damage_number");
            if (uss != null) root.styleSheets.Add(uss);

            // Make root pass clicks through — HUD / TurnInput below need them.
            root.pickingMode = PickingMode.Ignore;
        }

        public void Spawn(Vector3 worldPos, int damage, bool isCrit)
        {
            if (_root == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            var screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f) return; // behind camera — skip

            float panelY = Screen.height - screen.y;

            var label = new Label(damage.ToString());
            label.AddToClassList("damage-number");
            if (isCrit) label.AddToClassList("crit");
            label.pickingMode = PickingMode.Ignore;
            label.style.left = new StyleLength(screen.x);
            label.style.top  = new StyleLength(panelY);
            label.style.scale = new StyleScale(new Scale(Vector3.zero));
            label.style.opacity = new StyleFloat(1f);

            _root.Add(label);

            Debug.Log($"[Arena.Dmg] spawn dmg={damage} crit={isCrit} screen=({screen.x:F0},{panelY:F0})");

            // Single tween 0→1 driving 3-phase transform:
            //   t ∈ [0.00, 0.105]: scale 0 → 1.2
            //   t ∈ [0.105, 0.21]: scale 1.2 → 1.0
            //   t ∈ [0.21, 1.00]:  drift up 60px + alpha 1 → 0
            const float TotalDur = 0.76f;
            float startY = panelY;
            BillTween.Float(0f, 1f, TotalDur, t =>
            {
                if (label.parent == null) return;
                float scale;
                if (t < 0.105f)
                {
                    scale = Mathf.Lerp(0f, 1.2f, t / 0.105f);
                }
                else if (t < 0.21f)
                {
                    scale = Mathf.Lerp(1.2f, 1.0f, (t - 0.105f) / 0.105f);
                }
                else
                {
                    scale = 1.0f;
                    float driftT = (t - 0.21f) / 0.79f;
                    label.style.top = new StyleLength(startY - 60f * driftT);
                    label.style.opacity = new StyleFloat(1f - driftT);
                }
                label.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
            })?.OnComplete(() =>
            {
                if (label.parent != null) label.RemoveFromHierarchy();
            });
        }
    }
}
