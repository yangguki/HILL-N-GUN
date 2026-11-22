using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DSceneBuilder
    {
        static readonly Color[] AwningColors =
        {
            new Color(0.15f, 0.42f, 0.88f),
            new Color(0.98f, 0.82f, 0.12f),
            new Color(0.90f, 0.18f, 0.16f),
            new Color(0.96f, 0.55f, 0.10f),
            new Color(0.18f, 0.68f, 0.38f),
            new Color(0.62f, 0.18f, 0.72f),
            new Color(0.18f, 0.38f, 0.82f)
        };

        public static Transform BuildRoot(Transform parent)
        {
            var root = new GameObject("Greybox3D_Stage").transform;
            root.SetParent(parent, false);

            var catalog = Proto_Greybox3DCatalog.Load();
            BuildSky(root, catalog);
            BuildGround(root);
            BuildMesas(root, catalog);
            BuildStorefronts(root);
            BuildFence(root, catalog);
            BuildCratesAndBarrels(root, catalog);
            BuildCactiRocksGrass(root, catalog);
            EnsureLight(root);
            ApplySkybox(catalog);
            return root;
        }

        static void BuildSky(Transform root, Proto_Greybox3DCatalog catalog)
        {
            var sky = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sky.name = "Greybox3D_Sky";
            sky.transform.SetParent(root, false);
            sky.transform.localScale = Vector3.one * 180f;
            sky.transform.position = Vector3.zero;
            Proto_Greybox3DUtil.DestroySafe(sky.GetComponent<Collider>());
            sky.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedSky;
            sky.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void ApplySkybox(Proto_Greybox3DCatalog catalog)
        {
            if (catalog == null || catalog.SkyboxDay == null) return;
            var shader = Shader.Find("Skybox/Panoramic");
            if (shader == null) return;
            var mat = new Material(shader);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", catalog.SkyboxDay);
            if (mat.HasProperty("_Layout"))
                mat.SetFloat("_Layout", 0f);
            if (mat.HasProperty("_Mapping"))
                mat.SetFloat("_Mapping", 1f);
            RenderSettings.skybox = mat;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.72f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.72f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.38f, 0.22f);
            DynamicGI.UpdateEnvironment();
        }

        static void BuildGround(Transform root)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Greybox3D_Ground";
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -0.12f, 14f);
            ground.transform.localScale = new Vector3(Proto_GreyboxLayout.WorldWidth * 1.6f, 0.24f, 48f);
            var mr = ground.GetComponent<MeshRenderer>();
            mr.sharedMaterial = Proto_Greybox3DMaterials.SharedGround;
        }

        static void BuildMesas(Transform root, Proto_Greybox3DCatalog catalog)
        {
            // Far mesa silhouettes behind the town, left and right of the street.
            PlaceMesa(catalog, root, "MesaL", new Vector3(-11f, 0f, 32f), 3.2f, 180f);
            PlaceMesa(catalog, root, "MesaR", new Vector3(11.5f, 0f, 33f), 3.6f, 15f);
            PlaceMesa(catalog, root, "MesaC", new Vector3(-2f, 0f, 36f), 2.8f, -10f);
            PlaceMesa(catalog, root, "MesaFar", new Vector3(4f, 0f, 40f), 4.2f, 8f);
            PlaceMesa(catalog, root, "MesaFarL", new Vector3(-16f, 0f, 38f), 3.8f, 20f);
        }

        static void PlaceMesa(Proto_Greybox3DCatalog cat, Transform root, string name, Vector3 pos, float scale,
            float yaw)
        {
            var prefab = cat != null ? (cat.CliffLarge != null ? cat.CliffLarge : cat.RockTallA) : null;
            if (prefab != null)
            {
                Proto_Greybox3DSpawn.Prefab(prefab, root, "Greybox3D_" + name, pos, new Vector3(0f, yaw, 0f), scale);
                if (cat.CliffTop != null)
                    Proto_Greybox3DSpawn.Prefab(cat.CliffTop, root, "Greybox3D_" + name + "Top",
                        pos + new Vector3(0f, 1.6f * scale * 0.25f, 0f), new Vector3(0f, yaw + 30f, 0f), scale * 0.85f);
                return;
            }

            var mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Greybox3D_" + name;
            mesa.transform.SetParent(root, false);
            mesa.transform.position = pos + Vector3.up * 2.4f;
            mesa.transform.localScale = new Vector3(6f * scale * 0.35f, 5f, 4f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.Cel(new Color(0.72f, 0.42f, 0.28f), outline: 0.02f);
        }

        static void BuildStorefronts(Transform root)
        {
            int blocks = 7;
            float span = Proto_GreyboxLayout.WorldWidth * 1.15f;
            float blockW = span / blocks;
            float z = Proto_Greybox3DFootMap.FootWorldYToDepthZ(
                Proto_GreyboxLayout.ScreenYPercentToWorldY(32f)) + 3.2f;

            for (int i = 0; i < blocks; i++)
            {
                float cx = -span * 0.5f + blockW * (i + 0.5f);
                BuildOneStore(root, i, cx, z, blockW * 0.92f, AwningColors[i % AwningColors.Length]);
            }

            // Flanking wings slightly closer / angled like the concept sides.
            BuildOneStore(root, 20, -span * 0.55f - 1.2f, z - 3.5f, blockW * 0.9f, AwningColors[0]);
            BuildOneStore(root, 21, span * 0.55f + 1.2f, z - 3.5f, blockW * 0.9f, AwningColors[2]);
            BuildOneStore(root, 22, -span * 0.62f - 1.8f, z - 6.5f, blockW * 0.85f, AwningColors[1]);
            BuildOneStore(root, 23, span * 0.62f + 1.8f, z - 6.5f, blockW * 0.85f, AwningColors[3]);
        }

        static void BuildOneStore(Transform root, int index, float cx, float z, float width, Color awning)
        {
            float height = 3.6f + (index % 3) * 0.35f;
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Greybox3D_Store_" + index;
            face.transform.SetParent(root, false);
            face.transform.position = new Vector3(cx, height * 0.5f, z);
            face.transform.localScale = new Vector3(width, height, 1.8f);
            face.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;

            // western false-front parapet
            var parapet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parapet.name = "Parapet";
            parapet.transform.SetParent(face.transform, false);
            parapet.transform.localPosition = new Vector3(0f, 0.55f, -0.02f);
            parapet.transform.localScale = new Vector3(1.02f, 0.18f, 0.2f);
            parapet.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;

            // awning
            var awn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            awn.name = "Awning";
            awn.transform.SetParent(face.transform, false);
            awn.transform.localPosition = new Vector3(0f, 0.28f, -0.72f);
            awn.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            awn.transform.localScale = new Vector3(1.05f, 0.07f, 0.55f);
            awn.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Awning(awning);

            // awning stripes
            for (int s = 0; s < 4; s++)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.transform.SetParent(awn.transform, false);
                stripe.transform.localPosition = new Vector3(-0.4f + s * 0.26f, 0.6f, 0f);
                stripe.transform.localScale = new Vector3(0.08f, 0.35f, 1.02f);
                stripe.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Cel(Color.Lerp(awning, Color.white, 0.25f), outline: 0.002f);
            }

            // porch posts + boardwalk
            for (int p = -1; p <= 1; p += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.SetParent(face.transform, false);
                post.transform.localPosition = new Vector3(p * 0.38f, -0.15f, -0.62f);
                post.transform.localScale = new Vector3(0.06f, 0.38f, 0.06f);
                post.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;
            }

            var walk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walk.transform.SetParent(face.transform, false);
            walk.transform.localPosition = new Vector3(0f, -0.48f, -0.7f);
            walk.transform.localScale = new Vector3(1.05f, 0.05f, 0.4f);
            walk.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;

            // windows + door
            AddFacadePanel(face.transform, new Vector3(-0.22f, 0.08f, -0.52f), new Vector3(0.22f, 0.22f, 0.04f),
                new Color(0.20f, 0.32f, 0.48f));
            AddFacadePanel(face.transform, new Vector3(0.22f, 0.08f, -0.52f), new Vector3(0.22f, 0.22f, 0.04f),
                new Color(0.20f, 0.32f, 0.48f));
            AddFacadePanel(face.transform, new Vector3(0f, -0.28f, -0.52f), new Vector3(0.18f, 0.42f, 0.04f),
                new Color(0.32f, 0.18f, 0.10f));
        }

        static void AddFacadePanel(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = scale;
            p.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(color, outline: 0.006f);
        }

        static void BuildFence(Transform root, Proto_Greybox3DCatalog catalog)
        {
            float z = Proto_Greybox3DFootMap.FenceDepthZ;
            float gapHalf = Proto_GreyboxLayout.WorldWidth * 0.09f;
            float halfW = Proto_GreyboxLayout.WorldWidth * 0.55f;
            var wood = new Color(0.50f, 0.32f, 0.16f);
            BuildRailFence(root, -halfW, -gapHalf, z, wood);
            BuildRailFence(root, gapHalf, halfW, z, wood);
            foreach (float x in new[] { -gapHalf, gapHalf })
                AddFencePost(root, x, z, 1.35f, 0.14f, wood);

            var prefab = catalog != null ? catalog.Fence : null;
            if (prefab != null)
            {
                void Pack(float x0, float x1)
                {
                    float spacing = 0.85f;
                    int count = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / spacing));
                    for (int i = 0; i < count; i++)
                    {
                        float t = (i + 0.5f) / count;
                        float x = Mathf.Lerp(x0, x1, t);
                        var fence = Proto_Greybox3DSpawn.Prefab(prefab, root, "Greybox3D_FenceKit",
                            new Vector3(x, 0f, z + 0.12f), Vector3.zero, 1.7f);
                        Proto_Greybox3DSpawn.Tint(fence, wood, 0.01f);
                    }
                }
                Pack(-halfW, -gapHalf);
                Pack(gapHalf, halfW);
            }
        }

        static void BuildRailFence(Transform root, float x0, float x1, float z, Color wood)
        {
            if (x1 - x0 < 0.2f) return;
            float width = x1 - x0;
            float mid = (x0 + x1) * 0.5f;
            AddFencePost(root, x0, z, 1.25f, 0.12f, wood);
            AddFencePost(root, x1, z, 1.25f, 0.12f, wood);
            int midPosts = Mathf.Max(0, Mathf.RoundToInt(width / 1.1f) - 1);
            for (int i = 1; i <= midPosts; i++)
                AddFencePost(root, Mathf.Lerp(x0, x1, i / (float)(midPosts + 1)), z, 1.25f, 0.11f, wood);
            foreach (var y in new[] { 0.38f, 0.72f, 1.05f })
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Greybox3D_FenceRail";
                rail.transform.SetParent(root, false);
                rail.transform.position = new Vector3(mid, y, z);
                rail.transform.localScale = new Vector3(width + 0.08f, 0.09f, 0.08f);
                Proto_Greybox3DUtil.DestroySafe(rail.GetComponent<Collider>());
                rail.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(wood, outline: 0.01f);
            }
        }

        static void AddFencePost(Transform root, float x, float z, float height, float thick, Color wood)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "Greybox3D_FencePost";
            post.transform.SetParent(root, false);
            post.transform.position = new Vector3(x, height * 0.5f, z);
            post.transform.localScale = new Vector3(thick, height, thick);
            Proto_Greybox3DUtil.DestroySafe(post.GetComponent<Collider>());
            post.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(wood, outline: 0.01f);
        }

        static void BuildCratesAndBarrels(Transform root, Proto_Greybox3DCatalog catalog)
        {
            PlaceProp(catalog != null ? catalog.CrateLarge : null, root, "Crate",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(54f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(67f), 1.1f, new Vector3(0.8f, 0.8f, 0.8f));
            PlaceProp(catalog != null ? catalog.Crate : null, root, "Crate",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(16f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(58f), 1f, new Vector3(0.7f, 0.7f, 0.7f));
            PlaceProp(catalog != null ? catalog.Crate : null, root, "Crate",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(63f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(72f), 1.15f, new Vector3(0.85f, 0.85f, 0.85f));
            PlaceProp(catalog != null ? catalog.Barrel : null, root, "Barrel",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(22f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(64f), 1f, new Vector3(0.55f, 0.7f, 0.55f));
            PlaceProp(catalog != null ? catalog.Barrel : null, root, "Barrel",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(70f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(60f), 1f, new Vector3(0.55f, 0.7f, 0.55f));
        }

        static void PlaceProp(GameObject prefab, Transform root, string name, float footX, float footY, float scale,
            Vector3 cubeScale)
        {
            var pos = Proto_Greybox3DFootMap.FootToWorld(new Vector2(footX, footY));
            Proto_Greybox3DSpawn.PrefabOrCube(prefab, root, "Greybox3D_" + name, pos, new Vector3(0f, 15f, 0f), scale,
                cubeScale, Proto_Greybox3DMaterials.SharedWood);
        }

        static void BuildCactiRocksGrass(Transform root, Proto_Greybox3DCatalog catalog)
        {
            float fenceZ = Proto_Greybox3DFootMap.FenceDepthZ;
            float[] cactusX = { -6.2f, -4.1f, -2.4f, 3.6f, 5.4f, 7.2f };
            for (int i = 0; i < cactusX.Length; i++)
            {
                var prefab = catalog == null ? null :
                    ((i % 2 == 0 ? catalog.CactusTall : catalog.CactusShort) ?? catalog.CactusTall);
                float z = fenceZ - 1.0f - (i % 3) * 0.7f;
                var cactus = Proto_Greybox3DSpawn.PrefabOrCube(prefab, root, "Greybox3D_Cactus",
                    new Vector3(cactusX[i], 0f, z), Vector3.zero, 1.1f + (i % 3) * 0.15f,
                    new Vector3(0.22f, 1.1f, 0.22f),
                    Proto_Greybox3DMaterials.Cel(new Color(0.22f, 0.62f, 0.28f)));
                Proto_Greybox3DSpawn.Tint(cactus, new Color(0.28f, 0.62f, 0.22f), 0.012f);
            }

            // Side cacti near player like the concept
            var nearC = Proto_Greybox3DSpawn.PrefabOrCube(catalog != null ? catalog.CactusTall : null, root, "Greybox3D_CactusNear",
                new Vector3(-5.4f, 0f, Proto_Greybox3DFootMap.PlayerBandNearZ + 1.2f), Vector3.zero, 1.3f,
                new Vector3(0.25f, 1.3f, 0.25f), Proto_Greybox3DMaterials.Cel(new Color(0.22f, 0.62f, 0.28f)));
            Proto_Greybox3DSpawn.Tint(nearC, new Color(0.28f, 0.62f, 0.22f), 0.012f);

            Vector3[] rockPos =
            {
                new Vector3(-7.2f, 0f, fenceZ - 3f),
                new Vector3(7.6f, 0f, fenceZ - 2.4f),
                new Vector3(-3.2f, 0f, 2.5f),
                new Vector3(4.8f, 0f, 1.8f)
            };
            GameObject[] rocks = catalog == null
                ? new GameObject[0]
                : new[] { catalog.RockSmallA, catalog.RockSmallB, catalog.RockSandA, catalog.RockSmallC };
            for (int i = 0; i < rockPos.Length; i++)
            {
                var prefab = rocks.Length > 0 ? rocks[i % rocks.Length] : null;
                Proto_Greybox3DSpawn.PrefabOrCube(prefab, root, "Greybox3D_Rock", rockPos[i],
                    new Vector3(0f, i * 40f, 0f), 1.2f, new Vector3(0.5f, 0.3f, 0.4f),
                    Proto_Greybox3DMaterials.Cel(new Color(0.62f, 0.48f, 0.32f)));
            }

            if (catalog != null)
            {
                float[] gx = { -5.5f, -1.8f, 1.4f, 5.8f, -3.6f, 3.2f };
                for (int i = 0; i < gx.Length; i++)
                {
                    var g = i % 2 == 0 ? catalog.Grass : catalog.PatchGrass ?? catalog.Grass;
                    var grass = Proto_Greybox3DSpawn.Prefab(g, root, "Greybox3D_Grass",
                        new Vector3(gx[i], 0f, 1.5f + (i % 3) * 2.2f), new Vector3(0f, i * 25f, 0f), 1.1f);
                    Proto_Greybox3DSpawn.Tint(grass, new Color(0.36f, 0.52f, 0.16f), 0.006f);
                }
            }
        }

        static void EnsureLight(Transform root)
        {
            var lightGo = new GameObject("Greybox3D_Sun");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.78f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.None;

            var fillGo = new GameObject("Greybox3D_Fill");
            fillGo.transform.SetParent(root, false);
            fillGo.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.55f, 0.70f, 0.95f);
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
        }
    }
}
