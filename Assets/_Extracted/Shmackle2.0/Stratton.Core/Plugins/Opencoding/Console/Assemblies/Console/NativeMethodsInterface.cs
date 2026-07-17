using System;
using System.Reflection;
using Opencoding.Shared.Utils;
using UnityEngine;

namespace Opencoding.Console
{
	/// <summary>
	/// This is a rather unfortunate class that deals with interacting via reflection with the NativeMethods 
	/// class that lives outside the DLLs in Unity. It exists because NativeMethods.cs can't live in a DLL 
	/// because it contains DllImport attributes which will fail to compile on non-iOS platforms (where the 
	/// methods that the DllImport refers to don't exist).
	/// </summary>
	static class NativeMethodsInterface
	{
		private static MethodInfo _copyTextToClipboardMethod;
		private static MethodInfo _emailMethod;
		private static MethodInfo _getNativeScreenScaleFactorMethod;
		private static MethodInfo _canSendEmailMethod;

		static NativeMethodsInterface()
		{
			Type nativeMethodsType = null;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				nativeMethodsType = assembly.GetType("Opencoding.Console.NativeMethods");

				if (nativeMethodsType != null)
					break;
			}
			
			if(nativeMethodsType == null)
				throw new InvalidOperationException("Couldn't find the NativeMethods class. Make sure it's in your project somewhere.");

			_copyTextToClipboardMethod = nativeMethodsType.GetMethod("CopyTextToClipboard",BindingFlags.Public | BindingFlags.Static);
			_emailMethod = nativeMethodsType.GetMethod("SendEmail", BindingFlags.Public | BindingFlags.Static);
			_getNativeScreenScaleFactorMethod = nativeMethodsType.GetMethod("GetNativeScreenScaleFactor", BindingFlags.Public | BindingFlags.Static);
			_canSendEmailMethod = nativeMethodsType.GetMethod("CanSendEmail", BindingFlags.Public | BindingFlags.Static);

			if(_copyTextToClipboardMethod == null)
				throw new InvalidOperationException("Couldn't find method 'CopyTextToClipboard' in NativeMethods class");

			if (_emailMethod == null)
				throw new InvalidOperationException("Couldn't find method 'SendEmail' in NativeMethods class");

			if(_getNativeScreenScaleFactorMethod == null)
				throw new InvalidOperationException("Couldn't find method 'GetNativeScreenScaleFactor' in NativeMethods class");

			if (_canSendEmailMethod == null)
				throw new InvalidOperationException("Couldn't find method '_canSendEmailMethod' in NativeMethods class");
		}

		public static bool CanSendEmail
		{
			get
			{
				return (bool)_canSendEmailMethod.Invoke(null, null);
			}
		}
		
		public static void CopyTextToClipboard(string text)
		{
			_copyTextToClipboardMethod.Invoke(null, new object[] {text});
		}

		public static void SendEmail(Email email)
		{
			_emailMethod.Invoke(null, new object[] { email });
		}

		public static float GetNativeScreenScaleFactor()
		{
			return (float) _getNativeScreenScaleFactorMethod.Invoke(null, null);
		}
	}
}