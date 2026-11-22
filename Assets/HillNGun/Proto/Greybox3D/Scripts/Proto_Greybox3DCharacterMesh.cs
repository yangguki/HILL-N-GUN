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
    }
}
