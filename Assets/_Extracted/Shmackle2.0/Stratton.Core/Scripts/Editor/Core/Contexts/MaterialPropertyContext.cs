using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

namespace Stratton.Core.Editor
{
	public class MaterialPropertyContext
	{
		#region Fields

		static Dictionary<string, PropertyCopyData> copiedProperties;

		#endregion

		#region Public Methods

		[MenuItem("CONTEXT/Material/Copy Properties")]
		public static void CopyProperties()
		{
			var mat = Selection.activeObject as Material;
			if (mat == null)
			{
				Renderer rend = Selection.activeObject as Renderer;
				if (rend != null)
				{
					mat = rend.sharedMaterial;
				}
				else
				{
					GameObject go = Selection.gameObjects[0];
					rend = go.GetComponent<Renderer>();
					if (rend != null)
					{
						mat = rend.sharedMaterial;
					}
					else
					{
						Log.Warning(BaseLogChannel.Core, "First select material or renderer");
						return;
					}
				}
			}
			Shader sh = mat.shader;
			copiedProperties = new Dictionary<string, PropertyCopyData>();
			for (int i = 0; i < ShaderUtil.GetPropertyCount(sh); i++)
			{
				PropertyCopyData data = new PropertyCopyData();
				data.name = ShaderUtil.GetPropertyName(sh, i);
				data.type = ShaderUtil.GetPropertyType(sh, i);
				switch (data.type)
				{
					case ShaderUtil.ShaderPropertyType.Color:
						data.val = mat.GetColor(data.name);
						break;
					case ShaderUtil.ShaderPropertyType.Float:
					case ShaderUtil.ShaderPropertyType.Range:
						data.val = mat.GetFloat(data.name);
						break;
					case ShaderUtil.ShaderPropertyType.Vector:
						data.val = mat.GetVector(data.name);
						break;
					case ShaderUtil.ShaderPropertyType.TexEnv:
						data.val = mat.GetTexture(data.name);
						data.scale = mat.GetTextureScale(data.name);
						data.offset = mat.GetTextureOffset(data.name);
						break;
				}

				copiedProperties[data.name] = data;
				Log.Message(BaseLogChannel.Core, "Copied: " + data.name + " " + data.type + " " + data.val);
			}
		}


		[MenuItem("CONTEXT/Material/Paste Properties")]
		public static void PasteProperties()
		{
			if (copiedProperties == null || copiedProperties.Count == 0)
			{
				Log.Warning(BaseLogChannel.Core, "No properties has been copied");
				return;
			}
			var mat = Selection.activeObject as Material;
			if (mat == null && Selection.activeObject is GameObject)
			{
				mat = (Selection.activeObject as GameObject).GetComponent<Renderer>().sharedMaterial;
			}

			foreach (var data in copiedProperties.Values)
			{
				switch (data.type)
				{
					case ShaderUtil.ShaderPropertyType.Color:
						mat.SetColor(data.name, (Color) data.val);
						break;
					case ShaderUtil.ShaderPropertyType.Float:
					case ShaderUtil.ShaderPropertyType.Range:
						mat.SetFloat(data.name, (float) data.val);
						break;
					case ShaderUtil.ShaderPropertyType.Vector:
						mat.SetVector(data.name, (Vector4) data.val);
						break;
					case ShaderUtil.ShaderPropertyType.TexEnv:
						mat.SetTexture(data.name, (Texture) data.val);
						mat.SetTextureScale(data.name, data.scale);
						mat.SetTextureOffset(data.name, data.offset);
						break;
				}

				Log.Message(BaseLogChannel.Core, "Paste: " + data.name + " " + data.type + " " + data.val);
			}
			EditorUtility.SetDirty(mat);
		}

		#endregion

		public class PropertyCopyData
		{
			#region Fields

			public string name;
			public ShaderUtil.ShaderPropertyType type;
			public object val;
			public Vector2 scale;
			public Vector2 offset;

			#endregion
		}
	}
}