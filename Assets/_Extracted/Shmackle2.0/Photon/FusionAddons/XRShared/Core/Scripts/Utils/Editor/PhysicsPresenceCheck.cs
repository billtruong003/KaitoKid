#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;

namespace Fusion.XRShared.Tools
{
    [InitializeOnLoad]
    public class PhysicsPresenceCheck
    {
        const string DEFINE= "FUSION_PHYSICS_ADDON_AVAILABLE";

        static PhysicsPresenceCheck()
        {
            bool isFusionPhysicsClassMissing = Type.GetType("Fusion.Addons.Physics.NetworkRigidbody3D, Fusion.Addons.Physics") == null;
            if (isFusionPhysicsClassMissing == false)
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