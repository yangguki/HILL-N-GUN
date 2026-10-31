using System;
using UnityEngine;

namespace HillNGun.Data
{
    public enum TutorialTrigger { Tap, Timer, Move, RowChange, Shoot, Roll, Grenade, Skill }

    [Serializable]
    public class TutorialStep
    {
        public string textKey;
        [Tooltip("Shown instead of textKey on keyboard/mouse devices (optional)")]
        public string textKeyDesktop;
        public TutorialTrigger trigger = TutorialTrigger.Timer;
        [Tooltip("Step stays at least this long")]
        public float minSeconds = 1.5f;
        [Tooltip("Auto-advance after this long even if the action was not done (0 = never)")]
        public float maxSeconds = 12f;
    }

    /// <summary>First-PvE onboarding hints (FP 202: "PvE가 튜토리얼을 겸함", decision D13). Shown once; replay from Settings.</summary>
    [CreateAssetMenu(fileName = "tutorial_default", menuName = "HillNGun/Tutorial")]
    public class TutorialData : ScriptableObject
    {
        public bool enabled = true;
        [Tooltip("Only on this PvE stage index of a run (0 = first stage)")]
        public int stageIndex = 0;
        public float startDelay = 1.0f;
        public TutorialStep[] steps = new TutorialStep[0];
    }
}
