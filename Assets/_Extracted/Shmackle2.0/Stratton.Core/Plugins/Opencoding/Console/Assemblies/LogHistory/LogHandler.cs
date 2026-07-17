//#define UNITY_4
using UnityEngine;
using System.Collections;

namespace Opencoding.LogHistory
{
	/// <summary>
	/// This class wraps up the different behaviours of Unity 4 and 5 regarding log capturing. It won't be necessary
	/// when Unity 4 support is dropped.
	/// </summary>
	static public class LogHandler 
	{
		static Application.LogCallback callbacks = delegate {  };
		static public void RegisterLogCallback(Application.LogCallback callback)
		{
#if UNITY_4
			if(!Application.unityVersion.StartsWith("4"))
				Debug.LogError("This version of TouchConsole Pro is designed to work with Unity 4 only. If you've migrated to Unity 5, reimport the latest version from the asset store.");

			callbacks += callback;
			Application.RegisterLogCallbackThreaded(callbacks);
#else
			if(Application.unityVersion.StartsWith("4"))
				Debug.LogError("This version of TouchConsole Pro is designed to work with Unity 5 and newer only. Reimport the latest version from the asset store to get the Unity 4 version.");

			Application.logMessageReceivedThreaded += callback;
#endif
		}
 
		static public void UnRegisterLogCallback(Application.LogCallback callback)
		{
#if UNITY_4
			callbacks -= callback;
			Application.RegisterLogCallbackThreaded(callbacks);
#else
			Application.logMessageReceivedThreaded -= callback;
#endif
		}
	}
}