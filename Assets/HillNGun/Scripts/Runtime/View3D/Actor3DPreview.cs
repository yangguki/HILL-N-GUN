using UnityEngine;
using HillNGun.Data;

namespace HillNGun.View3D
{
    /// <summary>
    /// Lobby turntable: renders one 3D actor into a RenderTexture (far below the scene, own camera, actor layer only)
    /// so the IMGUI character tab can draw it. Rebuilds when the character or skin changes; idles when not drawn.
    /// </summary>
    public class Actor3DPreview : MonoBehaviour
    {
        private static Actor3DPreview _instance;
        private Camera _cam;
        private RenderTexture _rt;
        private Actor3D _actor;
        private string _key;
        private float _yaw, _lastDraw;
        private static readonly Vector3 Origin = new Vector3(0f, -500f, 50f);

        public static bool Available => Actor3DStage.Enabled && Actor3DStage.Data.lobbyPreview3D;

        /// <summary>Returns the preview texture for this character/skin (call every OnGUI frame it is shown).</summary>
        public static Texture Get(string characterId, SkinDefinition skin)
        {
            if (!Available) return null;
            if (_instance == null)
            {
                var go = new GameObject("Actor3DPreview");
                _instance = go.AddComponent<Actor3DPreview>();
            }
            return _instance.Show(characterId, skin);
        }

        private Texture Show(string id, SkinDefinition skin)
        {
            _lastDraw = Time.unscaledTime;
            string key = id + "|" + (skin != null ? skin.id : "-");
            if (key != _key)
            {
                _key = key;
                _actor?.Destroy();
                _actor = Actor3DStage.Create(id, transform, "Preview_" + id, skin);
                _yaw = Actor3DStage.Data.previewStartYaw;
            }
            EnsureCamera();
            if (!_cam.enabled) _cam.enabled = true;
            return _rt;
        }

        private void EnsureCamera()
        {
            var d = Actor3DStage.Data;
            if (_rt == null)
            {
                int s = Mathf.Clamp(d.previewTextureSize, 128, 2048);
                _rt = new RenderTexture(s, s, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4, name = "Actor3DPreviewRT" };
            }
            if (_cam != null) return;
            var go = new GameObject("PreviewCamera");
            go.transform.SetParent(transform, false);
            _cam = go.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = d.previewBackground;
            _cam.cullingMask = 1 << d.actorLayer;
            _cam.targetTexture = _rt;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;
            _cam.depth = -50;
            go.AddComponent<PreviewOutline>();
        }

        private void LateUpdate()
        {
            if (_cam == null) return;
            if (Time.unscaledTime - _lastDraw > 0.3f) { _cam.enabled = false; return; }
            var d = Actor3DStage.Data;
            if (_actor != null)
            {
                _yaw += d.previewSpinDegPerSec * Time.unscaledDeltaTime;
                float h = _actor.Rig.Height;
                _actor.Place(Origin, 1f / d.modelScale, 0f);
                var root = _actor.Rig.Root;
                root.position = Origin;
                root.rotation = Quaternion.Euler(-d.previewTiltDeg, 0f, 0f);
                root.localScale = Vector3.one;
                var pose = new ActorPose { defaultYaw = _yaw, maxSpeed = 3f, aimDir = Vector2.zero };
                _actor.Tick(Time.unscaledDeltaTime, pose);
                _actor.Rig.Yaw.localRotation = Quaternion.Euler(0f, _yaw, 0f);
                _cam.orthographicSize = h * 0.56f;
                _cam.transform.position = Origin + new Vector3(0f, h * 0.44f, -20f);
                _cam.transform.rotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            _actor?.Destroy();
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_instance == this) _instance = null;
        }

        /// <summary>Thinner outline for the close-up camera, restored for the field camera afterwards.</summary>
        private class PreviewOutline : MonoBehaviour
        {
            private void OnPreRender() => Actor3DStage.ApplyGlobals(Actor3DStage.Data.previewOutlineWidth);
            private void OnPostRender() => Actor3DStage.ApplyGlobals(Actor3DStage.Data.outlineWidth);
        }
    }
}
