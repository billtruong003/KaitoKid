using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Text;

namespace Stratton.Core.Editor
{
    public class DebugLogTracerPair
    {
        #region Fields

        public int HashCode;
        public int Id;

        #endregion

        #region Constructors

        public DebugLogTracerPair(int hashCode, int id)
        {
            HashCode = hashCode;
            Id = id;
        }

        #endregion
    }

    public class DebugLogTracerData
    {
        #region Fields

        public LogType LogType;
        public string Message;
        public string MessageToShowOnButton;
        public List<string> MethodPaths = new List<string>();
        public List<string> Files = new List<string>();
        public List<int> Lines = new List<int>();
        public string FullLog;
        public int Count;

        #endregion
    }

    public class DebugLogTracerWindow : EditorWindow
    {
        #region Fields

        private static DebugLogTracerWindow _instance;
        private Vector2 _scrollPos = new Vector2(0f, Mathf.Infinity);

        private bool _allowLog = true;
        private bool _allowWarning = true;
        private bool _allowError = true;
        private bool _collapse = true;
        private bool _allowNonStackTraceWarnings;
        private bool _autoScroll;

        private string _ignoreStr;
        private string _findStr;
        private bool _wasPlaying;

        private DebugLogTracerDetailsWindow _traceWindow;

        [NonSerialized] private GUIStyle _logButtonStyle;

        private Dictionary<int, DebugLogTracerData> _tracesDict = new Dictionary<int, DebugLogTracerData>();
        private List<DebugLogTracerPair> _tracesList = new List<DebugLogTracerPair>();
        private int _logCounter;
        private int _errorCounter;
        private int _warningCounter;
        private int _showTraceCounter;

        #endregion

        #region Public Methods

