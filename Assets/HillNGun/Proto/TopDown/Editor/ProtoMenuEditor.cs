using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.TopDown.Editor
{
    public static class ProtoMenuEditor
    {
        [MenuItem("HillNGun/Proto/Build Menu Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("ProtoMenu");
            go.AddComponent<ProtoMenu>();
            if (!EditorSceneManager.SaveScene(scene, ProtoAssets.MenuScene))
                throw new InvalidOperationException("[ProtoMenu] failed to save " + ProtoAssets.MenuScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProtoMenu] scene saved " + ProtoAssets.MenuScene);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
