using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    public static class Proto_GreyboxWorld2DVisibility
    {
        static readonly string[] ExtraHideNames =
        {
            "Greybox_Crosshair",
            "Greybox_AllyBullets",
            "Greybox_EnemyBullets",
            "Greybox_Grunts",
            "Greybox_WestPixel_Layer"
        };

        public static void SetWorldVisible(bool visible)
        {
            var stageRoot = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            if (stageRoot != null)
                SetSpriteRenderersEnabled(stageRoot.transform, visible);

            foreach (var name in ExtraHideNames)
            {
                var go = GameObject.Find(name);
                if (go != null)
                    go.SetActive(visible);
            }

            var player = GameObject.Find(Proto_GreyboxStageBuilder.PlayerName);
            if (player != null)
            {
                var sr = player.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.enabled = visible;
            }

            HideNamedTransformChildren("Greybox_AimLine", visible);
            HideNamedTransformChildren("Greybox_AimCone", visible);
            HideNamedTransformChildren("Greybox_GrenadeArc", visible);
        }

        static void HideNamedTransformChildren(string name, bool visible)
        {
            var go = GameObject.Find(name);
            if (go != null)
                go.SetActive(visible);
        }

        static void SetSpriteRenderersEnabled(Transform root, bool enabled)
        {
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name == Proto_GreyboxStageBuilder.PlayerName)
                    continue;
                sr.enabled = enabled;
            }
        }

        public static Camera Find2DWorldCamera()
        {
            var stageBuilder = Object.FindFirstObjectByType<Proto_GreyboxStageBuilder>();
            if (stageBuilder != null)
            {
                var cam = stageBuilder.GetComponentInChildren<Camera>(true);
                if (cam != null)
                    return cam;
            }

            var main = Camera.main;
            if (main != null && main.GetComponent<Proto_GreyboxCameraSetup>() != null)
                return main;
            return Object.FindFirstObjectByType<Proto_GreyboxCameraSetup>()?.GetComponent<Camera>();
        }
    }
}