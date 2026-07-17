using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Stratton.Core.Editor
{
	public class GameObjectContext : MonoBehaviour
	{
		#region PrivateMethods

		[MenuItem("GameObject/Support/Auto rename children duplicates", false, 0)]
		static void RenameDuplicates()
		{
			var go = Selection.activeObject as GameObject;
			Transform trans = go.transform;
			Dictionary<string, List<Transform>> duplicates = new Dictionary<string, List<Transform>>();
			HashSet<string> newCreatedNames = new HashSet<string>();
			AddChildrenToDictToRename(duplicates, trans);
			string newName = "";
			foreach (var pair in duplicates)
			{
				if (pair.Value.Count < 2)
				{
					continue;
				}
				int counter = 0;
				foreach (var dTrans in pair.Value)
				{
					do
					{
						counter++;
						newName = dTrans.name + "_" + counter;
					} while (duplicates.ContainsKey(newName) || newCreatedNames.Contains(newName));
					newCreatedNames.Add(newName);
					dTrans.name = newName;
				}
			}
			EditorUtility.SetDirty(go);
		}

		[MenuItem("CONTEXT/GameObject/Disconnect from prefab", false, 0)]
		static void DisconnectFromPrefab()
		{
			var go = Selection.activeObject as GameObject;
			PrefabUtility.DisconnectPrefabInstance(go);
		}

		[MenuItem("CONTEXT/Transform/Disconnect from prefab", false, 0)]
		static void DisconnectFromPrefabTrans()
		{
			var go = Selection.activeObject as GameObject;
			PrefabUtility.DisconnectPrefabInstance(go);
		}

		[MenuItem("CONTEXT/Transform/SetParent to NULL", false, 0)]
		static void SetPartentToNull()
		{
			var go = Selection.activeObject as GameObject;
			Transform trans = go.transform;
			trans.parent = null;
		}


		static void AddChildrenToDictToRename(Dictionary<string, List<Transform>> dict, Transform parent)
		{
			List<Transform> transList = null;
			if (!dict.TryGetValue(parent.name, out transList))
			{
				transList = new List<Transform>();
				transList.Add(parent);
				dict[parent.name] = transList;
			}
			else
			{
				transList.Add(parent);
			}

			for (int i = 0; i < parent.childCount; i++)
			{
				var child = parent.GetChild(i);
				AddChildrenToDictToRename(dict, child);
			}
		}

		#endregion
	}
}