using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum TimeOfDay { Noon, Afternoon, Sunset, Night }

    [Serializable]
    public class ParallaxLayer
    {
        [Tooltip("L0 Sky … L4 Cover/FX underlay. L3 = combat lane (no decor).")]
        public string id = "L0";
        public Sprite sprite;
        [Range(0f, 1.2f)] public float parallax = 0.1f;
        public Color tint = Color.white;
        public int sortingOrder = -40;
        [Tooltip("If true, never spawn ambient decor in this layer (L3 combat lane).")]
        public bool combatLane;
    }

    [CreateAssetMenu(fileName = "visual_", menuName = "HillNGun/Stage Visual Data")]
    public class StageVisualData : ScriptableObject
    {
        public string id = "visual_dust_1_1_noon";
        public TimeOfDay timeOfDay = TimeOfDay.Noon;
        [Tooltip("Global grading multiply applied over all layers")]
        public Color gradeTint = Color.white;
        [Range(0f, 2f)] public float gradeExposure = 1f;
        [Tooltip("Lantern / night additive lights (world logic x,y). Empty = none.")]
        public Vector2[] lanternPositions;
        public Color lanternColor = new Color(1f, 0.75f, 0.35f, 0.55f);
        [Tooltip("Exactly 5 slots L0..L4 (L5 FX/UI is code). Empty sprite = skip that slot.")]
        public ParallaxLayer[] layers = new ParallaxLayer[5];
        [Header("Ambient budgets (11 §6)")]
        [Range(0, 16)] public int dustMotes = 12;
        [Range(0, 8)] public int dustMotesLowTier = 4;
        [Range(0, 2)] public int tumbleweeds = 1;
        public bool saloonSignSway = true;
        [Tooltip("Darken/blur baked-in concept-art covers so StageData covers read clearly")]
        [Range(0f, 1f)] public float conceptCoverDim = 0.35f;

        public static void EnsureFiveLayers(StageVisualData v)
        {
            if (v == null) return;
            if (v.layers == null || v.layers.Length != 5)
            {
                var old = v.layers;
                v.layers = new ParallaxLayer[5];
                for (int i = 0; i < 5; i++)
                {
                    v.layers[i] = (old != null && i < old.Length && old[i] != null) ? old[i] : new ParallaxLayer { id = "L" + i };
                }
            }
            float[] para = { 0.12f, 0.35f, 0.55f, 0f, 0.9f };
            int[] ord = { -90, -80, -70, -60, -50 }; // above FieldView.BackgroundOrder (-100)
            for (int i = 0; i < 5; i++)
            {
                if (v.layers[i] == null) v.layers[i] = new ParallaxLayer();
                v.layers[i].id = "L" + i;
                v.layers[i].parallax = para[i];
                v.layers[i].sortingOrder = ord[i];
                v.layers[i].combatLane = i == 3;
            }
        }

        /// <summary>L3 combat lane must never receive decor props.</summary>
        public static bool AllowsDecorOnLayer(int layerIndex) => layerIndex != 3;
    }
}