        [MenuItem("Window/Other/DebugLogTracer")]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(DebugLogTracerWindow));
            window.name = "Debug Tracer";
            window.titleContent = new GUIContent(window.name);
        }

        #endregion

        #region Private Methods

        void OnEnable()
        {
            InitLogButtonStyle();
            if (_tracesDict == null || _tracesDict.Count == 0)
            {
                _logCounter = _errorCounter = _warningCounter = 0;
            }
            Application.logMessageReceived -= OnLogReceived;
			Application.logMessageReceived += OnLogReceived;
        }

        void OnDisable()
        {
			Application.logMessageReceived -= OnLogReceived;
			Application.logMessageReceivedThreaded -= OnLogReceived;
        }

        void OnGUI()
        {
            bool isPlaying = EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling;
            if (isPlaying != _wasPlaying && _wasPlaying == false)
            {
                Clear();
                _wasPlaying = isPlaying;
            }

            Color tempColor = GUI.color;
            GUILayout.BeginHorizontal();
            if (InspectorEditor.Button("All", 25f))
            {
                _allowLog = true;
                _allowError = true;
                _allowWarning = true;
            }
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(60f));
            if (InspectorEditor.Button("Log(" + _logCounter + ")", 50))
            {
                _allowLog = true;
                _allowError = false;
                _allowWarning = false;
            }
            _allowLog = EditorGUILayout.Toggle(_allowLog);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(85f));
            if (_warningCounter > 0)
            {
                GUI.color = Color.yellow;
            }
            if (InspectorEditor.Button("Warning(" + _warningCounter + ")", 75f))
            {
                _allowLog = false;
                _allowError = false;
                _allowWarning = true;
            }
            _allowWarning = EditorGUILayout.Toggle(_allowWarning);
            if (_warningCounter > 0)
            {
                GUI.color = tempColor;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(60f));
            if (_errorCounter > 0)
            {
                GUI.color = Color.red;
            }
            if (InspectorEditor.Button("Error(" + _errorCounter + ")", 65f))
            {
                _allowLog = false;
                _allowError = true;
                _allowWarning = false;
            }
            _allowError = EditorGUILayout.Toggle(_allowError);
            if (_errorCounter > 0)
            {
                GUI.color = tempColor;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(65f));
            EditorGUILayout.LabelField("Collapse", GUILayout.MaxWidth(55f));
            _collapse = EditorGUILayout.Toggle(_collapse);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(150f));
            EditorGUILayout.LabelField("NonStackTraceWarnings", GUILayout.MaxWidth(140f));
            _allowNonStackTraceWarnings = EditorGUILayout.Toggle(_allowNonStackTraceWarnings);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(75f));
            if (InspectorEditor.Button("AutoScroll", 70f))
            {
                _autoScroll = !_autoScroll;
            }
            _autoScroll = EditorGUILayout.Toggle(_autoScroll);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(120f));
            EditorGUILayout.LabelField("Ignore", GUILayout.MaxWidth(45f));
            _ignoreStr = EditorGUILayout.TextField(_ignoreStr);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            GUILayout.BeginHorizontal(EditorStyles.toolbarButton, GUILayout.MaxWidth(130f));
            EditorGUILayout.LabelField("Find", GUILayout.MaxWidth(30f));
            _findStr = EditorGUILayout.TextField(_findStr);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                Clear();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (_autoScroll)
            {
                _scrollPos.y = Mathf.Infinity;
            }
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            _showTraceCounter = 0;
            foreach (var trace in _tracesList)
            {
                DebugLogTracerData mainTrace = _tracesDict[trace.HashCode];
                if (_collapse)
                {
                    if (mainTrace.Count - 1 == trace.Id)
                    {
                        TryShowTrace(mainTrace);
                    }
                }
                else
                {
                    TryShowTrace(mainTrace);
                }
            }
            GUILayout.EndScrollView();
        }

        void Update()
        {
            if (EditorApplication.isCompiling && EditorApplication.isPlaying)
            {
                Log.Message(BaseLogChannel.Core, "Disable playmode while compiling");
                EditorApplication.isPlaying = false;
            }
        }

        private void Clear()
        {
            InitLogButtonStyle();
            _logCounter = _errorCounter = _warningCounter = 0;
            _tracesDict.Clear();
            _tracesList.Clear();
            if (_traceWindow != null)
            {
                _traceWindow.Repaint();
            }
        }

        private void InitLogButtonStyle()
        {
            try
            {
                var styleToCopy = EditorStyles.helpBox;
                _logButtonStyle = new GUIStyle(styleToCopy);
                _logButtonStyle.wordWrap = false;
                _logButtonStyle.alignment = TextAnchor.MiddleLeft;
                _logButtonStyle.fontSize = 11;
            }
            catch {}
        }

        void TryShowTrace(DebugLogTracerData trace)
        {
            if (trace.LogType == LogType.Log && _allowLog
                ||
                trace.LogType == LogType.Warning && _allowWarning &&
                (_allowNonStackTraceWarnings || trace.MethodPaths.Count > 0)
                ||
                (trace.LogType == LogType.Assert || trace.LogType == LogType.Error || trace.LogType == LogType.Exception) &&
                _allowError)
            {
                if (_ignoreStr.IsNotNullOrEmpty() && trace.Message.Contains(_ignoreStr))
                {
                    return;
                }
                if (_findStr.IsNotNullOrEmpty() && !trace.Message.Contains(_findStr))
                {
                    return;
                }
                ShowTrace(trace);
            }
        }

        void ShowTrace(DebugLogTracerData trace)
        {
            var prevColor = GUI.color;
            Color white = _showTraceCounter++ % 2 == 0 ? new Color(0.85f, 0.85f, 0.85f, 1f) : Color.white;
            GUI.color = trace.LogType == LogType.Log
                            ? white : trace.LogType == LogType.Warning ? Color.yellow : Color.red;
            if (_traceWindow != null && _traceWindow.Trace == trace)
            {
                GUI.color = Color.green;
            }
            GUILayout.BeginHorizontal(GUILayout.MaxWidth(position.width - 10f));
            if (_logButtonStyle == null)
            {
                InitLogButtonStyle();
            }
            if (GUILayout.Button(trace.MessageToShowOnButton, _logButtonStyle, GUILayout.MaxWidth(position.width - 40f)))
            {
                ShowTraceWindow(trace);
            }
            GUILayout.FlexibleSpace();
            if (_collapse)
            {
                GUILayout.Label(trace.Count.ToString(), GUILayout.Width(20f));
            }
            GUILayout.EndHorizontal();
            GUI.color = prevColor;
        }

        private void ShowTraceWindow(DebugLogTracerData trace)
        {
            if (_traceWindow == null)
            {
                _traceWindow = DebugLogTracerDetailsWindow.ShowWindow();
            }
            _traceWindow.Trace = trace;
            _traceWindow.Repaint();
        }

        void OnLogReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
            {
                _logCounter++;
            }
            else if (type == LogType.Warning)
            {
                _warningCounter++;
            }
            else
            {
                _errorCounter++;
            }
            string fullLog = message + "\n" + stackTrace;
            int hashCode = fullLog.GetHashCode();
            DebugLogTracerData trace = null;
            if (!_tracesDict.TryGetValue(hashCode, out trace))
            {
                string atStr = " (at ";
                if (message.Contains(atStr))
                {
                    string[] msglines = message.Split('\n');
                    StringBuilder sbMessage = new StringBuilder();
                    StringBuilder sbStack = new StringBuilder();
                    for (int i = 0; i < msglines.Length; i++)
                    {
                        if (msglines[i].Contains(atStr))
                        {
                            sbStack.AppendLine(msglines[i]);
                        }
                        else
                        {
                            sbMessage.AppendLine(msglines[i]);
                        }
                    }
                    sbStack.AppendLine(atStr + "BreakJump:0");
                    message = sbMessage.ToString();
                    stackTrace = sbStack + stackTrace;
                }
                trace = new DebugLogTracerData();
                _tracesDict[hashCode] = trace;
                trace.FullLog = fullLog;
                trace.LogType = type;
                trace.Message = message;
                trace.MessageToShowOnButton = message.Replace('\n', ' ');
                var lines = stackTrace.Split('\n');
                foreach (var line in lines)
                {
                    int atPos = line.IndexOf(atStr);
                    if (atPos < 0)
                    {
                        continue;
                    }
                    string methodPath = line.Substring(0, atPos);
                    string filePathAndLine = line.Substring(atPos + atStr.Length);
                    var fpSplit = filePathAndLine.Split(':');
                    string filePath = fpSplit[0];
                    filePath = Application.dataPath + "/" + filePath.Remove(0, "Assets/".Length);
                    int lineNumber = int.Parse(fpSplit[1].Substring(0, fpSplit[1].IndexOf(')')));
                    trace.Lines.Add(lineNumber);
                    trace.MethodPaths.Add(methodPath);
                    trace.Files.Add(filePath);
                }

                foreach (var mp in trace.MethodPaths)
                {
                    if (!mp.Contains("DebugLog") && !mp.Contains("UnityLogger"))
                    {
                        trace.MessageToShowOnButton += "    [" + mp + "]";
                        break;
                    }
                }
            }

            _tracesList.Add(new DebugLogTracerPair(hashCode, trace.Count));
            trace.Count++;

            Repaint();
        }

        #endregion
    }
}