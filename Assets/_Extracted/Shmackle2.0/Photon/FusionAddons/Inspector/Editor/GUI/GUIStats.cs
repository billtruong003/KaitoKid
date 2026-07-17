namespace Fusion.Addons.Inspector.Editor
{
	using UnityEngine;

	internal class GUIStat
	{
		public readonly GUIStyle   Style;
		public readonly GUIContent Content;

		public float Width;

		public GUIStat(GUIStyle style, string text, string tooltip, float width = 0.0f)
		{
			Style   = style;
			Content = new GUIContent(text, tooltip);
			Width   = width;

			if (width <= 0.0f && style != null)
			{
				Width = style.CalcSize(Content).x;
			}
		}
	}

	internal sealed class GUIIconStat : GUIStat
	{
		public readonly GUIContent IconContent;

		public GUIIconStat(GUIStyle style, string text, string iconText, string tooltip, float width = 0.0f) : base(style, text, tooltip, width)
		{
			IconContent = new GUIContent(iconText, tooltip);
		}
	}

	internal sealed class GUISortStat : GUIStat
	{
		private string _text;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUISortStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public GUIContent GetContent(int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_sortMode == sortMode)
				return Content;

			_sortMode = sortMode;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_UP} {_text}";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				Content.text = $"{GUISymbols.ARROW_DOWN} {_text}";
			}
			else
			{
				Content.text = _text;
			}

			return Content;
		}
	}

	internal sealed class GUINameStat : GUIStat
	{
		private string _text;
		private int    _total;
		private int    _filtered;
		private int    _sortMode;
		private int    _ascendingSortMode;
		private int    _descendingSortMode;

		public GUINameStat(GUIStyle style, string text, string tooltip, int ascendingSortMode, int descendingSortMode, float width = 0.0f) : base(style, text, tooltip, width)
		{
			_text               = text;
			_ascendingSortMode  = ascendingSortMode;
			_descendingSortMode = descendingSortMode;
		}

		public GUIContent GetContent(int total, int filtered, int sortMode)
		{
			if (sortMode != _ascendingSortMode && sortMode != _descendingSortMode)
			{
				sortMode = default;
			}

			if (_total == total && _filtered == filtered && _sortMode == sortMode)
				return Content;

			_total    = total;
			_filtered = filtered;
			_sortMode = sortMode;

			if (sortMode == _ascendingSortMode && sortMode != default)
			{
				Content.text = total == filtered ? $"{GUISymbols.ARROW_UP} {_text} ({total})" : $"{GUISymbols.ARROW_UP} {_text} ({filtered} / {total})";
			}
			else if (sortMode == _descendingSortMode && sortMode != default)
			{
				Content.text = total == filtered ? $"{GUISymbols.ARROW_DOWN} {_text} ({total})" : $"{GUISymbols.ARROW_DOWN} {_text} ({filtered} / {total})";
			}
			else
			{
				Content.text = total == filtered ? $"{_text} ({total})" : $"{_text} ({filtered} / {total})";
			}

			return Content;
		}
	}
}
