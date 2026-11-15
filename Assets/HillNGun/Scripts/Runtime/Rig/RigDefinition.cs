using System.Collections.Generic;
using UnityEngine;

namespace HillNGun.Rig
{
    /// <summary>
    /// Per-character cutout rig data (Resources/Rig/{id}_rig.json): one atlas, three views (down_3q / side / up_3q), each with
    /// designer parts (sprite rect + pivot + parent bone + draw order) and named joints, in the designer's canvas space
    /// (px, origin top-left, y down). Bones are named after parts so a Spine/DragonBones export can replace it 1:1.
    /// </summary>
    [System.Serializable]
    public class RigDefinition
    {
        public string character;
        public string atlas;
        public int[] atlasSize;
        public float packScale = 0.5f;
        public string source, status;
        public RigView[] directions;
        /// <summary>Per-character secondary-motion part (Buster scarf / Raven cape / Volt wrench) and its spring gain / clamp.</summary>
        public string secondaryPart = "scarf";
        public float secondaryGain = 1.6f, secondaryMaxDeg = 28f, hatMaxDeg = 12f;
        /// <summary>Conservative per-bone rotation limits (prefix match) for WORKING-DRAFT parts; fullLimits for roll / down.</summary>
        public RigLimit[] limits, fullLimits;

        public static float Limit(RigLimit[] l, string bone, float deg)
        {
            if (l == null) return deg;
            foreach (var x in l) if (x != null && !string.IsNullOrEmpty(x.prefix) && bone.StartsWith(x.prefix)) return Mathf.Clamp(deg, -x.maxDeg, x.maxDeg);
            return deg;
        }

        public const float AtlasPPU = 100f;
        public static readonly string[] ViewNames = { "down_3q", "side", "up_3q" };

        public RigView View(string name)
        {
            if (directions != null) foreach (var d in directions) if (d.name == name) return d;
            return null;
        }

        /// <summary>canvas px -> rig units (1 atlas px = 1/AtlasPPU).</summary>
        public float CanvasToUnits => packScale / AtlasPPU;

        private static readonly Dictionary<string, RigDefinition> _cache = new Dictionary<string, RigDefinition>();
        public static RigDefinition Load(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            if (_cache.TryGetValue(characterId, out var c)) return c;
            var ta = Resources.Load<TextAsset>("Rig/" + characterId + "_rig");
            RigDefinition def = null;
            if (ta != null) { def = JsonUtility.FromJson<RigDefinition>(ta.text); if (def != null && (def.directions == null || def.directions.Length == 0)) def = null; }
            _cache[characterId] = def;
            return def;
        }
    }

    [System.Serializable]
    public class RigView
    {
        public string name;
        public float[] canvas;
        public float[] figureBBox; // x0,y0,x1,y1 canvas px
        public float groundX;
        public RigJoint[] joints;
        public string[] drawOrder;
        public bool supportIK, supportVisible;
        public RigPart[] parts;

        public RigPart Part(string n) { foreach (var p in parts) if (p.name == n) return p; return null; }
        public bool TryJoint(string n, out Vector2 canvasPx)
        {
            if (joints != null) foreach (var j in joints) if (j.name == n) { canvasPx = new Vector2(j.x, j.y); return true; }
            canvasPx = Vector2.zero; return false;
        }
        /// <summary>Ground point (canvas px): bottom of the figure under the hip centre.</summary>
        public Vector2 GroundCanvas => new Vector2(groundX, figureBBox[3]);
        public float FigureHeightPx => figureBBox[3] - figureBBox[1];
    }

    [System.Serializable] public class RigJoint { public string name; public float x, y; }
    [System.Serializable] public class RigLimit { public string prefix; public float maxDeg; }

