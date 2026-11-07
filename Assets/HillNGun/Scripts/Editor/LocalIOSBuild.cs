// Local-only iOS Xcode export helper (no gameplay changes).
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HillNGun.EditorTools {
public static class LocalIOSBuild {
  public static void Export() {
    var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    var r = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = "Builds/iOS/Xcode", target = BuildTarget.iOS, options = BuildOptions.None });
    Debug.Log("iOS export: " + r.summary.result + " errors=" + r.summary.totalErrors);
    EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
  }
}}
