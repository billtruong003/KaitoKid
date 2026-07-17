using Opencoding.Console;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Stratton.Debugging
{
    public class DebugConsoleController : MonoBehaviour
    {
        public enum CustomTouchDetector
        {
            Default,
            MultiFingerSwipe
        }

        [SerializeField] protected DebugConsole _debugConsole;
        [SerializeField] protected CustomTouchDetector _customTouchDetector;
        [SerializeField] protected int _fingersCountForSwipe = 4;
        [SerializeField] protected GameObject _fpsCounterPrefab;
        protected GameObject _fpsCounter;

        protected bool _visible;
        protected Settings _settings;

        public void ChangeState()
        {
            SetOppositeState();
            DebugConsole.IsVisible = _visible;
        }

        public void ShowFPSCounter()
        {
            if (_fpsCounter == null)
            {
                _fpsCounter = Instantiate(_fpsCounterPrefab);
                _fpsCounter.transform.SetParent(transform, false);
            }
            _fpsCounter.SetActive(true);
        }

        public void HideFPSCounter()
        {
            if (_fpsCounter != null)
            {
                _fpsCounter.SetActive(false);
            }
        }

        protected void Awake()
        {
            _visible = DebugConsole.IsVisible;
            _settings = _debugConsole.Settings;
        }

        protected void Start()
        {
            if (_customTouchDetector == CustomTouchDetector.MultiFingerSwipe)
            {
                _debugConsole.CustomTouchDetector = new MultiFingerSwipeTouchDetector(_fingersCountForSwipe);
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard.current.onTextInput += HandleKeyInput;
#endif
        }

        protected void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            string inputString = UnityEngine.Input.inputString;
            for (int index1 = 0; index1 < _settings.OpenAndCloseKeys.Length; ++index1)
            {
                char openAndCloseKey = _settings.OpenAndCloseKeys[index1];
                for (int index2 = 0; index2 < inputString.Length; ++index2)
                {
                    if (inputString[index2] == openAndCloseKey)
                    {
                        ChangeState();
                        return;
                    }
                }
            }
#endif

            if (_debugConsole.CustomTouchDetector != null && _debugConsole.CustomTouchDetector.Update())
            {
                ChangeState();
                return;
            }

            if (DebugConsole.IsVisible != _visible)
            {
                SetOppositeState();
            }
        }

        private void HandleKeyInput(char inputCharacter)
        {
            for (int index = 0; index < _settings.OpenAndCloseKeys.Length; index++)
            {
                var character = _settings.OpenAndCloseKeys[index];
                if (inputCharacter == character)
                {
                    ChangeState();
                    break;
                }
            }
        }

        protected void SetOppositeState()
        {
            _visible = !_visible;
            _debugConsole.enabled = _visible;
        }
    }
}