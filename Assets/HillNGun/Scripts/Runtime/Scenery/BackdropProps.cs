using System.Collections.Generic;
using UnityEngine;
using HillNGun.Data;

namespace HillNGun.Scenery
{
    public static partial class BackdropBaker
    {
        // ------------------------------------------------------------------ buildings

        private static Color WallColor(StageThemeData t, int i)
        {
            switch (((i % 3) + 3) % 3) { case 0: return t.wallA; case 1: return t.wallB; default: return t.wallC; }
        }

        private static int StyleFor(string sign)
        {
            if (string.IsNullOrEmpty(sign)) return 0;
            var s = sign.ToUpperInvariant();
            if (s.Contains("SALOON")) return 1;
            if (s.Contains("SHERIFF")) return 2;
            if (s.Contains("BANK") || s.Contains("HOTEL")) return 3;
            if (s.Contains("FREIGHT") || s.Contains("STORAGE") || s.Contains("WATER") || s.Contains("LIVERY")) return 4;
            return 0;
        }

        /// <summary>
        /// One false-front building beside the street: a street-facing side wall receding in depth plus the front
        /// facade facing the viewer. Always outside the field edge (+ boardwalk), never over the lanes.
        /// </summary>
        private static void Facade(Job j, Ctx c, int side, float ly, float wU, float hU, string sign, int wallIdx, float walkU = 1.3f, int forceStyle = -1)
        {
            var t = c.t;
            var g = j.G;
            const float depth = 4.3f;
            float s = c.S(ly), s2 = c.S(ly + depth);
            float xi = c.E(side, ly).x + side * walkU * s;
            float xi2 = c.E(side, ly + depth).x + side * walkU * s2;
            float y0 = c.W(0, ly).y, y1 = c.W(0, ly + depth).y;
            float H = hU * s, H2 = hU * s2;
            var wall = WallColor(t, wallIdx);
            int style = forceStyle >= 0 ? forceStyle : StyleFor(sign);
            var dark = t.roof;

            // street-facing side wall (in shade), receding to the next building
            var sideCol = Shade(wall, 0.7f);
            g.Quad(new Vector2(xi, y0), new Vector2(xi2, y1), new Vector2(xi2, y1 + H2 * 0.82f), new Vector2(xi, y0 + H * 0.82f), Shade(sideCol, 0.9f), Shade(sideCol, 0.9f), sideCol, sideCol);
            for (int k = 1; k < 6; k++)
            {
                float f = k / 6f;
                g.Line(new Vector2(xi, y0 + H * 0.82f * f), new Vector2(xi2, y1 + H2 * 0.82f * f), 0.025f, A(dark, 0.25f));
            }
            g.Line(new Vector2(xi, y0 + H * 0.82f), new Vector2(xi2, y1 + H2 * 0.82f), 0.07f * s, Shade(dark, 0.9f));
            // side porch roof sticking out toward the street
            float aw = 0.5f * walkU;
            g.Quad(new Vector2(xi, y0 + H * 0.44f), new Vector2(xi2, y1 + H2 * 0.44f), new Vector2(xi2 - side * aw * s2, y1 + H2 * 0.40f), new Vector2(xi - side * aw * s, y0 + H * 0.40f), Shade(dark, 1.1f));

            // front facade
            float xo = xi + side * wU * s;
            float xL = Mathf.Min(xi, xo), xR = Mathf.Max(xi, xo);
            // lay the details (sign, door, windows) out over the on-screen part of the facade so signs are not cut
            float vL = Mathf.Max(xL, c.view.xMin + 0.15f), vR = Mathf.Min(xR, c.view.xMax - 0.15f);
            float cL = xL, cR = xR;
            if (vR - vL > 2.6f * s) { cL = vL; cR = vR; }
            float W = cR - cL;
            float mid = (cL + cR) * 0.5f;
            // ground contact shade
            float fullW = xR - xL, fullMid = (xL + xR) * 0.5f;
            g.SoftEllipse(new Vector2(fullMid, y0 - 0.05f * s), fullW * 0.6f, 0.35f * s, A(t.groundShadow, t.groundShadow.a * 1.2f), 20, 0.4f);
            g.RectV(xL, y0, xR, y0 + H * 0.82f, Shade(wall, 0.86f), wall);
            for (float py = y0 + 0.24f * s; py < y0 + H * 0.82f; py += 0.24f * s) g.Line(new Vector2(xL, py), new Vector2(xR, py), 0.02f, A(dark, 0.18f));
            // stepped false front + trim cap
            float pw = style == 4 ? W : W * 0.72f;
            g.Rect(xL, y0 + H * 0.80f, xR, y0 + H * 0.9f, wall);
            float ppw = style == 4 ? fullW : fullW * 0.72f;
            g.Rect(fullMid - ppw * 0.5f, y0 + H * 0.88f, fullMid + ppw * 0.5f, y0 + H, wall);
            g.Rect(mid - pw * 0.5f, y0 + H * 0.88f, mid + pw * 0.5f, y0 + H, wall);
            g.Rect(xL - 0.05f * s, y0 + H * 0.895f, xR + 0.05f * s, y0 + H * 0.915f, t.trim);
            g.Rect(Mathf.Min(mid - pw * 0.5f, fullMid - ppw * 0.5f) - 0.05f * s, y0 + H * 0.985f, Mathf.Max(mid + pw * 0.5f, fullMid + ppw * 0.5f) + 0.05f * s, y0 + H * 1.01f, t.trim);
            if (style == 4) g.Tri(new Vector2(xL - 0.1f * s, y0 + H * 0.9f), new Vector2(xR + 0.1f * s, y0 + H * 0.9f), new Vector2(fullMid, y0 + H * 1.12f), Shade(dark, 1.05f));
            // corner boards
            g.Rect(xL, y0, xL + 0.08f * s, y0 + H * 0.9f, Shade(t.trim, 0.85f));
            g.Rect(xR - 0.08f * s, y0, xR, y0 + H * 0.9f, Shade(t.trim, 0.85f));

            // sign board
            if (!string.IsNullOrEmpty(sign))
            {
                float sw = pw * 0.86f, sh = H * 0.12f, sy = y0 + H * 0.86f;
                g.Rect(mid - sw * 0.5f - 0.04f * s, sy - sh * 0.5f - 0.04f * s, mid + sw * 0.5f + 0.04f * s, sy + sh * 0.5f + 0.04f * s, Shade(dark, 0.9f));
                g.Rect(mid - sw * 0.5f, sy - sh * 0.5f, mid + sw * 0.5f, sy + sh * 0.5f, t.trim);
            }

            // upper windows
            int nW = W > 5f ? 3 : 2;
            for (int k = 0; k < nW; k++)
            {
                float cx = xL + W * (k + 1f) / (nW + 1f), ww = W * 0.1f;
                bool lit = t.litWindows > 0f && Hash(ly * 13f + k + side * 5f) < t.litWindows;
                Window(g, t, cx, y0 + H * 0.52f, ww, H * 0.17f, lit, s, style == 2 && k == 0);
            }
            if (style == 1) // saloon balcony rail
            {
                g.Rect(xL, y0 + H * 0.47f, xR, y0 + H * 0.49f, t.trim);
                for (float bx = xL + 0.15f * s; bx < xR; bx += 0.22f * s) g.Rect(bx, y0 + H * 0.47f, bx + 0.04f * s, y0 + H * 0.53f, Shade(t.trim, 0.9f));
                g.Rect(xL, y0 + H * 0.53f, xR, y0 + H * 0.55f, t.trim);
            }

            if (style == 4)
            {
                // warehouse barn door with X brace
                float dw = W * 0.34f, dh = H * 0.5f;
                g.Rect(mid - dw * 0.5f, y0, mid + dw * 0.5f, y0 + dh, Shade(wall, 0.72f));
                g.Line(new Vector2(mid - dw * 0.5f, y0), new Vector2(mid + dw * 0.5f, y0 + dh), 0.08f * s, Shade(t.trim, 0.8f));
                g.Line(new Vector2(mid + dw * 0.5f, y0), new Vector2(mid - dw * 0.5f, y0 + dh), 0.08f * s, Shade(t.trim, 0.8f));
                g.Rect(mid - dw * 0.5f - 0.05f * s, y0 + dh, mid + dw * 0.5f + 0.05f * s, y0 + dh + 0.06f * s, Shade(t.trim, 0.8f));
            }
            else
            {
                // porch shade, door, ground-floor windows
                g.RectV(xL, y0, xR, y0 + H * 0.4f, A(t.groundShadow, t.groundShadow.a * 0.6f), A(t.groundShadow, t.groundShadow.a * 1.1f));
                float dw = W * 0.15f, dh = H * 0.31f;
                g.Rect(mid - dw * 0.5f - 0.05f * s, y0, mid + dw * 0.5f + 0.05f * s, y0 + dh + 0.05f * s, t.trim);
                g.Rect(mid - dw * 0.5f, y0, mid + dw * 0.5f, y0 + dh, Shade(t.windowColor, 1.1f));
                if (style == 1)
                {
                    var sd = Mix(t.wallB, t.trim, 0.25f);
                    g.Rect(mid - dw * 0.48f, y0 + dh * 0.25f, mid - 0.02f * s, y0 + dh * 0.72f, sd);
                    g.Rect(mid + 0.02f * s, y0 + dh * 0.25f, mid + dw * 0.48f, y0 + dh * 0.72f, sd);
                    g.Line(new Vector2(mid - dw * 0.4f, y0 + dh * 0.62f), new Vector2(mid - 0.06f * s, y0 + dh * 0.62f), 0.025f, Shade(sd, 0.7f));
                    g.Line(new Vector2(mid + 0.06f * s, y0 + dh * 0.62f), new Vector2(mid + dw * 0.4f, y0 + dh * 0.62f), 0.025f, Shade(sd, 0.7f));
                }
                float gw = W * 0.18f;
                bool lit2 = t.litWindows > 0f && Hash(ly * 7f + side) < t.litWindows;
                Window(g, t, mid - W * 0.3f, y0 + H * 0.1f, gw, H * 0.2f, lit2, s, style == 2);
                Window(g, t, mid + W * 0.3f, y0 + H * 0.1f, gw, H * 0.2f, lit2, s, false);
                // awning + posts
                g.Quad(new Vector2(xL - 0.12f * s, y0 + H * 0.4f), new Vector2(xR + 0.12f * s, y0 + H * 0.4f), new Vector2(xR + 0.05f * s, y0 + H * 0.46f), new Vector2(xL - 0.05f * s, y0 + H * 0.46f), Shade(dark, 1.15f));
                g.Rect(xL - 0.12f * s, y0 + H * 0.39f, xR + 0.12f * s, y0 + H * 0.405f, Shade(dark, 0.8f));
                int posts = Mathf.Max(2, Mathf.RoundToInt(fullW / (1.8f * s)) + 1);
                for (int k = 0; k < posts; k++)
                {
                    float px = Mathf.Lerp(xL + 0.04f * s, xR - 0.12f * s, k / (float)(posts - 1));
                    g.Rect(px, y0 - 0.02f, px + 0.08f * s, y0 + H * 0.4f, Shade(dark, 1.25f));
                }
            }

            if (style == 2) Star(g, new Vector2(mid, y0 + H * 0.7f), H * 0.075f, new Color(0.98f, 0.8f, 0.3f), Shade(dark, 0.8f));
            if (style == 3) for (int k = 0; k < 2; k++) { float px = mid + (k == 0 ? -1f : 1f) * W * 0.12f; g.Rect(px - 0.06f * s, y0, px + 0.06f * s, y0 + H * 0.4f, Shade(t.trim, 0.92f)); }

            if (!string.IsNullOrEmpty(sign))
            {
                float sw = pw * 0.86f, sh = H * 0.12f, sy = y0 + H * 0.86f;
                j.Text(sign, new Vector2(mid, sy), sh * 0.62f, sw * 0.9f, t.signText);
            }
        }

