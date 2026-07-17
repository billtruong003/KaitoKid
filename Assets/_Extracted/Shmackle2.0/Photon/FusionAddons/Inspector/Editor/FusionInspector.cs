namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;
	using UnityEditor;

	public sealed partial class FusionInspector : EditorWindow
	{
		public const int FILTER_MODES       = 3;
		public const int STATE_SIZE_UNKNOWN = -999;

		public static readonly float   LeftWindowOffset  = 8.0f;
		public static readonly float   RightWindowOffset = 16.0f;
		public static readonly float   LineHeight        = EditorGUIUtility.singleLineHeight + 1.0f;
		public static readonly float   LinePadding       = 1.0f;
		public static readonly float   FieldPadding      = 40.0f;
		public static readonly float   HeaderHeight      = LineHeight + 3.0f;
		public static readonly float   FooterHeight      = LineHeight + 3.0f;
		public static readonly Color   HeaderColor       = new Color(0.2f, 0.2f, 0.2f, 1.0f);
		public static readonly Color   HoverColor        = new Color(0.25f, 0.25f, 0.25f, 1.0f);
		public static readonly Color   BackgroundColor   = new Color(0.22f, 0.22f, 0.22f, 1.0f);
		public static readonly Color   NormalTextColor   = new Color(0.92f, 0.92f, 0.92f, 1.0f);
		public static readonly Color   ActiveTextColor   = Color.white;
		public static readonly Color   InactiveTextColor = Color.grey;
		public static readonly float   SeparatorSize     = 0.75f;
		public static readonly float   SeparatorOffset   = SeparatorSize * 0.5f;
		public static readonly Color   SeparatorColor    = new Color(0.175f, 0.175f, 0.175f, 1.0f);
		public static readonly Color[] FilterColors      = new Color[] { Color.white, Color.green, Color.red };
		public static readonly float   SelectionDelay    = 0.35f;

		private FusionInspectorDB     _db;
		private GUIContent[]          _sections;
		private ComponentsInspector   _componentsInspector;
		private SceneObjectsInspector _sceneObjectsInspector;
		private PrefabsInspector      _prefabsInspector;

		private static GUIStat[] _gameModes;

		[MenuItem("Tools/Fusion/Inspector")]
		public static void Create()
		{
#if FUSION_INSPECTOR_DOCKED
			CreateWindow<FusionInspector>("Fusion Inspector");
#else
			FusionInspector inspector = EditorWindow.GetWindow<FusionInspector>(true, "Fusion Inspector");
			inspector.ShowUtility();
#endif
		}

		public void OnEnable()
		{
			minSize = new Vector2(256.0f, 128.0f);
		}

		internal static void ApplyLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin += padding;
			rect.yMax -= padding;
		}

		internal static void RestoreLinePadding(ref Rect rect, float padding = 0.0f)
		{
			if (padding <= 0.0f)
			{
				padding = FusionInspector.LinePadding;
			}

			rect.yMin -= padding;
			rect.yMax += padding;
		}

		internal static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			GUIColor.Set(FilterColors[(int)filter]);
			if (GUI.Button(rect, stat.Content, stat.Style) == true)
			{
				filter = (EFilterMode)(((int)filter + 1) % FILTER_MODES);
			}
			GUIColor.Reset();
		}

		internal static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref int sortMode, int primarySortMode, int secondarySortMode)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			GUISeparators.AddX(rect.xMin);

			if (GUI.Button(rect, stat.GetContent(sortMode), stat.Style) == true)
			{
				sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
			}

			if (icon != null)
			{
				ApplyLinePadding(ref rect, 2.0f);

				if (GUI.Button(rect, icon, stat.Style) == true)
				{
					sortMode = sortMode == primarySortMode ? secondarySortMode : primarySortMode;
				}

				RestoreLinePadding(ref rect, 2.0f);
			}
		}

		internal static void DrawIcon(GUIIconStat stat, ref Rect rect, bool isOn)
		{
			rect.xMin += rect.width;
			rect.width = stat.Width;

			if (isOn == true)
			{
				EditorGUI.LabelField(rect, stat.IconContent, GUIStyles.Icon);
			}
		}

		private void OnGUI()
		{
			if (Event.current.type == EventType.Layout)
				return;

			Initialize();

			Rect drawRect = new Rect(default, position.size);

			TextureUtility.DrawTexture(drawRect, BackgroundColor);

			float buttonHeight = EditorStyles.toolbarButton.fixedHeight;
			float buttonWidth  = drawRect.width / _sections.Length;

			for (int i = 0, count = _sections.Length; i < count; ++i)
			{
				if (GUI.Toggle(new Rect(drawRect.x + buttonWidth * i, drawRect.y, buttonWidth, buttonHeight), _db.CurrentSection == i, _sections[i], EditorStyles.toolbarButton) == true)
				{
					if (_db.CurrentSection != i)
					{
						_db.SetCurrentSection(i);
					}
				}
			}

			_db.Update();

			drawRect.yMin += buttonHeight;

			bool drawFooter = Application.isPlaying == true && _db.CurrentRunner != null && _db.CurrentRunner.IsRunning == true;
			if (drawFooter == true)
			{
				drawRect.yMax -= FooterHeight;
			}

			if (_db.CurrentSection == 0)
			{
				if (_sceneObjectsInspector == null)
				{
					_sceneObjectsInspector = new SceneObjectsInspector();
				}

				_sceneObjectsInspector.DrawGUI(position, drawRect, _db);
			}
			else if (_db.CurrentSection == 1)
			{
				if (_prefabsInspector == null)
				{
					_prefabsInspector = new PrefabsInspector();
				}

				_prefabsInspector.DrawGUI(position, drawRect, _db);
			}
			else if (_db.CurrentSection == 2)
			{
				if (_componentsInspector == null)
				{
					_componentsInspector = new ComponentsInspector();
				}

				_componentsInspector.DrawGUI(position, drawRect, _db);
			}

			if (drawFooter == true)
			{
				drawRect.y += drawRect.height;
				drawRect.height = FooterHeight;

				DrawFooterGUI(drawRect);
			}

			Repaint();
		}

		private void DrawFooterGUI(Rect rect)
		{
			const float spacing = 24.0f;

			ApplyLinePadding(ref rect);

			Rect footerRect = rect;
			footerRect.xMin += 8.0f;
			footerRect.xMax -= 8.0f;

			//----------------------------------------------------------------

			if (_db.CurrentRunner.GameMode != default)
			{
				GUIStat gameModeStat = _gameModes[(int)_db.CurrentRunner.GameMode];
				Rect gameModeRect = footerRect;
				gameModeRect.width = gameModeStat.Width + spacing;
				GUI.Label(gameModeRect, gameModeStat.Content, gameModeStat.Style);
				footerRect.xMin += gameModeRect.width;
			}

			//----------------------------------------------------------------

			Rect sessionTimeRect = footerRect;
			sessionTimeRect.width = 94.0f + spacing;
			GUI.Label(sessionTimeRect, $"Time: {_db.SessionTime:hh\\:mm\\:ss}");
			footerRect.xMin += sessionTimeRect.width;

			//----------------------------------------------------------------

			if (_db.RTTMs > 0)
			{
				Rect rttRect = footerRect;
				rttRect.width = 80.0f + spacing;
				GUI.Label(rttRect, $"RTT: {_db.RTTMs} ms");
				footerRect.xMin += rttRect.width;
			}

			//----------------------------------------------------------------

			if (_db.InBandwidth > 0.0f)
			{
				int trafficInKB = Mathf.CeilToInt(_db.InBandwidth / 1024.0f);
				Rect trafficInRect = footerRect;
				trafficInRect.width = 72.0f + spacing;
				GUI.Label(trafficInRect, trafficInKB < 1000 ? $"In: {trafficInKB} kB" : $"In: {(0.001f * trafficInKB):F2} MB");
				footerRect.xMin += trafficInRect.width;
			}

			//----------------------------------------------------------------

			if (_db.OutBandwidth > 0.0f)
			{
				int trafficOutKB = Mathf.CeilToInt(_db.OutBandwidth / 1024.0f);
				Rect trafficOutRect = footerRect;
				trafficOutRect.width = 80.0f + spacing;
				GUI.Label(trafficOutRect, trafficOutKB < 1000 ? $"Out: {trafficOutKB} kB" : $"Out: {(0.001f * trafficOutKB):F2} MB");
				footerRect.xMin += trafficOutRect.width;
			}

			//----------------------------------------------------------------

			Rect resetRect = new Rect(footerRect.xMax - 64.0f, footerRect.y, 64.0f, footerRect.height);
			if (GUI.Button(resetRect, "Reset") == true)
			{
				_db.ResetSession();
			}

			RestoreLinePadding(ref rect);

			TextureUtility.DrawTexture(new Rect(0.0f, rect.yMin - SeparatorSize, position.width, SeparatorSize * 2.0f), SeparatorColor);
		}

		private void Initialize()
		{
			if (_db == null)
			{
				_db = ScriptableObject.CreateInstance<FusionInspectorDB>();
				_db.hideFlags = HideFlags.HideAndDontSave;

				UnityEngine.Object.DontDestroyOnLoad(_db);
			}

			if (_sections == null)
			{
				_sections = new GUIContent[]
				{
					new GUIContent(" Scene Objects", EditorGUIUtility.IconContent("d_GameObject Icon").image as Texture2D),
					new GUIContent(" Prefabs",       EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D),
					new GUIContent(" Components",    EditorGUIUtility.IconContent("d_cs Script Icon").image as Texture2D),
				};
			}

			if (_gameModes == null)
			{
				GUIStat empty = new GUIStat(GUIStyles.LeftLabel, "", "");

				_gameModes = new GUIStat[] { empty, empty, empty, empty, empty, empty, empty, empty };
				_gameModes[(int)GameMode.Single]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Single)}", "");
				_gameModes[(int)GameMode.Shared]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Shared)}", "");
				_gameModes[(int)GameMode.Server]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Server)}", "");
				_gameModes[(int)GameMode.Host]             = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Host)}", "");
				_gameModes[(int)GameMode.Client]           = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.Client)}", "");
				_gameModes[(int)GameMode.AutoHostOrClient] = new GUIStat(GUIStyles.LeftLabel, $"Mode: {nameof(GameMode.AutoHostOrClient)}", "");
			}
		}
	}
}
