using System.Collections.Generic;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;

namespace Teabag.Player
{
    public sealed class DebugCommandConsole : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float VR_ACTIVATION_HOLD_TIME = 0.5f;
        private const float VR_SPAWN_DISTANCE = 1.5f;

        private bool _isVisible;
        private string _input = string.Empty;
        private Rect _windowRect;
        private bool _inputFocused;
        private bool _submitRequested;
        private int _refocusCountdown;
        private Vector2 _scrollPos;
        private int _historyIndex = -1;

        private GameObject _vrConsolePrefab;
        private DebugCommandRegistry _registry;
        private VRDebugConsole _vrConsole;
        private float _vrActivationTimer;
        private bool _isVR;
        private bool _loggedVRState;

        private void Awake()
        {
            Debug.Log("[DebugCommandConsole] Awake() called — component is alive.");
            _windowRect = new Rect(Screen.width / 2f - 250f, 10f, 500f, 300f);
            _registry = new DebugCommandRegistry();
            Debug.Log("[DebugCommandConsole] Registry created.");

            RegisterCommand(new SpawnWeaponCommand());
            RegisterCommand(new HealCommand());
            RegisterCommand(new GodCommand());
            RegisterCommand(new FlyCommand());
            RegisterCommand(new JoinCommand());
            RegisterCommand(new TeleportCommand());
            RegisterCommand(new ZoneCommand());
            RegisterCommand(new DamageCommand());
            RegisterCommand(new KillCommand());
            RegisterCommand(new RespawnCommand());
            RegisterCommand(new FpsCommand());
            RegisterCommand(new NetCommand());
            RegisterCommand(new RegionCommand());
            RegisterCommand(new PlayersCommand());
            RegisterCommand(new NameCommand());
            RegisterCommand(new AmmoCommand());
            RegisterCommand(new RandomSpawnCommand());
            RegisterCommand(new HelpCommand(_registry));
            Debug.Log($"[DebugCommandConsole] All commands registered. Count: {_registry.Commands.Count}");
        }

        public void SetVRConsolePrefab(GameObject prefab)
        {
            _vrConsolePrefab = prefab;
        }

        private void RegisterCommand(IDebugCommand command)
        {
            _registry.Register(command);
        }

        private void Update()
        {
            _isVR = UnityEngine.XR.XRSettings.isDeviceActive;

            if (_loggedVRState is false)
            {
                Debug.Log($"[DebugCommandConsole] Update() first frame — XRSettings.isDeviceActive={_isVR}, " +
                          $"XRSettings.loadedDeviceName='{UnityEngine.XR.XRSettings.loadedDeviceName}'");
                _loggedVRState = true;
            }

            if (_isVR)
            {
                UpdateVRActivation();
            }
            else
            {
                if (_inputFocused is false && Input.GetKeyDown(KeyCode.BackQuote))
                    _isVisible = !_isVisible;
            }
        }

        private void UpdateVRActivation()
        {
            bool leftJoystickDown = VRInputHandler.GetInputDown(true, InputType.JoystickPress);
            bool rightJoystickDown = VRInputHandler.GetInputDown(false, InputType.JoystickPress);

            if (leftJoystickDown || rightJoystickDown)
            {
                Debug.Log($"[DebugCommandConsole] VR Joystick — Left={leftJoystickDown}, Right={rightJoystickDown}, " +
                          $"Timer={_vrActivationTimer:F3}s / {VR_ACTIVATION_HOLD_TIME}s");
            }

            if (leftJoystickDown && rightJoystickDown)
            {
                _vrActivationTimer += Time.unscaledDeltaTime;
                if (_vrActivationTimer >= VR_ACTIVATION_HOLD_TIME)
                {
                    Debug.Log("[DebugCommandConsole] VR activation timer reached — calling ToggleVRConsole()");
                    _vrActivationTimer = 0f;
                    ToggleVRConsole();
                }
            }
            else
            {
                _vrActivationTimer = 0f;
            }
        }

