using UnityEngine;
using HillNGun.Core;
using HillNGun.Data;

namespace HillNGun.View3D
{
    /// <summary>
    /// Scene plumbing for the 3D actor view: data lookup, shared toon material, global toon shader params,
    /// and an overlay camera (child of the main 2D camera, same projection) that renders only the actor layer
    /// on top of the sprite field. Logic is untouched; disabling Actor3DData.useActor3D restores the sprite view.
    /// </summary>
    public class Actor3DStage : MonoBehaviour
    {
        public const string MaterialResource = "Actor3D/hng_toon";
        private static Actor3DStage _instance;
        private static Material _material;
        private Camera _main, _overlay;

        public static Actor3DData Data => ServiceLocator.Get<Actor3DData>() ?? Actor3DData.Fallback;
        public static Actor3DAnimData Anim => ServiceLocator.Get<Actor3DAnimData>() ?? Actor3DAnimData.Fallback;

        /// <summary>True when the 3D actor view is switched on and the toon shader is available.</summary>
        public static bool Enabled => Data.useActor3D && Material != null;

        public static Material Material
        {
            get
            {
                if (_material != null) return _material;
                var m = Resources.Load<Material>(MaterialResource);
                if (m == null)
                {
                    var sh = Shader.Find("HillNGun/Toon");
                    if (sh == null || !sh.isSupported) return null;
                    m = new Material(sh) { name = "hng_toon_runtime" };
                }
                m.enableInstancing = true;
                _material = m;
                return _material;
            }
        }

        public static void ApplyGlobals(float outlineWidth)
        {
            var d = Data;
            Shader.SetGlobalVector("_HNG_LightDir", d.lightDirection.normalized);
            Shader.SetGlobalVector("_HNG_ToonParams", new Vector4(d.shadowBrightness, d.rimStrength, d.bandSoftness, d.highlight));
            Shader.SetGlobalColor("_HNG_ShadowColor", d.shadowColor);
            Shader.SetGlobalColor("_HNG_OutlineColor", d.outlineColor);
            Shader.SetGlobalFloat("_HNG_OutlineWidth", outlineWidth);
        }

        /// <summary>Ensures the overlay camera exists for the current scene (call when a 3D actor is created).</summary>
        public static Actor3DStage Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("Actor3DStage");
            _instance = go.AddComponent<Actor3DStage>();
            return _instance;
        }

        public static Actor3D Create(string modelId, Transform parent, string name, SkinDefinition skin = null)
        {
            if (!Enabled) return null;
            var def = Data.GetModel(modelId);
            if (def == null) return null;
            Ensure();
            bool tinted = skin != null && skin.unlock != SkinUnlockType.Default && skin.recolorStrength > 0.001f;
            return new Actor3D(def, Material, Data, Anim, tinted ? skin.tint : Color.white, tinted ? skin.recolorStrength : 0f, parent, name);
        }

        private void Awake()
        {
            ApplyGlobals(Data.outlineWidth);
            Attach();
        }

        private void Attach()
        {
            _main = Camera.main;
            if (_main == null) return;
            int layer = Data.actorLayer;
            _main.cullingMask &= ~(1 << layer);
            var camGo = new GameObject("Actor3DCamera");
            camGo.transform.SetParent(_main.transform, false);
            _overlay = camGo.AddComponent<Camera>();
            _overlay.clearFlags = CameraClearFlags.Depth;
            _overlay.cullingMask = 1 << layer;
            _overlay.depth = _main.depth + 1f;
            _overlay.orthographic = _main.orthographic;
            _overlay.nearClipPlane = 0.1f;
            _overlay.farClipPlane = 200f;
            _overlay.allowHDR = false;
            _overlay.allowMSAA = true;
            _overlay.useOcclusionCulling = false;
            Sync();
        }

        private void Sync()
        {
            if (_overlay == null || _main == null) return;
            _overlay.orthographicSize = _main.orthographicSize;
            _overlay.fieldOfView = _main.fieldOfView;
            _overlay.rect = _main.rect;
        }

        private void LateUpdate()
        {
            if (_main == null || _overlay == null) { if (Camera.main != null && Camera.main != _main) Attach(); return; }
            ApplyGlobals(Data.outlineWidth);
            Sync();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_overlay != null) Destroy(_overlay.gameObject);
        }
    }
}
