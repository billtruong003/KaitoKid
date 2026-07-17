using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Stratton.Core;
using Object = UnityEngine.Object;

#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.
#pragma warning disable 0414 // private field assigned but not used.
#pragma warning disable 0618 // obsolete
#pragma warning disable 0649 // null value

namespace Stratton.Core.Editor
{
    public class InspectorEditor : UnityEditor.Editor
    {
        #region Fields

        protected static readonly float _valueWidth = 18f;
        protected static readonly float _valueHeight = 18f;
        private static readonly Dictionary<string, Vector2> _scrollPoses = new Dictionary<string, Vector2>();
        private static readonly Dictionary<string, bool> _isShowns = new Dictionary<string, bool>();

        #endregion

        #region Public Methods

        public static int DrawBitMask(Rect position, int mask, Type enumType, GUIContent aLabel,
                                      Func<int, bool> contentValidator = null)
        {
            var itemNames = new List<string>(Enum.GetNames(enumType));
            var itemValues = new List<int>(Enum.GetValues(enumType) as int[]);

            int count = 0;
            int val = mask;
            int maskVal = 0;

            if (contentValidator != null)
            {
                count = itemValues.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (!contentValidator(itemValues[i]))
                    {
                        itemValues.RemoveAt(i);
                        itemNames.RemoveAt(i);
                    }
                }
            }

            count = itemValues.Count;
            for (int i = 0; i < count; i++)
            {
                if (itemValues[i] != 0)
                {
                    if ((val & itemValues[i]) == itemValues[i])
                    {
                        maskVal |= 1 << i;
                    }
                }
                else if (val == 0)
                {
                    maskVal |= 1 << i;
                }
            }
            int newMaskVal = EditorGUI.MaskField(position, aLabel, maskVal, itemNames.ToArray());
            int changes = maskVal ^ newMaskVal;

            for (int i = 0; i < count; i++)
            {
                if ((changes & (1 << i)) != 0) // has this list item changed?
                {
                    if ((newMaskVal & (1 << i)) != 0) // has it been set?
                    {
                        if (itemValues[i] == 0) // special case: if "0" is set, just set the val to 0
                        {
                            val = 0;
                            break;
                        }
                        val |= itemValues[i];
                    }
                    else // it has been reset
                    {
                        val &= ~itemValues[i];
                    }
                }
            }
            return val;
        }

        public static int DrawBitMask(int mask, Type enumType, string label, Func<int, bool> contentValidator = null)
        {
            return DrawBitMask(mask, enumType, new GUIContent(label), contentValidator);
        }

        public static int DrawBitMask(int mask, Type enumType, GUIContent label, Func<int, bool> contentValidator = null)
        {
            var itemNames = new List<string>(Enum.GetNames(enumType));
            var itemValues = new List<int>(Enum.GetValues(enumType) as int[]);

            int count = 0;
            int val = mask;
            int maskVal = 0;

            if (contentValidator != null)
            {
                count = itemValues.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (!contentValidator(itemValues[i]))
                    {
                        itemValues.RemoveAt(i);
                        itemNames.RemoveAt(i);
                    }
                }
            }

            count = itemValues.Count;
            for (int i = 0; i < count; i++)
            {
                if (itemValues[i] != 0)
                {
                    if ((val & itemValues[i]) == itemValues[i])
                    {
                        maskVal |= 1 << i;
                    }
                }
                else if (val == 0)
                {
                    maskVal |= 1 << i;
                }
            }
            int newMaskVal = EditorGUILayout.MaskField(label, maskVal, itemNames.ToArray());
            int changes = maskVal ^ newMaskVal;

