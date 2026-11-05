using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using HillNGun.Core;

namespace HillNGun.UI
{
    /// <summary>
    /// A/B test helper for the character / cover scale presets: small corner label with the current option number
    /// (match scenes) and F1 / F2 / F3 shortcuts (PC). A change mid-fight restarts the current stage.
    /// Touch devices switch through the pause menu (비율 1/2/3 buttons).
    /// </summary>
    public class ScalePresetHud : MonoBehaviour
    {
        private static ScalePresetHud _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (_instance != null) return;
            var go = new GameObject("[ScalePresetHud]");
            _instance = go.AddComponent<ScalePresetHud>();
            DontDestroyOnLoad(go);
            Debug.Log($"[ScalePreset] current option {ScalePresets.Current} (default {ScalePresets.Data?.defaultOption}, cli {ScalePresets.CliOverride}) mul={ScalePresets.CharacterMul:F3}");
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || ScalePresets.Data == null) return;
            int n = kb.f1Key.wasPressedThisFrame ? 1 : kb.f2Key.wasPressedThisFrame ? 2 : kb.f3Key.wasPressedThisFrame ? 3 : 0;
            if (n > 0 && n != ScalePresets.Current) ScalePresets.SelectAndRestart(n);
        }

        private void OnGUI()
        {
            var d = ScalePresets.Data;
            if (d == null || !SceneManager.GetActiveScene().name.EndsWith("_Match")) return;
            Vector2 size = UIKit.Begin();
            GUI.depth = -40;
            var r = new Rect(size.x - 132f, 124f, 116f, 22f);
            UIKit.Label(r, string.Format(d.cornerLabelFormat, ScalePresets.Current), d.cornerLabelFontSize,
                new Color(1f, 1f, 1f, 0.75f), TextAnchor.MiddleRight, true);
            UIKit.End();
        }
    }
}
