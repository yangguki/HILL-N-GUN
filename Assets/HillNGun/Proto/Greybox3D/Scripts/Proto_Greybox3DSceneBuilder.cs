using HillNGun.Proto.Greybox;
using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DSceneBuilder
    {
        const float PropScale = 12f / 20f; // keep crates/fence/hay with the smaller hero

        static readonly Color[] AwningColors =
        {
            new Color(0.16f, 0.42f, 0.90f),
            new Color(0.98f, 0.62f, 0.10f),
            new Color(0.90f, 0.16f, 0.14f),
            new Color(0.98f, 0.84f, 0.12f),
            new Color(0.20f, 0.50f, 0.88f)
        };

        static readonly Color[] FacadeBrowns =
        {
            new Color(0.55f, 0.32f, 0.16f),
            new Color(0.48f, 0.26f, 0.12f),
            new Color(0.62f, 0.38f, 0.18f),
            new Color(0.42f, 0.22f, 0.10f),
            new Color(0.58f, 0.34f, 0.16f)
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
            // Negative scale flips the default sphere so interior faces render even
            // if the sky shader falls back to a back-face-culled unlit material.
            sky.transform.localScale = Vector3.one * -120f;
            sky.transform.position = Vector3.zero;
            Proto_Greybox3DUtil.DestroySafe(sky.GetComponent<Collider>());
            var skyMr = sky.GetComponent<MeshRenderer>();
            skyMr.sharedMaterial = Proto_Greybox3DMaterials.SharedSky;
            skyMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skyMr.receiveShadows = false;
            BuildClouds(root);
        }

        static void BuildClouds(Transform root)
        {
            // Big cartoon puffs, closer to the concept: white tops, softly shaded undersides.
            // Keep elevation inside the gameplay camera frustum (looks slightly down,
            // so clouds at y=16+ sat above the frame and never read as puffs).
            Vector3[] spots =
            {
                new Vector3(-8.5f, 8.4f, 16f), new Vector3(-2.2f, 9.6f, 19f), new Vector3(6.0f, 8.8f, 17.5f),
                new Vector3(11.0f, 10.0f, 21f), new Vector3(-13.0f, 9.2f, 22f), new Vector3(1.4f, 9.8f, 24f)
            };
            var top = Proto_Greybox3DMaterials.Unlit(new Color(1f, 1f, 1f));
            var shade = Proto_Greybox3DMaterials.Unlit(new Color(0.82f, 0.88f, 0.96f));
            for (int i = 0; i < spots.Length; i++)
            {
                var puff = new GameObject("Greybox3D_Cloud").transform;
                puff.SetParent(root, false);
                puff.position = spots[i];
                float s = 3.5f + (i % 3) * 0.85f;
                AddCloudLobe(puff, Vector3.zero, new Vector3(s * 1.7f, s * 0.85f, s), top);
                AddCloudLobe(puff, new Vector3(0.38f, 0.18f, 0.08f), new Vector3(s * 1.05f, s * 0.75f, s * 0.85f), top);
                AddCloudLobe(puff, new Vector3(-0.36f, 0.12f, -0.06f), new Vector3(s * 0.95f, s * 0.70f, s * 0.80f), top);
                AddCloudLobe(puff, new Vector3(0.08f, -0.16f, 0.04f), new Vector3(s * 1.35f, s * 0.45f, s * 0.90f), shade);
                AddCloudLobe(puff, new Vector3(0.55f, 0.02f, -0.10f), new Vector3(s * 0.70f, s * 0.55f, s * 0.65f), top);
            }
        }

        static void AddCloudLobe(Transform parent, Vector3 local, Vector3 scale, Material mat)
        {
            var lobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lobe.transform.SetParent(parent, false);
            lobe.transform.localPosition = local;
            lobe.transform.localScale = scale;
            Proto_Greybox3DUtil.DestroySafe(lobe.GetComponent<Collider>());
            var r = lobe.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void ApplySkybox(Proto_Greybox3DCatalog catalog)
        {
            // Do not use the pale Kenney panorama as the camera skybox — it
            // washed the upper frame even when the gradient dome was present.
            RenderSettings.skybox = Proto_Greybox3DMaterials.SharedSky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.10f, 0.32f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.95f, 0.62f, 0.30f);
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
            // Layered buttes at two depths, with atmospheric haze on the far row.
            BuildButte(root, "Greybox3D_Mesa", new Vector3(-11.5f, 0f, 26f), 5.4f, 4.6f, 18f, 0.12f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(-6.5f, 0f, 28f), 3.6f, 3.4f, -8f, 0.18f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(0.6f, 0f, 30f), 4.8f, 5.2f, 6f, 0.22f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(7.2f, 0f, 27.5f), 4.2f, 3.8f, -14f, 0.16f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(13.0f, 0f, 29f), 5.8f, 4.8f, 10f, 0.20f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(-16.0f, 0f, 36f), 7.0f, 6.0f, 20f, 0.42f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(-2.0f, 0f, 40f), 8.2f, 6.8f, -6f, 0.50f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(12.5f, 0f, 38f), 6.5f, 5.6f, 12f, 0.46f);
            BuildButte(root, "Greybox3D_Mesa", new Vector3(4.5f, 0f, 44f), 9.0f, 7.2f, 4f, 0.58f);
        }

        static void BuildButte(Transform root, string name, Vector3 pos, float width, float height, float yaw, float haze)
        {
            var butte = new GameObject(name).transform;
            butte.SetParent(root, false);
            butte.position = pos;
            butte.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Five tapering strata, darker at the base, flat top. Haze tints distant
            // buttes toward sky blue so the row has near/far depth like the concept.
            Color[] strata =
            {
                new Color(0.42f, 0.14f, 0.08f),
                new Color(0.58f, 0.20f, 0.10f),
                new Color(0.74f, 0.30f, 0.14f),
                new Color(0.86f, 0.40f, 0.18f),
                new Color(0.92f, 0.52f, 0.26f)
            };
            var hazeCol = new Color(0.42f, 0.62f, 0.92f);
            float y = 0f;
            for (int i = 0; i < strata.Length; i++)
            {
                float t = i / (float)(strata.Length - 1);
                float h = height * (i == 0 ? 0.28f : i == strata.Length - 1 ? 0.12f : 0.20f);
                float w = width * Mathf.Lerp(1f, 0.62f, t);
                // Mid ledge on the 3rd band, like a weathered step.
                if (i == 2) w *= 1.08f;
                var col = Color.Lerp(strata[i], hazeCol, haze);
                var layer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                layer.name = "MesaStratum";
                layer.transform.SetParent(butte, false);
                layer.transform.localPosition = new Vector3(0f, y + h * 0.5f, 0f);
                layer.transform.localScale = new Vector3(w, h * 0.5f, w * 0.78f);
                Proto_Greybox3DUtil.DestroySafe(layer.GetComponent<Collider>());
                var mr = layer.GetComponent<MeshRenderer>();
                mr.sharedMaterial = Proto_Greybox3DMaterials.CelShared(col, haze > 0.35f ? 0.0f : 0.008f);
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                y += h * 0.92f;
            }

            // Flat cap so the silhouette reads as a butte, not a cone.
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "MesaCap";
            cap.transform.SetParent(butte, false);
            cap.transform.localPosition = new Vector3(0f, y + 0.04f, 0f);
            float capW = width * 0.58f;
            cap.transform.localScale = new Vector3(capW, 0.05f, capW * 0.78f);
            Proto_Greybox3DUtil.DestroySafe(cap.GetComponent<Collider>());
            cap.GetComponent<MeshRenderer>().sharedMaterial =
                Proto_Greybox3DMaterials.CelShared(Color.Lerp(new Color(0.90f, 0.55f, 0.28f), hazeCol, haze), 0.0f);
            cap.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void BuildStorefronts(Transform root)
        {
            // Concept: about 3 facades per side, angled along the street with gaps
            // that show desert and cacti, plus 2–3 at the far end.
            float z = Proto_Greybox3DFootMap.FenceDepthZ + 1.6f;
            float zBack = z + 3.4f;
            var left = new[]
            {
                (cx: -7.8f, zz: z + 0.55f, yaw: -14f, w: 1.45f),
                (cx: -5.1f, zz: z + 0.10f, yaw: -8f, w: 1.35f),
                (cx: -2.35f, zz: z + 0.70f, yaw: -12f, w: 1.40f)
            };
            var right = new[]
            {
                (cx: 2.35f, zz: z + 0.65f, yaw: 12f, w: 1.38f),
                (cx: 5.15f, zz: z + 0.15f, yaw: 8f, w: 1.50f),
                (cx: 7.85f, zz: z + 0.50f, yaw: 14f, w: 1.42f)
            };
            var back = new[]
            {
                (cx: -3.8f, zz: zBack, yaw: -4f, w: 1.48f),
                (cx: 0.15f, zz: zBack + 0.45f, yaw: 0f, w: 1.40f),
                (cx: 4.0f, zz: zBack, yaw: 4f, w: 1.46f)
            };
            for (int i = 0; i < left.Length; i++)
                BuildOneStore(root, i, left[i].cx, left[i].zz, left[i].w, AwningColors[i % AwningColors.Length], left[i].yaw);
            for (int i = 0; i < right.Length; i++)
                BuildOneStore(root, 10 + i, right[i].cx, right[i].zz, right[i].w, AwningColors[(i + 2) % AwningColors.Length], right[i].yaw);
            for (int i = 0; i < back.Length; i++)
                BuildOneStore(root, 20 + i, back[i].cx, back[i].zz, back[i].w, AwningColors[(i + 1) % AwningColors.Length], back[i].yaw);
        }

        static void BuildOneStore(Transform root, int index, float cx, float z, float width, Color awning, float yaw = 0f)
        {
            float height = 1.72f + (index % 3) * 0.22f;
            float depth = 1.15f;
            var facade = FacadeBrowns[index % FacadeBrowns.Length];
            var plankA = Color.Lerp(facade, Color.black, 0.08f);
            var plankB = Color.Lerp(facade, new Color(0.70f, 0.48f, 0.24f), 0.18f);
            var trim = new Color(0.28f, 0.14f, 0.07f);

            var store = new GameObject("Greybox3D_Store_" + index).transform;
            store.SetParent(root, false);
            store.position = new Vector3(cx, 0f, z);
            store.rotation = Quaternion.Euler(0f, yaw, 0f);

            // Building mass sits behind the false front so the street face isn't a stacked box.
            var mass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mass.name = "Mass";
            mass.transform.SetParent(store, false);
            mass.transform.localPosition = new Vector3(0f, height * 0.38f, 0.28f);
            mass.transform.localScale = new Vector3(width * 0.92f, height * 0.72f, depth);
            mass.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.CelShared(plankA, 0.008f);
            Proto_Greybox3DUtil.DestroySafe(mass.GetComponent<Collider>());

            // Vertical planks on the false front.
            int boards = 8;
            float plankW = width / boards;
            for (int i = 0; i < boards; i++)
            {
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "Plank";
                plank.transform.SetParent(store, false);
                float x = -width * 0.5f + plankW * (i + 0.5f);
                plank.transform.localPosition = new Vector3(x, height * 0.48f, -depth * 0.48f);
                plank.transform.localScale = new Vector3(plankW * 0.90f, height * 0.96f, 0.07f);
                plank.GetComponent<MeshRenderer>().sharedMaterial =
                    Proto_Greybox3DMaterials.CelShared(i % 2 == 0 ? facade : plankB, 0.004f);
                Proto_Greybox3DUtil.DestroySafe(plank.GetComponent<Collider>());
            }

            // Stepped / peaked false-front parapet above the roof line.
            float parapetH = 0.34f + (index % 2) * 0.10f;
            AddFacadePanel(store, new Vector3(0f, height + parapetH * 0.22f, -depth * 0.48f),
                new Vector3(width * 1.02f, parapetH * 0.55f, 0.08f), trim);
            if (index % 2 == 0)
            {
                // Peaked center step.
                AddFacadePanel(store, new Vector3(0f, height + parapetH * 0.62f, -depth * 0.48f),
                    new Vector3(width * 0.55f, parapetH * 0.42f, 0.09f), facade);
            }
            else
            {
                AddFacadePanel(store, new Vector3(0f, height + parapetH * 0.58f, -depth * 0.48f),
                    new Vector3(width * 0.72f, parapetH * 0.28f, 0.09f), facade);
            }

            // Door + two framed windows with panes.
            var doorCol = new Color(0.28f, 0.14f, 0.07f);
            AddFacadePanel(store, new Vector3(0f, 0.38f, -depth * 0.52f), new Vector3(0.28f, 0.72f, 0.05f), doorCol);
            AddFacadePanel(store, new Vector3(0.08f, 0.36f, -depth * 0.55f), new Vector3(0.04f, 0.04f, 0.03f),
                new Color(0.72f, 0.62f, 0.28f));
            AddWindowWithPanes(store, new Vector3(-width * 0.28f, 0.85f, -depth * 0.52f), 0.28f, 0.28f);
            AddWindowWithPanes(store, new Vector3(width * 0.28f, 0.85f, -depth * 0.52f), 0.28f, 0.28f);

            // Porch, posts, and striped awning.
            AddFacadePanel(store, new Vector3(0f, 0.05f, -depth * 0.78f), new Vector3(width * 1.08f, 0.08f, 0.55f),
                new Color(0.40f, 0.22f, 0.10f));
            for (int p = -1; p <= 1; p += 2)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Post";
                post.transform.SetParent(store, false);
                post.transform.localPosition = new Vector3(p * width * 0.38f, 0.42f, -depth * 0.92f);
                post.transform.localScale = new Vector3(0.07f, 0.38f, 0.07f);
                post.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.CelShared(trim, 0.008f);
                Proto_Greybox3DUtil.DestroySafe(post.GetComponent<Collider>());
            }

            int stripes = 5;
            float awnZ = -depth * 0.95f;
            float awnY = height * 0.62f;
            for (int s = 0; s < stripes; s++)
            {
                var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "AwningStripe";
                stripe.transform.SetParent(store, false);
                float x = -width * 0.5f + (width / stripes) * (s + 0.5f);
                stripe.transform.localPosition = new Vector3(x, awnY, awnZ);
                stripe.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                stripe.transform.localScale = new Vector3(width / stripes * 0.96f, 0.045f, 0.62f);
                var col = s % 2 == 0 ? awning : Color.Lerp(awning, Color.white, 0.55f);
                stripe.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.CelShared(col, 0.004f);
                Proto_Greybox3DUtil.DestroySafe(stripe.GetComponent<Collider>());
            }

            // Sign board on the false front.
            AddFacadePanel(store, new Vector3(0f, height + 0.02f, -depth * 0.56f),
                new Vector3(width * 0.48f, 0.16f, 0.04f), new Color(0.30f, 0.16f, 0.08f));
            AddFacadePanel(store, new Vector3(0f, height + 0.02f, -depth * 0.58f),
                new Vector3(width * 0.36f, 0.08f, 0.03f), new Color(0.90f, 0.72f, 0.28f));
        }

        static void AddWindowWithPanes(Transform parent, Vector3 center, float w, float h)
        {
            var glass = new Color(0.45f, 0.70f, 0.78f);
            var wood = new Color(0.26f, 0.13f, 0.07f);
            AddFacadePanel(parent, center, new Vector3(w, h, 0.03f), glass);
            AddFacadePanel(parent, center, new Vector3(w + 0.04f, 0.03f, 0.04f), wood);
            AddFacadePanel(parent, center + new Vector3(0f, h * 0.5f, 0f), new Vector3(w + 0.04f, 0.03f, 0.04f), wood);
            AddFacadePanel(parent, center + new Vector3(0f, -h * 0.5f, 0f), new Vector3(w + 0.04f, 0.03f, 0.04f), wood);
            AddFacadePanel(parent, center + new Vector3(-w * 0.5f, 0f, 0f), new Vector3(0.03f, h, 0.04f), wood);
            AddFacadePanel(parent, center + new Vector3(w * 0.5f, 0f, 0f), new Vector3(0.03f, h, 0.04f), wood);
            AddFacadePanel(parent, center, new Vector3(0.03f, h, 0.045f), wood);
        }

        static void AddFacadePanel(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = scale;
            p.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.CelShared(color, 0.006f);
            Proto_Greybox3DUtil.DestroySafe(p.GetComponent<Collider>());
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
            light.intensity = 1.08f;
            light.shadows = LightShadows.None;

            var fillGo = new GameObject("Greybox3D_Fill");
            fillGo.transform.SetParent(root, false);
            fillGo.transform.rotation = Quaternion.Euler(18f, 145f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.40f, 0.58f, 0.90f);
            fill.intensity = 0.20f;
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
