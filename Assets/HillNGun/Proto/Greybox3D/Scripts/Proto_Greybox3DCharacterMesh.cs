using UnityEngine;

namespace HillNGun.Proto.Greybox3D
{
    public static class Proto_Greybox3DCharacterMesh
    {
        static readonly Color HeroHat = new Color(0.48f, 0.12f, 0.78f);
        static readonly Color HeroHatLight = new Color(0.62f, 0.22f, 0.88f);
        static readonly Color HeroShirt = new Color(0.55f, 0.16f, 0.82f);
        static readonly Color HeroPants = new Color(0.16f, 0.38f, 0.82f);
        static readonly Color HeroScarf = new Color(0.92f, 0.10f, 0.14f);
        static readonly Color HeroHair = new Color(0.28f, 0.14f, 0.07f);
        static readonly Color HeroSkin = new Color(0.94f, 0.74f, 0.58f);
        static readonly Color HeroFace = new Color(0.97f, 0.80f, 0.64f);
        static readonly Color HeroBoot = new Color(0.36f, 0.18f, 0.08f);
        static readonly Color Leather = new Color(0.28f, 0.14f, 0.06f);
        static readonly Color Silver = new Color(0.78f, 0.80f, 0.86f);
        static readonly Color SilverDark = new Color(0.48f, 0.50f, 0.56f);

        static readonly Color GruntHat = new Color(0.42f, 0.28f, 0.14f);
        static readonly Color GruntHatLight = new Color(0.62f, 0.42f, 0.22f);
        static readonly Color GruntShirt = new Color(0.70f, 0.42f, 0.22f);
        static readonly Color GruntPants = new Color(0.16f, 0.36f, 0.80f);
        static readonly Color GruntScarf = new Color(0.90f, 0.10f, 0.12f);
        static readonly Color GruntBoot = new Color(0.32f, 0.16f, 0.08f);

        // Shared outfit (brown hat, red bandana, warm shirt, blue jeans) with 3 tint/hat cuts.
        static readonly Color[] GruntHatV = { new Color(0.42f, 0.28f, 0.14f), new Color(0.55f, 0.38f, 0.18f), new Color(0.28f, 0.18f, 0.10f) };
        static readonly Color[] GruntHatLightV = { new Color(0.62f, 0.42f, 0.22f), new Color(0.72f, 0.52f, 0.28f), new Color(0.45f, 0.30f, 0.16f) };
        static readonly Color[] GruntShirtV = { new Color(0.66f, 0.32f, 0.16f), new Color(0.74f, 0.48f, 0.24f), new Color(0.58f, 0.36f, 0.18f) };
        static readonly Color[] GruntVestV = { new Color(0.58f, 0.22f, 0.12f), new Color(0.62f, 0.34f, 0.16f), new Color(0.48f, 0.26f, 0.12f) };

        public static Transform BuildHero(Transform parent) => BuildCowboy(parent, "Greybox3D_Hero", true, 0);

        public static Transform BuildGrunt(Transform parent, int variant = 0) =>
            BuildCowboy(parent, "Greybox3D_Grunt", false, variant);

        static Transform BuildCowboy(Transform parent, string name, bool hero, int variant)
        {
            var catalog = Proto_Greybox3DCatalog.Load();
            Transform root = null;
            if (catalog != null && catalog.HasMini)
                root = InstantiateMini(parent, name, catalog, hero, variant);
            else if (catalog != null && catalog.HasCowboy)
                root = InstantiateCowboy(parent, name, catalog, hero, variant);
            if (root == null)
            {
                Debug.LogWarning(
                    $"[Greybox3D] {name} using primitive fallback. catalog={(catalog != null)} hasMini={(catalog != null && catalog.HasMini)} hasCowboy={(catalog != null && catalog.HasCowboy)}");
                root = BuildFallback(parent, name, hero);
            }

            AttachBlob(root);
            if (root.GetComponent<Proto_Greybox3DCombatFx>() == null)
                root.gameObject.AddComponent<Proto_Greybox3DCombatFx>();
            return root;
        }

        static Transform InstantiateMini(Transform parent, string name, Proto_Greybox3DCatalog catalog, bool hero, int variant)
        {
            var source = hero ? catalog.MiniHero : catalog.MiniGrunt;
            if (source == null) return null;

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var go = Object.Instantiate(source, root);
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

            var pal = hero
                ? (catalog.MiniColormapHero != null ? catalog.MiniColormapHero : catalog.MiniColormap)
                : GruntPalette(catalog, variant);
            var bodyMat = Proto_Greybox3DMaterials.CelShared(Color.white, 0.014f, pal);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = bodyMat;

            FitHeight(go.transform, 1f);
            AttachMiniGun(go.transform, catalog, hero);

            var animator = go.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = go.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            var driver = go.GetComponent<Proto_Greybox3DAnimDriver>() ?? go.AddComponent<Proto_Greybox3DAnimDriver>();
            driver.Bind(animator, catalog, useMini: true);

            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0)
            {
                Proto_Greybox3DUtil.DestroySafe(root.gameObject);
                return null;
            }
            return root;
        }

