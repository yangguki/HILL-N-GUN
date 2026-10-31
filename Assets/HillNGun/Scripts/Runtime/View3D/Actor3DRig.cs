using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.View3D
{
    /// <summary>
    /// Hierarchical joint rig for one stylized actor, built entirely from code (ToonMeshBuilder) from an Actor3DModelDef.
    /// Model space: +Y up, +Z forward (facing), +X right. Root carries world placement + view tilt; Yaw turns the whole actor.
    /// Bone transforms are animated by Actor3DAnimator; this class only builds and owns them.
    /// </summary>
    public sealed class Actor3DRig
    {
        public readonly Actor3DModelDef Def;
        public Transform Root, Yaw, Body, RollPivot, Pelvis, Spine, Head, Cape;
        public Transform ShoulderL, ShoulderR, ElbowL, ElbowR, HandL, HandR;
        public Transform HipL, HipR, KneeL, KneeR, FootL, FootR;
        public Transform Stars;
        public Transform[] Tail;
        public Transform Horse, HorseHead;
        public Transform[] HorseUpper, HorseLower; // FL, FR, BL, BR
        public readonly List<Renderer> Renderers = new List<Renderer>();
        public readonly List<Renderer> StarRenderers = new List<Renderer>();

        // dimensions the animator needs
        public float LegLength, Thigh, Shin, FootHeight, PelvisHeight, RollPivotHeight, HorseBackHeight, Height;
        public bool Mounted => Def.mounted;

        private readonly Material _mat;
        private readonly int _layer;
        private readonly string _meshKey;
        private Color _tint = Color.white;
        private float _tintStrength;

        private static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();

        public Actor3DRig(Actor3DModelDef def, Material mat, int layer, Color skinTint, float skinStrength, Transform parent, string name)
        {
            Def = def;
            _mat = mat;
            _layer = layer;
            _tint = skinTint;
            _tintStrength = skinStrength;
            _meshKey = def.id + "|" + (skinStrength > 0.001f ? ColorUtility.ToHtmlStringRGB(skinTint) + skinStrength.ToString("F2") : "base");
            Build(parent, name);
        }

        // ---------- palette (skin recolor on outfit fabrics only) ----------
        private Color Outfit(Color c)
        {
            if (_tintStrength <= 0.001f) return c;
            Color.RGBToHSV(_tint, out float th, out float ts, out float tv);
            Color.RGBToHSV(c, out _, out float s, out float v);
            float lift = tv * 0.95f;
            float nv = Mathf.Min(1f, (1f - lift) * v + lift * (0.3f + 0.75f * Mathf.Pow(v, 0.5f)));
            float ns = s + (ts - s) * 0.9f;
            float nh = Mathf.Repeat(th - (1f - nv) * 0.07f * ts, 1f);
            return Color.Lerp(c, Color.HSVToRGB(nh, ns, nv), _tintStrength);
        }

        private Color HatC => Def.recolorHat ? Outfit(Def.hatColor) : Def.hatColor;
        private Color ShirtC => Outfit(Def.shirt);
        private Color VestC => Outfit(Def.vest);
        private Color PantsC => Outfit(Def.pants);
        private Color CoatC => Outfit(Def.coat);
        private static Color Darker(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        // ---------- hierarchy helpers ----------
        private Transform Node(string n, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(n);
            go.layer = _layer;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private void Skin(Transform bone, string part, System.Action<ToonMeshBuilder> build, List<Renderer> list = null)
        {
            string key = _meshKey + "|" + part;
            if (!MeshCache.TryGetValue(key, out var mesh) || mesh == null)
            {
                var b = new ToonMeshBuilder();
                build(b);
                if (b.IsEmpty) return;
                mesh = b.Build(key);
                MeshCache[key] = mesh;
            }
            var go = new GameObject(part + "_mesh");
            go.layer = _layer;
            go.transform.SetParent(bone, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = _mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            (list ?? Renderers).Add(r);
        }

        // ---------- build ----------
        private void Build(Transform parent, string name)
        {
            var d = Def;
            float r = d.limbRadius;
            FootHeight = 0.08f;
            LegLength = d.legLength;
            Thigh = (LegLength - FootHeight) * 0.5f;
            Shin = Thigh;

            Root = Node(name, parent, Vector3.zero);
            Yaw = Node("Yaw", Root, Vector3.zero);
            Body = Node("Body", Yaw, Vector3.zero);

            float baseY = 0f;
            if (d.mounted) { BuildHorse(); baseY = HorseBackHeight; }
            PelvisHeight = d.mounted ? baseY + 0.12f : LegLength;
            RollPivotHeight = PelvisHeight * 0.85f;
            RollPivot = Node("RollPivot", Body, new Vector3(0f, RollPivotHeight, 0f));
            Pelvis = Node("Pelvis", RollPivot, new Vector3(0f, PelvisHeight - RollPivotHeight, 0f));

            float hw = d.hipWidth, sw = d.shoulderWidth * Mathf.Sqrt(d.bulk), dep = d.bodyDepth;
            float th = d.torsoHeight;

            // pelvis: hips + belt (+ coat skirt)
            Skin(Pelvis, "pelvis", b =>
            {
                b.Ellipsoid(new Vector3(0f, 0.03f, 0f), new Vector3(hw * 0.62f, 0.14f, dep * 0.52f), d.overalls ? VestC : PantsC);
                if (!d.overalls)
                {
                    b.Ellipsoid(new Vector3(0f, 0.13f, 0f), new Vector3(hw * 0.66f, 0.05f, dep * 0.56f), d.belt);
                    b.OutlineWeight = 0.3f;
                    b.Box(new Vector3(0f, 0.13f, dep * 0.55f), new Vector3(0.08f, 0.06f, 0.03f), new Color(0.85f, 0.7f, 0.3f));
                    b.OutlineWeight = 1f;
                    // holster on the right hip
                    if (d.weapon == Actor3DWeapon.Pistol || d.weapon == Actor3DWeapon.DualPistol)
                        b.Box(new Vector3(hw * 0.62f, -0.02f, 0f), new Vector3(0.06f, 0.16f, 0.1f), Darker(d.belt, 1.3f));
                }
                if (d.longCoat)
                {
                    float len = LegLength * 0.62f;
                    b.Frustum(new Vector3(0f, 0.14f, -0.01f), new Vector3(0f, 0.14f - len, -0.06f), dep * 0.56f, dep * 0.78f, CoatC, 12, true, (hw * 0.7f) / (dep * 0.56f));
                }
            });

            // legs
            float hipX = hw * 0.36f;
            HipL = Node("HipL", Pelvis, new Vector3(-hipX, -0.03f, 0f));
            HipR = Node("HipR", Pelvis, new Vector3(hipX, -0.03f, 0f));
            KneeL = Node("KneeL", HipL, new Vector3(0f, -Thigh, 0f));
            KneeR = Node("KneeR", HipR, new Vector3(0f, -Thigh, 0f));
            FootL = Node("FootL", KneeL, new Vector3(0f, -Shin, 0f));
            FootR = Node("FootR", KneeR, new Vector3(0f, -Shin, 0f));
            foreach (var side in new[] { "L", "R" })
            {
                var hip = side == "L" ? HipL : HipR;
                var knee = side == "L" ? KneeL : KneeR;
                var foot = side == "L" ? FootL : FootR;
                Skin(hip, "thigh" + side, b => b.Capsule(Vector3.zero, new Vector3(0f, -Thigh, 0f), r * 1.35f, r * 1.15f, PantsC));
                Skin(knee, "shin" + side, b =>
                {
                    b.Capsule(Vector3.zero, new Vector3(0f, -Shin * 0.5f, 0f), r * 1.15f, r * 1.05f, PantsC);
                    b.Frustum(new Vector3(0f, -Shin * 0.42f, 0f), new Vector3(0f, -Shin, 0f), r * 1.35f, r * 1.2f, d.boots, 10);
                });
                Skin(foot, "foot" + side, b =>
                {
                    b.Ellipsoid(new Vector3(0f, -FootHeight * 0.45f, 0.07f), new Vector3(r * 1.3f, FootHeight * 0.55f, 0.15f), d.boots);
                    b.Box(new Vector3(0f, -FootHeight * 0.8f, -0.03f), new Vector3(r * 2.2f, FootHeight * 0.4f, 0.08f), Darker(d.boots, 0.7f));
                });
            }

            // torso
            Spine = Node("Spine", Pelvis, new Vector3(0f, 0.1f, 0f));
            Skin(Spine, "torso", b => BuildTorso(b, sw, dep, th));

            // cape (own bone so it can flutter)
            if (d.cape)
            {
                Cape = Node("Cape", Spine, new Vector3(0f, th * 0.92f, -dep * 0.42f));
                float len = th + LegLength * 0.62f;
                Skin(Cape, "cape", b =>
                {
                    var col = CoatC;
                    b.Frustum(Vector3.zero, new Vector3(0f, -len, -0.06f), 0.05f, 0.075f, col, 12, true, sw * 0.62f / 0.05f);
                    b.OutlineWeight = 0.6f;
                    for (int i = -2; i <= 2; i++)
                        b.Cone(new Vector3(i * sw * 0.2f, -len + 0.02f, -0.06f), new Vector3(i * sw * 0.22f, -len - 0.12f - (i & 1) * 0.06f, -0.07f), 0.06f, Darker(col, 0.85f), 6);
                    b.OutlineWeight = 1f;
                    // collar
                    b.Ellipsoid(new Vector3(0f, 0.02f, 0.12f), new Vector3(sw * 0.5f, 0.06f, 0.2f), Darker(col, 1.1f));
                });
            }

            // head
            float hr = d.headRadius;
            Head = Node("Head", Spine, new Vector3(0f, th + 0.03f, 0f));
            Skin(Head, "head", b => BuildHead(b, hr));

            // arms
            float shY = th * 0.86f, shX = sw * 0.5f + r * 0.4f;
            float ua = d.armLength * 0.5f, fa = d.armLength * 0.44f;
            ShoulderL = Node("ShoulderL", Spine, new Vector3(-shX, shY, 0f));
            ShoulderR = Node("ShoulderR", Spine, new Vector3(shX, shY, 0f));
            ElbowL = Node("ElbowL", ShoulderL, new Vector3(0f, -ua, 0f));
            ElbowR = Node("ElbowR", ShoulderR, new Vector3(0f, -ua, 0f));
            HandL = Node("HandL", ElbowL, new Vector3(0f, -fa, 0f));
            HandR = Node("HandR", ElbowR, new Vector3(0f, -fa, 0f));
            float ar = r * Mathf.Lerp(1f, 1.35f, Mathf.Clamp01(d.bulk - 1f) * 3f);
            Color sleeve = d.bareArms ? d.skin : (d.longCoat ? CoatC : ShirtC);
            Color shoulderCol = d.hasVest && !d.longCoat ? VestC : sleeve;
            foreach (var side in new[] { "L", "R" })
            {
                var sh = side == "L" ? ShoulderL : ShoulderR;
                var el = side == "L" ? ElbowL : ElbowR;
                var hand = side == "L" ? HandL : HandR;
                Skin(sh, "upperarm" + side, b =>
                {
                    b.Sphere(Vector3.zero, ar * 1.25f, shoulderCol);
                    b.Capsule(Vector3.zero, new Vector3(0f, -ua, 0f), ar * 1.1f, ar * 0.95f, sleeve);
                });
                Skin(el, "forearm" + side, b =>
                {
                    b.Capsule(Vector3.zero, new Vector3(0f, -fa, 0f), ar * 0.95f, ar * 0.85f, d.bareArms ? d.skin : (d.longCoat ? CoatC : ShirtC));
                    if (d.longCoat || d.weapon == Actor3DWeapon.Rifle) b.Frustum(new Vector3(0f, -fa * 0.65f, 0f), new Vector3(0f, -fa, 0f), ar * 1.05f, ar * 1.15f, d.longCoat ? Darker(CoatC, 0.85f) : Darker(sleeve, 0.85f), 8);
                    b.Sphere(new Vector3(0f, -fa - r * 0.6f, 0f), r * 1.15f, d.skin);
                });
            }
            BuildWeapons();

            // tail (boss)
            if (d.tail) BuildTail(dep);

            // dizzy stars
            Stars = Node("Stars", Root, Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                var s = Node("Star" + i, Stars, Vector3.zero);
                Skin(s, "star", b => BuildStar(b, 0.11f, Color.white), StarRenderers);
            }
            Stars.gameObject.SetActive(false);

            Height = PelvisHeight + 0.1f + th + hr * 2.2f;
        }

        private void BuildTorso(ToonMeshBuilder b, float sw, float dep, float th)
        {
            var d = Def;
            float bulk = d.bulk;
            var chestC = d.longCoat ? CoatC : (d.overalls ? ShirtC : ShirtC);
            // chest (wider top) + belly
            b.Ellipsoid(new Vector3(0f, th * 0.6f, 0f), new Vector3(sw * 0.5f, th * 0.46f, dep * 0.5f * Mathf.Pow(bulk, 0.3f)), chestC);
            b.Ellipsoid(new Vector3(0f, th * 0.22f, 0f), new Vector3(sw * 0.4f, th * 0.3f, dep * 0.46f), d.overalls ? VestC : chestC);
            if (d.stripedShirt)
            {
                var stripe = Darker(VestC, 0.75f);
                for (int i = 0; i < 3; i++)
                {
                    float y = th * (0.3f + i * 0.2f);
                    float k = 1f - Mathf.Abs(y - th * 0.6f) / (th * 0.6f);
                    b.Ellipsoid(new Vector3(0f, y, 0f), new Vector3(sw * (0.42f + 0.1f * k) * 1.04f, th * 0.045f, dep * 0.53f), stripe);
                }
            }
            if (d.hasVest)
            {
                // vest: slightly larger shell, shirt strip shows at the open front
                b.Ellipsoid(new Vector3(0f, th * 0.55f, -0.005f), new Vector3(sw * 0.53f, th * 0.42f, dep * 0.53f * Mathf.Pow(bulk, 0.3f)), VestC);
                b.OutlineWeight = 0.4f;
                b.Box(new Vector3(0f, th * 0.55f, dep * 0.5f * Mathf.Pow(bulk, 0.3f) + 0.005f), new Vector3(sw * 0.22f, th * 0.6f, 0.04f), d.longCoat ? ShirtC : chestC == VestC ? ShirtC : Darker(chestC, 1.05f));
                b.OutlineWeight = 1f;
            }
            if (d.longCoat)
            {
                // lapels + back seam
                b.Box(new Vector3(0f, th * 0.5f, -dep * 0.52f), new Vector3(0.03f, th * 0.7f, 0.02f), Darker(CoatC, 0.8f));
                b.Ellipsoid(new Vector3(0f, th * 0.95f, -0.02f), new Vector3(sw * 0.48f, 0.07f, dep * 0.55f), Darker(CoatC, 0.9f));
            }
            if (d.overalls)
            {
                var oc = VestC;
                b.Box(new Vector3(0f, th * 0.42f, dep * 0.47f), new Vector3(sw * 0.5f, th * 0.42f, 0.05f), oc);
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Box(new Vector3(s * sw * 0.2f, th * 0.78f, dep * 0.4f), new Vector3(0.05f, th * 0.36f, 0.04f), oc);
                    // straps cross on the back (visible from behind)
                    b.Box(new Vector3(0f, th * 0.62f, -dep * 0.49f), new Vector3(0.05f, th * 0.75f, 0.04f), oc, Quaternion.Euler(0f, 0f, s * 28f));
                }
                b.Box(new Vector3(0f, th * 0.5f, dep * 0.5f + 0.02f), new Vector3(sw * 0.2f, th * 0.12f, 0.02f), Darker(oc, 0.8f));
            }
            if (d.bandolier)
            {
                var q = Quaternion.Euler(0f, 0f, 38f);
                b.Box(new Vector3(0f, th * 0.55f, dep * 0.47f), new Vector3(0.09f, th * 1.15f, 0.04f), d.belt, q);
                b.Box(new Vector3(0f, th * 0.55f, -dep * 0.47f), new Vector3(0.09f, th * 1.15f, 0.04f), d.belt, Quaternion.Euler(0f, 0f, -38f));
                for (int i = -2; i <= 2; i++)
                {
                    var p = new Vector3(0f, th * 0.55f, dep * 0.52f) + q * new Vector3(0f, i * 0.09f, 0f);
                    b.Frustum(p + q * new Vector3(-0.05f, 0f, 0f), p + q * new Vector3(0.05f, 0f, 0f), 0.025f, 0.025f, d.accent, 8);
                }
            }
            if (d.neckerchief)
            {
                b.Frustum(new Vector3(0f, th * 0.92f, 0f), new Vector3(0f, th * 1.02f, 0f), dep * 0.42f, dep * 0.38f, d.accent, 12);
                b.Cone(new Vector3(0f, th * 0.95f, dep * 0.4f), new Vector3(0f, th * 0.68f, dep * 0.5f), 0.09f, d.accent, 4);
                // knot at the back
                b.Sphere(new Vector3(0f, th * 0.96f, -dep * 0.42f), 0.05f, d.accent);
                b.Cone(new Vector3(0.02f, th * 0.94f, -dep * 0.45f), new Vector3(0.09f, th * 0.78f, -dep * 0.5f), 0.035f, d.accent, 5);
                b.Cone(new Vector3(-0.02f, th * 0.94f, -dep * 0.45f), new Vector3(-0.07f, th * 0.8f, -dep * 0.5f), 0.035f, d.accent, 5);
            }
            if (d.wrenchOnBack)
            {
                var q = Quaternion.Euler(0f, 0f, -35f);
                var c = new Vector3(0f, th * 0.55f, -dep * 0.55f);
                b.Box(c, new Vector3(0.06f, th * 1.0f, 0.04f), d.metal, q);
                b.Box(c + q * new Vector3(0f, th * 0.52f, 0f), new Vector3(0.18f, 0.08f, 0.05f), d.metal, q);
                b.Box(c + q * new Vector3(-0.06f, th * 0.6f, 0f), new Vector3(0.05f, 0.1f, 0.05f), d.metal, q);
                b.Box(c + q * new Vector3(0.06f, th * 0.6f, 0f), new Vector3(0.05f, 0.1f, 0.05f), d.metal, q);
                b.Box(c + q * new Vector3(0f, th * 0.15f, 0.025f), new Vector3(0.08f, th * 0.6f, 0.02f), d.belt, Quaternion.Euler(0f, 0f, 30f));
            }
            // neck
            b.Frustum(new Vector3(0f, th * 0.85f, 0f), new Vector3(0f, th * 1.05f, 0f), 0.07f * Mathf.Sqrt(bulk), 0.065f * Mathf.Sqrt(bulk), d.skin, 8, false);
        }

        private void BuildHead(ToonMeshBuilder b, float hr)
        {
            var d = Def;
            if (d.face == Actor3DFace.Snake)
            {
                // snake head: long snout forward, flat top, yellow eyes with slit pupils
                b.Ellipsoid(new Vector3(0f, hr * 0.85f, hr * 0.15f), new Vector3(hr * 0.88f, hr * 0.78f, hr * 1.15f), d.skin);
                b.Ellipsoid(new Vector3(0f, hr * 0.6f, hr * 0.65f), new Vector3(hr * 0.62f, hr * 0.42f, hr * 0.75f), Darker(d.skin, 1.12f));
                b.Ellipsoid(new Vector3(0f, hr * 0.48f, hr * 0.6f), new Vector3(hr * 0.55f, hr * 0.2f, hr * 0.7f), d.shirt);
                b.OutlineWeight = 0.25f;
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Ellipsoid(new Vector3(s * hr * 0.55f, hr * 1.02f, hr * 0.62f), new Vector3(hr * 0.2f, hr * 0.2f, hr * 0.17f), d.eyes);
                    b.Box(new Vector3(s * hr * 0.6f, hr * 1.02f, hr * 0.76f), new Vector3(0.025f, hr * 0.26f, 0.02f), new Color(0.08f, 0.06f, 0.05f), Quaternion.Euler(0f, s * 30f, 0f));
                    b.Sphere(new Vector3(s * hr * 0.18f, hr * 0.78f, hr * 1.24f), hr * 0.05f, new Color(0.1f, 0.08f, 0.06f));
                }
                for (int i = 0; i < 4; i++)
                    b.Ellipsoid(new Vector3((i % 2 == 0 ? -1 : 1) * hr * 0.25f, hr * 1.45f, hr * (0.4f - i * 0.35f)), new Vector3(hr * 0.2f, hr * 0.05f, hr * 0.16f), Darker(d.skin, 0.7f));
                b.OutlineWeight = 1f;
            }
            else
            {
                b.Ellipsoid(new Vector3(0f, hr * 0.95f, 0f), new Vector3(hr, hr * 1.02f, hr * 0.95f), d.skin);
                // ears
                b.Ellipsoid(new Vector3(-hr * 0.97f, hr * 0.9f, -hr * 0.05f), new Vector3(hr * 0.12f, hr * 0.2f, hr * 0.14f), Darker(d.skin, 0.95f));
                b.Ellipsoid(new Vector3(hr * 0.97f, hr * 0.9f, -hr * 0.05f), new Vector3(hr * 0.12f, hr * 0.2f, hr * 0.14f), Darker(d.skin, 0.95f));
                b.OutlineWeight = 0f;
                for (int s = -1; s <= 1; s += 2)
                {
                    b.Ellipsoid(new Vector3(s * hr * 0.34f, hr * 1.06f, hr * 0.86f), new Vector3(hr * 0.1f, hr * 0.15f, hr * 0.06f), d.eyes);
                    b.Sphere(new Vector3(s * hr * 0.31f, hr * 1.11f, hr * 0.91f), hr * 0.035f, Color.white);
                    b.Box(new Vector3(s * hr * 0.36f, hr * 1.3f, hr * 0.84f), new Vector3(hr * 0.26f, hr * 0.06f, 0.02f), Darker(d.hair, 0.8f), Quaternion.Euler(0f, 0f, s * -8f));
                }
                b.OutlineWeight = 0.3f;
                b.Sphere(new Vector3(0f, hr * 0.9f, hr * 0.95f), hr * 0.13f, Darker(d.skin, 0.93f));
                if (d.mustache)
                {
                    b.Ellipsoid(new Vector3(-hr * 0.2f, hr * 0.7f, hr * 0.9f), new Vector3(hr * 0.24f, hr * 0.08f, hr * 0.08f), d.hair, Quaternion.Euler(0f, 0f, 15f));
                    b.Ellipsoid(new Vector3(hr * 0.2f, hr * 0.7f, hr * 0.9f), new Vector3(hr * 0.24f, hr * 0.08f, hr * 0.08f), d.hair, Quaternion.Euler(0f, 0f, -15f));
                }
                b.OutlineWeight = 1f;
                if (d.face == Actor3DFace.Bandana || d.face == Actor3DFace.Mask)
                {
                    var c = d.accent;
                    b.Ellipsoid(new Vector3(0f, hr * 0.58f, hr * 0.06f), new Vector3(hr * 1.05f, hr * 0.42f, hr * 1.0f), c);
                    b.Cone(new Vector3(0f, hr * 0.42f, hr * 0.85f), new Vector3(0f, hr * 0.05f, hr * 0.95f), hr * 0.35f, Darker(c, 0.9f), 4);
                    b.Sphere(new Vector3(0f, hr * 0.62f, -hr * 1.0f), hr * 0.14f, c);
                }
                switch (d.hairStyle)
                {
                    case Actor3DHair.Short:
                        b.Ellipsoid(new Vector3(0f, hr * 1.12f, -hr * 0.16f), new Vector3(hr * 1.05f, hr * 0.85f, hr * 0.9f), d.hair);
                        break;
                    case Actor3DHair.Long:
                        b.Ellipsoid(new Vector3(0f, hr * 1.12f, -hr * 0.14f), new Vector3(hr * 1.07f, hr * 0.9f, hr * 0.92f), d.hair);
                        b.Ellipsoid(new Vector3(0f, hr * 0.15f, -hr * 0.62f), new Vector3(hr * 0.98f, hr * 1.25f, hr * 0.48f), d.hair, Quaternion.Euler(-8f, 0f, 0f));
                        b.Ellipsoid(new Vector3(-hr * 0.85f, hr * 0.5f, hr * 0.1f), new Vector3(hr * 0.25f, hr * 0.75f, hr * 0.3f), d.hair);
                        b.Ellipsoid(new Vector3(hr * 0.85f, hr * 0.5f, hr * 0.1f), new Vector3(hr * 0.25f, hr * 0.75f, hr * 0.3f), d.hair);
                        break;
                    case Actor3DHair.Curly:
                        b.Ellipsoid(new Vector3(0f, hr * 1.1f, -hr * 0.12f), new Vector3(hr * 1.04f, hr * 0.88f, hr * 0.92f), d.hair);
                        for (int i = 0; i < 9; i++)
                        {
                            float a = Mathf.Lerp(-150f, 150f, i / 8f) * Mathf.Deg2Rad;
                            b.Sphere(new Vector3(Mathf.Sin(a) * hr * 0.98f, hr * (0.85f + 0.12f * (i % 2)), -Mathf.Cos(a) * hr * 0.92f), hr * 0.26f, Darker(d.hair, i % 2 == 0 ? 1f : 0.88f), 8, 6);
                        }
                        break;
                }
            }
            BuildHat(b, hr);
        }

        private void BuildHat(ToonMeshBuilder b, float hr)
        {
            var d = Def;
            var hc = HatC;
            var band = d.hatBand;
            float y0 = hr * (d.face == Actor3DFace.Snake ? 1.45f : 1.55f);
            var tilt = Quaternion.Euler(-6f, 0f, 0f);
            Vector3 P(float x, float y, float z) => tilt * new Vector3(x, y, z);
            switch (d.hat)
            {
                case Actor3DHat.Cowboy:
                case Actor3DHat.Wide:
                {
                    float brim = d.hat == Actor3DHat.Wide ? hr * 1.7f : hr * 1.4f;
                    b.Disc(P(0f, y0, 0f), tilt * Vector3.up, brim, 0.035f, hc, 18, 1.08f);
                    b.Ellipsoid(P(-brim * 0.9f, y0 + 0.05f, 0f), new Vector3(brim * 0.18f, 0.04f, brim * 0.55f), hc, tilt * Quaternion.Euler(0f, 0f, -25f), 8, 5);
                    b.Ellipsoid(P(brim * 0.9f, y0 + 0.05f, 0f), new Vector3(brim * 0.18f, 0.04f, brim * 0.55f), hc, tilt * Quaternion.Euler(0f, 0f, 25f), 8, 5);
                    float ch = d.hat == Actor3DHat.Wide ? hr * 0.35f : hr * 0.5f;
                    b.Frustum(P(0f, y0, 0f), P(0f, y0 + ch, 0f), hr * 0.78f, hr * 0.66f, hc, 12, false);
                    b.Ellipsoid(P(0f, y0 + ch, 0f), new Vector3(hr * 0.66f, hr * 0.16f, hr * 0.62f), Darker(hc, 0.92f), tilt);
                    b.Frustum(P(0f, y0 + 0.01f, 0f), P(0f, y0 + hr * 0.16f, 0f), hr * 0.8f, hr * 0.77f, band, 12, false);
                    break;
                }
                case Actor3DHat.WitchWide:
                {
                    float brim = hr * 1.72f;
                    b.Disc(P(0f, y0, 0f), tilt * Vector3.up, brim, 0.035f, hc, 20, 1.05f);
                    b.Frustum(P(0f, y0, 0f), P(0f, y0 + hr * 0.6f, -hr * 0.05f), hr * 0.8f, hr * 0.55f, hc, 12, false);
                    b.Cone(P(0f, y0 + hr * 0.58f, -hr * 0.05f), P(0f, y0 + hr * 1.5f, -hr * 0.55f), hr * 0.56f, Darker(hc, 0.95f), 12);
                    b.Frustum(P(0f, y0 + 0.01f, 0f), P(0f, y0 + hr * 0.18f, 0f), hr * 0.82f, hr * 0.78f, band, 12, false);
                    b.OutlineWeight = 0.4f;
                    b.Ellipsoid(P(hr * 0.62f, y0 + hr * 0.12f, hr * 0.35f), new Vector3(hr * 0.15f, hr * 0.12f, hr * 0.08f), d.accent, tilt);
                    b.OutlineWeight = 1f;
                    break;
                }
                case Actor3DHat.Bowler:
                {
                    b.Disc(P(0f, y0 - hr * 0.08f, 0f), tilt * Vector3.up, hr * 1.18f, 0.03f, hc, 16, 1.05f);
                    b.Ellipsoid(P(0f, y0 + hr * 0.12f, 0f), new Vector3(hr * 0.88f, hr * 0.66f, hr * 0.85f), hc, tilt);
                    b.Frustum(P(0f, y0 - hr * 0.06f, 0f), P(0f, y0 + hr * 0.12f, 0f), hr * 0.9f, hr * 0.89f, band, 12, false);
                    break;
                }
            }
        }

        private void BuildWeapons()
        {
            var d = Def;
            switch (d.weapon)
            {
                case Actor3DWeapon.Pistol:
                    Skin(HandR, "weaponR", b => Pistol(b));
                    break;
                case Actor3DWeapon.DualPistol:
                    Skin(HandR, "weaponR", b => Pistol(b));
                    Skin(HandL, "weaponL", b => Pistol(b));
                    break;
                case Actor3DWeapon.Shotgun:
                    Skin(HandR, "weaponR", b =>
                    {
                        b.Box(new Vector3(0f, 0.08f, 0.02f), new Vector3(0.08f, 0.32f, 0.1f), d.wood);
                        b.Frustum(new Vector3(-0.025f, -0.04f, 0.06f), new Vector3(-0.025f, -0.62f, 0.06f), 0.032f, 0.032f, d.metal, 8);
                        b.Frustum(new Vector3(0.025f, -0.04f, 0.06f), new Vector3(0.025f, -0.62f, 0.06f), 0.032f, 0.032f, d.metal, 8);
                        b.Box(new Vector3(0f, -0.3f, 0.02f), new Vector3(0.09f, 0.16f, 0.06f), d.wood);
                    });
                    break;
                case Actor3DWeapon.Rifle:
                    Skin(HandR, "weaponR", b =>
                    {
                        b.Box(new Vector3(0f, 0.12f, 0.0f), new Vector3(0.06f, 0.34f, 0.1f), d.wood);
                        b.Box(new Vector3(0f, -0.2f, 0.04f), new Vector3(0.06f, 0.36f, 0.06f), d.wood);
                        b.Frustum(new Vector3(0f, -0.05f, 0.08f), new Vector3(0f, -0.82f, 0.08f), 0.022f, 0.02f, d.metal, 8);
                        if (d.id == "raven") b.Frustum(new Vector3(0f, -0.02f, 0.14f), new Vector3(0f, -0.26f, 0.14f), 0.03f, 0.035f, Darker(d.metal, 0.7f), 8);
                    });
                    break;
                case Actor3DWeapon.Dynamite:
                    Skin(HandR, "weaponR", b =>
                    {
                        b.Frustum(new Vector3(-0.1f, -0.03f, 0.03f), new Vector3(0.1f, -0.03f, 0.03f), 0.04f, 0.04f, d.accent, 8);
                        b.OutlineWeight = 0.3f;
                        b.Frustum(new Vector3(0.1f, -0.03f, 0.03f), new Vector3(0.16f, 0.03f, 0.03f), 0.008f, 0.008f, new Color(0.2f, 0.18f, 0.15f), 5);
                        b.OutlineWeight = 0f;
                        b.Sphere(new Vector3(0.17f, 0.045f, 0.03f), 0.03f, new Color(1f, 0.85f, 0.3f), 6, 4);
                        b.OutlineWeight = 1f;
                    });
                    break;
            }
        }

        private void Pistol(ToonMeshBuilder b)
        {
            var d = Def;
            b.Box(new Vector3(0f, -0.02f, -0.01f), new Vector3(0.05f, 0.1f, 0.06f), d.wood, Quaternion.Euler(-15f, 0f, 0f));
            b.Box(new Vector3(0f, -0.1f, 0.05f), new Vector3(0.05f, 0.08f, 0.08f), d.metal);
            b.Frustum(new Vector3(0f, -0.08f, 0.07f), new Vector3(0f, -0.3f, 0.07f), 0.022f, 0.02f, d.metal, 8);
        }

        private void BuildTail(float dep)
        {
            var d = Def;
            int n = Mathf.Max(3, d.tailSegments);
            float seg = d.tailLength / n;
            Tail = new Transform[n];
            var parent = Pelvis;
            var pos = new Vector3(0f, -0.04f, -dep * 0.45f);
            for (int i = 0; i < n; i++)
            {
                var t = Node("Tail" + i, parent, pos);
                Tail[i] = t;
                float r0 = Mathf.Lerp(0.13f, 0.035f, i / (float)n), r1 = Mathf.Lerp(0.13f, 0.035f, (i + 1) / (float)n);
                int idx = i;
                Skin(t, "tail" + i, b =>
                {
                    b.Capsule(Vector3.zero, new Vector3(0f, 0f, -seg), r0, r1, d.tailColor, 10);
                    b.OutlineWeight = 0.3f;
                    b.Ellipsoid(new Vector3(0f, r0 * 0.75f, -seg * 0.5f), new Vector3(r0 * 0.7f, r0 * 0.3f, seg * 0.3f), d.tailPattern);
                    b.OutlineWeight = 1f;
                    if (idx == n - 1)
                        for (int k = 0; k < 3; k++)
                            b.Ellipsoid(new Vector3(0f, 0f, -seg - 0.05f - k * 0.075f), new Vector3(0.06f - k * 0.008f, 0.045f, 0.045f), k % 2 == 0 ? d.tailPattern : Darker(d.tailPattern, 0.75f));
                });
                parent = t;
                pos = new Vector3(0f, 0f, -seg);
            }
        }

        private void BuildHorse()
        {
            var d = Def;
            var hc = d.horseColor;
            float legH = 0.78f;
            HorseBackHeight = legH + 0.42f;
            Horse = Node("Horse", Body, Vector3.zero);
            var torso = Node("HorseBody", Horse, new Vector3(0f, legH + 0.2f, 0f));
            Skin(torso, "horseBody", b =>
            {
                b.Ellipsoid(new Vector3(0f, 0f, 0f), new Vector3(0.3f, 0.3f, 0.66f), hc);
                b.Ellipsoid(new Vector3(0f, 0.02f, 0.45f), new Vector3(0.29f, 0.31f, 0.3f), hc);
                b.Frustum(new Vector3(0f, 0.1f, 0.5f), new Vector3(0f, 0.55f, 0.82f), 0.17f, 0.13f, hc, 10, false);
                // saddle + blanket
                b.Box(new Vector3(0f, 0.27f, -0.02f), new Vector3(0.5f, 0.06f, 0.5f), d.accent == default ? d.belt : Darker(d.vest, 0.9f));
                b.Ellipsoid(new Vector3(0f, 0.3f, -0.02f), new Vector3(0.2f, 0.07f, 0.24f), d.belt);
                // mane
                for (int i = 0; i < 4; i++)
                    b.Box(new Vector3(0f, 0.32f + i * 0.09f, 0.55f + i * 0.07f), new Vector3(0.06f, 0.12f, 0.1f), d.horseMane, Quaternion.Euler(-35f, 0f, 0f));
                // tail
                b.Frustum(new Vector3(0f, 0.1f, -0.62f), new Vector3(0f, -0.4f, -0.82f), 0.07f, 0.03f, d.horseMane, 8);
            });
            HorseHead = Node("HorseHead", torso, new Vector3(0f, 0.56f, 0.84f));
            Skin(HorseHead, "horseHead", b =>
            {
                b.Ellipsoid(new Vector3(0f, -0.06f, 0.16f), new Vector3(0.13f, 0.14f, 0.3f), hc, Quaternion.Euler(30f, 0f, 0f));
                b.Ellipsoid(new Vector3(0f, -0.2f, 0.36f), new Vector3(0.1f, 0.09f, 0.1f), Darker(hc, 0.8f));
                b.Cone(new Vector3(-0.07f, 0.05f, 0.02f), new Vector3(-0.09f, 0.18f, -0.02f), 0.04f, hc, 6);
                b.Cone(new Vector3(0.07f, 0.05f, 0.02f), new Vector3(0.09f, 0.18f, -0.02f), 0.04f, hc, 6);
                b.OutlineWeight = 0f;
                b.Sphere(new Vector3(-0.12f, 0.0f, 0.18f), 0.025f, new Color(0.08f, 0.06f, 0.05f));
                b.Sphere(new Vector3(0.12f, 0.0f, 0.18f), 0.025f, new Color(0.08f, 0.06f, 0.05f));
                b.OutlineWeight = 1f;
            });
            HorseUpper = new Transform[4];
            HorseLower = new Transform[4];
            var offs = new[] { new Vector3(-0.17f, legH, 0.45f), new Vector3(0.17f, legH, 0.45f), new Vector3(-0.17f, legH, -0.45f), new Vector3(0.17f, legH, -0.45f) };
            for (int i = 0; i < 4; i++)
            {
                var up = Node("HLegU" + i, Horse, offs[i]);
                var lo = Node("HLegL" + i, up, new Vector3(0f, -legH * 0.5f, 0f));
                HorseUpper[i] = up; HorseLower[i] = lo;
                Skin(up, "hlegU", b => b.Capsule(Vector3.zero, new Vector3(0f, -legH * 0.5f, 0f), 0.09f, 0.065f, hc, 8));
                Skin(lo, "hlegL", b =>
                {
                    b.Frustum(Vector3.zero, new Vector3(0f, -legH * 0.42f, 0f), 0.06f, 0.055f, hc, 8);
                    b.Frustum(new Vector3(0f, -legH * 0.4f, 0f), new Vector3(0f, -legH * 0.5f, 0.01f), 0.07f, 0.075f, new Color(0.15f, 0.12f, 0.1f), 8);
                });
            }
        }

        private static void BuildStar(ToonMeshBuilder b, float r, Color c)
        {
            b.Sphere(Vector3.zero, r * 0.45f, c, 8, 5);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                b.Cone(dir * r * 0.25f, dir * r * 1.1f, r * 0.3f, c, 4);
            }
        }

        public void SetLayer(int layer)
        {
            foreach (var t in Root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
        }
    }
}
