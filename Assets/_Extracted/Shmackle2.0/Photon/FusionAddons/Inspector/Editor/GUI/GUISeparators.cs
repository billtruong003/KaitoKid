namespace Fusion.Addons.Inspector.Editor
{
	using System.Collections.Generic;
	using UnityEngine;

	public static class GUISeparators
	{
		public static readonly List<GUISeparator> X = new List<GUISeparator>();
		public static readonly List<GUISeparator> Y = new List<GUISeparator>();

		public static void AddX(float position, float relativeSize = 1.0f)
		{
			X.Add(new GUISeparator(position, 2.0f));
		}

		public static void AddY(float position, float relativeSize = 1.0f)
		{
			Y.Add(new GUISeparator(position, 2.0f));
		}

		public static void DrawAll(float windowWidth, float headerPosition, float headerHeight, float scrollPosition, float scrollHeight)
		{
			foreach (GUISeparator separator in X)
			{
				TextureUtility.DrawTexture(new Rect(separator.Position - FusionInspector.SeparatorSize * separator.RelativeSize * 0.5f, headerPosition, FusionInspector.SeparatorSize * separator.RelativeSize, headerHeight), FusionInspector.SeparatorColor);
				TextureUtility.DrawTexture(new Rect(separator.Position - FusionInspector.SeparatorSize * separator.RelativeSize * 0.5f, scrollPosition, FusionInspector.SeparatorSize * separator.RelativeSize, scrollHeight), FusionInspector.SeparatorColor);
			}

			foreach (GUISeparator separator in Y)
			{
				TextureUtility.DrawTexture(new Rect(0.0f, separator.Position - FusionInspector.SeparatorSize * separator.RelativeSize * 0.5f, windowWidth, FusionInspector.SeparatorSize * separator.RelativeSize), FusionInspector.SeparatorColor);
			}

			X.Clear();
			Y.Clear();
		}

		public static void Clear()
		{
			X.Clear();
			Y.Clear();
		}
	}

	public readonly struct GUISeparator
	{
		public readonly float Position;
		public readonly float RelativeSize;

		public GUISeparator(float position, float relativeSize)
		{
			Position     = position;
			RelativeSize = relativeSize;
		}
	}
}