        private static void Window(Geo g, StageThemeData t, float cx, float y, float w, float h, bool lit, float s, bool bars)
        {
            g.Rect(cx - w * 0.5f - 0.05f * s, y - 0.05f * s, cx + w * 0.5f + 0.05f * s, y + h + 0.05f * s, t.trim);
            g.RectV(cx - w * 0.5f, y, cx + w * 0.5f, y + h, lit ? t.windowLit : Shade(t.windowColor, 1.2f), lit ? Shade(t.windowLit, 0.85f) : t.windowColor);
            g.Rect(cx - 0.015f * s, y, cx + 0.015f * s, y + h, A(t.trim, 0.8f));
            g.Rect(cx - w * 0.5f, y + h * 0.48f, cx + w * 0.5f, y + h * 0.52f, A(t.trim, 0.8f));
            if (bars) for (int i = 1; i < 4; i++) { float bx = cx - w * 0.5f + w * i / 4f; g.Rect(bx - 0.012f, y, bx + 0.012f, y + h, new Color(0.15f, 0.15f, 0.16f)); }
        }

        private static void Star(Geo g, Vector2 c, float r, Color col, Color edge)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                float rr = pass == 0 ? r * 1.18f : r;
                var cc = pass == 0 ? edge : col;
                var inner = new Vector2[5];
                for (int i = 0; i < 5; i++) { float a = Mathf.PI * 0.5f + (i + 0.5f) * Mathf.PI * 2f / 5f; inner[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr * 0.42f; }
                g.Poly(inner, cc);
                for (int i = 0; i < 5; i++)
                {
                    float a = Mathf.PI * 0.5f + i * Mathf.PI * 2f / 5f;
                    var tip = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr;
                    g.Tri(inner[(i + 4) % 5], inner[i], tip, cc);
                }
            }
        }

