using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    /// <summary>
    /// Screen layout in the safe-area panel. Units are canvas pixels (reference 2556x1179, match 0.5).
    /// Rects use a bottom-left origin so they map straight onto anchoredPosition.
    /// </summary>
    public static class ProtoSafeLayout
    {
        public const float MinButtonPoints = 44f;
        public const float IosPointsPerInch = 163f;
        public static readonly Vector2 Reference = new Vector2(2556f, 1179f);
        public const float Match = 0.5f;

        public struct PixelFrame
        {
            public float OrthoSize;
            public Vector2 Center;
            public float MinX, MaxX, MinY, MaxY;
        }

        public struct Slots
        {
            public Rect Move, Aim, Fire, Bomb, Roll, Skin, Hud, Result, Restart, Menu;
        }

        public static float PixelsPerPoint(float dpi, float screenW, float screenH)
        {
            if (dpi >= 50f) return dpi / IosPointsPerInch;
            float shortSide = Mathf.Min(screenW, screenH);
            if (shortSide >= 1000f) return 3f;
            if (shortSide >= 700f) return 2f;
            return 1f;
        }

        public static float CanvasScale(float screenW, float screenH)
        {
            if (screenW < 2f || screenH < 2f) return 1f;
            float lw = Mathf.Log(screenW / Reference.x, 2f);
            float lh = Mathf.Log(screenH / Reference.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(lw, lh, Match));
        }

        public static float MinButtonCanvas(float screenW, float screenH, float dpi)
        {
            float scale = Mathf.Max(0.01f, CanvasScale(screenW, screenH));
            return MinButtonPoints * PixelsPerPoint(dpi, screenW, screenH) / scale;
        }

        public static Slots Place(float panelW, float panelH, float minCanvas)
        {
            panelW = Mathf.Max(panelW, 320f);
            panelH = Mathf.Max(panelH, 220f);
            float btn = Mathf.Max(minCanvas, 96f);
            float stick = Mathf.Max(btn * 1.65f, btn + 8f);
            float gap = Mathf.Max(10f, btn * 0.16f);
            float margin = Mathf.Max(8f, btn * 0.1f);
            FitRow(panelW, ref btn, ref stick, ref gap, ref margin, minCanvas);

            float y = margin;
            float xRight = panelW - margin;
            var roll = new Rect(xRight - btn, y, btn, btn);
            var bomb = new Rect(roll.x - gap - btn, y, btn, btn);
            var fire = new Rect(bomb.x - gap - btn, y, btn, btn);
            var aim = new Rect(fire.x - gap - stick, y, stick, stick);
            var move = new Rect(margin, y, stick, stick);
            if (aim.xMin < move.xMax + gap)
            {
                float raised = margin + stick + gap;
                if (raised + btn <= panelH - margin)
                {
                    fire.y = bomb.y = roll.y = raised;
                    aim = new Rect(panelW - margin - stick, margin, stick, stick);
                }
            }

            // Skin / mode toggle: top-right HUD corner inside the safe area (not mid-dirt).
            float skinW = Mathf.Max(btn * 1.55f, minCanvas);
            float skinH = Mathf.Max(btn, minCanvas);
            var skin = new Rect(panelW - margin - skinW, panelH - margin - skinH, skinW, skinH);
            float hudH = Mathf.Clamp(Mathf.Max(48f, minCanvas * 0.55f), 36f, panelH * 0.22f);
            float hudW = Mathf.Max(80f, skin.xMin - gap - margin);
            var hud = new Rect(margin, panelH - margin - hudH, hudW, hudH);

            // End-result stack centered in the safe panel: result → 다시하기 → 메뉴로.
            float restartW = Mathf.Max(btn * 2.6f, minCanvas * 2f);
            float restartH = Mathf.Max(btn, minCanvas);
            float resultH = Mathf.Max(64f, hudH);
            float resultW = Mathf.Min(480f, panelW - margin * 2f);
            float stackH = resultH + gap + restartH + gap + restartH;
            float stackY = (panelH - stackH) * 0.5f;
            var menu = new Rect((panelW - restartW) * 0.5f, stackY, restartW, restartH);
            var restart = new Rect(menu.x, menu.yMax + gap, restartW, restartH);
            var result = new Rect((panelW - resultW) * 0.5f, restart.yMax + gap, resultW, resultH);
            ClampInside(ref result, panelW, panelH, margin);
            ClampInside(ref restart, panelW, panelH, margin);
            ClampInside(ref menu, panelW, panelH, margin);

            return new Slots
            {
                Move = move, Aim = aim, Fire = fire, Bomb = bomb, Roll = roll,
                Skin = skin, Hud = hud, Result = result, Restart = restart, Menu = menu
            };
        }

        public static bool Contains(Rect panel, Rect box, float slop = 0.75f)
        {
            return box.width > 1f && box.height > 1f
                && box.xMin >= panel.xMin - slop && box.yMin >= panel.yMin - slop
                && box.xMax <= panel.xMax + slop && box.yMax <= panel.yMax + slop;
        }

        public static PixelFrame PixelFit(float aspect) => PixelFit(aspect, -1.6f, 19.6f, 0f, 14.6f);

        public static PixelFrame PixelFit(float aspect, float minX, float maxX, float minY, float maxY)
        {
            aspect = Mathf.Max(aspect, 0.2f);
            float y0 = ProtoArena.ToPixel(new Vector2(0f, minY)).y;
            float y1 = ProtoArena.ToPixel(new Vector2(0f, maxY)).y;
            float halfW = (maxX - minX) * 0.5f;
            float halfH = (y1 - y0) * 0.5f;
            return new PixelFrame
            {
                OrthoSize = Mathf.Max(halfH, halfW / aspect),
                Center = new Vector2((minX + maxX) * 0.5f, (y0 + y1) * 0.5f),
                MinX = minX, MaxX = maxX, MinY = y0, MaxY = y1
            };
        }

        public static bool PixelShowsArena(float aspect) =>
            PixelShowsArena(aspect, -1.6f, 19.6f, 0f, 14.6f);

        /// <summary>
        /// True when a camera fitted to the given arena frame (optional pad, same as ProtoPresentation.FitPixel)
        /// still contains that frame. Used for 19.5:9 checks on TopDown and Frontline.
        /// </summary>
        public static bool PixelShowsArena(float aspect, float minX, float maxX, float minY, float maxY, float pad = 0f)
        {
            var cam = PixelFit(aspect, minX - pad, maxX + pad, minY - pad, maxY + pad);
            var want = PixelFit(aspect, minX, maxX, minY, maxY);
            float halfW = cam.OrthoSize * Mathf.Max(aspect, 0.2f);
            return want.MinX >= cam.Center.x - halfW - 0.02f
                && want.MaxX <= cam.Center.x + halfW + 0.02f
                && want.MinY >= cam.Center.y - cam.OrthoSize - 0.02f
                && want.MaxY <= cam.Center.y + cam.OrthoSize + 0.02f;
        }

        /// <summary>
        /// Uniform scale so the BG plate fits inside the target rect (contain / letterbox).
        /// Kept for callers that still need a contain measure.
        /// </summary>
        public static float BackgroundContainScale(float worldW, float worldH, float unitsW, float unitsH)
        {
            if (unitsW < 0.01f || unitsH < 0.01f || worldW < 0.01f || worldH < 0.01f) return 1f;
            return Mathf.Min(worldW / unitsW, worldH / unitsH);
        }

        /// <summary>
        /// Uniform scale so the BG plate covers the target rect (no letterbox). Edge crop is OK.
        /// </summary>
        public static float BackgroundCoverScale(float worldW, float worldH, float unitsW, float unitsH)
        {
            if (unitsW < 0.01f || unitsH < 0.01f || worldW < 0.01f || worldH < 0.01f) return 1f;
            return Mathf.Max(worldW / unitsW, worldH / unitsH);
        }

        static void FitRow(float panelW, ref float btn, ref float stick, ref float gap, ref float margin, float minCanvas)
        {
            for (int i = 0; i < 8; i++)
            {
                float need = margin + stick + gap + stick + gap + btn * 3f + gap * 2f + margin;
                if (need <= panelW) return;
                float scale = panelW / need;
                btn = Mathf.Max(minCanvas, btn * scale);
                stick = Mathf.Max(btn, stick * scale);
                gap = Mathf.Max(8f, gap * scale);
                margin = Mathf.Max(6f, margin * scale);
            }
        }

        static void ClampInside(ref Rect box, float panelW, float panelH, float margin)
        {
            if (box.width > panelW - margin * 2f) box.width = Mathf.Max(8f, panelW - margin * 2f);
            if (box.height > panelH - margin * 2f) box.height = Mathf.Max(8f, panelH - margin * 2f);
            box.x = Mathf.Clamp(box.x, margin, Mathf.Max(margin, panelW - margin - box.width));
            box.y = Mathf.Clamp(box.y, margin, Mathf.Max(margin, panelH - margin - box.height));
        }
    }
}
