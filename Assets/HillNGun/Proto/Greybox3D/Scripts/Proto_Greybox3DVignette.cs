using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Cheap Built-in blit: mild vignette + slight warm grade.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class Proto_Greybox3DVignette : MonoBehaviour
    {
        Material _mat;

        void OnEnable() => EnsureMat();

        void EnsureMat()
        {
            if (_mat != null) return;
            var cat = Proto_Greybox3DCatalog.Load();
            Shader sh = cat != null ? cat.VignetteShader : null;
            if (sh == null)
                sh = Shader.Find("HillNGun/Proto/Greybox3D/Vignette");
            if (sh == null) return;
            _mat = new Material(sh);
            _mat.SetFloat("_Vignette", 0.10f);
            _mat.SetFloat("_Warmth", 0.14f);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            EnsureMat();
            if (_mat == null)
            {
                Graphics.Blit(src, dest);
                return;
            }
            Graphics.Blit(src, dest, _mat);
        }
    }
}
