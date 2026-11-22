using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DSceneBuilder
    {
        const float PropScale = 12f / 20f; // keep crates/fence/hay with the smaller hero

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
            BuildClouds(root);
        }

        static void BuildClouds(Transform root)
        {
            Vector3[] spots =
            {
                new Vector3(-8f, 15.5f, 24f), new Vector3(-2f, 17f, 27f), new Vector3(6f, 16.2f, 25f),
                new Vector3(11f, 18f, 29f), new Vector3(-12f, 16.5f, 30f), new Vector3(1.5f, 19f, 32f),
                new Vector3(8f, 14.8f, 22f), new Vector3(-5f, 18.5f, 34f), new Vector3(14f, 17.2f, 33f)
            };
            var mat = Proto_Greybox3DMaterials.CelShared(new Color(1f, 1f, 1f), 0.0f);
            for (int i = 0; i < spots.Length; i++)
            {
                var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Greybox3D_Cloud";
                puff.transform.SetParent(root, false);
                puff.transform.position = spots[i];
                float s = 2.8f + (i % 3) * 0.85f;
                puff.transform.localScale = new Vector3(s * 2.2f, s * 0.85f, s);
                Proto_Greybox3DUtil.DestroySafe(puff.GetComponent<Collider>());
                var r = puff.GetComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // second lobe
                var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lobe.transform.SetParent(puff.transform, false);
                lobe.transform.localPosition = new Vector3(0.35f, 0.15f, 0.1f);
                lobe.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
                Proto_Greybox3DUtil.DestroySafe(lobe.GetComponent<Collider>());
                lobe.GetComponent<MeshRenderer>().sharedMaterial = mat;
                lobe.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var lobe2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                lobe2.transform.SetParent(puff.transform, false);
                lobe2.transform.localPosition = new Vector3(-0.32f, 0.08f, -0.08f);
                lobe2.transform.localScale = new Vector3(0.62f, 0.75f, 0.65f);
                Proto_Greybox3DUtil.DestroySafe(lobe2.GetComponent<Collider>());
                lobe2.GetComponent<MeshRenderer>().sharedMaterial = mat;
                lobe2.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
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
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.40f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.90f, 0.62f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.26f, 0.12f);
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
            Proto_Greybox3DUtil.DestroySafe(ground.GetComponent<Collider>());

            // Subtle sand-color patches so the playfield isn't a flat fill.
            Color[] patches =
            {
                new Color(0.58f, 0.32f, 0.14f),
                new Color(0.62f, 0.36f, 0.16f),
                new Color(0.70f, 0.40f, 0.18f)
            };
            Vector3[] pos =
            {
                new Vector3(-2.4f, 0.01f, 3.2f), new Vector3(3.1f, 0.01f, 5.5f),
                new Vector3(-4.2f, 0.01f, 8.0f), new Vector3(1.2f, 0.01f, 10.5f),
                new Vector3(4.8f, 0.01f, 2.4f), new Vector3(-1.0f, 0.01f, 13f)
            };
            for (int i = 0; i < pos.Length; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                p.name = "Greybox3D_SandPatch";
                p.transform.SetParent(root, false);
                p.transform.position = pos[i];
                p.transform.localScale = new Vector3(1.6f + (i % 3) * 0.5f, 0.012f, 1.1f + (i % 2) * 0.4f);
                Proto_Greybox3DUtil.DestroySafe(p.GetComponent<Collider>());
                p.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.CelShared(patches[i % patches.Length], 0f);
            }
        }

        static void BuildMesas(Transform root, Proto_Greybox3DCatalog catalog)
        {
            // Far mesa silhouettes behind the town, left and right of the street.
            PlaceMesa(catalog, root, "MesaL", new Vector3(-11f, 0f, 32f), 3.2f, 180f);
            PlaceMesa(catalog, root, "MesaL2", new Vector3(-14.5f, 0f, 35f), 2.6f, 160f);
            PlaceMesa(catalog, root, "MesaR", new Vector3(11.5f, 0f, 33f), 3.6f, 15f);
            PlaceMesa(catalog, root, "MesaR2", new Vector3(15.5f, 0f, 36f), 3.0f, 25f);
            PlaceMesa(catalog, root, "MesaC", new Vector3(-2f, 0f, 36f), 2.8f, -10f);
            PlaceMesa(catalog, root, "MesaFar", new Vector3(4f, 0f, 40f), 4.2f, 8f);
            PlaceMesa(catalog, root, "MesaFarL", new Vector3(-16f, 0f, 38f), 3.8f, 20f);
            PlaceMesa(catalog, root, "MesaFarR", new Vector3(8.5f, 0f, 42f), 3.4f, -15f);
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
                AddMesaBands(root, pos, scale);
                return;
            }

            var mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Greybox3D_" + name;
            mesa.transform.SetParent(root, false);
            mesa.transform.position = pos + Vector3.up * 2.4f;
            mesa.transform.localScale = new Vector3(6f * scale * 0.35f, 5f, 4f);
            mesa.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(new Color(0.78f, 0.28f, 0.14f), 0.02f);
            AddMesaBands(root, pos, scale);
            return;
        }

        static void AddMesaBands(Transform root, Vector3 pos, float scale)
        {
            Color[] bands =
            {
                new Color(0.82f, 0.26f, 0.12f),
                new Color(0.58f, 0.16f, 0.08f),
                new Color(0.90f, 0.38f, 0.16f)
            };
            for (int i = 0; i < bands.Length; i++)
            {
                var band = GameObject.CreatePrimitive(PrimitiveType.Cube);
                band.name = "MesaBand";
                band.transform.SetParent(root, false);
                float y = 0.55f * scale + i * 0.55f * scale;
                band.transform.position = pos + new Vector3(0f, y, 0f);
                float w = (3.2f - i * 0.25f) * scale * 0.55f;
                band.transform.localScale = new Vector3(w, 0.16f * scale * 0.35f, w * 0.7f);
                Proto_Greybox3DUtil.DestroySafe(band.GetComponent<Collider>());
                band.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.CelShared(bands[i], 0.008f);
                band.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void BuildStorefronts(Transform root)
        {
            // Concept: wooden facades with colored awnings lining both sides of the
            // street, sized for the smaller BS-scale characters, plus a short back row.
            float z = Proto_Greybox3DFootMap.FenceDepthZ + 1.35f;
            float zBack = z + 2.2f;
            float[] leftX = { -7.15f, -5.55f, -3.95f, -2.40f };
            float[] rightX = { 2.40f, 3.95f, 5.55f, 7.15f };
            float[] backX = { -1.7f, 0.15f, 1.9f };
            for (int i = 0; i < leftX.Length; i++)
                BuildOneStore(root, i, leftX[i], z, 1.22f, AwningColors[i % AwningColors.Length], -6f);
            for (int i = 0; i < rightX.Length; i++)
                BuildOneStore(root, 10 + i, rightX[i], z, 1.22f, AwningColors[(i + 3) % AwningColors.Length], 6f);
            for (int i = 0; i < backX.Length; i++)
                BuildOneStore(root, 20 + i, backX[i], zBack, 1.15f, AwningColors[(i + 1) % AwningColors.Length], 0f);
        }

        static void BuildOneStore(Transform root, int index, float cx, float z, float width, Color awning,
            float yaw = 0f)
        {
            float height = 1.85f + (index % 3) * 0.16f;
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Greybox3D_Store_" + index;
            face.transform.SetParent(root, false);
            face.transform.position = new Vector3(cx, height * 0.5f, z);
            face.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            face.transform.localScale = new Vector3(width, height, 1.55f);
            face.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;
            Proto_Greybox3DUtil.DestroySafe(face.GetComponent<Collider>());

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

            // Horizontal plank hints on the facade (few polys, reads as boarded wood).
            for (int i = 0; i < 3; i++)
                AddFacadePanel(face.transform, new Vector3(0f, -0.36f + i * 0.28f, -0.501f),
                    new Vector3(0.98f, 0.03f, 0.02f), new Color(0.48f, 0.28f, 0.14f));

            // Sign board under the parapet.
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(face.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.42f, -0.58f);
            sign.transform.localScale = new Vector3(0.55f, 0.16f, 0.04f);
            Proto_Greybox3DUtil.DestroySafe(sign.GetComponent<Collider>());
            sign.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(new Color(0.55f, 0.22f, 0.12f), 0.008f);

            // Scalloped awning edge
            for (int s = 0; s < 6; s++)
            {
                var scallop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                scallop.name = "Scallop";
                scallop.transform.SetParent(awn.transform, false);
                scallop.transform.localPosition = new Vector3(-0.42f + s * 0.168f, -0.7f, 0.42f);
                scallop.transform.localScale = new Vector3(0.18f, 1.6f, 0.22f);
                Proto_Greybox3DUtil.DestroySafe(scallop.GetComponent<Collider>());
                scallop.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.Awning(Color.Lerp(awning, Color.black, 0.08f));
            }

            // Window frames
            AddWindowFrame(face.transform, new Vector3(-0.22f, 0.08f, -0.53f), 0.26f, 0.26f);
            AddWindowFrame(face.transform, new Vector3(0.22f, 0.08f, -0.53f), 0.26f, 0.26f);

            // Hanging sign board (icon only, no text)
            var hang = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hang.name = "HangSign";
            hang.transform.SetParent(face.transform, false);
            hang.transform.localPosition = new Vector3(0.28f, 0.05f, -0.82f);
            hang.transform.localScale = new Vector3(0.22f, 0.18f, 0.03f);
            Proto_Greybox3DUtil.DestroySafe(hang.GetComponent<Collider>());
            hang.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(new Color(0.45f, 0.18f, 0.10f), 0.008f);
            var icon = GameObject.CreatePrimitive(index % 2 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            icon.name = "HangIcon";
            icon.transform.SetParent(hang.transform, false);
            icon.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            icon.transform.localScale = new Vector3(0.45f, 0.08f, 0.45f);
            Proto_Greybox3DUtil.DestroySafe(icon.GetComponent<Collider>());
            icon.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(new Color(0.90f, 0.72f, 0.25f), 0.004f);
            var hook = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hook.transform.SetParent(face.transform, false);
            hook.transform.localPosition = new Vector3(0.28f, 0.18f, -0.82f);
            hook.transform.localScale = new Vector3(0.02f, 0.12f, 0.02f);
            Proto_Greybox3DUtil.DestroySafe(hook.GetComponent<Collider>());
            hook.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedWood;
        }

        static void AddWindowFrame(Transform parent, Vector3 center, float w, float h)
        {
            var wood = new Color(0.32f, 0.18f, 0.10f);
            float t = 0.03f;
            AddFacadePanel(parent, center + new Vector3(0f, h * 0.5f, 0f), new Vector3(w + t, t, 0.03f), wood);
            AddFacadePanel(parent, center + new Vector3(0f, -h * 0.5f, 0f), new Vector3(w + t, t, 0.03f), wood);
            AddFacadePanel(parent, center + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(t, h, 0.03f), wood);
            AddFacadePanel(parent, center + new Vector3(w * 0.5f, 0f, 0f), new Vector3(t, h, 0.03f), wood);
        }

        static void AddFacadePanel(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = scale;
            p.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.CelShared(color, 0.006f);
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
                AddFencePost(root, x, z, 1.35f * PropScale, 0.14f * PropScale, wood);

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
                            new Vector3(x, 0f, z + 0.12f), Vector3.zero, 1.7f * PropScale);
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
            AddFencePost(root, x0, z, 1.25f * PropScale, 0.12f * PropScale, wood);
            AddFencePost(root, x1, z, 1.25f * PropScale, 0.12f * PropScale, wood);
            int midPosts = Mathf.Max(0, Mathf.RoundToInt(width / 1.1f) - 1);
            for (int i = 1; i <= midPosts; i++)
                AddFencePost(root, Mathf.Lerp(x0, x1, i / (float)(midPosts + 1)), z, 1.25f * PropScale, 0.11f * PropScale, wood);
            foreach (var y in new[] { 0.38f, 0.72f, 1.05f })
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = "Greybox3D_FenceRail";
                rail.transform.SetParent(root, false);
                rail.transform.position = new Vector3(mid, y, z);
                rail.transform.localScale = new Vector3(width + 0.08f, 0.09f * PropScale, 0.08f * PropScale);
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
                Proto_GreyboxLayout.ScreenYPercentToWorldY(67f), 1.05f, new Vector3(0.85f, 0.85f, 0.85f));
            PlaceProp(catalog != null ? catalog.Crate : null, root, "Crate",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(16f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(58f), 0.85f, new Vector3(0.7f, 0.7f, 0.7f));
            // Cover crate near the player — about chest height on the 1.14 near hero.
            PlaceProp(catalog != null ? catalog.Crate : null, root, "Crate",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(63f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(78f), 1.25f, new Vector3(1.0f, 1.05f, 1.0f));
            PlaceProp(catalog != null ? catalog.Barrel : null, root, "Barrel",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(22f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(64f), 1f * PropScale, new Vector3(0.55f, 0.7f, 0.55f) * PropScale);
            PlaceProp(catalog != null ? catalog.Barrel : null, root, "Barrel",
                Proto_GreyboxLayout.ScreenXPercentToWorldX(70f),
                Proto_GreyboxLayout.ScreenYPercentToWorldY(60f), 1f * PropScale, new Vector3(0.55f, 0.7f, 0.55f) * PropScale);
            BuildHayBales(root);
            BuildWaterTower(root);
        }

        static void BuildHayBales(Transform root)
        {
            var straw = Proto_Greybox3DMaterials.CelShared(new Color(0.78f, 0.62f, 0.22f), 0.008f);
            Vector3[] pos =
            {
                new Vector3(-4.6f, 0.16f, 6.2f),
                new Vector3(-4.15f, 0.16f, 6.7f),
                new Vector3(5.1f, 0.16f, 7.0f)
            };
            for (int i = 0; i < pos.Length; i++)
            {
                var hay = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hay.name = "Greybox3D_Hay";
                hay.transform.SetParent(root, false);
                hay.transform.position = pos[i];
                hay.transform.rotation = Quaternion.Euler(90f, 20f * i, 0f);
                hay.transform.localScale = new Vector3(0.55f, 0.38f, 0.55f) * PropScale;
                Proto_Greybox3DUtil.DestroySafe(hay.GetComponent<Collider>());
                hay.GetComponent<MeshRenderer>().sharedMaterial = straw;
            }
        }

        static void BuildWaterTower(Transform root)
        {
            var wood = Proto_Greybox3DMaterials.SharedWood;
            var tankCol = Proto_Greybox3DMaterials.CelShared(new Color(0.55f, 0.22f, 0.16f), 0.01f);
            var tower = new GameObject("Greybox3D_WaterTower").transform;
            tower.SetParent(root, false);
            tower.position = new Vector3(7.2f, 0f, Proto_Greybox3DFootMap.FenceDepthZ + 3.2f);
            for (int i = 0; i < 4; i++)
            {
                float ang = 45f + i * 90f;
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.transform.SetParent(tower, false);
                float rad = 0.55f;
                leg.transform.localPosition = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * rad, 1.3f,
                    Mathf.Sin(ang * Mathf.Deg2Rad) * rad);
                leg.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
                Proto_Greybox3DUtil.DestroySafe(leg.GetComponent<Collider>());
                leg.GetComponent<MeshRenderer>().sharedMaterial = wood;
            }
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.transform.SetParent(tower, false);
            tank.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            tank.transform.localScale = new Vector3(1.5f, 0.55f, 1.5f);
            Proto_Greybox3DUtil.DestroySafe(tank.GetComponent<Collider>());
            tank.GetComponent<MeshRenderer>().sharedMaterial = tankCol;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.transform.SetParent(tower, false);
            cap.transform.localPosition = new Vector3(0f, 3.35f, 0f);
            cap.transform.localScale = new Vector3(1.35f, 0.45f, 1.35f);
            Proto_Greybox3DUtil.DestroySafe(cap.GetComponent<Collider>());
            cap.GetComponent<MeshRenderer>().sharedMaterial = tankCol;
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
                    Proto_Greybox3DMaterials.Cel(new Color(0.14f, 0.70f, 0.18f)));
                Proto_Greybox3DSpawn.Tint(cactus, new Color(0.16f, 0.68f, 0.18f), 0.012f);
            }

            // Side cacti near player like the concept
            var nearC = Proto_Greybox3DSpawn.PrefabOrCube(catalog != null ? catalog.CactusTall : null, root, "Greybox3D_CactusNear",
                new Vector3(-5.4f, 0f, Proto_Greybox3DFootMap.PlayerBandNearZ + 1.2f), Vector3.zero, 1.3f,
                new Vector3(0.25f, 1.3f, 0.25f), Proto_Greybox3DMaterials.Cel(new Color(0.14f, 0.70f, 0.18f)));
            Proto_Greybox3DSpawn.Tint(nearC, new Color(0.16f, 0.68f, 0.18f), 0.012f);

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

            BuildTumbleweed(root, new Vector3(-3.8f, 0.28f, 4.2f));
            BuildTumbleweed(root, new Vector3(4.4f, 0.28f, 5.0f));
        }

        static void BuildTumbleweed(Transform root, Vector3 pos)
        {
            var tw = new GameObject("Greybox3D_Tumbleweed").transform;
            tw.SetParent(root, false);
            tw.position = pos;
            var mat = Proto_Greybox3DMaterials.CelShared(new Color(0.55f, 0.38f, 0.18f), 0.006f);
            for (int i = 0; i < 3; i++)
            {
                var ring = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ring.transform.SetParent(tw, false);
                ring.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
                ring.transform.localRotation = Quaternion.Euler(i * 60f, i * 40f, 15f);
                Proto_Greybox3DUtil.DestroySafe(ring.GetComponent<Collider>());
                ring.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            tw.gameObject.AddComponent<Proto_Greybox3DSpin>().DegreesPerSecond = 22f;
        }

        static void EnsureLight(Transform root)
        {
            var lightGo = new GameObject("Greybox3D_Sun");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.84f, 0.62f);
            light.intensity = 1.02f;
            light.shadows = LightShadows.None;

            var fillGo = new GameObject("Greybox3D_Fill");
            fillGo.transform.SetParent(root, false);
            fillGo.transform.rotation = Quaternion.Euler(18f, 145f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.40f, 0.58f, 0.90f);
            fill.intensity = 0.22f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.46f, 0.30f, 0.16f);
            RenderSettings.fog = false;
        }
    }

    sealed class Proto_Greybox3DSpin : MonoBehaviour
    {
        public float DegreesPerSecond = 20f;
        void Update()
        {
            transform.Rotate(0f, DegreesPerSecond * Time.deltaTime, DegreesPerSecond * 0.35f * Time.deltaTime, Space.World);
        }
    }
}
