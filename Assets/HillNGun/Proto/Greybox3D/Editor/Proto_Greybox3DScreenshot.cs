using System.IO;
using HillNGun.Proto.Greybox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D.Editor
{
    public static class Proto_Greybox3DScreenshot
    {
        const int CompareW = 1920;
        const int CompareH = 1080;
        const int HalfW = 960;

        static void Ensure3DFactory()
        {
            if (!Proto_GreyboxViewPresenterRegistry.HasFactory)
                Proto_GreyboxViewPresenterRegistry.RegisterFactory(() => new Proto_Greybox3DViewHost());
        }

        static Proto_GreyboxPlayerController OpenAndBuild()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var builder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            builder?.Build();
            Ensure3DFactory();
            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            return player != null ? player.GetComponent<Proto_GreyboxPlayerController>() : null;
        }

        public static void Capture3dACompare()
        {
            var controller = OpenAndBuild();
            if (controller == null)
            {
                Debug.LogError("3D compare: player missing.");
                EditorApplication.Exit(1);
                return;
            }

            const float dt = 0.02f;
            controller.RuntimeTuning.EnemyFireInterval = 9999f;
            controller.SetWestPixelSkin(true);
            for (int i = 0; i < 80; i++)
                controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);

            var foot = controller.Motor.FootPosition;
            var mapped = Proto_Greybox3DFootMap.FootToWorld(foot);
            Debug.Log(
                $"Capture3dACompare view2D then view3D foot=({foot.x:F2},{foot.y:F2}) mapped=({mapped.x:F2},{mapped.z:F2})");

            controller.SetViewMode(Proto_GreyboxViewMode.View2D, persist: false);
            Canvas.ForceUpdateCanvases();
            var tex2D = CaptureActiveWorldCam(CompareW, CompareH);
            var half2D = ScaleTexture(tex2D, HalfW, CompareH);

            controller.SetViewMode(Proto_GreyboxViewMode.View3D, persist: false);
            controller.Simulate(new Proto_GreyboxInput(), dt);
            Canvas.ForceUpdateCanvases();
            var tex3D = CaptureActiveWorldCam(CompareW, CompareH);
            var half3D = ScaleTexture(tex3D, HalfW, CompareH);

            var stitched = new Texture2D(CompareW, CompareH, TextureFormat.RGB24, false);
            stitched.SetPixels(0, 0, HalfW, CompareH, half2D.GetPixels());
            stitched.SetPixels(HalfW, 0, HalfW, CompareH, half3D.GetPixels());
            stitched.Apply();
            WritePng(stitched, "v4_greybox/3d_a_compare.png");
            Object.DestroyImmediate(tex2D);
            Object.DestroyImmediate(tex3D);
            Object.DestroyImmediate(half2D);
            Object.DestroyImmediate(half3D);
            Object.DestroyImmediate(stitched);
            EditorApplication.Exit(0);
        }

        public static void Capture3dACombat()
        {
            var controller = OpenAndBuild();
            if (controller == null)
            {
                Debug.LogError("3D combat: player missing.");
                EditorApplication.Exit(1);
                return;
            }

            const float dt = 0.02f;
            controller.RuntimeTuning.EnemyFireInterval = 0.7f;
            controller.RuntimeTuning.Wave1Count = 2;
            controller.SetViewMode(Proto_GreyboxViewMode.View3D, persist: false);

            // Keep the player alive: 3 HP would otherwise DOWN before the pose.
            for (int i = 0; i < 200; i++)
            {
                controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = i % 4 != 0 }, dt);
                if (i % 25 == 0)
                    controller.ResetHp();
            }

            controller.ResetHp();
            var overlay = GameObject.Find(Proto_GreyboxResultUi.RootName);
            if (overlay != null)
                overlay.SetActive(false);

            controller.PoseAimStickDrag(new Vector2(0.4f, 4.8f));
            controller.Simulate(new Proto_GreyboxInput { FireQuickAuto = true }, dt);
            Canvas.ForceUpdateCanvases();

            var tex = CaptureActiveWorldCam(CompareW, CompareH);
            WritePng(tex, "v4_greybox/3d_a_combat.png");
            Object.DestroyImmediate(tex);
            EditorApplication.Exit(0);
        }

        static Texture2D CaptureActiveWorldCam(int width, int height)
        {
            Camera cam = null;
            if (Proto_GreyboxViewModeState.Is3D)
                cam = Proto_GreyboxViewPresenterRegistry.Ensure()?.WorldCamera;
            if (cam == null || !cam.enabled)
                cam = Camera.main;
            if (cam == null || !cam.enabled)
                cam = Proto_GreyboxWorld2DVisibility.Find2DWorldCamera();

            if (cam == null)
                return Texture2D.blackTexture;

            cam.aspect = Proto_GreyboxLayout.TargetAspect;
            if (cam.orthographic)
                Proto_GreyboxScreenMetrics.ConfigureCaptureCamera(cam);
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            cam.targetTexture = prev;
            RenderTexture.active = prevActive;
            rt.Release();
            Object.DestroyImmediate(rt);
            return tex;
        }

        static Texture2D ScaleTexture(Texture2D src, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var dst = new Texture2D(width, height, TextureFormat.RGB24, false);
            dst.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            dst.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }

        static void WritePng(Texture2D tex, string relative)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Builds");
            var path = Path.Combine(dir, relative);
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"Greybox3D screenshot saved: {path}");
        }
    }
}