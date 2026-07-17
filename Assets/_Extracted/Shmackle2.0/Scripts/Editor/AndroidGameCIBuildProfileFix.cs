#if UNITY_ANDROID


using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;


public class AndroidGameCIBuildProfileFix : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => -1000; // run very early

    private static readonly string TempKeystorePath = Path.GetFullPath(Path.Combine("Temp", "ci_android_keystore.jks"));

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report == null)
            return;

        var opts = ParseUnityCommandLineArgs();

        // ensure bundleVersionCode is applied when using Build Profiles
        if (opts.TryGetValue("androidVersionCode", out var verCode) && int.TryParse(verCode, out var code))
        {
            PlayerSettings.Android.bundleVersionCode = code;
        }

        try
        {
            ApplyAndroidSigningFromArgs(opts);
            ApplyAndroidExportFromArgs(opts);
            ApplyAndroidTargetSdkFromArgs(opts);
            ApplyAndroidSymbolsFromArgs(opts);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CI Preprocessor] Failed to apply Android settings from args: {ex}");
            throw;
        }
    }


    public void OnPostprocessBuild(BuildReport report)
    {
        // Clean temp keystore if we created one
        try
        {
            if (File.Exists(TempKeystorePath))
                File.Delete(TempKeystorePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CI Preprocessor] Could not delete temporary keystore: {ex.Message}");
        }
    }


    private static Dictionary<string, string> ParseUnityCommandLineArgs()
    {
        var dict = new Dictionary<string, string>();
        var args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (!a.StartsWith("-"))
                continue;

            var key = a.TrimStart('-');
            var hasValue = (i + 1) < args.Length && !args[i + 1].StartsWith("-");
            var value = hasValue ? args[i + 1] : "";

            // Mirror game-ci behavior: last flag wins if duplicates
            dict[key] = value;
        }

        return dict;
    }


    private static void ApplyAndroidSigningFromArgs(Dictionary<string, string> arguments)
    {
        // game-ci flags
        arguments.TryGetValue("androidKeystoreName", out var keystoreName);
        arguments.TryGetValue("androidKeystorePass", out var keystorePass);
        arguments.TryGetValue("androidKeyaliasName", out var keyaliasName);
        arguments.TryGetValue("androidKeyaliasPass", out var keyaliasPass);


        // Not used by game-ci's AndroidSettings, but often supplied to the action:
        // allow decoding ourselves so users can set only -androidKeystoreBase64
        arguments.TryGetValue("androidKeystoreBase64", out var keystoreB64);

        string finalKeystorePath = keystoreName;

        if (!string.IsNullOrEmpty(keystoreB64))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(TempKeystorePath)!);

            // Tolerate accidental newlines/spaces
            var cleaned = keystoreB64.Trim().Replace("\r", "").Replace("\n", "");
            var bytes = Convert.FromBase64String(cleaned);
            File.WriteAllBytes(TempKeystorePath, bytes);
            finalKeystorePath = TempKeystorePath;

            Debug.Log("[CI Preprocessor] Decoded androidKeystoreBase64 to Temp/.");
        }


        // If we have either a path or base64 (decoded), apply signing
        if (!string.IsNullOrEmpty(finalKeystorePath) || !string.IsNullOrEmpty(keystorePass) || !string.IsNullOrEmpty(keyaliasName) || !string.IsNullOrEmpty(keyaliasPass))
        {
            if (!string.IsNullOrEmpty(finalKeystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = finalKeystorePath;
            }

            if (!string.IsNullOrEmpty(keystorePass))
                PlayerSettings.Android.keystorePass = keystorePass;


            if (!string.IsNullOrEmpty(keyaliasName))
                PlayerSettings.Android.keyaliasName = keyaliasName;


            if (!string.IsNullOrEmpty(keyaliasPass))
                PlayerSettings.Android.keyaliasPass = keyaliasPass;

            Debug.Log("[CI Preprocessor] Applied Android signing from command-line args.");
        }
        else
        {
            Debug.Log("[CI Preprocessor] No Android signing args found; leaving PlayerSettings as-is.");
        }
    }


    private static void ApplyAndroidExportFromArgs(Dictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue("androidExportType", out var exportType) || string.IsNullOrEmpty(exportType))
            return;


        // Mirrors game-ci's AndroidSettings.Apply
        switch (exportType)
        {
            case "androidStudioProject":
                EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                SetBuildAppBundle(false);
                break;
            case "androidAppBundle":
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                SetBuildAppBundle(true);
                break;
            case "androidPackage":
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                SetBuildAppBundle(false);
                break;
        }
    }


    private static void SetBuildAppBundle(bool value) { EditorUserBuildSettings.buildAppBundle = value; }

    private static void ApplyAndroidTargetSdkFromArgs(Dictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue("androidTargetSdkVersion", out var v) || string.IsNullOrEmpty(v))
            return;

        try
        {
            var parsed = (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), v);
            PlayerSettings.Android.targetSdkVersion = parsed;
        }
        catch
        {
            Debug.LogWarning($"[CI Preprocessor] Could not parse androidTargetSdkVersion \"{v}\"; leaving default.");
        }
    }

    private static void ApplyAndroidSymbolsFromArgs(Dictionary<string, string> arguments)
    {
        if (!arguments.TryGetValue("androidSymbolType", out var v) || string.IsNullOrEmpty(v))
            return;

        // UserBuildSettings.DebugSymbols.level via reflection (matches game-ci)
        SetDebugSymbols(v);
    }

    private static void SetDebugSymbols(string enumName)
    {
        var dbgType = Type.GetType("UnityEditor.Android.UserBuildSettings+DebugSymbols, UnityEditor.Android.Extensions");
        if (dbgType == null)
            return;

        var levelProp = dbgType.GetProperty("level", BindingFlags.Static | BindingFlags.Public);
        if (levelProp == null)
            return;

        var enumType = Type.GetType("Unity.Android.Types.DebugSymbolLevel, Unity.Android.Types");
        if (enumType == null)
            return;

        if (!Enum.TryParse(enumType, enumName, false, out var val))
            return;
        levelProp.SetValue(null, val);
    }
}
#endif