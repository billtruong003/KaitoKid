using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mythfall.Input
{
    /* ==================== UNITY HIERARCHY SETUP ====================
     *
     * Scene: GameplayScene (Canvas Screen Space - Overlay)
     * Path:  Canvas → JoystickAnchor → VirtualJoystick (this script attached)
     *
     * Required hierarchy:
     *   VirtualJoystick (RectTransform, this script + Image bg, anchor bottom-left,
     *                    pivot 0.5,0.5, position (220,220) from corner, size 220x220)
     *     └── Handle (RectTransform, Image inner thumb sprite, anchor center,
     *                 pivot 0.5,0.5, anchoredPosition 0,0, size 100x100)
     *
     * Components on root GameObject:
     *   - Image (background sprite — circular ring, alpha ~0.5; raycastTarget = true)
     *   - VirtualJoystick (this script)
     *
     * Components on Handle child:
     *   - Image (thumb sprite — solid circle; raycastTarget = false so events fall through to root)
     *
     * Serialized fields to assign in Inspector:
     *   - background (RectTransform) → drag VirtualJoystick (self) RectTransform
     *   - handle     (RectTransform) → drag Handle child RectTransform
     *   - radius (float, default 90) → handle clamp radius in pixels (slightly less than bg half-size)
     *
     * Scene requirements:
     *   - Canvas Render Mode = Screen Space - Overlay (eventCamera null is fine)
     *   - GraphicRaycaster on Canvas
     *   - EventSystem GameObject in scene
     *
     * Behavior:
     *   - On pointer down anywhere in background: handle snaps to that point.
     *   - On drag: handle follows pointer, clamped within radius.
     *   - On pointer up: handle returns to center, MoveVector reset to zero.
     *   - MoveVector is normalized: (0,0) at center, (1,0) at right edge of radius, etc.
     *
     * Disabling: when GameplayScene unloads, OnDisable resets MoveVector so the
     * next scene's PlayerBase doesn't read stale joystick input.
     *
     * ============================================================ */

    [RequireComponent(typeof(RectTransform))]
    public class VirtualJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField] float radius = 90f;

        Vector2 _input;

        public Vector2 Input => _input;

        void Awake()
        {
            if (background == null) background = (RectTransform)transform;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            // Make sure player movement halts when scene transitions away from this joystick.
            _input = Vector2.zero;
            MobileInputManager.MoveVector = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }

        public void OnPointerDown(PointerEventData eventData) => UpdateFromPointer(eventData);

        public void OnDrag(PointerEventData eventData) => UpdateFromPointer(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            MobileInputManager.MoveVector = Vector2.zero;
        }

        void UpdateFromPointer(PointerEventData eventData)
        {
            if (background == null) return;

            // Convert screen point → local point inside background rect (center = 0,0).
            // pressEventCamera is null for Screen Space - Overlay canvases — that's fine.
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, eventData.position, eventData.pressEventCamera, out var local))
                return;

            Vector2 clamped = local;
            float effectiveRadius = Mathf.Max(1f, radius);
            if (clamped.magnitude > effectiveRadius)
                clamped = clamped.normalized * effectiveRadius;

            if (handle != null) handle.anchoredPosition = clamped;

            _input = clamped / effectiveRadius;
            MobileInputManager.MoveVector = _input;
        }
    }
}
