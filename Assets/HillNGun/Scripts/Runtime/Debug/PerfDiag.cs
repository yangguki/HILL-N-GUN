using System;
using UnityEngine;

namespace HillNGun.DebugTools
{
    /// <summary>
    /// Diagnostic-only feature switches for draw-call attribution runs (command line, never set by default):
    /// -diag-nodust (skip motion dust puffs), -diag-nolabels (hide actor name TextMeshes). Results measured with these
    /// switches are attribution evidence only; shipped builds never pass them.
    /// </summary>
    public static class PerfDiag
    {
        private static bool _init, _noDust, _noLabels, _noRig;
        private static void Init()
        {
            if (_init) return; _init = true;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.Equals("-diag-nodust", StringComparison.OrdinalIgnoreCase)) _noDust = true;
                if (a.Equals("-diag-nolabels", StringComparison.OrdinalIgnoreCase)) _noLabels = true;
                if (a.Equals("-diag-norig", StringComparison.OrdinalIgnoreCase)) _noRig = true;
            }
            if (_noDust || _noLabels || _noRig) Debug.Log($"[PerfDiag] DIAGNOSTIC run: noDust={_noDust} noLabels={_noLabels} noRig={_noRig} (attribution only)");
        }
        public static bool NoDust { get { Init(); return _noDust; } }
        public static bool NoLabels { get { Init(); return _noLabels; } }
        /// <summary>-diag-norig: cutout rigs fall back to the style-4 sprite (rig render-cost A/B only).</summary>
        public static bool NoRig { get { Init(); return _noRig; } }
    }
}