        private static float Hash(float x)
        {
            float h = Mathf.Sin(x * 12.9898f + 78.233f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        // ------------------------------------------------------------------ props (logic-position wrappers)

        private static void Fence(Geo g, Ctx c, float ly)
        {
            var t = c.t;
            float s = c.S(ly);
            float a = -0.8f, b = c.m.sizeX + 0.8f;
            float h = 0.5f * s;
            var p0 = c.W(a, ly); var p1 = c.W(b, ly);
            g.SoftLine(p0 + new Vector2(0.1f, -0.06f), p1 + new Vector2(0.1f, -0.06f), 0.14f * s, A(t.groundShadow, t.groundShadow.a));
            var wood = t.fenceColor;
            for (float lx = a; lx <= b + 0.01f; lx += 1.6f)
            {
                var p = c.W(lx, ly);
                float lean = 0.03f * Mathf.Sin(lx * 3.1f);
                g.Quad(p + new Vector2(-0.05f * s, 0f), p + new Vector2(0.05f * s, 0f), p + new Vector2(0.05f * s + lean, h * 1.08f), p + new Vector2(-0.05f * s + lean, h * 1.08f), Shade(wood, 0.8f), Shade(wood, 0.8f), wood, wood);
            }
            foreach (float f in new[] { 0.42f, 0.82f })
            {
                g.Line(p0 + new Vector2(0f, h * f), p1 + new Vector2(0f, h * f), 0.07f * s, wood);
                g.Line(p0 + new Vector2(0f, h * f + 0.02f * s), p1 + new Vector2(0f, h * f + 0.02f * s), 0.02f * s, Mix(wood, Color.white, 0.25f));
            }
        }

        private static void RockItem(Geo g, Ctx c, float lx, float ly, float szU) => RockW(g, c.t, c.W(lx, ly), szU * c.S(ly), Hash(lx * 3.7f + ly));
        private static void Barrel(Geo g, Ctx c, float lx, float ly) => BarrelW(g, c.t, c.W(lx, ly), c.S(ly));
        private static void CrateItem(Geo g, Ctx c, float lx, float ly, float k = 1f) => CrateW(g, c.t, c.W(lx, ly), c.S(ly) * k);
        private static void Cactus(Geo g, Ctx c, float lx, float ly, float hU) => CactusW(g, c.t, c.W(lx, ly), hU * c.S(ly), Hash(lx + ly * 9f));
        private static void Skull(Geo g, Ctx c, float lx, float ly) => SkullW(g, c.t, c.W(lx, ly), c.S(ly), Hash(lx * 5f + ly) < 0.5f);
        private static void StaticWeed(Geo g, Ctx c, float lx, float ly) => WeedW(g, c.t, c.W(lx, ly), c.S(ly), Hash(lx + ly));

        private static void HitchRail(Geo g, Ctx c, float lx, float ly)
        {
            var t = c.t; float s = c.S(ly);
            var p = c.W(lx, ly);
            float h = 0.55f * s;
            g.SoftEllipse(p + new Vector2(0.05f, -0.03f), 0.18f * s, 0.06f * s, A(t.groundShadow, t.groundShadow.a), 10);
            g.SoftEllipse(c.W(lx, ly + 1.2f) + new Vector2(0.05f, -0.03f), 0.18f * s, 0.06f * s, A(t.groundShadow, t.groundShadow.a), 10);
            var a = c.W(lx, ly); var b = c.W(lx, ly + 1.2f);
            g.Rect(a.x - 0.04f * s, a.y, a.x + 0.04f * s, a.y + h, Shade(t.fenceColor, 0.85f));
            g.Rect(b.x - 0.04f * s, b.y, b.x + 0.04f * s, b.y + h, Shade(t.fenceColor, 0.85f));
            g.Line(a + new Vector2(0f, h * 0.92f), b + new Vector2(0f, h * 0.92f), 0.07f * s, t.fenceColor);
        }

        private static void Sacks(Geo g, Ctx c, float lx, float ly)
        {
            var t = c.t; float s = c.S(ly); var p = c.W(lx, ly);
            var sack = new Color(0.82f, 0.74f, 0.58f);
            g.SoftEllipse(p + new Vector2(0.05f, -0.02f), 0.42f * s, 0.1f * s, A(t.groundShadow, t.groundShadow.a), 12);
            g.Ellipse(p + new Vector2(-0.16f * s, 0.16f * s), 0.2f * s, 0.17f * s, Shade(sack, 0.9f), 12);
            g.Ellipse(p + new Vector2(0.16f * s, 0.15f * s), 0.21f * s, 0.16f * s, sack, 12);
            g.Ellipse(p + new Vector2(0f, 0.36f * s), 0.19f * s, 0.14f * s, Mix(sack, Color.white, 0.12f), 12);
        }

        private static void WaterTower(Geo g, Ctx c, float lx, float ly)
        {
            var t = c.t; float s = c.S(ly); var p = c.W(lx, ly);
            float h = 4.2f * s, w = 2.2f * s;
            var wood = Mix(t.wallB, t.roof, 0.35f);
            g.SoftEllipse(p + new Vector2(0.2f, -0.05f), w * 0.8f, 0.3f * s, A(t.groundShadow, t.groundShadow.a), 16);
            float legTop = h * 0.55f;
            foreach (float f in new[] { -0.42f, -0.14f, 0.14f, 0.42f })
                g.Line(p + new Vector2(w * f * 1.1f, 0f), p + new Vector2(w * f * 0.85f, legTop), 0.1f * s, Shade(wood, 0.75f));
            g.Line(p + new Vector2(-w * 0.45f, legTop * 0.1f), p + new Vector2(w * 0.4f, legTop * 0.9f), 0.05f * s, Shade(wood, 0.7f));
            g.Line(p + new Vector2(w * 0.45f, legTop * 0.1f), p + new Vector2(-w * 0.4f, legTop * 0.9f), 0.05f * s, Shade(wood, 0.7f));
            g.Rect(p.x - w * 0.42f, p.y + legTop * 0.5f, p.x + w * 0.42f, p.y + legTop * 0.5f + 0.05f * s, Shade(wood, 0.7f));
            float ty0 = legTop, ty1 = h * 0.9f;
            g.Quad(p + new Vector2(-w * 0.5f, ty0), p + new Vector2(w * 0.5f, ty0), p + new Vector2(w * 0.5f, ty1), p + new Vector2(-w * 0.5f, ty1), Shade(wood, 0.85f), wood, wood, Shade(wood, 0.85f));
            for (float f = -0.4f; f < 0.5f; f += 0.13f) g.Line(p + new Vector2(w * f, ty0), p + new Vector2(w * f, ty1), 0.02f, A(t.roof, 0.35f));
            foreach (float f in new[] { 0.2f, 0.75f }) g.Line(p + new Vector2(-w * 0.52f, Mathf.Lerp(ty0, ty1, f)), p + new Vector2(w * 0.52f, Mathf.Lerp(ty0, ty1, f)), 0.06f * s, new Color(0.3f, 0.28f, 0.28f));
            g.Tri(p + new Vector2(-w * 0.6f, ty1), p + new Vector2(w * 0.6f, ty1), p + new Vector2(0f, h * 1.12f), Shade(t.roof, 1.1f));
            // spout
            g.Line(p + new Vector2(w * 0.5f, ty0 + 0.2f * s), p + new Vector2(w * 0.85f, ty0 - 0.3f * s), 0.09f * s, new Color(0.3f, 0.28f, 0.28f));
        }

        private static void Crossbuck(Job j, Ctx c, float lx, float ly)
        {
            var g = j.G; var t = c.t; float s = c.S(ly); var p = c.W(lx, ly);
            float h = 1.6f * s;
            g.SoftEllipse(p + new Vector2(0.06f, -0.03f), 0.18f * s, 0.06f * s, A(t.groundShadow, t.groundShadow.a), 10);
            g.Rect(p.x - 0.045f * s, p.y, p.x + 0.045f * s, p.y + h, new Color(0.9f, 0.88f, 0.84f));
            var cc = p + new Vector2(0f, h * 0.85f);
            var d1 = new Vector2(0.5f, 0.3f) * s; var d2 = new Vector2(0.5f, -0.3f) * s;
            g.Line(cc - d1, cc + d1, 0.12f * s, new Color(0.95f, 0.94f, 0.9f));
            g.Line(cc - d2, cc + d2, 0.12f * s, new Color(0.95f, 0.94f, 0.9f));
            g.Line(cc - d1 * 0.9f, cc + d1 * 0.9f, 0.03f * s, new Color(0.2f, 0.18f, 0.18f));
            g.Line(cc - d2 * 0.9f, cc + d2 * 0.9f, 0.03f * s, new Color(0.2f, 0.18f, 0.18f));
        }

        private static void TelegraphPole(Geo g, Ctx c, float lx, float ly)
        {
            var t = c.t; float s = c.S(ly); var p = c.W(lx, ly);
            float h = 3.0f * s;
            var wood = Shade(t.fenceColor, 0.9f);
            g.SoftEllipse(p + new Vector2(0.06f, -0.03f), 0.16f * s, 0.05f * s, A(t.groundShadow, t.groundShadow.a), 10);
            g.Quad(p + new Vector2(-0.06f * s, 0f), p + new Vector2(0.06f * s, 0f), p + new Vector2(0.04f * s, h), p + new Vector2(-0.04f * s, h), wood);
            g.Rect(p.x - 0.4f * s, p.y + h * 0.9f, p.x + 0.4f * s, p.y + h * 0.9f + 0.07f * s, wood);
            foreach (float f in new[] { -0.32f, -0.12f, 0.12f, 0.32f }) g.Ellipse(p + new Vector2(f * s, h * 0.9f + 0.11f * s), 0.035f * s, 0.045f * s, new Color(0.6f, 0.75f, 0.7f), 6);
        }

        // ------------------------------------------------------------------ props (world-position core)

        private static void RockW(Geo g, StageThemeData t, Vector2 p, float size, float h)
        {
            var col = Shade(t.rockColor, 0.9f + h * 0.2f);
            g.SoftEllipse(p + new Vector2(size * 0.25f, -size * 0.05f), size * 1.25f, size * 0.3f, A(t.groundShadow, t.groundShadow.a * 1.1f), 14);
            g.Ellipse(p + new Vector2(0f, size * 0.42f), size, size * 0.55f, Shade(col, 0.78f), 7, h * 3f);
            g.Ellipse(p + new Vector2(-size * 0.12f, size * 0.55f), size * 0.78f, size * 0.42f, col, 7, h * 5f);
            g.Ellipse(p + new Vector2(-size * 0.3f, size * 0.72f), size * 0.38f, size * 0.2f, Mix(col, Color.white, 0.22f), 6, h * 2f);
        }

        private static void BarrelW(Geo g, StageThemeData t, Vector2 p, float s)
        {
            float w = 0.26f * s, h = 0.62f * s;
            var wood = Mix(t.fenceColor, t.wallB, 0.3f);
            g.SoftEllipse(p + new Vector2(0.08f * s, -0.02f), w * 1.5f, 0.09f * s, A(t.groundShadow, t.groundShadow.a * 1.1f), 12);
            g.Quad(p + new Vector2(-w, 0f), p + new Vector2(w, 0f), p + new Vector2(w, h), p + new Vector2(-w, h), Shade(wood, 0.75f), Shade(wood, 1.05f), Shade(wood, 1.05f), Shade(wood, 0.75f));
            g.Ellipse(p + new Vector2(0f, h * 0.5f), w * 1.06f, h * 0.48f, A(Shade(wood, 1.05f), 0.5f), 14);
            foreach (float f in new[] { 0.16f, 0.84f }) g.Rect(p.x - w * 1.02f, p.y + h * f - 0.025f * s, p.x + w * 1.02f, p.y + h * f + 0.025f * s, new Color(0.25f, 0.22f, 0.2f));
            g.Ellipse(p + new Vector2(0f, h), w, w * 0.32f, Mix(wood, Color.white, 0.18f), 14);
        }

        private static void CrateW(Geo g, StageThemeData t, Vector2 p, float s)
        {
            float w = 0.3f * s, h = 0.48f * s, d = 0.14f * s;
            var wood = Mix(t.wallB, t.trim, 0.25f);
            g.SoftEllipse(p + new Vector2(0.1f * s, -0.02f), w * 1.6f, 0.09f * s, A(t.groundShadow, t.groundShadow.a * 1.1f), 12);
            g.Rect(p.x - w, p.y, p.x + w, p.y + h, wood);
            g.Quad(p + new Vector2(-w, h), p + new Vector2(w, h), p + new Vector2(w + d * 0.4f, h + d), p + new Vector2(-w + d * 0.4f, h + d), Mix(wood, Color.white, 0.2f));
            g.Quad(p + new Vector2(w, 0f), p + new Vector2(w + d * 0.4f, d), p + new Vector2(w + d * 0.4f, h + d), p + new Vector2(w, h), Shade(wood, 0.72f));
            var line = Shade(wood, 0.62f);
            g.Line(p + new Vector2(-w, 0f), p + new Vector2(w, h), 0.035f * s, line);
            g.Rect(p.x - w, p.y, p.x + w, p.y + 0.04f * s, line);
            g.Rect(p.x - w, p.y + h - 0.04f * s, p.x + w, p.y + h, line);
            g.Rect(p.x - w, p.y, p.x - w + 0.04f * s, p.y + h, line);
            g.Rect(p.x + w - 0.04f * s, p.y, p.x + w, p.y + h, line);
        }

        private static void CactusW(Geo g, StageThemeData t, Vector2 p, float h, float rnd)
        {
            var col = t.cactusColor;
            var dark = Shade(col, 0.72f); var lite = Mix(col, new Color(0.85f, 0.95f, 0.7f), 0.3f);
            float w = h * 0.13f;
            g.SoftEllipse(p + new Vector2(h * 0.18f, -0.02f), h * 0.32f, h * 0.07f, A(t.groundShadow, t.groundShadow.a * 1.1f), 14);
            void Stem(float x0, float y0, float x1, float y1, float ww)
            {
                g.Quad(new Vector2(x0 - ww, y0), new Vector2(x0 + ww, y0), new Vector2(x1 + ww, y1), new Vector2(x1 - ww, y1), dark, col, col, dark);
                g.Ellipse(new Vector2(x1, y1), ww, ww * 0.9f, col, 10);
                g.Line(new Vector2(x0 - ww * 0.25f, y0), new Vector2(x1 - ww * 0.25f, y1), ww * 0.22f, lite);
            }
            Stem(p.x, p.y, p.x, p.y + h, w);
            float side = rnd < 0.5f ? -1f : 1f;
            float ay = p.y + h * (0.35f + rnd * 0.15f);
            g.Line(new Vector2(p.x, ay), new Vector2(p.x + side * w * 2.3f, ay), w * 1.3f, col);
            Stem(p.x + side * w * 2.3f, ay - w * 0.4f, p.x + side * w * 2.3f, ay + h * 0.32f, w * 0.75f);
            if (rnd > 0.3f)
            {
                float by = p.y + h * (0.5f + rnd * 0.1f);
                g.Line(new Vector2(p.x, by), new Vector2(p.x - side * w * 2f, by), w * 1.1f, col);
                Stem(p.x - side * w * 2f, by - w * 0.4f, p.x - side * w * 2f, by + h * 0.24f, w * 0.65f);
            }
        }

        private static void SkullW(Geo g, StageThemeData t, Vector2 p, float s, bool flip)
        {
            var bone = t.boneColor; var dark = Shade(bone, 0.55f);
            float k = flip ? -1f : 1f;
            g.SoftEllipse(p + new Vector2(0.04f * s, -0.01f), 0.32f * s, 0.07f * s, A(t.groundShadow, t.groundShadow.a), 12);
            g.Ellipse(p + new Vector2(0f, 0.13f * s), 0.17f * s, 0.12f * s, bone, 14);
            g.Quad(p + new Vector2(-0.07f * s, 0.02f * s), p + new Vector2(0.07f * s, 0.02f * s), p + new Vector2(0.11f * s, 0.14f * s), p + new Vector2(-0.11f * s, 0.14f * s), bone);
            g.Ellipse(p + new Vector2(-0.07f * s, 0.15f * s), 0.035f * s, 0.03f * s, dark, 8);
            g.Ellipse(p + new Vector2(0.07f * s, 0.15f * s), 0.035f * s, 0.03f * s, dark, 8);
            g.Ellipse(p + new Vector2(0f, 0.04f * s), 0.03f * s, 0.018f * s, dark, 6);
            // horns
            g.Line(p + new Vector2(-0.15f * s, 0.2f * s), p + new Vector2(-0.3f * s, 0.24f * s), 0.05f * s, bone);
            g.Line(p + new Vector2(-0.3f * s, 0.24f * s), p + new Vector2(-0.34f * s, 0.33f * s * k), 0.04f * s, bone);
            g.Line(p + new Vector2(0.15f * s, 0.2f * s), p + new Vector2(0.3f * s, 0.24f * s), 0.05f * s, bone);
            g.Line(p + new Vector2(0.3f * s, 0.24f * s), p + new Vector2(0.34f * s, 0.33f * s), 0.04f * s, bone);
        }

        private static void WeedW(Geo g, StageThemeData t, Vector2 p, float s, float h)
        {
            float r = 0.3f * s;
            var c0 = Mix(t.grass, new Color(0.55f, 0.4f, 0.25f), 0.5f);
            var cc = p + new Vector2(0f, r);
            g.SoftEllipse(p + new Vector2(0.05f * s, -0.01f), r * 1.1f, r * 0.25f, A(t.groundShadow, t.groundShadow.a), 12);
            g.Ellipse(cc, r * 0.85f, r * 0.85f, A(Shade(c0, 0.7f), 0.35f), 14);
            float a = h * 10f;
            for (int i = 0; i < 16; i++)
            {
                a += 2.39996f;
                float b = a + 1.7f + Hash(h + i) * 1.2f;
                var p1 = cc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * (0.7f + 0.3f * Hash(i + h * 3f));
                var p2 = cc + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r * (0.6f + 0.4f * Hash(i * 2f + h));
                g.Line(p1, p2, 0.022f * s, i % 3 == 0 ? Shade(c0, 0.7f) : c0);
            }
        }

        // ------------------------------------------------------------------ vignette

        private static void Vignette(Geo g, Rect r, Rect view, StageThemeData t)
        {
            if (t.vignette <= 0.001f) return;
            const int nx = 16, ny = 10;
            var cc = view.center;
            Color At(float x, float y)
            {
                float dx = (x - cc.x) / (view.width * 0.5f), dy = (y - cc.y) / (view.height * 0.5f);
                float d = Mathf.Sqrt(dx * dx * 0.7f + dy * dy * 0.55f);
                float a = t.vignette * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.15f, d));
                return A(t.vignetteColor, Mathf.Clamp01(a));
            }
            for (int iy = 0; iy < ny; iy++)
                for (int ix = 0; ix < nx; ix++)
                {
                    float x0 = r.xMin + r.width * ix / nx, x1 = r.xMin + r.width * (ix + 1) / nx;
                    float y0 = r.yMin + r.height * iy / ny, y1 = r.yMin + r.height * (iy + 1) / ny;
                    g.Quad(new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1), At(x0, y0), At(x1, y0), At(x1, y1), At(x0, y1));
                }
        }
    }
}
