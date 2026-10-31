using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;

namespace HillNGun.Editor
{
    public static class BuildHelper
    {
        private const string BUILD_FOLDER = "Builds";
        private const string PRODUCT_NAME = "HillNGun";

        private static readonly string[] SCENE_PATHS = new string[]
        {
            "Assets/HillNGun/Scenes/Title.unity",
            "Assets/HillNGun/Scenes/ModeSelect.unity",
            "Assets/HillNGun/Scenes/CharacterSelect.unity",
            "Assets/HillNGun/Scenes/PvE_Match.unity",
            "Assets/HillNGun/Scenes/PvP_Match.unity",
            "Assets/HillNGun/Scenes/M1_Sandbox.unity",
            "Assets/HillNGun/Scenes/Result.unity"
        };

        [MenuItem("HillNGun/Build/Update Build Settings", false, 100)]
        public static void UpdateBuildSettings()
        {
            var scenes = SCENE_PATHS
                .Where(path => File.Exists(path))
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            EditorBuildSettings.scenes = scenes;

            Debug.Log($"Build Settings updated with {scenes.Length} scenes:");
            foreach (var scene in scenes)
            {
                Debug.Log($"  - {scene.path}");
            }
        }

        [MenuItem("HillNGun/Build/Build All Platforms", false, 110)]
        public static void BuildAllPlatforms()
        {
            UpdateBuildSettings();
            BuildWindowsStandalone();
            BuildMacOSStandalone();
            BuildAndroid();

            Debug.Log("All builds completed!");
            EditorUtility.DisplayDialog("빌드 완료", "모든 플랫폼 빌드가 완료되었습니다.", "확인");
        }

        [MenuItem("HillNGun/Build/Build Windows", false, 120)]
        public static void BuildWindowsStandalone()
        {
            UpdateBuildSettings();
            string buildPath = Path.Combine(BUILD_FOLDER, "Windows", $"{PRODUCT_NAME}.exe");
            BuildPlayer(BuildTarget.StandaloneWindows64, buildPath, "Windows");
        }

        [MenuItem("HillNGun/Build/Build macOS", false, 121)]
        public static void BuildMacOSStandalone()
        {
            UpdateBuildSettings();
            string buildPath = Path.Combine(BUILD_FOLDER, "macOS", $"{PRODUCT_NAME}.app");
            BuildPlayer(BuildTarget.StandaloneOSX, buildPath, "macOS");
        }

        [MenuItem("HillNGun/Build/Build Android APK", false, 122)]
        public static void BuildAndroid()
        {
            UpdateBuildSettings();
            ConfigureAndroidSettings();
            string buildPath = Path.Combine(BUILD_FOLDER, "Android", $"{PRODUCT_NAME}.apk");
            BuildPlayer(BuildTarget.Android, buildPath, "Android");
        }

        private static void BuildPlayer(BuildTarget target, string locationPath, string platformName)
        {
            string directory = Path.GetDirectoryName(locationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = locationPath,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log($"Building {platformName}...");

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"{platformName} build succeeded: {summary.totalSize} bytes");
                Debug.Log($"Output: {locationPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"{platformName} build failed with {summary.totalErrors} errors");
            }
        }

        private static string[] GetEnabledScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .ToArray();
        }

        private static void ConfigureAndroidSettings()
        {
            PlayerSettings.companyName = "yangguki";
            PlayerSettings.productName = "HillNGun";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yangguki.hillngun");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.Android.useCustomKeystore = false;

            Application.targetFrameRate = 60;

            EditorUserBuildSettings.buildAppBundle = false;
        }

