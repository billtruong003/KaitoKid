using System;
using System.Collections;
using UnityEngine;

namespace Opencoding.Console.Scripts.UI.Utilities
{
	static class ScreenshotCapture
	{
		public static void CaptureScreenshot(Settings.SelectedScreenshotCaptureMode captureMode, Action<byte[]> screenshotTakenAction)
		{
			if (captureMode == Settings.SelectedScreenshotCaptureMode.DISABLED)
			{
				screenshotTakenAction(null);
				return;
			}

			if (captureMode == Settings.SelectedScreenshotCaptureMode.CAPTURE_MAIN_CAMERA)
			{
				CaptureMainCamera(screenshotTakenAction);
			}
			else if(captureMode == Settings.SelectedScreenshotCaptureMode.CAPTURE_FULL_SCREEN)
			{
				CaptureFullScreen(screenshotTakenAction);
			}
			else
			{
				throw new InvalidOperationException("Unsupported screenshot capture mode");
			}
		}

		private static void CaptureMainCamera(Action<byte[]> screenshotTakenAction)
		{
			var camera = Camera.main;
			var texture = new Texture2D((int)camera.pixelWidth, (int)camera.pixelHeight, TextureFormat.RGB24, false);
			var existingTargetTexture = Camera.main.targetTexture;
			var temporaryTexture = RenderTexture.GetTemporary((int)camera.pixelWidth, (int)camera.pixelHeight, 8, RenderTextureFormat.ARGB32);

			Camera.main.targetTexture = temporaryTexture;
			Camera.main.Render();
			Camera.main.targetTexture = existingTargetTexture;

			var previousActiveRenderTexture = RenderTexture.active;
			RenderTexture.active = temporaryTexture;

			texture.ReadPixels(new Rect(0, 0, camera.pixelWidth, camera.pixelHeight), 0, 0);
			texture.Apply();
			RenderTexture.active = previousActiveRenderTexture;

			temporaryTexture.Release();

			screenshotTakenAction(texture.EncodeToPNG());
		}

		private static void CaptureFullScreen(Action<byte[]> screenshotTakenAction)
		{
		    if (DebugConsole.IsVisible)
		    {
		        DebugConsole.IsVisible = false;
		        DebugConsole.Instance.StartCoroutine(WaitForCapture(screenshotTakenAction, showConsoleAfter: true));
		    }
		    else
		    {
                DebugConsole.Instance.StartCoroutine(WaitForCapture(screenshotTakenAction, showConsoleAfter: false));
            }
		}

		private static IEnumerator WaitForCapture(Action<byte[]> screenshotTakenAction, bool showConsoleAfter)
		{
			yield return null;
			yield return new WaitForEndOfFrame();

			var texture = CaptureFullScreenTexture();

            if(showConsoleAfter)
		        DebugConsole.Instance.ShowConsoleInstantly();

			screenshotTakenAction(texture.EncodeToPNG());
		}

	    private static Texture2D CaptureFullScreenTexture()
	    {
	        var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
	        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
	        texture.Apply();
	        return texture;
	    }
	}
}
