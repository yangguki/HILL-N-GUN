using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    public static class Proto_GreyboxBuild
    {
        const string ScenePath = "Assets/HillNGun/Proto/Greybox/Scenes/Proto_GreyboxStage.unity";
        const string OutputPath = "Builds/v4_greybox/HillNGun_v4_greybox.apk";

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

            Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
            Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();

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

        // v3 proto used Builds/iOS/.team + automatic signing at xcodebuild time, and
        // com.yanginkuk.hillngun.proto. Greybox uses the same Apple team (cert OU 9KSCW68JQW)
        // and a distinct id so it can sit beside the main game / v3 proto.
        const string IosOutputDir = "Builds/v4_greybox/ios";
        const string IosBundleId = "com.yangguki.hillngun.greybox";
        const string IosTeamId = "9KSCW68JQW";

        public static void BuildIOS()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("iOS build support module is not installed for this Unity editor.");
                EditorApplication.Exit(2);
                return;
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError("Greybox scene missing: " + ScenePath);
                EditorApplication.Exit(1);
                return;
            }

            Proto_GreyboxWestPixelCatalogRebuild.RebuildCatalog();
            Proto_GreyboxFxV2CatalogRebuild.RebuildCatalog();

            string full = Path.GetFullPath(IosOutputDir);
            if (Directory.Exists(full))
                Directory.Delete(full, true);
            Directory.CreateDirectory(Path.GetDirectoryName(full) ?? IosOutputDir);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.productName = "HillNGun Greybox";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, IosBundleId);
            PlayerSettings.iOS.appleDeveloperTeamID = IosTeamId;
            // Do not write appleEnableAutomaticSigning: Unity 6 leaves ProjectSettings dirty.

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = IosOutputDir,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            Debug.Log("Greybox iOS export starting -> " + full);
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                AllowBothLandscapes(full);
                Debug.Log($"Greybox iOS export succeeded: {summary.totalTime}");
                Debug.Log("Output: " + full);
                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"Greybox iOS export failed: {summary.result}, errors={summary.totalErrors}");
            EditorApplication.Exit(1);
        }

        static void AllowBothLandscapes(string xcode)
        {
            string plist = Path.Combine(xcode, "Info.plist");
            if (!File.Exists(plist)) return;
            string text = File.ReadAllText(plist);
            const string block =
                "<key>UISupportedInterfaceOrientations</key>\n    <array>\n      <string>UIInterfaceOrientationLandscapeLeft</string>\n      <string>UIInterfaceOrientationLandscapeRight</string>\n    </array>";
            int key = text.IndexOf("<key>UISupportedInterfaceOrientations</key>", System.StringComparison.Ordinal);
            if (key < 0) return;
            int end = text.IndexOf("</array>", key, System.StringComparison.Ordinal);
            if (end < 0) return;
            text = text.Substring(0, key) + block + text.Substring(end + "</array>".Length);
            if (!text.Contains("UISupportedInterfaceOrientations~ipad"))
            {
                string ipad = block.Replace("UISupportedInterfaceOrientations", "UISupportedInterfaceOrientations~ipad");
                text = text.Replace(block, block + "\n    " + ipad);
            }
            File.WriteAllText(plist, text);
        }
    }
}
