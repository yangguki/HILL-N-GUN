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

        public static Transform BuildHero(Transform parent) => BuildCowboy(parent, "Greybox3D_Hero", true);

        public static Transform BuildGrunt(Transform parent) => BuildCowboy(parent, "Greybox3D_Grunt", false);

        static Transform BuildCowboy(Transform parent, string name, bool hero)
        {
            var catalog = Proto_Greybox3DCatalog.Load();
            Transform root = null;
            if (catalog != null && catalog.HasCowboy)
                root = InstantiateCowboy(parent, name, catalog, hero);
            if (root == null)
            {
                Debug.LogWarning(
                    $"[Greybox3D] {name} using primitive fallback. catalog={(catalog != null)} hasCowboy={(catalog != null && catalog.HasCowboy)}");
                root = BuildFallback(parent, name, hero);
            }

            AttachBlob(root);
            return root;
        }

        static Transform InstantiateCowboy(Transform parent, string name, Proto_Greybox3DCatalog catalog, bool hero)
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
            Recolor(go, hero);
            var animator = go.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            var driver = go.GetComponent<Proto_Greybox3DAnimDriver>() ?? go.AddComponent<Proto_Greybox3DAnimDriver>();
            driver.Bind(animator, catalog);
            FitHeight(go.transform, 1f);
            DressCowboy(go.transform, hero);
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

        static void Recolor(GameObject go, bool hero)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var shared = r.sharedMaterials;
                var mats = new Material[shared.Length];
                for (int i = 0; i < shared.Length; i++)
                {
                    string n = shared[i] != null ? shared[i].name : "";
                    mats[i] = Proto_Greybox3DMaterials.Cel(ColorFor(n, hero), outline: 0.012f);
                }
                r.sharedMaterials = mats;
            }
        }

        static Color ColorFor(string materialName, bool hero)
        {
            string n = materialName.ToLowerInvariant();
            if (n.Contains("hathlight") || n.Contains("hatlight") || n.Contains("hat_light"))
                return hero ? HeroHatLight : GruntHatLight;
            if (n.Contains("hat"))
                return hero ? HeroHat : GruntHat;
            if (n.Contains("jacket") || n.Contains("top") || n.Contains("shirt"))
                return hero ? HeroShirt : GruntShirt;
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
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.16f, 0.42f), hat);
            AddPart(root, PrimitiveType.Cylinder, new Vector3(0f, 1.00f, 0f), new Vector3(0.85f, 0.035f, 0.85f), hat);
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

        static void DressCowboy(Transform model, bool hero)
        {
            var head = FindBone(model, "Head");
            var neck = FindBone(model, "Neck") ?? head;
            var hips = FindBone(model, "Hips") ?? model;
            var fist = FindBone(model, "Fist.R", "Hand.R", "LowerArm.R");

            Color hat = hero ? HeroHat : GruntHat;
            Color hatLight = hero ? HeroHatLight : GruntHatLight;
            Color scarf = hero ? HeroScarf : GruntScarf;

            // Wide-brim hat with a red band, world-up, covering the pack's small hat.
            if (head != null)
            {
                var hatRoot = NewPart(head, "DressHat");
                SnapUpright(hatRoot, head.position + Vector3.up * 0.08f);
                SetWorldScale(hatRoot, Vector3.one);
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.10f, 0f),
                    new Vector3(0.38f, 0.10f, 0.38f), hat);          // crown
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f),
                    new Vector3(0.72f, 0.022f, 0.72f), hatLight);    // wide brim (not a platter)
                AddPrimitive(hatRoot, PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f),
                    new Vector3(0.40f, 0.018f, 0.40f), scarf);       // red band

                // Dark brown hair so the back of the head isn't a bald flesh oval.
                var hair = NewPart(head, "DressHair");
                SnapUpright(hair, head.position + Vector3.up * 0.02f + Vector3.back * 0.02f);
                SetWorldScale(hair, Vector3.one);
                AddPrimitive(hair, PrimitiveType.Sphere, new Vector3(0f, 0.02f, -0.02f),
                    new Vector3(0.34f, 0.26f, 0.32f), HeroHair);
            }

            // Bandana knot visible from behind, at the nape.
            if (neck != null)
            {
                var knot = NewPart(neck, "DressBandanaKnot");
                SnapUpright(knot, neck.position + Vector3.back * 0.07f + Vector3.up * 0.02f);
                SetWorldScale(knot, Vector3.one);
                AddPrimitive(knot, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f),
                    new Vector3(0.07f, 0.06f, 0.07f), scarf);
                AddPrimitive(knot, PrimitiveType.Cube, new Vector3(-0.04f, -0.03f, -0.01f),
                    new Vector3(0.035f, 0.07f, 0.02f), scarf);
                AddPrimitive(knot, PrimitiveType.Cube, new Vector3(0.04f, -0.03f, -0.01f),
                    new Vector3(0.035f, 0.07f, 0.02f), scarf);
            }

            // Belt + holster on the right hip.
            var belt = NewPart(hips, "DressBelt");
            SnapUpright(belt, hips.position + Vector3.up * 0.05f);
            SetWorldScale(belt, Vector3.one);
            AddPrimitive(belt, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f),
                new Vector3(0.38f, 0.018f, 0.26f), new Color(0.18f, 0.10f, 0.06f));
            AddPrimitive(belt, PrimitiveType.Cube, new Vector3(0.14f, -0.06f, 0.02f),
                new Vector3(0.08f, 0.14f, 0.05f), new Color(0.16f, 0.09f, 0.05f));

            if (hero && fist != null)
            {
                var gun = NewPart(fist, "DressRevolver");
                gun.localPosition = Vector3.zero;
                gun.localRotation = Quaternion.identity;
                SetWorldScale(gun, Vector3.one);
                // Barrel along the fist; a modest local offset so it reads in the right hand.
                AddPrimitive(gun, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.05f),
                    new Vector3(0.035f, 0.06f, 0.08f), new Color(0.16f, 0.16f, 0.18f));
                AddPrimitive(gun, PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0.14f),
                    new Vector3(0.03f, 0.08f, 0.03f), new Color(0.18f, 0.18f, 0.20f));
                gun.localRotation = Quaternion.Euler(90f, 0f, 0f);
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
