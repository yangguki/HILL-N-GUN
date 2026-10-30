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
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            EditorUserBuildSettings.buildAppBundle = false;
        }

        public static void BuildFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            string platform = "all";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-platform" && i + 1 < args.Length)
                {
                    platform = args[i + 1].ToLower();
                }
            }

            UpdateBuildSettings();

            switch (platform)
            {
                case "windows":
                    BuildWindowsStandalone();
                    break;
                case "macos":
                case "osx":
                    BuildMacOSStandalone();
                    break;
                case "android":
                    BuildAndroid();
                    break;
                case "all":
                default:
                    BuildAllPlatforms();
                    break;
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
