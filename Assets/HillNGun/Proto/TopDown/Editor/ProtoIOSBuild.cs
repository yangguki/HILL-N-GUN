using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HillNGun.Proto.TopDown.Editor
{
    public static class ProtoIOSBuild
    {
        const string OutDir = "Builds/iOS/ProtoXcode";
        const string BundleId = "com.yanginkuk.hillngun.proto";
        const string AppName = "HNG Proto";
        const string TeamFile = "Builds/iOS/.team";

        [MenuItem("HillNGun/Proto/Export iOS")]
        public static void Export()
        {
            string full = Path.GetFullPath(OutDir);
            string forbidden = Path.GetFullPath("Builds/iOS/Xcode");
            if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), forbidden.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("refusing to overwrite Builds/iOS/Xcode");

            string backup = Path.GetFullPath("Builds/proto_topdown/settings-backup");
            CopyTree(Path.GetFullPath("ProjectSettings"), backup);
            Debug.Log("[ProtoIOS] settings backup " + backup);

            string prevId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS);
            string prevName = PlayerSettings.productName;
            string prevTeam = PlayerSettings.iOS.appleDeveloperTeamID;
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            // Do not touch appleEnableAutomaticSigning: Unity 6 serializes it as an enum and writing
            // true/false leaves ProjectSettings dirty after quit even when the backup is restored.
            int code = 1;
            try
            {
                PlayerSettings.productName = AppName;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
                ApplyTeamFile();
                if (Directory.Exists(full)) Directory.Delete(full, true);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ProtoAssets.MenuScene, ProtoAssets.Scene, ProtoAssets.FrontlineScene },
                    locationPathName = OutDir,
                    target = BuildTarget.iOS,
                    options = BuildOptions.None
                });
                code = report.summary.result == BuildResult.Succeeded ? 0 : 1;
                if (code == 0) AllowBothLandscapes(full);
                Debug.Log("[ProtoIOS] export " + report.summary.result + " errors=" + report.summary.totalErrors + " path=" + OutDir);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                code = 1;
            }
            finally
            {
                PlayerSettings.productName = prevName;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, prevId);
                PlayerSettings.iOS.appleDeveloperTeamID = prevTeam;
                if (EditorUserBuildSettings.activeBuildTarget != prevTarget)
                {
                    var group = BuildPipeline.GetBuildTargetGroup(prevTarget);
                    EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.FromBuildTargetGroup(group), prevTarget);
                }
                CopyTree(backup, Path.GetFullPath("ProjectSettings"));
                // Quit can re-serialize in-memory PlayerSettings over the restored files; copy again on exit.
                string restore = backup;
                EditorApplication.quitting += () =>
                {
                    try { CopyTree(restore, Path.GetFullPath("ProjectSettings")); }
                    catch (Exception e) { Debug.LogException(e); }
                };
            }
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        static void AllowBothLandscapes(string xcode)
        {
            string plist = Path.Combine(xcode, "Info.plist");
            if (!File.Exists(plist)) return;
            string text = File.ReadAllText(plist);
            const string block = "<key>UISupportedInterfaceOrientations</key>\n    <array>\n      <string>UIInterfaceOrientationLandscapeLeft</string>\n      <string>UIInterfaceOrientationLandscapeRight</string>\n    </array>";
            int key = text.IndexOf("<key>UISupportedInterfaceOrientations</key>", StringComparison.Ordinal);
            if (key < 0) return;
            int end = text.IndexOf("</array>", key, StringComparison.Ordinal);
            if (end < 0) return;
            text = text.Substring(0, key) + block + text.Substring(end + "</array>".Length);
            if (!text.Contains("UISupportedInterfaceOrientations~ipad"))
            {
                string ipad = block.Replace("UISupportedInterfaceOrientations", "UISupportedInterfaceOrientations~ipad");
                text = text.Replace(block, block + "\n    " + ipad);
            }
            File.WriteAllText(plist, text);
        }

        static void ApplyTeamFile()
        {
            if (!File.Exists(TeamFile)) return;
            string team = null;
            var lines = File.ReadAllLines(TeamFile);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                team = line;
                break;
            }
            if (string.IsNullOrEmpty(team)) return;
            PlayerSettings.iOS.appleDeveloperTeamID = team;
        }

        static void CopyTree(string src, string dst)
        {
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
            foreach (var dir in Directory.GetDirectories(src))
                CopyTree(dir, Path.Combine(dst, Path.GetFileName(dir)));
        }
    }
}