    [System.Serializable]
    public class RigPart
    {
        public string name, sprite, joint, parent;
        public float[] rect;        // atlas px (x, y from bottom, w, h)
        public float[] pivot;       // normalized (Unity, y up)
        public float[] pivotCanvas; // canvas px (y down)
        public float[] posTopLeft, size;
        public int zorder;
        public bool far;
        public Vector2 PivotCanvas => new Vector2(pivotCanvas[0], pivotCanvas[1]);
    }

    // ------------------------------------------------------------------ clips
    [System.Serializable] public class RigKey { public float t; public float rot = Unset, x = Unset, y = Unset; public const float Unset = 1e9f; public static bool Has(float v) => v < 1e8f; }
    [System.Serializable] public class RigTrack { public string bone; public RigKey[] keys; }
    [System.Serializable] public class RigClip { public string name, layer; public bool loop; public float duration = 1f; public RigTrack[] tracks; }
    [System.Serializable] public class RigViewSettings { public string name; public float aimCenterDeg, legScale = 1f, liftScale = 1f; }

    [System.Serializable]
    public class RigSettings
    {
        public float crossfade = 0.08f, fullBlend = 0.08f, wholeBodyMotionScale = 0.3f, walkCycleTiles = 1.6f, runSpeed01 = 0.75f;
        public float viewDownBelowDeg = -30f, viewUpAboveDeg = 35f, viewHysteresisDeg = 6f;
        public float aimMaxDeg = 32f, aimTorsoShare = 0.22f, aimHeadShare = 0.3f;
        public float hatSpring = 0.9f, scarfSpring = 1.6f, springStiffness = 70f, springDamping = 9f;
        /// <summary>v16 throw (visual only): pull-back time, throw-only arm limits, share of the arc skipped for an instant release.</summary>
        public float throwPullback = 0.15f, throwArmLimitDeg = 160f, throwForearmLimitDeg = 90f, throwArcStartShare = 0.25f, throwSoftBandDeg = 15f;
        [Tooltip("v17: two-handed gun lowered (barrel down, deg) in the off hand while the near arm throws (view only)")] public float throwGunLowerDeg = 20f;
        public float throwGunDrop = 0.05f;
        public RigViewSettings[] views;
        public RigViewSettings View(string n) { if (views != null) foreach (var v in views) if (v.name == n) return v; return new RigViewSettings { name = n }; }
    }

    [System.Serializable]
    public class RigClipSet
    {
        public RigSettings settings = new RigSettings();
        public RigClip[] clips;

        public RigClip Clip(string n) { if (clips != null) foreach (var c in clips) if (c.name == n) return c; return null; }

        private static RigClipSet _shared;
        public static RigClipSet Shared
        {
            get
            {
                if (_shared != null) return _shared;
                var ta = Resources.Load<TextAsset>("Rig/rig_clips");
                _shared = ta != null ? JsonUtility.FromJson<RigClipSet>(ta.text) : new RigClipSet();
                if (_shared.settings == null) _shared.settings = new RigSettings();
                return _shared;
            }
        }

        /// <summary>Sample one track channel (rot/x/y) with smoothstep between keys; NaN channels are skipped.</summary>
        public static bool Sample(RigTrack tr, float t, int channel, out float v)
        {
            v = 0f;
            if (tr?.keys == null || tr.keys.Length == 0) return false;
            RigKey a = null, b = null;
            foreach (var k in tr.keys)
            {
                float kv = Ch(k, channel); if (!RigKey.Has(kv)) continue;
                if (k.t <= t) a = k;
                if (k.t >= t && b == null) b = k;
            }
            if (a == null && b == null) return false;
            if (a == null) { v = Ch(b, channel); return true; }
            if (b == null || b.t <= a.t) { v = Ch(a, channel); return true; }
            float u = Mathf.SmoothStep(0f, 1f, (t - a.t) / (b.t - a.t));
            v = Mathf.Lerp(Ch(a, channel), Ch(b, channel), u);
            return true;
        }
        private static float Ch(RigKey k, int c) => c == 0 ? k.rot : (c == 1 ? k.x : k.y);
    }
}
