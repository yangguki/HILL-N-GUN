using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DCharacterMesh
    {
        static readonly Color HeroHat = new Color(0.45f, 0.14f, 0.72f);
        static readonly Color HeroHatLight = new Color(0.58f, 0.22f, 0.85f);
        static readonly Color HeroShirt = new Color(0.50f, 0.16f, 0.78f);
        static readonly Color HeroPants = new Color(0.16f, 0.32f, 0.78f);
        static readonly Color HeroScarf = new Color(0.92f, 0.12f, 0.14f);
        static readonly Color HeroHair = new Color(0.12f, 0.07f, 0.04f);
        static readonly Color HeroSkin = new Color(0.92f, 0.74f, 0.58f);
        static readonly Color HeroFace = new Color(0.96f, 0.80f, 0.64f);

        static readonly Color GruntHat = new Color(0.42f, 0.28f, 0.14f);
        static readonly Color GruntHatLight = new Color(0.62f, 0.42f, 0.22f);
        static readonly Color GruntShirt = new Color(0.62f, 0.38f, 0.20f);
        static readonly Color GruntPants = new Color(0.16f, 0.34f, 0.76f);
        static readonly Color GruntScarf = new Color(0.90f, 0.12f, 0.14f);

        // Shared outfit (brown hat, red bandana, warm shirt, blue jeans) with 3 tint/hat cuts.
        static readonly Color[] GruntHatV = { new Color(0.42f, 0.28f, 0.14f), new Color(0.55f, 0.38f, 0.18f), new Color(0.28f, 0.18f, 0.10f) };
        static readonly Color[] GruntHatLightV = { new Color(0.62f, 0.42f, 0.22f), new Color(0.72f, 0.52f, 0.28f), new Color(0.45f, 0.30f, 0.16f) };
        static readonly Color[] GruntShirtV = { new Color(0.62f, 0.38f, 0.20f), new Color(0.72f, 0.32f, 0.16f), new Color(0.70f, 0.52f, 0.22f) };

        public static Transform BuildHero(Transform parent) => BuildCowboy(parent, "Greybox3D_Hero", true, 0);

        public static Transform BuildGrunt(Transform parent, int variant = 0) =>
            BuildCowboy(parent, "Greybox3D_Grunt", false, variant);

        static Transform BuildCowboy(Transform parent, string name, bool hero, int variant)
        {
            var catalog = Proto_Greybox3DCatalog.Load();
            Transform root = null;
            if (catalog != null && catalog.HasCowboy)
                root = InstantiateCowboy(parent, name, catalog, hero, variant);
            if (root == null)
            {
                Debug.LogWarning(
                    $"[Greybox3D] {name} using primitive fallback. catalog={(catalog != null)} hasCowboy={(catalog != null && catalog.HasCowboy)}");
                root = BuildFallback(parent, name, hero);
            }

            AttachBlob(root);
            if (root.GetComponent<Proto_Greybox3DCombatFx>() == null)
                root.gameObject.AddComponent<Proto_Greybox3DCombatFx>();
            return root;
        }

        static Transform InstantiateCowboy(Transform parent, string name, Proto_Greybox3DCatalog catalog, bool hero, int variant)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var go = Object.Instantiate(catalog.CowboyMale, root);
            go.name = "Model";
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            StripColliders(go);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                smr.updateWhenOffscreen = true;
                smr.enabled = true;
                if (smr.sharedMesh == null)
                    Debug.LogWarning($"[Greybox3D] {name} skinned renderer '{smr.name}' has no mesh.");
            }
            Recolor(go, hero, variant);
            var animator = go.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            var driver = go.GetComponent<Proto_Greybox3DAnimDriver>() ?? go.AddComponent<Proto_Greybox3DAnimDriver>();
            driver.Bind(animator, catalog);
            FitHeight(go.transform, 1f);
            DressCowboy(go.transform, root, hero, variant);
            var rends = go.GetComponentsInChildren<Renderer>(true);
            int skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
            var b = new Bounds();
            if (rends.Length > 0)
            {
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            }
            var matNames = new System.Collections.Generic.List<string>();
            foreach (var r in rends)
            {
                foreach (var m in r.sharedMaterials)
                    matNames.Add(m != null ? m.name : "null");
            }
            Debug.Log(
                $"[Greybox3D] {name} cowboy='{catalog.CowboyMale.name}' rends={rends.Length} skinned={skinned} bounds={b.size} pos={go.transform.position} mats={string.Join(",", matNames)}");
            if (rends.Length == 0)
            {
                Proto_Greybox3DUtil.DestroySafe(root.gameObject);
                return null;
            }
            return root;
        }

        static void FitHeight(Transform t, float targetHeight)
        {
            t.localScale = Vector3.one;
            t.localPosition = Vector3.zero;
            var rends = t.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            var bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);
            if (bounds.size.y < 0.01f) return;
            t.localScale = Vector3.one * (targetHeight / bounds.size.y);
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++)
                bounds.Encapsulate(rends[i].bounds);
            var worldDelta = new Vector3(0f, -bounds.min.y, 0f);
            if (t.parent != null)
                t.localPosition += t.parent.InverseTransformVector(worldDelta);
            else
                t.position += worldDelta;
        }

        static void Recolor(GameObject go, bool hero, int variant = 0)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var shared = r.sharedMaterials;
                var mats = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    string n = shared[i] != null ? shared[i].name : "";
                    mats[i] = Proto_Greybox3DMaterials.Cel(ColorFor(n, hero, variant), outline: 0.012f);
                }
                r.sharedMaterials = mats;
            }
        }

        static Color ColorFor(string materialName, bool hero, int variant)
        {
            string n = materialName.ToLowerInvariant();
            int v = Mathf.Abs(variant) % 3;
            if (n.Contains("hathlight") || n.Contains("hatlight") || n.Contains("hat_light"))
                return hero ? HeroHatLight : GruntHatLightV[v];
            if (n.Contains("hat"))
                return hero ? HeroHat : GruntHatV[v];
            if (n.Contains("jacket") || n.Contains("top") || n.Contains("shirt"))
                return hero ? HeroShirt : GruntShirtV[v];
            if (n.Contains("pant") || n.Contains("jean") || n.Contains("leg"))
                return hero ? HeroPants : GruntPants;
            if (n.Contains("scarf") || n.Contains("bandana") || n.Contains("kerchief"))
                return hero ? HeroScarf : GruntScarf;
            if (n.Contains("hair"))
                return HeroHair;
            if (n.Contains("face"))
                return HeroFace;
            if (n.Contains("skin"))
                return HeroSkin;
            if (n.Contains("gun") || n.Contains("metal") || n.Contains("revolver"))
                return new Color(0.18f, 0.18f, 0.20f);
            return hero ? HeroShirt : GruntShirt;
        }

        static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                Proto_Greybox3DUtil.DestroySafe(c);
        }

        static void AttachBlob(Transform root)
        {
            var blob = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blob.name = "BlobShadow";
            blob.transform.SetParent(root, false);
            blob.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            blob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            blob.transform.localScale = new Vector3(0.85f, 0.55f, 1f);
            Proto_Greybox3DUtil.DestroySafe(blob.GetComponent<Collider>());
            blob.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.SharedBlob;
        }

        static Transform BuildFallback(Transform parent, string name, bool hero)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            Color hat = hero ? HeroHat : GruntHat;
            Color shirt = hero ? HeroShirt : GruntShirt;
            Color pants = hero ? HeroPants : GruntPants;
            Color scarf = hero ? HeroScarf : GruntScarf;
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.40f, 0.36f, 0.32f), pants);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 0.52f, 0f), new Vector3(0.44f, 0.28f, 0.38f), shirt);
            AddPart(root, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0.02f), new Vector3(0.36f, 0.36f, 0.34f), HeroFace);
            AddPart(root, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.14f), new Vector3(0.28f, 0.07f, 0.06f), scarf);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.12f, 0f), new Vector3(0.30f, 0.12f, 0.30f), hat);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.00f, 0f), new Vector3(0.52f, 0.022f, 0.52f), hat);
            AddPart(root, PrimitiveType.Cube, new Vector3(0.28f, 0.42f, 0.16f), new Vector3(0.08f, 0.05f, 0.28f),
                new Color(0.18f, 0.18f, 0.20f));
            return root;
        }

        static void AddPart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(color);
        }

        static void DressCowboy(Transform model, Transform body, bool hero, int variant)
        {
            var head = FindBone(model, "Head");
            var neck = FindBone(model, "Neck") ?? head;
            var hips = FindBone(model, "Hips") ?? model;
            var fist = FindBone(model, "Fist.R", "Hand.R", "LowerArm.R");
            int v = Mathf.Abs(variant) % 3;

            Color hat = hero ? HeroHat : GruntHatV[v];
            Color hatLight = hero ? HeroHatLight : GruntHatLightV[v];
            Color scarf = hero ? HeroScarf : GruntScarf;
            float brim = hero ? 0.48f : (v == 1 ? 0.54f : v == 2 ? 0.42f : 0.48f);
            float crown = hero ? 0.28f : (v == 2 ? 0.26f : 0.29f);

            // Parent dress to the body and follow bones in LateUpdate so bone scale cannot drift the pieces.
            if (head != null)
            {
                var hatRoot = Proto_Greybox3DDressFollow.Attach(head, body, "DressHat", 0.08f, 0f, 0f, Vector3.one, true);
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.10f, 0f),
                    new Vector3(crown, 0.085f, crown), hat);
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                    new Vector3(brim, 0.016f, brim), hatLight);
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f),
                    new Vector3(crown + 0.02f, 0.016f, crown + 0.02f), scarf);

                var hair = Proto_Greybox3DDressFollow.Attach(head, body, "DressHair", 0.02f, -0.04f, 0f, Vector3.one, true);
                AddPrimitive(hair, PrimitiveType.Sphere, new Vector3(0f, 0.02f, -0.02f),
                    new Vector3(0.34f, 0.26f, 0.32f), HeroHair);
            }

            if (neck != null)
            {
                // Knot sits on the nape, clearly visible from the camera behind the hero.
                var knot = Proto_Greybox3DDressFollow.Attach(neck, body, "DressBandanaKnot", 0.02f, -0.09f, 0f, Vector3.one, true);
                AddPrimitive(knot, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f),
                    new Vector3(0.10f, 0.08f, 0.10f), scarf);
                AddPrimitive(knot, PrimitiveType.Cube, new Vector3(-0.05f, -0.05f, -0.02f),
                    new Vector3(0.045f, 0.10f, 0.025f), scarf);
                AddPrimitive(knot, PrimitiveType.Cube, new Vector3(0.05f, -0.05f, -0.02f),
                    new Vector3(0.045f, 0.10f, 0.025f), scarf);
            }

            var belt = Proto_Greybox3DDressFollow.Attach(hips, body, "DressBelt", 0.05f, 0f, 0f, Vector3.one, true);
            AddPrimitive(belt, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f),
                new Vector3(0.38f, 0.018f, 0.26f), new Color(0.18f, 0.10f, 0.06f));
            AddPrimitive(belt, PrimitiveType.Cube, new Vector3(0.14f, -0.06f, 0.02f),
                new Vector3(0.08f, 0.14f, 0.05f), new Color(0.16f, 0.09f, 0.05f));

            if (hero && fist != null)
            {
                var gun = Proto_Greybox3DDressFollow.AttachLocal(fist, "DressRevolver",
                    new Vector3(0f, 0.02f, 0.04f), Quaternion.Euler(90f, 0f, 0f), Vector3.one);
                AddPrimitive(gun, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.05f),
                    new Vector3(0.035f, 0.06f, 0.08f), new Color(0.16f, 0.16f, 0.18f));
                AddPrimitive(gun, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0.14f),
                    new Vector3(0.03f, 0.08f, 0.03f), new Color(0.18f, 0.18f, 0.20f));
            }
        }

        static Transform FindBone(Transform root, params string[] names)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var n in names)
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name == n) return all[i];
            }
            foreach (var n in names)
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i].name.EndsWith(n) || all[i].name.Contains(n))
                        return all[i];
            }
            return null;
        }

        static Transform NewPart(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            return t;
        }

        static void SnapUpright(Transform t, Vector3 worldPos)
        {
            t.position = worldPos;
            t.rotation = Quaternion.identity;
        }

        static void SetWorldScale(Transform t, Vector3 world)
        {
            var ls = t.lossyScale;
            t.localScale = new Vector3(
                world.x / Mathf.Max(1e-5f, ls.x),
                world.y / Mathf.Max(1e-5f, ls.y),
                world.z / Mathf.Max(1e-5f, ls.z));
        }

        static void AddPrimitive(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 worldScale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            Proto_Greybox3DUtil.DestroySafe(go.GetComponent<Collider>());
            SetWorldScale(go.transform, worldScale);
            go.GetComponent<MeshRenderer>().sharedMaterial = Proto_Greybox3DMaterials.Cel(color, outline: 0.012f);
        }
    }
}
