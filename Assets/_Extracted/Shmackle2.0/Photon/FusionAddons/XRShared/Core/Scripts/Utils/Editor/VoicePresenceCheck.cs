#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;

namespace Fusion.XRShared.Tools
{
    [InitializeOnLoad]
    public class VoicePresenceCheck
    {
        const string DEFINE= "PHOTON_VOICE_AVAILABLE";

        static VoicePresenceCheck()
        {
            bool isFusionVoiceBridgeClassMissing = Type.GetType("Photon.Voice.Fusion.FusionVoiceClient, PhotonVoice.Fusion") == null;
            if (isFusionVoiceBridgeClassMissing == false)
            {
                var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));

                if (defines.Contains(DEFINE) == false)
                {
                    defines = $"{defines};{DEFINE}";
                    PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
                }
            }
        }
    }
}
#endif