        [MenuItem("HillNGun/Build/Configure Android Settings", false, 115)]
        public static void ConfigureAndroidSettingsMenu()
        {
            ConfigureAndroidSettings();
            Debug.Log("[BuildHelper] Android settings configured:");
            Debug.Log($"  Company: {PlayerSettings.companyName}");
            Debug.Log($"  Product: {PlayerSettings.productName}");
            Debug.Log($"  Bundle ID: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}");
            Debug.Log($"  Min SDK: API {(int)PlayerSettings.Android.minSdkVersion}");
            Debug.Log($"  Scripting: IL2CPP, ARM64");
            EditorUtility.DisplayDialog("Android 설정", "Android 빌드 설정이 완료되었습니다.", "확인");
        }

        public static void BuildFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            string platform = "all";
            bool skipSetup = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-platform" && i + 1 < args.Length)
                {
                    platform = args[i + 1].ToLower();
                }
                if (args[i] == "-skipSetup")
                {
                    skipSetup = true;
                }
            }

            try
            {
                if (!skipSetup)
                {
                    Debug.Log("[BuildHelper] Running setup...");
                    GameSetup.SetupAllHeadless();
                }

                UpdateBuildSettings();

                bool success = false;
                switch (platform)
                {
                    case "windows":
                        success = BuildWindowsStandaloneHeadless();
                        break;
                    case "macos":
                    case "osx":
                        success = BuildMacOSStandaloneHeadless();
                        break;
                    case "android":
                        success = BuildAndroidHeadless();
                        break;
                    case "all":
                    default:
                        success = BuildAllPlatformsHeadless();
                        break;
                }

                if (!success)
                {
                    Debug.LogError("[BuildHelper] Build failed!");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log("[BuildHelper] Build completed successfully!");
                    EditorApplication.Exit(0);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BuildHelper] Build failed with exception: {e.Message}\n{e.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        private static bool BuildAllPlatformsHeadless()
        {
            bool success = true;
            success &= BuildWindowsStandaloneHeadless();
            success &= BuildMacOSStandaloneHeadless();
            success &= BuildAndroidHeadless();
            return success;
        }

        private static bool BuildWindowsStandaloneHeadless()
        {
            string buildPath = Path.Combine(BUILD_FOLDER, "Windows", $"{PRODUCT_NAME}.exe");
            return BuildPlayerHeadless(BuildTarget.StandaloneWindows64, buildPath, "Windows");
        }

        private static bool BuildMacOSStandaloneHeadless()
        {
            string buildPath = Path.Combine(BUILD_FOLDER, "macOS", $"{PRODUCT_NAME}.app");
            return BuildPlayerHeadless(BuildTarget.StandaloneOSX, buildPath, "macOS");
        }

        private static bool BuildAndroidHeadless()
        {
            ConfigureAndroidSettings();
            string buildPath = Path.Combine(BUILD_FOLDER, "Android", $"{PRODUCT_NAME}.apk");
            return BuildPlayerHeadless(BuildTarget.Android, buildPath, "Android");
        }

        private static bool BuildPlayerHeadless(BuildTarget target, string locationPath, string platformName)
        {
            string directory = Path.GetDirectoryName(locationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = locationPath,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildHelper] Building {platformName}...");

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildHelper] {platformName} build succeeded: {summary.totalSize} bytes at {locationPath}");
                return true;
            }
            else
            {
                Debug.LogError($"[BuildHelper] {platformName} build failed with {summary.totalErrors} errors");
                return false;
            }
        }

        [MenuItem("HillNGun/Build/Open Build Folder", false, 200)]
        public static void OpenBuildFolder()
        {
            string fullPath = Path.GetFullPath(BUILD_FOLDER);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            EditorUtility.RevealInFinder(fullPath);
        }

        [MenuItem("HillNGun/Build/Clean Build Folder", false, 201)]
        public static void CleanBuildFolder()
        {
            if (!EditorUtility.DisplayDialog("빌드 폴더 정리",
                $"'{BUILD_FOLDER}' 폴더의 모든 빌드를 삭제하시겠습니까?",
                "삭제", "취소"))
            {
                return;
            }

            string fullPath = Path.GetFullPath(BUILD_FOLDER);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                Debug.Log("Build folder cleaned.");
            }
        }
    }
}