        [ContextMenu("Show VR Console")]
        private void ToggleVRConsole()
        {
            Debug.Log("[DebugCommandConsole] ToggleVRConsole() called.");

            if (_vrConsole is object)
            {
                Debug.Log("[DebugCommandConsole] Destroying existing VR console.");
                Destroy(_vrConsole.gameObject);
                _vrConsole = null;
                return;
            }

            var gorillaService = ServiceLocator.Get<IGorillaService>();
            bool hasLocalGorilla = gorillaService?.HasLocalGorilla ?? false;
            var localGorilla = hasLocalGorilla ? gorillaService.LocalGorilla as Gorilla : null;
            Transform head = localGorilla != null ? localGorilla.headTransform : null;
            Debug.Log($"[DebugCommandConsole] LocalGorilla exists={hasLocalGorilla}, headTransform={head}");

            if (head is null)
            {
                var cam = Camera.main;
                Debug.Log($"[DebugCommandConsole] No head transform — Camera.main={cam}");
                if (cam is null)
                {
                    Debug.LogWarning("[DebugCommandConsole] No Camera.main found — cannot spawn VR console.");
                    return;
                }
                head = cam.transform;
            }

            Vector3 forward = head.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 spawnPos = head.position + forward * VR_SPAWN_DISTANCE;
            Quaternion spawnRot = Quaternion.LookRotation(forward, Vector3.up);

            if (_vrConsolePrefab is null)
            {
                Debug.LogError("[DebugCommandConsole] VR console prefab not assigned. Wire it via PlayerDebugController.");
                return;
            }

            var consoleObj = Instantiate(_vrConsolePrefab, spawnPos, spawnRot);
            consoleObj.name = "VRDebugConsole";
            _vrConsole = consoleObj.GetComponent<VRDebugConsole>();
            _vrConsole.Initialize(_registry);
        }

        private void OnGUI()
        {
            if (_isVR || _isVisible is false)
                return;

            _windowRect = GUILayout.Window(99, _windowRect, DrawWindow, "Debug Console");
        }

        private void DrawWindow(int id)
        {
            _inputFocused = GUI.GetNameOfFocusedControl() == "ConsoleInput";

            if (Event.current.type == EventType.KeyDown && _inputFocused)
            {
                if (Event.current.keyCode == KeyCode.Return)
                {
                    Event.current.Use();
                    _submitRequested = true;
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    Event.current.Use();
                    NavigateHistory(-1);
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    Event.current.Use();
                    NavigateHistory(1);
                }
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            IReadOnlyList<string> log = _registry.Log;
            for (int i = 0; i < log.Count; i++)
                GUILayout.Label(log[i]);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ConsoleInput");
            _input = GUILayout.TextField(_input);
            bool submitted = GUILayout.Button("Execute", GUILayout.Width(70));
            GUILayout.EndHorizontal();

            if (_refocusCountdown > 0)
            {
                _refocusCountdown--;
                if (_refocusCountdown == 0)
                    GUI.FocusControl("ConsoleInput");
            }

            if ((submitted || _submitRequested) && !string.IsNullOrWhiteSpace(_input))
            {
                _submitRequested = false;
                GameLogger.Info($"[DebugConsole] {_input}");

                _registry.Execute(_input);
                _scrollPos.y = float.MaxValue;
                _input = string.Empty;
                _historyIndex = -1;
                _refocusCountdown = 2;
            }
            else
            {
                _submitRequested = false;
            }

            GUILayout.Label(_isVR
                ? "Hold both joystick clicks to toggle VR console."
                : "Press ` to toggle. Type 'help' for available commands.");
            GUI.DragWindow();
        }

        private void NavigateHistory(int direction)
        {
            IReadOnlyList<string> history = _registry.History;
            if (history.Count == 0)
                return;

            if (_historyIndex == -1)
            {
                if (direction < 0)
                    _historyIndex = history.Count - 1;
                else
                    return;
            }
            else
            {
                _historyIndex += direction;
                _historyIndex = Mathf.Clamp(_historyIndex, 0, history.Count - 1);
            }

            _input = history[_historyIndex];
        }

        private void OnDestroy()
        {
            if (_vrConsole is object)
                Destroy(_vrConsole.gameObject);
        }
#endif
    }
}