            for (int i = 0; i < count; i++)
            {
                if ((changes & (1 << i)) != 0) // has this list item changed?
                {
                    if ((newMaskVal & (1 << i)) != 0) // has it been set?
                    {
                        if (itemValues[i] == 0) // special case: if "0" is set, just set the val to 0
                        {
                            val = 0;
                            break;
                        }
                        val |= itemValues[i];
                    }
                    else // it has been reset
                    {
                        val &= ~itemValues[i];
                    }
                }
            }
            return val;
        }

        public static void ShowNotification(string msg, bool allowDisplayDialog = false, bool printLog = false)
        {
            if (EditorWindow.focusedWindow != null)
            {
                EditorWindow.focusedWindow.ShowNotification(new GUIContent(msg));
            }
            else if (allowDisplayDialog)
            {
                EditorUtility.DisplayDialog(msg, msg, "OK");
            }
            if (printLog)
            {
                Log.Message(BaseLogChannel.Core, msg);
            }
        }

        public static Vector2 GetScrollPos(string id)
        {
            Vector2 v = Vector2.zero;
            _scrollPoses.TryGetValue(id, out v);
            return v;
        }

        public static void SetScrollPos(string id, Vector2 pos)
        {
            _scrollPoses[id] = pos;
        }

        public static bool GetIsShown(string id, bool defaultVal = false)
        {
            bool v = false;
            if (!_isShowns.TryGetValue(id, out v))
            {
                v = defaultVal;
            }
            return v;
        }

        public static void SetIsShown(string id, bool isShown)
        {
            _isShowns[id] = isShown;
        }

        public static void AddSpace()
        {
            EditorGUILayout.Space();
        }

        public static Rect CalcLayoutSimpleRect(float fixedWidth = -1f)
        {
            return GUILayoutUtility.GetRect(fixedWidth > 0 ? fixedWidth : _valueWidth, _valueHeight, "TextField");
        }

        public static void ProgressBar(float value, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            EditorGUI.ProgressBar(rect, value, label);
        }

        public static float Slider(float value, float minVal, float maxVal, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            return EditorGUI.Slider(rect, label, value, minVal, maxVal);
        }

        public static void Label(string value, GUIStyle style = null, float width = -1f)
        {
            bool isWidthSet = true;
            if (width <= 0f)
            {
                isWidthSet = false;
                width = _valueWidth;
            }
            if (style == null)
            {
                if (isWidthSet)
                {
                    EditorGUILayout.LabelField(value, GUILayout.Width(width));
                }
                else
                {
                    EditorGUILayout.LabelField(value);
                }
            }
            else
            {
                Rect rect = GUILayoutUtility.GetRect(width, _valueHeight, "TextField");
                EditorGUI.LabelField(rect, value, style);
            }
        }

        public static string TextField(string value, GUIContent label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            return EditorGUI.TextField(rect, label, value);
        }

        public static string TextField(string value, string label = "")
        {
            return TextField(value, new GUIContent(label));
        }

        public static string TextField(string value, string label, Vector2 size)
        {
            Rect rect = GUILayoutUtility.GetRect(size.x, size.y, "TextField");
            if (string.IsNullOrEmpty(label))
            {
                return EditorGUI.TextField(rect, value);
            }
            return EditorGUI.TextField(rect, label, value);
        }

        public static string TextField(string value, string label, float width)
        {
            Rect rect = GUILayoutUtility.GetRect(width, _valueHeight, "TextField");
            if (string.IsNullOrEmpty(label))
            {
                return EditorGUI.TextField(rect, value);
            }
            return EditorGUI.TextField(rect, label, value);
        }

        public static bool TextField(string value, ref string newValue, string label = "")
        {
            string str = TextField(value, label);
            newValue = str;
            return str != value;
        }

        public static string TextArea(string value, string label = "", float heightFactor = 4f)
        {
            if (label.Length > 0)
            {
                Label(label);
            }
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight * heightFactor, "TextArea");
            return EditorGUI.TextArea(rect, value);
        }

        public static Vector2 Vector2View(Vector2 v, string label, bool addSpace = true)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            Vector2 ret = EditorGUI.Vector2Field(rect, label, v);
            if (addSpace)
            {
                AddSpace();
            }
            return ret;
        }

        public static Vector3 Vector3View(Vector3 v, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight * 2f, "TextField");
            Vector3 ret = EditorGUI.Vector3Field(rect, label, v);
            AddSpace();
            return ret;
        }

        public static Vector4 Vector4View(Vector4 v, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight * 2f, "TextField");
            Vector4 ret = EditorGUI.Vector4Field(rect, label, v);
            AddSpace();
            return ret;
        }

        public static void TransformView(Transform t, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            EditorGUI.ObjectField(rect, label, t, typeof(Transform));
        }

        public static Object ObjectView(Object o, Type type, string label = "", bool allowDrag = false, string tooltip=null)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            Object obj = null;
            if (label.Length > 0)
            {
                if (tooltip.IsNullOrEmpty())
                {
                    obj = EditorGUI.ObjectField(rect, label, o, type);
                }
                else
                {
                    obj = EditorGUI.ObjectField(rect, new GUIContent(label, tooltip), o, type);
                }
            }
            else
            {
                obj = EditorGUI.ObjectField(rect, o, type, true);
            }
            if (obj != null && allowDrag)
            {
                Event e = Event.current;
                if (GUILayoutUtility.GetLastRect().Contains(e.mousePosition) && e.type == EventType.MouseDrag)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new[] { obj };
                    DragAndDrop.StartDrag("drag");
                    Event.current.Use();
                }
            }
            return obj;
        }

        public static Sprite SpriteView(Sprite sprite, string label, float spriteFieldMinWidth = 0.625f)
        {
            float properViewWidth = EditorGUIUtility.currentViewWidth - 50;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(properViewWidth * (1 - spriteFieldMinWidth)));
            sprite =
                EditorGUILayout.ObjectField(sprite, typeof(Sprite),
                    GUILayout.MinWidth(properViewWidth * spriteFieldMinWidth)) as Sprite;
            GUILayout.EndHorizontal();
            return sprite;
        }

        public static bool Button(string label, float width = -1f, float height = -1f)
        {
            if (width < 0f)
            {
                width = _valueWidth;
            }
            if (height < 0f)
            {
                height = _valueHeight;
            }
            Rect rect = GUILayoutUtility.GetRect(width, height, "TextField");
            return GUI.Button(rect, label);
        }

        public static bool Button(string label, float width, float height, params GUILayoutOption[] options)
        {
            if (width < 0f)
            {
                width = _valueWidth;
            }
            if (height < 0f)
            {
                height = _valueHeight;
            }
            Rect rect = GUILayoutUtility.GetRect(width, height, "TextField", options);
            return GUI.Button(rect, label);
        }

        public static bool Button(string label, bool enabled)
        {
            GUI.enabled = enabled;
            try
            {
                return Button(label);
            }
            finally
            {
                GUI.enabled = true;
            }
        }

        public static bool Button(string label, Color c, bool enabled = true)
        {
            GUI.enabled = enabled;
            Color def = GUI.color;
            GUI.color = c;
            try
            {
                return Button(label);
            }
            finally
            {
                GUI.color = def;
                GUI.enabled = true;
            }
        }

        public static void DictionaryField<TKey, TValue>(Dictionary<TKey, TValue> dict, ref Vector2 scrollPosition,
                                                         float expectedHeight = 100f)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(expectedHeight));
            EditorGUILayout.BeginVertical();
            foreach (var pair in dict)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(pair.Key.ToString());
                GUILayout.Label(pair.Value.ToString());
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        public static void ListField<T>(IEnumerable list, ref bool show, ref Vector2 scrollPosition, string label = "",
                                        float expectedHeight = 100f)
            where T : Object
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            if (show)
            {
                int count = 0;
                foreach (var val in list)
                {
                    count++;
                }

                float height = Mathf.Min(expectedHeight, _valueHeight * Mathf.Max(count, 1)) + 5; // +5 is for the border
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                {
                    if (count == 0)
                    {
                        Label("Empty", EditorStyles.centeredGreyMiniLabel);
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical();
                        {
                            int id = 0;
                            foreach (var val in list)
                            {
                                EditorGUILayout.ObjectField(id.ToString(), val as T, typeof(T));
                                id++;
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }


        public static void ListFieldAdv(ReorderableList reorderableList, ref bool show, string label = "",
                                        bool includeClearButton = false)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(
                new Rect(rect.x, rect.y, rect.width - (includeClearButton ? 60 : 0), EditorGUIUtility.singleLineHeight),
                show, new GUIContent(label), true);

            if (show)
            {
                reorderableList.DoLayoutList();
            }
            if (
                includeClearButton &&
                GUI.Button(new Rect(rect.x + rect.width - 60, rect.y, 60, EditorGUIUtility.singleLineHeight), "Clear"))
            {
                reorderableList.list.Clear();
            }
        }

        public static void ListField<T>(ICollection<T> list, ref Vector2 scrollPosition, ref bool show, string label = "",
                                        float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * Mathf.Max(list.Count, 1)) + 5;// +5 is for the border

            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                {
                    EditorGUILayout.BeginVertical();
                    {
                        int id = 0;

                        foreach (var val in list)
                        {
                            GUILayout.Label(id + ": " + val);
                            id++;
                        }

                        if (id == 0)
                        {
                            Label("Empty", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        public static void ListFieldWithButtons<T>(ICollection<T> list, ref Vector2 scrollPosition, ref bool show,
                                                   string buttonText, Action<T> action, string label = "",
                                                   float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * Mathf.Max(list.Count, 1)) + 5; // +5 is for the border

            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                {
                    EditorGUILayout.BeginVertical();
                    {
                        int i = 0;

                        foreach (var elem in list)
                        {
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.Label(i + ": " + elem);

                                if (GUILayout.Button(buttonText))
                                {
                                    action(elem);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                            i++;
                        }

                        if (i == 0)
                        {
                            Label("Empty", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        public static void ListField(List<string> list, ref Vector2 scrollPosition, ref bool show, string label = "",
                                     bool showAddRemove = true, float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * list.Count) + 5; // +5 is for the border
            if (showAddRemove)
            {
                height += _valueHeight;
            }
            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                EditorGUILayout.BeginVertical();

                for (int i = 0; i < list.Count; i++)
                {
                    if (showAddRemove)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Label(i + ":", null, 40);
                        list[i] = TextField(list[i], "");
                        if (Button("Remove", 20f))
                        {
                            list.RemoveAt(i--);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        list[i] = TextField(list[i], i + ":");
                    }
                }

                if (showAddRemove)
                {
                    if (Button("Add"))
                    {
                        if (list.Count > 0)
                        {
                            list.Add(list.Last());
                        }
                        else
                        {
                            list.Add("");
                        }
                        scrollPosition.y = Mathf.Infinity;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
            }
        }

        public static void ListIntField(List<int> list, ref Vector2 scrollPosition, ref bool show, string label = "",
                                        bool showAddRemove = true, float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * list.Count) + 5; // +5 is for the border
            if (showAddRemove)
            {
                height += _valueHeight;
            }
            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                EditorGUILayout.BeginVertical();

                for (int i = 0; i < list.Count; i++)
                {
                    if (showAddRemove)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Label(i + ":");
                        list[i] = IntField(list[i], "");
                        if (Button("Remove", 20f))
                        {
                            list.RemoveAt(i--);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        list[i] = IntField(list[i], i + ":");
                    }
                }

                if (showAddRemove)
                {
                    if (Button("Add"))
                    {
                        list.Add(0);
                        scrollPosition.y = Mathf.Infinity;
                    }
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
            }
        }

        public static void ListEnumField<T>(List<T> list, ref Vector2 scrollPosition, ref bool show,
                                            string label = "", bool showAddRemove = true, float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * list.Count) + 5; // +5 is for the border
            if (showAddRemove)
            {
                height += _valueHeight;
            }
            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                EditorGUILayout.BeginVertical();

                for (int i = 0; i < list.Count; i++)
                {
                    if (showAddRemove)
                    {
                        EditorGUILayout.BeginHorizontal();
                        Label(i + ":");
                        list[i] = (T) (object) EnumField((Enum) ((object) list[i]), "");
                        if (Button("Remove", 20f))
                        {
                            list.RemoveAt(i--);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        list[i] = (T) (object) EnumField((Enum) ((object) list[i]), i + ":");
                    }
                }

                if (showAddRemove)
                {
                    if (Button("Add"))
                    {
                        var val = (Enum.GetValues(typeof(T)) as int[])[0];
                        list.Add((T) (object) val);
                    }
                    scrollPosition.y = Mathf.Infinity;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();
            }
        }

        public static void ListField(List<Vector2> list, ref Vector2 scrollPosition, ref bool show, string label = "",
                                     float expectedHeight = 100f)
        {
            if (list == null)
            {
                Label(label + " - NULL");
                return;
            }

            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            show = EditorGUI.Foldout(rect, show, new GUIContent(label), true);
            float height = Mathf.Min(expectedHeight, _valueHeight * Mathf.Max(list.Count, 1)) + 5; // +5 is for the border

            if (show)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox,
                    GUILayout.Height(height));
                {
                    EditorGUILayout.BeginVertical();
                    {
                        if (list.Count == 0)
                        {
                            Label("Empty", EditorStyles.centeredGreyMiniLabel);
                        }
                        else
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                list[i] = Vector2View(list[i], i + ":");
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        public static Enum EnumField(string label, Enum enumVal)
        {
            return EnumField(enumVal, label);
        }

        public static Enum EnumField(Enum enumVal, string label = "")
        {
            if (string.IsNullOrEmpty(label))
            {
                return EditorGUILayout.EnumPopup(enumVal);
            }
            return EditorGUILayout.EnumPopup(label, enumVal);
        }

        public static int IntField(string label, int val)
        {
            return IntField(val, label);
        }

        public static int IntField(int val, string label = "")
        {
            if (string.IsNullOrEmpty(label))
            {
                return EditorGUILayout.IntField(val);
            }

            return EditorGUILayout.IntField(label, val);
        }

        public static int IntFieldVerticalLabel(int val, string label = "")
        {
            EditorGUILayout.BeginVertical();
            Label(label);
            int retVal = EditorGUILayout.IntField(val);
            EditorGUILayout.EndVertical();
            return retVal;
        }

        public static float FloatField(string label, float val)
        {
            return FloatField(val, label);
        }

        public static float FloatField(float val, string label = "", float width = -1f)
        {
            if (string.IsNullOrEmpty(label))
            {
                if (width > 0f)
                {
                    return EditorGUILayout.FloatField(val, GUILayout.Width(width));
                }
                return EditorGUILayout.FloatField(val);
            }
            if (width > 0f)
            {
                return EditorGUILayout.FloatField(label, val, GUILayout.Width(width));
            }
            return EditorGUILayout.FloatField(label, val);
        }

        public static void TextureView(Texture2D tex, float width, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, "TextField");
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, true);
        }

        public static void HelpBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        public static bool FoldOut(bool show, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight);
            return EditorGUI.Foldout(rect, show, label);
        }

        public static bool BoolField(string label, bool value, bool isOnLeft = false)
        {
            return BoolField(new GUIContent(label), value, isOnLeft);
        }

        public static bool BoolField(GUIContent guiContent, bool value, bool isOnLeft = false)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            if (isOnLeft)
            {
                return EditorGUI.ToggleLeft(rect, guiContent, value);
            }
            return EditorGUI.Toggle(rect, guiContent, value);
        }

        //call it in method OnSceneGUI();
        /* Commented out, because "Tools" namespace couldn't be find after namespaces refactoring.
        public static int DrawEditablePoints(Vector3[] points, Color color, float size = 0.09f)
        {
            Handles.color = color;
            if (Tools.current != Tool.View && Event.current.type == EventType.Layout)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    HandleUtility.AddControl(-i - 1, HandleUtility.DistanceToLine(points[i], points[i]));
                }
            }
            int wasMoved = -1;

            if (Tools.current != Tool.View)
            {
                HandleUtility.AddDefaultControl(0);
            }

            for (int i = 0; i < points.Length; i++)
            {
                if (Tools.current == Tool.Move)
                {
#if UNITY_LE_4_3
//Undo.SetSnapshotTarget(script, "Moved Point");
#else
                    //Undo.RecordObject(target, "Moved Point");
#endif
                    Handles.SphereCap(-i - 1, points[i], Quaternion.identity,
                        HandleUtility.GetHandleSize(points[i]) * size * 2);
                    Vector3 pre = points[i];
                    Vector3 post = Handles.PositionHandle(points[i], Quaternion.identity);
                    if (pre != post)
                    {
                        points[i] = post;
                        wasMoved = i;
                    }
                }
                else
                {
                    Handles.SphereCap(-i - 1, points[i], Quaternion.identity, HandleUtility.GetHandleSize(points[i]) * size);
                }
            }
            return wasMoved;
        }
        */

        public static SearchablePopupData SearchablePopup(SearchablePopupData data)
        {
            data.id = SearchablePopup(data.id, data.label, data.strings, ref data.searchShow, ref data.searchScrollPos,
                ref data.searchString);
            return data;
        }

        public static int SearchablePopup(int id, string label, string[] strings, ref bool searchShow,
                                          ref Vector2 searchScrollPos, ref string searchString)
        {
            if (searchString == null)
            {
                searchString = "";
            }
            GUILayout.BeginVertical();
            if (string.IsNullOrEmpty(label))
            {
                id = EditorGUILayout.Popup(id, strings);
            }
            else
            {
                id = EditorGUILayout.Popup(label, id, strings);
            }
            if (searchShow = EditorGUILayout.Foldout(searchShow, "Search for:"))
            {
                searchScrollPos = EditorGUILayout.BeginScrollView(searchScrollPos,
                    GUILayout.Height(searchString.Length > 0 ? 128 : _valueHeight * 2f));
                searchString = EditorGUILayout.TextField(searchString);
                if (!string.IsNullOrEmpty(searchString))
                {
                    for (int i = 0; i < strings.Length; i++)
                    {
                        var str = strings[i];
                        if (!string.IsNullOrEmpty(str) && str.Contains(searchString))
                        {
                            if (GUILayout.Button(str))
                            {
                                id = i;
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            return id;
        }


        public static int Popup(int id, string label, string[] strings)
        {
            if (string.IsNullOrEmpty(label))
            {
                id = EditorGUILayout.Popup(id, strings);
            }
            else
            {
                id = EditorGUILayout.Popup(label, id, strings);
            }

            return id;
        }

        public static void SearchField(ref string filter)
        {
            var cachedColor = GUI.color;

            EditorGUILayout.BeginHorizontal();
            {
                if (filter.IsNotNullOrEmpty())
                {
                    GUI.color = Color.green;
                }

                filter = TextField(filter, new GUIContent("Search", "Use this field to filter values in inspector."));

                if (filter.IsNotNullOrEmpty())
                {
                    GUI.color = cachedColor;
                }

                if (Button("X", 20f, 20f, GUILayout.MaxWidth(20f), GUILayout.MaxHeight(20f)))
                {
                    filter = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Change GUI color and cache previous one, to allow reverting it. Remember to always revert, each time you change color using this method.
        /// </summary>
        /// <param name="cachedColors"></param>
        /// <param name="newColor"></param>
        public static void ChangeGUIColor(ref List<Color> cachedColors, Color newColor)
        {
            if (cachedColors.Count > 200)
            {
                Log.Warning(BaseLogChannel.Core, "CachedColors collection overflow. You may missing call RevertGUIColor()");
                return;
            }
            cachedColors.Add(GUI.color);
            GUI.color = newColor;
        }

        public static void RevertGUIColor(ref List<Color> cachedColors)
        {
            if (cachedColors.Count <= 0)
            {
                return;
            }

            GUI.color = cachedColors.Last();
            cachedColors.RemoveAt(cachedColors.Count - 1);
        }

        public static int MaskField(string label, int mask, string[] displayOptions)
        {
            Rect rect = GUILayoutUtility.GetRect(_valueWidth, _valueHeight, "TextField");
            return EditorGUI.MaskField(rect, label, mask, displayOptions);
        }

        public static DateTime DateTimeField(DateTime dateTime, string label = "")
        {
            if (label.IsNotNullOrEmpty())
            {
                Label(label);
            }
            EditorGUILayout.BeginHorizontal();
            float lastLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 10f;
            int day = IntFieldVerticalLabel(dateTime.Day, "Day");
            int month = IntFieldVerticalLabel(dateTime.Month, "Month");
            int year = IntFieldVerticalLabel(dateTime.Year, "Year");
            AddSpace();
            int minute = IntFieldVerticalLabel(dateTime.Minute, "Min");
            int hour = IntFieldVerticalLabel(dateTime.Hour, "Hour");
            EditorGUIUtility.labelWidth = lastLabelWidth;
            EditorGUILayout.EndHorizontal();
            dateTime = new DateTime(year, month, day, hour, minute, 0);
            return dateTime;
        }

        public static void CreateAsset(string name, ScriptableObject objectToAsset, string pathExt = "")
        {
            if (pathExt == "")
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (path == "")
                {
                    path = "Assets";
                }
                else if (Path.GetExtension(path) != "")
                {
                    path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
                }
                pathExt = path;
            }
            string assetPathAndName = "";
            if (File.Exists(pathExt + "/" + name + ".asset"))
            {
                if (EditorUtility.DisplayDialog("Warning!",
                    "Are you sure you want to replace asset " + name + "?", "Yes", "No"))
                {
                    AssetDatabase.RenameAsset(pathExt + "/" + name + ".asset",
                        name + "_" + DateTime.Now.ToISOString() + "_old.asset");
                    assetPathAndName = pathExt + "/" + name + ".asset";
                }
                else
                {
                    assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(pathExt + "/" + name + "_new.asset");
                }
            }
            else
            {
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(pathExt + "/" + name + ".asset");
            }
            Log.Message(BaseLogChannel.Core, assetPathAndName);
            AssetDatabase.CreateAsset(objectToAsset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = objectToAsset;
        }

        public static void CopyToClipboard(string textToCopy, bool showNotification = false)
        {
            GUIUtility.systemCopyBuffer = textToCopy;

            if (showNotification)
            {
                ShowNotification(string.Format("{0}\n{1}", textToCopy, "Copied to clipboard"));
            }
        }

        /// <summary>
        /// Gets unique ID for current <see cref="UnityEditor.Editor.target"/>. This ID contains name of the method or property, which called this method.
        /// </summary>
        /// <param name="methodName">Auto-insert parameter. Do NOT use it.</param>
        /// <returns>Unique ID.</returns>
        public string GetUniqueID([CallerMemberName] string methodName = "")
        {
            return target != null ? target.GetHashCode() + "_" + methodName : "null_" + methodName;
        }

        public bool DrawDefaultInspectorWithoutScriptField(string[] ignoreFields = null)
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            SerializedProperty it = serializedObject.GetIterator();
            it.NextVisible(true);
            while (it.NextVisible(false))
            {
                if (ignoreFields == null || !ignoreFields.Contains(it.name))
                {
                    EditorGUILayout.PropertyField(it, true);
                }
            }
            serializedObject.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        public void ListField(List<string> list, string scrollId, string showId, string label = "",
                              bool showAddRemove = true, float expectedHeight = 100f)
        {
            bool isShown = GetIsShown(showId);
            Vector2 scrollPos = GetScrollPos(scrollId);
            ListField(list, ref scrollPos, ref isShown, label, showAddRemove, expectedHeight);
            SetIsShown(showId, isShown);
            SetScrollPos(scrollId, scrollPos);
        }

        public void DropArea<T>(string name, Action<T[]> dropAction, Vector2 size = default(Vector2))
            where T : Object
        {
            if (size == default(Vector2))
            {
                size = new Vector2(0f, 50f);
            }
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(size.x, size.y, GUILayout.ExpandWidth(true));

            GUI.Box(dropArea, name, new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            });

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                    {
                        return;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        dropAction(ResolveProjectHierarchy<T>(DragAndDrop.objectReferences).ToArray());
                    }
                    break;
            }
        }

        #endregion

        #region Private Methods

        private List<T> ResolveProjectHierarchy<T>(Object[] objects)
            where T : Object
        {
            var returnList = new List<T>();
            foreach (var o in objects)
            {
                if (o is DefaultAsset)
                {
                    var assetPath = AssetDatabase.GetAssetPath(o);
                    var assets = new List<Object>();
                    foreach (
                        var file in Directory.GetFiles(Application.dataPath + "/" + assetPath.Replace("Assets/", "") + "/"))
                    {
                        assets.Add(AssetDatabase.LoadAssetAtPath(assetPath + "/" + Path.GetFileName(file), typeof(Object)));
                    }
                    returnList.AddRange(ResolveProjectHierarchy<T>(assets.ToArray()));
                }
                else if (o is T)
                {
                    returnList.Add((T) o);
                }
            }
            return returnList;
        }

        #endregion

        public class SearchablePopupData
        {
            #region Fields

            public int id;
            public string label;
            public string[] strings;
            public bool searchShow;
            public Vector2 searchScrollPos;
            public string searchString;

            #endregion
        }
    }
}