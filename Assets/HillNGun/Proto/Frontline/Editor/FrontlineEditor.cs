using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HillNGun.Data;
using HillNGun.Proto.TopDown;

namespace HillNGun.Proto.Frontline.Editor
{
    public static class FrontlineEditor
    {
        const string AutoArg = "-proto-frontline-auto";
        const string ScenePath = "Assets/HillNGun/Scenes/Proto_Frontline.unity";

        [InitializeOnLoadMethod]
        static void Boot()
        {
            if (!Has(AutoArg)) return;
            EditorApplication.update += Watch;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Path.Combine(FrontlineMatch.OutputDir, "auto.done")))
            {
                EditorApplication.Exit(0);
                return;
            }
            EditorApplication.update += SetupOnce;
        }

        static void SetupOnce()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= SetupOnce;
            try
            {
                Directory.CreateDirectory(FrontlineMatch.OutputDir);
                File.Delete(Path.Combine(FrontlineMatch.OutputDir, "auto.done"));
                File.Delete(Path.Combine(FrontlineMatch.OutputDir, "auto_stage.txt"));
                File.Delete(Path.Combine(FrontlineMatch.OutputDir, "auto.txt"));
                Setup();
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void Watch()
        {
            if (!EditorApplication.isPlaying)
            {
                if (File.Exists(Path.Combine(FrontlineMatch.OutputDir, "auto.done")))
                    EditorApplication.Exit(0);
                return;
            }
            if (FrontlineMatch.AutoFinished) EditorApplication.isPlaying = false;
        }

        [MenuItem("HillNGun/Proto/Build Frontline Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ProtoFrontline");
            var match = go.AddComponent<FrontlineMatch>();
            match.Buster = Load<CharacterData>(ProtoAssets.Buster);
            match.Rock = Load<CoverData>(ProtoAssets.Rock);
            match.Hay = Load<CoverData>(ProtoAssets.Hay);
            match.Crate = Load<CoverData>(ProtoAssets.Crate);
            match.Rules = Load<MatchRulesData>(ProtoAssets.Rules);
            match.MoveFeel = Load<MovementFeelData>(ProtoAssets.FeelMove);
            match.Feel = Load<FeelData>(ProtoAssets.Feel);
            match.Downed = Load<DownedData>(ProtoAssets.Downed);
            match.SpriteDown = Load<Sprite>(ProtoAssets.SpriteDown);
            match.SpriteUp = Load<Sprite>(ProtoAssets.SpriteUp);
            match.SpriteSide = Load<Sprite>(ProtoAssets.SpriteSide);
            match.PixelArt = Load<ProtoPixelArt>(ProtoAssets.PixelArtFrontline);
            match.SplitAxis = FrontlineSplitAxis.TopBottom;
            if (match.Buster == null || match.Rules == null || match.SpriteDown == null)
                throw new InvalidOperationException("[Frontline] required assets failed to load");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Frontline] scene saved " + ScenePath);
        }

        static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static bool Has(string arg)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == arg) return true;
            return false;
        }
    }
}
