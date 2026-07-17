using System;
using UnityEditor;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEditor.Callbacks;
using Stratton.CI;
using Stratton.Core.Types;
using System.IO;

namespace Stratton.Core.Editor
{
    public static class AddPushNotificationsCapability
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
            
            string mainTarget = pbxProject.GetUnityMainTargetGuid();

            var manager = new ProjectCapabilityManager(projectPath, "Entitlements.entitlements", null, mainTarget);
            manager.AddPushNotifications(BuildSettings.Instance.BuildStage == BaseStageType.Dev);
            manager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
            manager.WriteToFile();

            // Add necessary frameworks.
            if (!pbxProject.ContainsFramework(mainTarget, "UserNotifications.framework"))
            {
                pbxProject.AddFrameworkToProject(mainTarget, "UserNotifications.framework", false);
                File.WriteAllText(projectPath, pbxProject.WriteToString());
            }

            // Get plist
            string plistPath = pathToBuildProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            PlistElementDict rootDict = plist.root;

            rootDict.SetBoolean("FirebaseAppDelegateProxyEnabled", true);

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }
    }
}