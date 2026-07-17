#nullable enable
// ArenaAimController — drag-aim mechanic.
//
// Spawned by MyTurnState.Enter, destroyed in Exit. Reads Input System Mouse
// (works because activeInputHandler=2 from D.U1). Updates a LineRenderer
// every frame while dragging; on release outside dead zone, fires
// ShotReleasedEvent. State machine routes that to NetClient.Send("shoot", ...).
//
// Slingshot direction: aim is opposite of drag (`aimDir = -drag.normalized`).
// Dead zone 10% of MaxDragWorld prevents accidental tap-fires.
//
// World plane: Y=0 (or Y=origin.y if SetOrigin(transform) was called by D.U8
// weapon prefab wire-up). For D.U4a, origin defaults to Vector3.zero so
// drag-aim works against an arbitrary scene without weapon prefabs yet.

using BillGameCore;
using RadiantArena.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RadiantArena.Weapons
{
    public class ArenaAimController : MonoBehaviour
    {
        const float MaxDragWorld = 3.0f;
        const float DeadZone = 0.10f;

        LineRenderer? _line;
        Camera? _cam;
        Vector3 _dragStart;
        bool _dragging;
        Transform? _origin;

        void Awake()
        {
            _cam = Camera.main;
            if (_cam == null) Debug.LogWarning("[Arena.Aim] No Main Camera tagged — drag-aim will use identity ray.");

            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 0;
            _line.startWidth = 0.05f;
            _line.endWidth = 0.05f;
            _line.useWorldSpace = true;
            _line.numCornerVertices = 2;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.4f, 0.9f, 0.5f, 0.9f);
                _line.material = mat;
            }
            else
            {
                Debug.LogWarning("[Arena.Aim] No URP Unlit shader found; LineRenderer may render pink.");
            }

            Debug.Log("[Arena.Aim] ArenaAimController ready");
        }

        /// <summary>D.U8 will pass a weapon prefab's transform so drag-aim originates from the weapon model position.</summary>
        public void SetOrigin(Transform? originTransform)
        {
            _origin = originTransform;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null || _cam == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragStart = ScreenToWorld(mouse.position.ReadValue());
                _dragging = true;
            }

            if (_dragging && mouse.leftButton.isPressed)
            {
                var current = ScreenToWorld(mouse.position.ReadValue());
                var drag = current - _dragStart;
                var power = Mathf.Clamp01(drag.magnitude / MaxDragWorld);
                var aimDir = drag.sqrMagnitude > 0.0001f ? -drag.normalized : Vector3.forward;
                var angle = Mathf.Atan2(aimDir.z, aimDir.x);

                Bill.Events.Fire(new AimUpdatedEvent { angle = angle, power = power });
                DrawLine(power, aimDir);
            }

            if (_dragging && mouse.leftButton.wasReleasedThisFrame)
            {
                var current = ScreenToWorld(mouse.position.ReadValue());
                var drag = current - _dragStart;
                var power = Mathf.Clamp01(drag.magnitude / MaxDragWorld);
                _dragging = false;
                ClearLine();
                Bill.Events.Fire(new AimClearedEvent());

                if (power < DeadZone)
                {
                    Debug.Log($"[Arena.Aim] release in dead zone (power={power:F2}) — canceled");
                    return;
                }

                var aimDir = -drag.normalized;
                var angle = Mathf.Atan2(aimDir.z, aimDir.x);
                Debug.Log($"[Arena.Aim] shot fired angle={angle:F2} power={power:F2}");
                Bill.Events.Fire(new ShotReleasedEvent { angle = angle, power = power });
            }
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            if (_cam == null) return Vector3.zero;
            var ray = _cam.ScreenPointToRay(screen);
            var planeY = _origin != null ? _origin.position.y : 0f;
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter);
            return Vector3.zero;
        }

        void DrawLine(float power, Vector3 aimDir)
        {
            if (_line == null) return;
            var origin = _origin != null ? _origin.position : Vector3.zero;
            var end = origin + aimDir * (power * MaxDragWorld);
            _line.positionCount = 2;
            _line.SetPosition(0, origin);
            _line.SetPosition(1, end);
        }

        void ClearLine()
        {
            if (_line != null) _line.positionCount = 0;
        }
    }
}
