using UnityEngine;

namespace Opencoding.Shared.Utils
{
	public static class UIUtilities
	{
		private static GUIContent _guiContent = new GUIContent();
		/// <summary>
		/// Creates a texture of the specified size, filled with the specified colour.
		/// </summary>
		public static Texture2D CreateTexture(int width, int height, Color col)
		{
			Color[] pix = new Color[width * height];

			for (int i = 0; i < pix.Length; i++)

				pix[i] = col;

			Texture2D result = new Texture2D(width, height);

			result.wrapMode = TextureWrapMode.Repeat;

			result.SetPixels(pix);

			result.Apply();

			result.hideFlags = HideFlags.HideAndDontSave;
        
			return result;
		}

		public static Texture2D CreateTexture(Color col)
		{
			return CreateTexture(1, 1, col);
		}

		public static GUIContent TempGUIContent(string text)
		{
			_guiContent.text = text;
			_guiContent.image = null;
			return _guiContent;
		}

		public static GUIContent TempGUIContent(string text, Texture2D image)
		{
			_guiContent.text = text;
			_guiContent.image = image;
			return _guiContent;
		}

		public static GUIContent TempGUIContent(Texture2D image)
		{
			_guiContent.text = "";
			_guiContent.image = image;
			return _guiContent;
		}
	}
}