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

        // GameObject.Find skips inactive objects, so roots hidden for 3D could never be found again
        // when returning to a 2D style (grunts / crosshair / bullets stayed hidden). Remember them.
        static readonly System.Collections.Generic.List<GameObject> s_hidden = new();

        public static void SetWorldVisible(bool visible)
        {
            if (visible)
            {
                foreach (var hidden in s_hidden)
                    if (hidden != null)
                        hidden.SetActive(true);
                s_hidden.Clear();
            }

            var stageRoot = GameObject.Find(Proto_GreyboxStageBuilder.RootName);
            if (stageRoot != null)
                SetSpriteRenderersEnabled(stageRoot.transform, visible);

            foreach (var name in ExtraHideNames)
            {
                SetActiveTracked(GameObject.Find(name), visible);
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
            SetActiveTracked(GameObject.Find(name), visible);
        }

        static void SetActiveTracked(GameObject go, bool visible)
        {
            if (go == null) return;
            if (!visible && go.activeSelf && !s_hidden.Contains(go))
                s_hidden.Add(go);
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
            // Never pick the 3D camera: ViewRoot is parented under the stage builder,
            // so GetComponentInChildren<Camera> can return it after a 3D switch.
            var setups = Object.FindObjectsByType<Proto_GreyboxCameraSetup>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var setup in setups)
            {
                var cam = setup.GetComponent<Camera>();
                if (cam != null)
                    return cam;
            }

            var main = Camera.main;
            if (main != null && main.GetComponent<Proto_GreyboxCameraSetup>() != null)
                return main;
            return null;
        }
    }
}