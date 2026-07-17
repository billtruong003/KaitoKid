using System;
using Opencoding.LogHistory;
using UnityEngine;

namespace Opencoding.Console
{
	public static class Utils
	{
		public static void GetImageAndStyleForHistoryItem(LogHistoryItem historyItem, ImageFilesContainer imageFiles, out GUIStyle style, out Texture2D image)
		{
			style = null;
			image = null;
			switch (historyItem._Type)
			{
				case LogHistoryLogType.Exception:
					image = imageFiles._ExceptionIcon;
					style = GUIStyles.ExceptionLabelStyle;
					break;
				case LogHistoryLogType.Assert:
					image = imageFiles._AssertIcon;
					style = GUIStyles.AssertLabelStyle;
					break;
				case LogHistoryLogType.Error:
					image = imageFiles._ErrorIcon;
					style = GUIStyles.ErrorLabelStyle;
					break;
				case LogHistoryLogType.Log:
					image = imageFiles._InfoIcon;
					style = GUIStyles.InfoLabelStyle;
					break;
				case LogHistoryLogType.Warning:
					image = imageFiles._WarningIcon;
					style = GUIStyles.WarningLabelStyle;
					break;
				case LogHistoryLogType.ConsoleInput:
					image = imageFiles._ConsoleInputIcon;
					style = GUIStyles.InfoLabelStyle;
					break;
				default:
					throw new InvalidOperationException("Unrecognised log type");
			}
		}
	}
}