using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>
    /// Concept3 street props. Prefers designer v2 cutout/tile textures; falls back to primitives.
    /// Turret / character accessories are deferred; slice I leaves Kenney characters as in slice H.
    /// </summary>
    public static class Proto_Greybox3DProps
    {
        public static void BuildStreetProps(Transform root)
        {
            if (root == null) return;
            var cat = Proto_Greybox3DCatalog.Load();
            BuildCart(root, new Vector3(-3.4f, 0f, 7.4f), 12f, cat);
            BuildCart(root, new Vector3(4.6f, 0f, 11.2f), -18f, cat);
            BuildStall(root, new Vector3(-5.2f, 0f, 5.6f), 8f, cat);
            BuildStall(root, new Vector3(5.6f, 0f, 8.8f), -12f, cat);
            BuildLamp(root, new Vector3(-2.2f, 0f, 4.2f), cat);
            BuildLamp(root, new Vector3(2.6f, 0f, 6.8f), cat);
            BuildLamp(root, new Vector3(-1.4f, 0f, 12.5f), cat);
            BuildRockPile(root, new Vector3(-6.4f, 0f, 9.2f), cat);
            BuildRockPile(root, new Vector3(6.8f, 0f, 13.0f), cat);
            BuildFenceSegment(root, new Vector3(0f, 0f, 15.6f), 0f, cat);
            // Turret mesh deferred: user asked not to apply turret this slice (characters will be replaced later).
        }

        static void BuildCart(Transform root, Vector3 pos, float yaw, Proto_Greybox3DCatalog cat)
        {
            var cart = new GameObject("Greybox3D_Cart").transform;
            cart.SetParent(root, false);
            cart.position = pos;
            cart.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = Proto_Greybox3DMaterials.EnvTile(cat != null ? cat.EnvDeckWood : null, new Vector2(1.2f, 0.8f), new Color(0.72f, 0.48f, 0.26f));
            var dark = Proto_Greybox3DMaterials.CelShared(new Color(0.28f, 0.14f, 0.07f), 0.008f);
            Box(cart, "Bed", new Vector3(0f, 0.38f, 0f), new Vector3(1.15f, 0.12f, 0.62f), wood);
            Box(cart, "SideL", new Vector3(0f, 0.52f, 0.28f), new Vector3(1.15f, 0.22f, 0.06f), wood);
            Box(cart, "SideR", new Vector3(0f, 0.52f, -0.28f), new Vector3(1.15f, 0.22f, 0.06f), wood);
            Box(cart, "Front", new Vector3(0.54f, 0.50f, 0f), new Vector3(0.06f, 0.18f, 0.58f), wood);
            PlaceWheel(cart, "WheelL", new Vector3(-0.38f, 0.20f, 0.34f), cat, dark);
            PlaceWheel(cart, "WheelR", new Vector3(-0.38f, 0.20f, -0.34f), cat, dark);
            PlaceWheel(cart, "WheelL2", new Vector3(0.38f, 0.20f, 0.34f), cat, dark);
            PlaceWheel(cart, "WheelR2", new Vector3(0.38f, 0.20f, -0.34f), cat, dark);
        }

        static void PlaceWheel(Transform parent, string name, Vector3 local, Proto_Greybox3DCatalog cat, Material fallback)
        {
            if (cat != null && cat.EnvPropWheel != null)
            {
                var q = CutoutQuad(parent, name, cat.EnvPropWheel, local, Quaternion.Euler(0f, 0f, 0f), new Vector3(0.42f, 0.42f, 1f), new Vector2(0.5f, 0.5f));
                // Face outward along local Z so the wheel is readable from the street.
                q.localRotation = Quaternion.Euler(0f, 90f, 0f);
                return;
            }
            Cyl(parent, name, local, new Vector3(0.08f, 0.20f, 0.20f), fallback, 90f);
        }

        static void BuildStall(Transform root, Vector3 pos, float yaw, Proto_Greybox3DCatalog cat)
        {
            var stall = new GameObject("Greybox3D_Stall").transform;
            stall.SetParent(root, false);
            stall.position = pos;
            stall.rotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = Proto_Greybox3DMaterials.EnvTile(cat != null ? cat.EnvWallWood : null, new Vector2(1f, 1f), new Color(0.62f, 0.40f, 0.20f));
            var post = Proto_Greybox3DMaterials.CelShared(new Color(0.30f, 0.16f, 0.08f), 0.008f);
            Box(stall, "Counter", new Vector3(0f, 0.42f, 0f), new Vector3(1.05f, 0.10f, 0.55f), wood);
            Box(stall, "Shelf", new Vector3(0f, 0.18f, 0.05f), new Vector3(0.95f, 0.08f, 0.40f), wood);
            Cyl(stall, "PostL", new Vector3(-0.46f, 0.70f, -0.18f), new Vector3(0.07f, 0.70f, 0.07f), post, 0f);
            Cyl(stall, "PostR", new Vector3(0.46f, 0.70f, -0.18f), new Vector3(0.07f, 0.70f, 0.07f), post, 0f);
            Cyl(stall, "PostLB", new Vector3(-0.46f, 0.70f, 0.22f), new Vector3(0.07f, 0.70f, 0.07f), post, 0f);
            Cyl(stall, "PostRB", new Vector3(0.46f, 0.70f, 0.22f), new Vector3(0.07f, 0.70f, 0.07f), post, 0f);

            if (cat != null && cat.EnvAwningStall != null)
            {
                var canopy = Box(stall, "Canopy", new Vector3(0f, 1.18f, 0f), new Vector3(1.12f, 0.04f, 0.85f),
                    Proto_Greybox3DMaterials.EnvTile(cat.EnvAwningStall, new Vector2(1f, 1f), Color.white));
                canopy.localRotation = Quaternion.Euler(12f, 0f, 0f);
                if (cat.EnvAwningStallEdge != null)
                {
                    var edge = CutoutQuad(stall, "CanopyEdge", cat.EnvAwningStallEdge,
                        new Vector3(0f, 1.04f, 0.40f), Quaternion.Euler(8f, 0f, 0f), new Vector3(1.12f, 0.18f, 1f),
                        new Vector2(0.5f, 1f));
                    var mr = edge.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_MainTex"))
                        mr.sharedMaterial.SetTextureScale("_MainTex", new Vector2(1f, 1f));
                }
            }
            else
            {
                int stripes = 8;
                for (int i = 0; i < stripes; i++)
                {
                    var col = i % 2 == 0 ? new Color(0.86f, 0.12f, 0.14f) : new Color(0.96f, 0.94f, 0.90f);
                    var stripe = Box(stall, "CanopyStripe", new Vector3(-0.5f + (i + 0.5f) / stripes * 1.1f, 1.18f, 0f),
                        new Vector3(1.1f / stripes * 1.02f, 0.04f, 0.85f), Proto_Greybox3DMaterials.CelShared(col, 0.003f));
                    stripe.localRotation = Quaternion.Euler(12f, 0f, 0f);
                }
            }
        }

        static void BuildLamp(Transform root, Vector3 pos, Proto_Greybox3DCatalog cat)
        {
            var lamp = new GameObject("Greybox3D_LampPost").transform;
            lamp.SetParent(root, false);
            lamp.position = pos;
            if (cat != null && cat.EnvPropLamp != null)
            {
                // 256x512 sprite, pivot bottom-centre, two lanterns.
                CutoutQuad(lamp, "LampSprite", cat.EnvPropLamp, new Vector3(0f, 0f, 0f),
                    Quaternion.identity, new Vector3(0.7f, 1.4f, 1f), new Vector2(0.5f, 0f));
                if (cat.EnvPropLampGlow != null)
                {
                    var glowMat = Proto_Greybox3DMaterials.Glow(Color.white, 1.6f);
                    if (glowMat.HasProperty("_MainTex")) glowMat.SetTexture("_MainTex", cat.EnvPropLampGlow);
                    Billboard(lamp, "GlowL", new Vector3(-0.12f, 1.15f, 0.02f), Vector3.one * 0.28f, glowMat);
                    Billboard(lamp, "GlowR", new Vector3(0.12f, 1.15f, 0.02f), Vector3.one * 0.28f, glowMat);
                }
                return;
            }
            var wood = Proto_Greybox3DMaterials.CelShared(new Color(0.32f, 0.16f, 0.08f), 0.01f);
            var metal = Proto_Greybox3DMaterials.CelShared(new Color(0.35f, 0.32f, 0.28f), 0.008f);
            var glow = Proto_Greybox3DMaterials.Glow(new Color(1f, 0.82f, 0.35f, 0.95f), 1.8f);
            Cyl(lamp, "Pole", new Vector3(0f, 0.95f, 0f), new Vector3(0.07f, 0.95f, 0.07f), wood, 0f);
            Box(lamp, "Arm", new Vector3(0.18f, 1.82f, 0f), new Vector3(0.36f, 0.05f, 0.05f), metal);
            Box(lamp, "Cage", new Vector3(0.32f, 1.70f, 0f), new Vector3(0.16f, 0.18f, 0.16f), metal);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb";
            bulb.transform.SetParent(lamp, false);
            bulb.transform.localPosition = new Vector3(0.32f, 1.68f, 0f);
            bulb.transform.localScale = Vector3.one * 0.12f;
            Proto_Greybox3DUtil.DestroySafe(bulb.GetComponent<Collider>());
            bulb.GetComponent<MeshRenderer>().sharedMaterial = glow;
        }

        static void BuildRockPile(Transform root, Vector3 pos, Proto_Greybox3DCatalog cat)
        {
            if (cat == null || cat.EnvPropRockPile == null) return;
            var pile = new GameObject("Greybox3D_RockPile").transform;
            pile.SetParent(root, false);
            pile.position = pos;
            CutoutQuad(pile, "Pile", cat.EnvPropRockPile, Vector3.zero, Quaternion.identity,
                new Vector3(1.6f, 0.8f, 1f), new Vector2(0.5f, 0.05f));
        }

        static void BuildFenceSegment(Transform root, Vector3 pos, float yaw, Proto_Greybox3DCatalog cat)
        {
            if (cat == null || cat.EnvFenceWood == null) return;
            var fence = new GameObject("Greybox3D_PropFence").transform;
            fence.SetParent(root, false);
            fence.position = pos;
            fence.rotation = Quaternion.Euler(0f, yaw, 0f);
            // 1 tile ≈ 2 m, 2 posts per tile.
            var mat = Proto_Greybox3DMaterials.Cutout(cat.EnvFenceWood, Color.white, 0.5f, new Vector2(3f, 1f));
            var q = Quad(fence, "FenceSprite", new Vector3(0f, 0.55f, 0f), Quaternion.identity, new Vector3(6f, 1.1f, 1f), mat);
        }

        static void BuildTurretDisplay(Transform root, Vector3 pos, float yaw, Proto_Greybox3DCatalog cat)
        {
            var turret = new GameObject("Greybox3D_TurretDisplay").transform;
            turret.SetParent(root, false);
            turret.position = pos;
            turret.rotation = Quaternion.Euler(0f, yaw, 0f);
            var tex = cat != null ? cat.CharColormapTurret : null;
            var mat = Proto_Greybox3DMaterials.CelShared(new Color(1.1f, 1.1f, 1.1f, 1f), 0.012f, tex);
            // Simple proxy using turret colormap swatches (EYE planar, BODY wrap, S-slots).
            // Not a gameplay enemy.
            var body = Cyl(turret, "Body", new Vector3(0f, 0.55f, 0f), new Vector3(0.70f, 0.50f, 0.70f), mat, 0f);
            StampCylinderBodyUV(body);
            var head = Cyl(turret, "Head", new Vector3(0f, 1.15f, 0f), new Vector3(0.62f, 0.28f, 0.62f), mat, 0f);
            StampCylinderBodyUV(head);
            var eye = Quad(turret, "Eye", new Vector3(0f, 1.15f, 0.32f), Quaternion.identity, new Vector3(0.50f, 0.50f, 1f), mat);
            StampEyeUV(eye);
            Cyl(turret, "Gun", new Vector3(-0.42f, 0.85f, 0.10f), new Vector3(0.12f, 0.28f, 0.12f), mat, 90f);
            Cyl(turret, "LegL", new Vector3(-0.22f, 0.18f, 0.18f), new Vector3(0.08f, 0.22f, 0.08f), mat, 12f);
            Cyl(turret, "LegR", new Vector3(0.22f, 0.18f, 0.18f), new Vector3(0.08f, 0.22f, 0.08f), mat, -12f);
            Cyl(turret, "LegB", new Vector3(0f, 0.18f, -0.24f), new Vector3(0.08f, 0.22f, 0.08f), mat, 0f);
        }

        static void StampEyeUV(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var m = Object.Instantiate(mf.sharedMesh);
            var v = m.vertices;
            var uv = new Vector2[v.Length];
            // Planar map into EYE cell (u0,v0,du,dv) = (0, 0.5, 0.5, 0.5), centre (0.25, 0.75).
            for (int i = 0; i < v.Length; i++)
                uv[i] = new Vector2(0.25f + v[i].x * 0.48f, 0.75f + v[i].y * 0.48f);
            m.uv = uv;
            mf.sharedMesh = m;
        }

        static void StampCylinderBodyUV(Transform t)
        {
            var mf = t.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            var m = Object.Instantiate(mf.sharedMesh);
            var v = m.vertices;
            var uv = new Vector2[v.Length];
            // BODY cell (0.5, 0.5, 0.5, 0.5): U once around, V up the drum.
            for (int i = 0; i < v.Length; i++)
            {
                float ang = Mathf.Atan2(v[i].z, v[i].x);
                float u = 0.5f + (ang / (Mathf.PI * 2f) + 0.5f) * 0.5f;
                float vv = 0.5f + Mathf.Clamp01(v[i].y + 0.5f) * 0.5f;
                uv[i] = new Vector2(u, vv);
            }
            m.uv = uv;
            mf.sharedMesh = m;
        }

        static Transform CutoutQuad(Transform parent, string name, Texture tex, Vector3 local, Quaternion rot, Vector3 scale, Vector2 pivot)
        {
            var mat = Proto_Greybox3DMaterials.Cutout(tex, Color.white, 0.5f, Vector2.one);
            var t = Quad(parent, name, local, rot, scale, mat);
            // Shift so pivot sits at local origin (default quad is centered).
            t.localPosition = local + rot * new Vector3((0.5f - pivot.x) * scale.x, (0.5f - pivot.y) * scale.y, 0f);
            return t;
        }

        static Transform Quad(Transform parent, string name, Vector3 local, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = UnitQuad();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Transform Billboard(Transform parent, string name, Vector3 local, Vector3 scale, Material mat)
        {
            return Quad(parent, name, local, Quaternion.identity, scale, mat);
        }

        static Mesh s_quad;
        static Mesh UnitQuad()
        {
            if (s_quad != null) return s_quad;
            s_quad = new Mesh { name = "Greybox3D_PropQuad" };
            s_quad.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            s_quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            s_quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            s_quad.RecalculateBounds();
            s_quad.RecalculateNormals();
            return s_quad;
        }

        static Transform Box(Transform parent, string name, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            return go.transform;
        }

        static Transform Cyl(Transform parent, string name, Vector3 local, Vector3 scale, Material mat, float xRot)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            return go.transform;
        }
    }
}
