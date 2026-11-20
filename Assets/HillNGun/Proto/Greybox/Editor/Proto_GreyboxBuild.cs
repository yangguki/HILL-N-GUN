using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    public static class Proto_GreyboxBuild
    {
        const string ScenePath = "Assets/HillNGun/Proto/Greybox/Scenes/Proto_GreyboxStage.unity";
        const string OutputPath = "Builds/v4_greybox/HillNGun_v4_greybox_1120.apk";

        /// <summary>
        /// Batchmode entry: builds ONLY the greybox scene. Does not modify EditorBuildSettings.
        /// Landscape and a distinct package name are applied for this APK; callers should restore
        /// tracked ProjectSettings.asset afterwards.
        /// </summary>
        public static void BuildAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Android build support module is not installed for this Unity editor.");
                EditorApplication.Exit(2);
                return;
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Greybox scene missing: " + ScenePath);
                EditorApplication.Exit(1);
                return;
            }

            var dir = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Landscape for this proto APK. Do not touch EditorBuildSettings.scenes.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Distinct id/name so this APK can sit beside the main game. Debug keystore (project default).
            PlayerSettings.productName = "HillNGun Greybox";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yangguki.hillngun.greybox");
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log("Greybox Android build starting -> " + Path.GetFullPath(OutputPath));
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Greybox Android build succeeded: {summary.totalSize} bytes, {summary.totalTime}");
                Debug.Log("Output: " + Path.GetFullPath(OutputPath));
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"Greybox Android build failed: {summary.result}, errors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }
    }
}