        static Texture GruntPalette(Proto_Greybox3DCatalog catalog, int variant)
        {
            int v = Mathf.Abs(variant) % 3;
            if (v == 1 && catalog.MiniColormapGruntB != null) return catalog.MiniColormapGruntB;
            if (v == 2 && catalog.MiniColormapGruntC != null) return catalog.MiniColormapGruntC;
            return catalog.MiniColormapGrunt != null ? catalog.MiniColormapGrunt : catalog.MiniColormap;
        }

        static void AttachMiniGun(Transform model, Proto_Greybox3DCatalog catalog, bool hero)
        {
            if (catalog.MiniGun == null) return;
            // Hip-holstered on the torso so the gun is readable from the default
            // behind-the-hero camera (arm-right hang hid it inside the body).
            var hold = FindBone(model, "torso", "Torso") ??
                       FindBone(model, "arm-right", "Arm-Right", "arm.right");
            if (hold == null) return;
            var gun = Object.Instantiate(catalog.MiniGun, hold);
            gun.name = "MiniGun";
            gun.transform.localPosition = new Vector3(0.22f, -0.08f, 0.12f);
            gun.transform.localRotation = Quaternion.Euler(-80f, 90f, 0f);
            gun.transform.localScale = Vector3.one * 0.9f;
            StripColliders(gun);
            var gunTex = hero && catalog.MiniColormapHero != null ? catalog.MiniColormapHero : catalog.MiniColormap;
            var gunMat = Proto_Greybox3DMaterials.CelShared(new Color(0.85f, 0.86f, 0.90f), 0.012f, gunTex);
            foreach (var r in gun.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = gunMat;
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
            HideSourceRenderers(go);
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
                return Silver;
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

        static void HideSourceRenderers(GameObject model)
        {
            // Keep bones + Animator, hide the adult-proportioned pack mesh
            // so the chubby chibi dress is the visible silhouette.
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
        }

        static void DressCowboy(Transform model, Transform body, bool hero, int variant)
        {
            // Static chubby chibi in local space of the 1-unit body so the
            // silhouette stays readable from behind: wide hat, hair cap,
            // bandana tails, short purple shirt, belt/holster, blue jeans, boots.
            int v = Mathf.Abs(variant) % 3;
            Color hat = hero ? HeroHat : GruntHatV[v];
            Color hatLight = hero ? HeroHatLight : GruntHatLightV[v];
            Color scarf = hero ? HeroScarf : GruntScarf;
            Color shirt = hero ? HeroShirt : GruntShirtV[v];
            Color vest = hero ? HeroShirt : GruntVestV[v];
            Color pants = hero ? HeroPants : GruntPants;
            Color boot = hero ? HeroBoot : GruntBoot;
            Color skin = HeroSkin;
            Color face = HeroFace;
            Color hair = HeroHair;

            float brim = hero ? 0.78f : (v == 1 ? 0.62f : v == 2 ? 0.52f : 0.58f);
            float crown = hero ? 0.30f : (v == 2 ? 0.24f : 0.28f);

            var dress = NewPart(body, "DressRoot");

            var boots = NewPart(dress, "DressBoots");
            Proto_Greybox3DSmoothPrim.Sphere(boots, "DressBootL", new Vector3(-0.09f, 0.055f, 0.05f),
                new Vector3(0.14f, 0.10f, 0.20f), boot, 0.012f);
            Proto_Greybox3DSmoothPrim.Sphere(boots, "DressBootR", new Vector3(0.09f, 0.055f, 0.05f),
                new Vector3(0.14f, 0.10f, 0.20f), boot, 0.012f);

            Proto_Greybox3DSmoothPrim.CapsuleY(dress, "DressLegL", new Vector3(-0.09f, 0.22f, 0.02f),
                new Vector3(0.16f, 0.18f, 0.16f), pants, 0.012f);
            Proto_Greybox3DSmoothPrim.CapsuleY(dress, "DressLegR", new Vector3(0.09f, 0.22f, 0.02f),
                new Vector3(0.16f, 0.18f, 0.16f), pants, 0.012f);

            var belt = NewPart(dress, "DressBelt");
            belt.localPosition = new Vector3(0f, 0.36f, 0.01f);
            Proto_Greybox3DSmoothPrim.DiskY(belt, "Belt", Vector3.zero, new Vector3(0.34f, 0.05f, 0.26f), Leather, 0.01f);
            Proto_Greybox3DSmoothPrim.Part(belt, "Buckle", Proto_Greybox3DSmoothPrim.SphereMesh(),
                new Vector3(0f, 0f, 0.13f), new Vector3(0.07f, 0.055f, 0.04f), Silver, 0.008f);
            Proto_Greybox3DSmoothPrim.Part(belt, "Holster", Proto_Greybox3DSmoothPrim.CapsuleMesh(),
                new Vector3(0.15f, -0.09f, -0.04f), new Vector3(0.08f, 0.16f, 0.07f), Leather, 0.01f,
                Quaternion.Euler(20f, 20f, 12f));
            Proto_Greybox3DSmoothPrim.Part(belt, "StrapL", Proto_Greybox3DSmoothPrim.CapsuleMesh(),
                new Vector3(-0.08f, 0.08f, -0.02f), new Vector3(0.035f, 0.13f, 0.03f), Leather, 0.008f,
                Quaternion.Euler(8f, 0f, 22f));
            Proto_Greybox3DSmoothPrim.Part(belt, "StrapR", Proto_Greybox3DSmoothPrim.CapsuleMesh(),
                new Vector3(0.08f, 0.08f, -0.02f), new Vector3(0.035f, 0.13f, 0.03f), Leather, 0.008f,
                Quaternion.Euler(8f, 0f, -22f));

            var torso = NewPart(dress, "DressTorso");
            torso.localPosition = new Vector3(0f, 0.52f, 0.02f);
            Proto_Greybox3DSmoothPrim.Sphere(torso, "Belly", new Vector3(0f, 0.00f, 0.02f),
                new Vector3(0.32f, 0.24f, 0.26f), shirt, 0.014f);
            if (!hero)
            {
                Proto_Greybox3DSmoothPrim.Sphere(torso, "Vest", new Vector3(0f, 0.02f, 0.04f),
                    new Vector3(0.30f, 0.20f, 0.20f), vest, 0.012f);
            }
            Proto_Greybox3DSmoothPrim.CapsuleY(dress, "Neck", new Vector3(0f, 0.64f, 0.02f),
                new Vector3(0.12f, 0.07f, 0.12f), skin, 0.01f);

            var sl = NewPart(dress, "DressSleeveL");
            sl.localPosition = new Vector3(-0.20f, 0.54f, 0.00f);
            sl.localRotation = Quaternion.Euler(8f, 0f, 22f);
            Proto_Greybox3DSmoothPrim.CapsuleY(sl, "Sleeve", new Vector3(0f, -0.05f, 0f),
                new Vector3(0.13f, 0.13f, 0.13f), shirt, 0.012f);
            Proto_Greybox3DSmoothPrim.Sphere(sl, "Arm", new Vector3(0f, -0.15f, 0.01f),
                new Vector3(0.11f, 0.14f, 0.11f), skin, 0.01f);

            var sr = NewPart(dress, "DressSleeveR");
            if (hero)
            {
                sr.localPosition = new Vector3(0.20f, 0.54f, 0.03f);
                sr.localRotation = Quaternion.Euler(16f, 8f, -20f);
            }
            else
            {
                sr.localPosition = new Vector3(0.18f, 0.58f, 0.06f);
                sr.localRotation = Quaternion.Euler(-70f, 8f, -10f);
            }
            Proto_Greybox3DSmoothPrim.CapsuleY(sr, "Sleeve", new Vector3(0f, -0.05f, 0f),
                new Vector3(0.13f, 0.13f, 0.13f), shirt, 0.012f);
            Proto_Greybox3DSmoothPrim.Sphere(sr, "Arm", new Vector3(0f, -0.15f, 0.01f),
                new Vector3(0.11f, 0.14f, 0.11f), skin, 0.01f);

            var head = NewPart(dress, "DressHead");
            head.localPosition = new Vector3(0f, 0.74f, 0.02f);
            Proto_Greybox3DSmoothPrim.Sphere(head, "Head", Vector3.zero, new Vector3(0.32f, 0.30f, 0.30f), face, 0.014f);
            if (!hero)
            {
                Proto_Greybox3DSmoothPrim.Sphere(head, "Mask", new Vector3(0f, -0.07f, 0.12f),
                    new Vector3(0.30f, 0.15f, 0.16f), scarf, 0.012f);
            }

            // Hair helmet under the brim: covers the back and sides of the
            // head so the rear view is brown hair, not a bald skin sphere.
            var hairRoot = NewPart(dress, "DressHair");
            hairRoot.localPosition = new Vector3(0f, 0.74f, 0.00f);
            Proto_Greybox3DSmoothPrim.Sphere(hairRoot, "HairBack", new Vector3(0f, 0.02f, -0.07f),
                new Vector3(0.34f, 0.28f, 0.26f), hair, 0.012f);
            Proto_Greybox3DSmoothPrim.Sphere(hairRoot, "HairSideL", new Vector3(-0.14f, 0.00f, -0.02f),
                new Vector3(0.14f, 0.18f, 0.16f), hair, 0.01f);
            Proto_Greybox3DSmoothPrim.Sphere(hairRoot, "HairSideR", new Vector3(0.14f, 0.00f, -0.02f),
                new Vector3(0.14f, 0.18f, 0.16f), hair, 0.01f);
            Proto_Greybox3DSmoothPrim.Sphere(hairRoot, "HairTop", new Vector3(0f, 0.10f, -0.02f),
                new Vector3(0.28f, 0.10f, 0.24f), hair, 0.01f);

            var hatRoot = NewPart(dress, "DressHat");
            hatRoot.localPosition = new Vector3(0f, 0.90f, 0.02f);
            Proto_Greybox3DSmoothPrim.DiskY(hatRoot, "Brim", Vector3.zero,
                new Vector3(brim, 0.032f, brim), hatLight, 0.014f);
            // Cowboy crown: short cylinder + dented cap, not a bowler sphere.
            Proto_Greybox3DSmoothPrim.DiskY(hatRoot, "Crown", new Vector3(0f, 0.08f, 0f),
                new Vector3(crown, 0.14f, crown * 0.92f), hat, 0.014f);
            Proto_Greybox3DSmoothPrim.Sphere(hatRoot, "CrownDent", new Vector3(0f, 0.15f, 0.01f),
                new Vector3(crown * 0.72f, 0.05f, crown * 0.62f), hatLight, 0.008f);
            Proto_Greybox3DSmoothPrim.DiskY(hatRoot, "Band", new Vector3(0f, 0.025f, 0f),
                new Vector3(crown + 0.04f, 0.04f, crown + 0.04f), scarf, 0.01f);

            var knot = NewPart(dress, "DressBandanaKnot");
            knot.localPosition = new Vector3(0f, 0.58f, -0.16f);
            Proto_Greybox3DSmoothPrim.Sphere(knot, "Knot", Vector3.zero, new Vector3(0.11f, 0.10f, 0.11f), scarf, 0.012f);
            var tailL = Proto_Greybox3DSmoothPrim.CapsuleY(knot, "DressBandanaTailL", new Vector3(-0.06f, -0.14f, -0.02f),
                new Vector3(0.055f, 0.20f, 0.035f), scarf, 0.01f);
            tailL.localRotation = Quaternion.Euler(18f, 12f, 18f);
            var tailR = Proto_Greybox3DSmoothPrim.CapsuleY(knot, "DressBandanaTailR", new Vector3(0.06f, -0.14f, -0.02f),
                new Vector3(0.055f, 0.20f, 0.035f), scarf, 0.01f);
            tailR.localRotation = Quaternion.Euler(18f, -12f, -18f);

            var gun = NewPart(hero ? dress : sr, "DressRevolver");
            if (hero)
            {
                gun.localPosition = new Vector3(0.24f, 0.40f, 0.14f);
                gun.localRotation = Quaternion.Euler(12f, 22f, 0f);
            }
            else
            {
                gun.localPosition = new Vector3(0f, -0.22f, 0.04f);
                gun.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            Proto_Greybox3DSmoothPrim.Part(gun, "Barrel", Proto_Greybox3DSmoothPrim.CapsuleMesh(),
                new Vector3(0f, 0.03f, 0.12f), new Vector3(0.035f, 0.11f, 0.035f), Silver, 0.01f,
                Quaternion.Euler(90f, 0f, 0f));
            Proto_Greybox3DSmoothPrim.Part(gun, "Body", Proto_Greybox3DSmoothPrim.SphereMesh(),
                new Vector3(0f, 0.02f, 0.05f), new Vector3(0.05f, 0.06f, 0.07f), Silver, 0.01f);
            Proto_Greybox3DSmoothPrim.Part(gun, "Grip", Proto_Greybox3DSmoothPrim.CapsuleMesh(),
                new Vector3(0f, -0.02f, 0.02f), new Vector3(0.03f, 0.07f, 0.025f), Leather, 0.008f);
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
