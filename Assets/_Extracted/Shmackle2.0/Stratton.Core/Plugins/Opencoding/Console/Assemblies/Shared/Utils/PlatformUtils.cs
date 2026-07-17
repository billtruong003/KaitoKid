using System.Linq;
using UnityEngine;

namespace Opencoding.Shared.Utils
{
	public static class PlatformUtils
	{
		public static bool IsDesktop
		{
			get;
			private set;
		}

		public static bool IsMobile
		{
			get;
			private set;
		}

		private static readonly RuntimePlatform[] _desktopRuntimePlatforms = new RuntimePlatform[]
				{
			RuntimePlatform.WindowsPlayer, RuntimePlatform.WindowsEditor, RuntimePlatform.LinuxEditor, RuntimePlatform.LinuxPlayer,
			RuntimePlatform.OSXPlayer, RuntimePlatform.OSXEditor, RuntimePlatform.WebGLPlayer
				};

		static PlatformUtils()
		{
			IsMobile = true;
			IsDesktop = false;

			foreach (RuntimePlatform platform in _desktopRuntimePlatforms)
			{
				if (Application.platform == platform)
				{
					IsDesktop = true;
					IsMobile = false;
					break;
				}
			}
		}
	}
}