using System;
using UnityEditor;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEditor.Callbacks;

namespace Stratton.Core.Editor
{
    // Automatically set the "Always Embed Swift Standard Libraries" option to "No" in UnityFramework Target in XCode
    public static class DisableEmbedSwiftLibs
    {
        [PostProcessBuildAttribute(Int32.MaxValue)] //We want this code to run last!
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuildProject)
        {
#if UNITY_IOS
            if (buildTarget != BuildTarget.iOS) return; // Make sure its iOS build
            
            // Getting access to the xcode project file
            string projectPath = pathToBuildProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(projectPath);
            
            // Getting the UnityFramework Target and changing build settings
            string target = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(target, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");

            // After we're done editing the build settings we save it 
            pbxProject.WriteToFile(projectPath);
#endif
        }
    }
}