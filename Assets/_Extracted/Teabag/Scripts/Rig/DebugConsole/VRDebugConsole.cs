#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Teabag.Player
{
    public sealed class VRDebugConsole : MonoBehaviour
    {
        private const int MAX_LOG_LINES = 12;
        private const int MAX_INPUT_LENGTH = 64;

        [SerializeField] private TMP_Text _logText;
        [SerializeField] private TMP_Text _inputText;

        private DebugCommandRegistry _registry;
        private string _inputBuffer = string.Empty;
        private int _historyIndex = -1;

        public void Initialize(DebugCommandRegistry registry)
        {
            _registry = registry;
            RefreshDisplay();
        }

        public void OnKeyPressed(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.Escape:
                    Destroy(gameObject);
                    return;

                case KeyCode.Return:
                    SubmitInput();
                    break;

                case KeyCode.Backspace:
                    if (_inputBuffer.Length > 0)
                        _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    break;

                case KeyCode.UpArrow:
                    NavigateHistory(-1);
                    break;

                case KeyCode.DownArrow:
                    NavigateHistory(1);
                    break;

                case KeyCode.Space:
                    if (_inputBuffer.Length < MAX_INPUT_LENGTH)
                        _inputBuffer += " ";
                    break;

                default:
                    string keyName = keyCode.ToString();
                    if (keyName.Length == 1 && _inputBuffer.Length < MAX_INPUT_LENGTH)
                    {
                        _inputBuffer += keyName.ToLower();
                    }
                    else if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9
                             && _inputBuffer.Length < MAX_INPUT_LENGTH)
                    {
                        _inputBuffer += (char)('0' + (keyCode - KeyCode.Alpha0));
                    }
                    else if (keyCode == KeyCode.Period && _inputBuffer.Length < MAX_INPUT_LENGTH)
                    {
                        _inputBuffer += ".";
                    }
                    else if (keyCode == KeyCode.Minus && _inputBuffer.Length < MAX_INPUT_LENGTH)
                    {
                        _inputBuffer += "-";
                    }
                    break;
            }

            RefreshDisplay();
        }

        public void ExecuteQuickCommand(string command)
        {
            _inputBuffer = command;
            SubmitInput();
        }

        private void SubmitInput()
        {
            if (string.IsNullOrWhiteSpace(_inputBuffer))
                return;

            _registry.Execute(_inputBuffer);
            _inputBuffer = string.Empty;
            _historyIndex = -1;
            RefreshDisplay();
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

            _inputBuffer = history[_historyIndex];
        }

        private void RefreshDisplay()
        {
            if (_logText is object)
            {
                IReadOnlyList<string> log = _registry.Log;
                int startIndex = Mathf.Max(0, log.Count - MAX_LOG_LINES);
                var sb = new System.Text.StringBuilder();
                for (int i = startIndex; i < log.Count; i++)
                {
                    if (sb.Length > 0)
                        sb.Append('\n');
                    sb.Append(log[i]);
                }
                _logText.text = sb.ToString();
            }

            if (_inputText is object)
            {
                _inputText.text = $"> {_inputBuffer}_";
            }
        }
    }
}
#endif
