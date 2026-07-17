using UnityEngine;

namespace Opencoding.Console
{
	public class EmailTemplateFilesContainer : ScriptableObject
	{
		public TextAsset _LogEmailHeaderTextAsset = null;
		public TextAsset _LogEmailFooterTextAsset = null;
	}
}