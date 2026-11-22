using System;
using HillNGun.Proto.Greybox;
using UnityEngine;
using UnityEngine.UI;

namespace HillNGun.Proto.Greybox3D
{
    /// <summary>Surfaces 3D presenter failures without taking down the 2D player loop.</summary>
    public static class Proto_Greybox3DFault
    {
        public static string LastError { get; private set; }

        public static void ResetForTests() => LastError = null;

        public static void Report(string context, Exception e)
        {
            LastError = $"{context}: {e.GetType().Name}: {e.Message}";
            Debug.LogError("[Greybox3D] " + LastError + "\n" + e);
            Proto_GreyboxRuntimeDebugUi.SetFaultLine(LastError);
            EnsureBanner();
        }

        static void EnsureBanner()
        {
            if (string.IsNullOrEmpty(LastError)) return;
            var existing = GameObject.Find("Greybox3D_FaultBanner");
            Text text;
            if (existing == null)
            {
                var hud = GameObject.Find("Greybox_Hud");
                if (hud == null) return;
                var go = new GameObject("Greybox3D_FaultBanner", typeof(RectTransform));
                go.transform.SetParent(hud.transform, false);
                var rt = go.GetComponent<RectTransform>();
                Proto_GreyboxHudBuilder.ApplyRect(rt, 8f, 12f, 92f, 22f);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.35f, 0.05f, 0.05f, 0.82f);
                img.raycastTarget = false;
                var labelGo = new GameObject("Text", typeof(RectTransform));
                labelGo.transform.SetParent(go.transform, false);
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(8f, 4f);
                lrt.offsetMax = new Vector2(-8f, -4f);
                text = labelGo.AddComponent<Text>();
                text.raycastTarget = false;
                text.alignment = TextAnchor.UpperLeft;
                text.color = new Color(1f, 0.85f, 0.7f);
                text.fontSize = 16;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                           Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font != null) text.font = font;
            }
            else
                text = existing.GetComponentInChildren<Text>();
            if (text != null)
                text.text = "3D view error (2D should still work): " + LastError;
        }
    }
}
