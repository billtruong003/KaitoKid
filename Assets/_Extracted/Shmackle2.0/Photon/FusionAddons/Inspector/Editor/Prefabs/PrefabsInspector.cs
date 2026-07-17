namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;

	internal sealed class PrefabsInspector
	{
		private Vector2             _scrollPosition;
		private UnityEngine.Object  _selectionObject;
		private float               _selectionTime;
		private List<ComponentInfo> _tempComponents = new List<ComponentInfo>();

		private static GUINameStat _objectStat;
		private static GUIIconStat _isSpawnableStat;
		private static GUIIconStat _isMasterClientObjectStat;
		private static GUIIconStat _allowStateAuthorityOverrideStat;
		private static GUIIconStat _destroyWhenStateAuthorityLeavesStat;
		private static GUISortStat _interestModeStat;
		private static GUISortStat _stateSizeStat;

		private static GUIContent   _objectContent;
		private static GUIContent[] _interestModeContents;
		private static GUIContent   _stateSizeContent;
		private static GUIContent   _filterContent;
		private static GUIContent   _fieldsContent;

		private static float _fieldsWidth1;
		private static float _fieldsWidth2;
		private static float _fieldsWidth3;

		public PrefabsInspector()
		{
			Texture2D prefabIcon = EditorGUIUtility.IconContent("d_Prefab Icon").image as Texture2D;
			Texture2D filterIcon = EditorGUIUtility.IconContent("d_FilterByType@2x").image as Texture2D;
			Texture2D fieldsIcon = EditorGUIUtility.IconContent("d__Menu@2x").image as Texture2D;

			float headerHeight   = FusionInspector.HeaderHeight;
			float stateSizeWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent("8888 B")).x;

			_objectStat = new GUINameStat(GUIStyles.LeftBoldLabel, "Object", "Name of the object", (int)EPrefabSortMode.NameAscending, (int)EPrefabSortMode.NameDescending);

			_isSpawnableStat                     = new GUIIconStat(GUIStyles.Icon, "✿", "✔", $"Is Spawnable", headerHeight);
			_isMasterClientObjectStat            = new GUIIconStat(GUIStyles.Icon, "❖", "✔", $"Is Master Client Object (Shared Mode)", headerHeight);
			_allowStateAuthorityOverrideStat     = new GUIIconStat(GUIStyles.Icon, "✪", "✔", $"Allow State Authority Override (Shared Mode)", headerHeight);
			_destroyWhenStateAuthorityLeavesStat = new GUIIconStat(GUIStyles.Icon, "☢", "✔", $"Destroy When State Authority Leaves (Shared Mode)", headerHeight);
			_interestModeStat                    = new GUISortStat(GUIStyles.RightBoldLabel, "Interest", "Object Interest Mode", (int)EPrefabSortMode.InterestModeAscending, (int)EPrefabSortMode.InterestModeDescending);
			_stateSizeStat                       = new GUISortStat(GUIStyles.RightBoldLabel, "State", "Size of the networked state of the object in Bytes", (int)EPrefabSortMode.StateSizeAscending, (int)EPrefabSortMode.StateSizeDescending, stateSizeWidth);

			_objectContent        = new GUIContent(prefabIcon);
			_interestModeContents = new GUIContent[] { new GUIContent("Area"), new GUIContent("Global"), new GUIContent("Explicit") };
			_stateSizeContent     = new GUIContent("", "Size of the networked state of the object in Bytes");
			_filterContent        = new GUIContent(filterIcon, "Filter by component type");
			_fieldsContent        = new GUIContent(fieldsIcon, "Visible fields");

			float sortWidth = GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"{GUISymbols.ARROW_UP} ")).x - GUIStyles.RightBoldLabel.CalcSize(new GUIContent($"")).x;
			_interestModeStat.Width += sortWidth;
			_stateSizeStat.Width    += sortWidth;

			_fieldsWidth1 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Destroy When State Authority Leaves")).x;
			_fieldsWidth2 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Min. Field Fill")).x;
			_fieldsWidth3 = FusionInspector.FieldPadding + EditorStyles.toggle.CalcSize(new GUIContent("Min. Field Fill")).x;
		}

		public void DrawGUI(Rect window, Rect region, FusionInspectorDB db)
		{
			EPrefabField visibleFields    = db.Prefabs.VisibleFields | EPrefabField.Object;
			EPrefabField toggleableFields = ~EPrefabField.Object;

			db.Prefabs.VisibleFields = visibleFields;

			DrawPrefabsGUI(window, region, db, visibleFields, toggleableFields);
		}

		private void DrawPrefabsGUI(Rect window, Rect region, FusionInspectorDB db, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			Prefabs prefabs = db.Prefabs;
			if (prefabs.All.Count <= 0)
				return;

			bool showIsSpawnable                     = visibleFields.Has(EPrefabField.IsSpawnable);
			bool showIsMasterClientObject            = visibleFields.Has(EPrefabField.IsMasterClientObject);
			bool showAllowStateAuthorityOverride     = visibleFields.Has(EPrefabField.AllowStateAuthorityOverride);
			bool showDestroyWhenStateAuthorityLeaves = visibleFields.Has(EPrefabField.DestroyWhenStateAuthorityLeaves);
			bool showInterestMode                    = visibleFields.Has(EPrefabField.InterestMode);
			bool showStateSize                       = visibleFields.Has(EPrefabField.StateSize);

			GUISeparators.Clear();

			region.xMin += FusionInspector.LeftWindowOffset;
			region.xMax -= FusionInspector.RightWindowOffset;

			_objectStat.Width = region.width;

			if (showIsSpawnable                     == true) { _objectStat.Width -= _isSpawnableStat.Width;                     } else { prefabs.IsSpawnableFilter                     = default; }
			if (showIsMasterClientObject            == true) { _objectStat.Width -= _isMasterClientObjectStat.Width;            } else { prefabs.IsMasterClientObjectFilter            = default; }
			if (showAllowStateAuthorityOverride     == true) { _objectStat.Width -= _allowStateAuthorityOverrideStat.Width;     } else { prefabs.AllowStateAuthorityOverrideFilter     = default; }
			if (showDestroyWhenStateAuthorityLeaves == true) { _objectStat.Width -= _destroyWhenStateAuthorityLeavesStat.Width; } else { prefabs.DestroyWhenStateAuthorityLeavesFilter = default; }
			if (showInterestMode                    == true) { _objectStat.Width -= _interestModeStat.Width;                    }
			if (showStateSize                       == true) { _objectStat.Width -= _stateSizeStat.Width;                       }

			Rect headerRect = region;
			headerRect.width  = _objectStat.Width;
			headerRect.height = FusionInspector.HeaderHeight;

			GUISeparators.AddY(headerRect.yMin, 2.0f);
			GUISeparators.AddY(headerRect.yMax, 2.0f);

			TextureUtility.DrawTexture(new Rect(0.0f, headerRect.yMin, window.width, headerRect.height), FusionInspector.HeaderColor);

			FusionInspector.ApplyLinePadding(ref headerRect);

			Rect componentFilterRect = headerRect;
			componentFilterRect.xMin = componentFilterRect.xMax - FusionInspector.HeaderHeight;
			GUIColor.Set(FusionInspector.FilterColors[Mathf.Min(prefabs.ComponentFilterInclude.Count + prefabs.ComponentFilterExclude.Count, 1)]);
			if (GUI.Button(componentFilterRect, _filterContent, GUIStyles.Icon) == true)
			{
				prefabs.ComponentFilterFoldout = !prefabs.ComponentFilterFoldout;
				prefabs.VisibleFieldsFoldout = false;
			}
			GUIColor.Reset();

			Rect nameFilterRect = headerRect;
			nameFilterRect.xMax = componentFilterRect.xMin;
			nameFilterRect.xMin = nameFilterRect.xMax - headerRect.width * 0.4f;
			nameFilterRect.y += 0.5f;
			if (nameFilterRect.height > EditorGUIUtility.singleLineHeight)
			{
				nameFilterRect.y += (nameFilterRect.height - EditorGUIUtility.singleLineHeight) * 0.5f;
				nameFilterRect.height = EditorGUIUtility.singleLineHeight;
			}
			prefabs.ObjectNameFilter = EditorGUI.TextField(nameFilterRect, prefabs.ObjectNameFilter, GUIStyles.SearchField);

			Rect nameRect = headerRect;
			nameRect.xMax = nameFilterRect.xMin;
			if (GUI.Button(nameRect, _objectStat.GetContent(prefabs.All.Count, prefabs.Filtered.Count, (int)prefabs.SortMode), _objectStat.Style) == true)
			{
				ToggleSortMode(ref prefabs.SortMode, EPrefabSortMode.NameAscending, EPrefabSortMode.NameDescending);
			}

			if (showIsSpawnable                     == true) { DrawHeader(_isSpawnableStat,                     ref headerRect, ref prefabs.IsSpawnableFilter);                     }
			if (showIsMasterClientObject            == true) { DrawHeader(_isMasterClientObjectStat,            ref headerRect, ref prefabs.IsMasterClientObjectFilter);            }
			if (showAllowStateAuthorityOverride     == true) { DrawHeader(_allowStateAuthorityOverrideStat,     ref headerRect, ref prefabs.AllowStateAuthorityOverrideFilter);     }
			if (showDestroyWhenStateAuthorityLeaves == true) { DrawHeader(_destroyWhenStateAuthorityLeavesStat, ref headerRect, ref prefabs.DestroyWhenStateAuthorityLeavesFilter); }
			if (showInterestMode                    == true) { DrawHeader(_interestModeStat, null,              ref headerRect, ref prefabs.SortMode, EPrefabSortMode.InterestModeAscending, EPrefabSortMode.InterestModeDescending); }
			if (showStateSize                       == true) { DrawHeader(_stateSizeStat,    null,              ref headerRect, ref prefabs.SortMode, EPrefabSortMode.StateSizeDescending,   EPrefabSortMode.StateSizeAscending);     }

			headerRect.xMin += headerRect.width;
			headerRect.width = FusionInspector.RightWindowOffset;
			if (GUI.Button(headerRect, _fieldsContent, GUIStyles.Icon) == true)
			{
				prefabs.VisibleFieldsFoldout = !prefabs.VisibleFieldsFoldout;
				prefabs.ComponentFilterFoldout = false;
			}

			FusionInspector.RestoreLinePadding(ref headerRect);

			region.yMin += headerRect.height;

			DrawFieldsGUI(ref region, prefabs, visibleFields, toggleableFields);
			DrawComponentFilterGUI(ref region, prefabs, db.Components);

			GUISeparators.AddY(region.y, 2.0f);

			Rect hoverRect      = default;
			Rect scrollRect     = new Rect(region.x, region.y, region.width + FusionInspector.RightWindowOffset, region.height);
			Rect scrollViewRect = new Rect(0.0f, 0.0f, region.width, FusionInspector.LineHeight * prefabs.Filtered.Count);

			GUISeparators.AddX(headerRect.xMin);

			_scrollPosition = GUI.BeginScrollView(scrollRect, _scrollPosition, scrollViewRect);

			float   minScrollY    = _scrollPosition.y;
			float   maxScrollY    = _scrollPosition.y + region.height;
			Rect    drawRect      = new Rect(0.0f, 0.0f, 0.0f, FusionInspector.LineHeight);
			Vector2 mousePosition = Event.current.mousePosition;

			for (int i = 0, count = prefabs.Filtered.Count; i < count; ++i)
			{
				if (drawRect.yMax < minScrollY)
				{
					drawRect.y += drawRect.height;
					continue;
				}
				else if (drawRect.y > maxScrollY)
				{
					break;
				}

				PrefabInfo prefab = prefabs.Filtered[i];

				drawRect.xMin  = 0.0f;
				drawRect.width = _objectStat.Width;

				if (mousePosition.x >= region.xMin && mousePosition.x <= region.xMax && mousePosition.y >= minScrollY && mousePosition.y <= maxScrollY && mousePosition.y > drawRect.yMin && mousePosition.y < drawRect.yMax)
				{
					hoverRect = drawRect;
					hoverRect.xMin = 0.0f;
					hoverRect.xMax = window.width;
					hoverRect.yMin = Mathf.Max(minScrollY, hoverRect.yMin);
					hoverRect.yMax = Mathf.Min(hoverRect.yMax, maxScrollY);
					TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);
				}

				FusionInspector.ApplyLinePadding(ref drawRect);

				GUIColor.Set(prefab.IsActive == true ? FusionInspector.ActiveTextColor : FusionInspector.InactiveTextColor);
				_objectContent.text = prefab.Name;
				if (GUI.Button(drawRect, _objectContent, GUIStyles.ObjectField) == true)
				{
					if (_selectionObject == prefab.GameObject && Time.unscaledTime < (_selectionTime + FusionInspector.SelectionDelay))
					{
						Selection.SetActiveObjectWithContext(prefab.GameObject, _selectionObject);
						_selectionObject = default;
						_selectionTime   = default;
					}
					else
					{
						EditorGUIUtility.PingObject(prefab.GameObject);
						_selectionObject = prefab.GameObject;
						_selectionTime   = Time.unscaledTime;
					}
				}
				GUIColor.Reset();

				if (showIsSpawnable                     == true) { FusionInspector.DrawIcon(_isSpawnableStat,                     ref drawRect, prefab.IsSpawnable);                     }
				if (showIsMasterClientObject            == true) { FusionInspector.DrawIcon(_isMasterClientObjectStat,            ref drawRect, prefab.IsMasterClientObject);            }
				if (showAllowStateAuthorityOverride     == true) { FusionInspector.DrawIcon(_allowStateAuthorityOverrideStat,     ref drawRect, prefab.AllowStateAuthorityOverride);     }
				if (showDestroyWhenStateAuthorityLeaves == true) { FusionInspector.DrawIcon(_destroyWhenStateAuthorityLeavesStat, ref drawRect, prefab.DestroyWhenStateAuthorityLeaves); }

				if (showInterestMode == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _interestModeStat.Width;

					EditorGUI.LabelField(drawRect, _interestModeContents[(int)prefab.InterestMode], GUIStyles.RightLabel);
				}

				if (showStateSize == true)
				{
					drawRect.xMin += drawRect.width;
					drawRect.width = _stateSizeStat.Width;

					EditorGUI.LabelField(drawRect, prefab.StateSize.GetLabel(), GUIStyles.RightLabel);
				}

				FusionInspector.RestoreLinePadding(ref drawRect);

				drawRect.y += drawRect.height;
			}

			GUI.EndScrollView();

			if (hoverRect != default)
			{
				hoverRect.xMin  = 0.0f;
				hoverRect.width = scrollRect.x;
				hoverRect.y += scrollRect.y;
				hoverRect.y -= _scrollPosition.y;
				TextureUtility.DrawTexture(hoverRect, FusionInspector.HoverColor);
			}

			float scrollHeight = Mathf.Min(scrollRect.height, scrollViewRect.height);

			GUISeparators.AddY(scrollRect.yMin + scrollHeight);

			GUISeparators.DrawAll(window.width, headerRect.yMin, headerRect.height, scrollRect.yMin, scrollHeight);
		}

		private void DrawFieldsGUI(ref Rect sectionRect, Prefabs prefabs, EPrefabField visibleFields, EPrefabField toggleableFields)
		{
			if (prefabs.VisibleFieldsFoldout == false)
				return;

			float xOffset = 3.0f;
			float yOffset = 6.0f;

			Rect innerRect = sectionRect;
			innerRect.xMin += xOffset;
			innerRect.xMax -= xOffset;
			innerRect.height = EditorGUIUtility.singleLineHeight;

			float totalWidth      = _fieldsWidth1 + _fieldsWidth2 + _fieldsWidth3;
			float widthMultiplier = innerRect.width / totalWidth;

			//----------------------------------------------------------------

			Rect configRect = innerRect;
			configRect.xMin = innerRect.xMin;
			configRect.xMax = configRect.xMin + _fieldsWidth1 * widthMultiplier;

			configRect.y += yOffset;

			EditorGUI.LabelField(configRect, "Configuration", EditorStyles.boldLabel);
			configRect.y += configRect.height;
			configRect.y += yOffset;

			DrawToggle(ref configRect, "Is Spawnable",                        EPrefabField.IsSpawnable);
			DrawToggle(ref configRect, "Is Master Client Object",             EPrefabField.IsMasterClientObject);
			DrawToggle(ref configRect, "Allow State Authority Override",      EPrefabField.AllowStateAuthorityOverride);
			DrawToggle(ref configRect, "Destroy When State Authority Leaves", EPrefabField.DestroyWhenStateAuthorityLeaves);
			DrawToggle(ref configRect, "Object Interest Mode",                EPrefabField.InterestMode);

			configRect.y += yOffset;

			//----------------------------------------------------------------

			Rect otherRect = innerRect;
			otherRect.xMin = configRect.xMax + xOffset;
			otherRect.xMax = otherRect.xMin + _fieldsWidth2 * widthMultiplier;

			otherRect.y += yOffset;

			EditorGUI.LabelField(otherRect, "Other", EditorStyles.boldLabel);
			otherRect.y += otherRect.height;
			otherRect.y += yOffset;

			DrawToggle(ref otherRect, "State Size", EPrefabField.StateSize);

			otherRect.y += yOffset;

			//----------------------------------------------------------------

			sectionRect.yMin = Mathf.Max(configRect.yMin, otherRect.yMin);

			return;

			void DrawToggle(ref Rect rect, string label, EPrefabField field)
			{
				EditorGUI.BeginDisabledGroup(toggleableFields.Has(field) == false);

				bool previousState = prefabs.VisibleFields.Has(field);
				bool currentState  = EditorGUI.ToggleLeft(rect, label, previousState);

				EditorGUI.EndDisabledGroup();

				if (currentState != previousState)
				{
					prefabs.ToggleVisibleField(field);
				}

				rect.y += rect.height;
			}
		}

		private void DrawComponentFilterGUI(ref Rect sectionRect, Prefabs prefabs, Components components)
		{
			prefabs.ComponentFilterInclude.Clear();
			prefabs.ComponentFilterExclude.Clear();

			if (prefabs.ComponentFilterFoldout == false)
			{
				for (int i = 0, count = components.All.Count; i < count; ++i)
				{
					ComponentInfo component = components.All[i];

					if (component.Prefabs.FilterMode == EFilterMode.Include)
					{
						prefabs.ComponentFilterInclude.Add(component.Type);
					}
					else if (component.Prefabs.FilterMode == EFilterMode.Exclude)
					{
						prefabs.ComponentFilterExclude.Add(component.Type);
					}
				}

				return;
			}

			_tempComponents.Clear();
			_tempComponents.AddRange(components.All);

			if (components.SortMode != EComponentSortMode.TypeNameAscending)
			{
				Components.Sort(_tempComponents, EComponentSortMode.TypeNameAscending);
			}

			float rowWidth = Mathf.Clamp(sectionRect.width, 160.0f, 224.0f);
			int   columns  = Mathf.Max(1, Mathf.FloorToInt(sectionRect.width / rowWidth));
			int   rows     = _tempComponents.Count / columns;

			if (_tempComponents.Count > (rows * columns))
			{
				++rows;
			}

			float xOffset    = 3.0f;
			float yOffset    = 6.0f;
			float lineHeight = EditorGUIUtility.singleLineHeight;

			Rect filterRect = sectionRect;
			filterRect.height = rows * lineHeight + yOffset * 2;
			sectionRect.yMin += filterRect.height;

			filterRect.xMin += xOffset;
			filterRect.xMax -= xOffset;
			filterRect.yMin += yOffset;
			filterRect.yMax -= yOffset;

			for (int i = 0, count = _tempComponents.Count; i < count; ++i)
			{
				int column = i / rows;
				int row    = i - rows * column;

				ComponentInfo component = _tempComponents[i];

				Rect componentRect = filterRect;
				componentRect.width /= columns;
				componentRect.height = lineHeight;
				componentRect.x += componentRect.width * column;
				componentRect.y += componentRect.height * row;

				ComponentInfo.ObjectContext context = component.Prefabs;

				if (EditorGUI.ToggleLeft(componentRect, context.ObjectFilter.GetLabel(component.TypeName, context.AllObjectCount.Value, context.FilteredObjectCount.Value), false, context.FilteredObjectCount.Value > 0 ? GUIStyles.ActiveToggle : GUIStyles.InactiveToggle) == true)
				{
					context.FilterMode = (EFilterMode)(((int)context.FilterMode + 1) % FusionInspector.FILTER_MODES);
				}

				if (context.FilterMode == EFilterMode.Include)
				{
					prefabs.ComponentFilterInclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CHECKMARK);
				}
				else if (context.FilterMode == EFilterMode.Exclude)
				{
					prefabs.ComponentFilterExclude.Add(component.Type);

					EditorGUI.LabelField(componentRect, GUISymbols.CROSS);
				}
			}

			_tempComponents.Clear();
		}

		private static void DrawHeader(GUIStat stat, ref Rect rect, ref EFilterMode filter)
		{
			FusionInspector.DrawHeader(stat, ref rect, ref filter);
		}

		private static void DrawHeader(GUISortStat stat, GUIContent icon, ref Rect rect, ref EPrefabSortMode sortMode, EPrefabSortMode primarySortMode, EPrefabSortMode secondarySortMode)
		{
			int intSortMode = (int)sortMode;
			FusionInspector.DrawHeader(stat, icon, ref rect, ref intSortMode, (int)primarySortMode, (int)secondarySortMode);
			sortMode = (EPrefabSortMode)intSortMode;
		}

		private static void ToggleSortMode(ref EPrefabSortMode sortMode, EPrefabSortMode primaryMode, EPrefabSortMode secondaryMode)
		{
			sortMode = sortMode == primaryMode ? secondaryMode : primaryMode;
		}
	}
}
