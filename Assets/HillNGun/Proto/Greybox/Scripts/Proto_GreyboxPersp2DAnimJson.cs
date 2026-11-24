using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HillNGun.Proto.Greybox
{
    /// <summary>Parse direction_map_8way and muzzle_px from designer anim JSON (runtime + tests).</summary>
    public static class Proto_GreyboxPersp2DAnimJson
    {
        public const float CellPx = 512f;
        public const float PivotPxX = 256f;
        public const float PivotPxYFromTop = 400f;

        struct MapEntry
        {
            public string Source;
            public bool FlipX;
        }

        static readonly Dictionary<string, MapEntry> HeroMap = new();
        static readonly Dictionary<string, MapEntry> BanditMap = new();
        static string s_heroJson;
        static string s_banditJson;

        public static void Warm(Proto_GreyboxPersp2DCatalog cat)
        {
            if (cat == null) return;
            var hero = cat.heroAnimJson != null ? cat.heroAnimJson.text : null;
            var bandit = cat.banditAnimJson != null ? cat.banditAnimJson.text : null;
            if (hero != null && hero != s_heroJson)
            {
                s_heroJson = hero;
                ParseDirectionMap(hero, HeroMap);
            }

            if (bandit != null && bandit != s_banditJson)
            {
                s_banditJson = bandit;
                ParseDirectionMap(bandit, BanditMap);
            }
        }

        public static void Resolve8Way(string who, string logicalDir, out string spriteDir, out bool flipX)
        {
            var map = who == "bandit" ? BanditMap : HeroMap;
            if (map.Count == 0 || !map.TryGetValue(logicalDir ?? "S", out var e))
            {
                spriteDir = logicalDir ?? "S";
                flipX = false;
                if (who != "bandit")
                    Proto_GreyboxPersp2DDirection.ResolveLegacyMirror(logicalDir, out spriteDir, out flipX);
                return;
            }

            spriteDir = e.Source;
            flipX = e.FlipX;
        }

        public static Vector2 MuzzleWorldFromFoot(Vector2 foot, float uniformScale, string who, string logicalDir,
            bool flipX, int frame = 0)
        {
            var json = who == "bandit" ? s_banditJson : s_heroJson;
            if (string.IsNullOrEmpty(json))
            {
                Proto_GreyboxPersp2DCatalog cat = Proto_GreyboxPersp2DCatalog.LoadRuntime();
                Warm(cat);
                json = who == "bandit" ? s_banditJson : s_heroJson;
            }

            if (!TryGetMuzzlePx(json, logicalDir, frame, out var mx, out var my))
                return foot;

            if (flipX) mx = CellPx - mx;
            float ppu = Proto_GreyboxPersp2DCatalog.ImportPpu;
            float s = uniformScale / ppu;
            float dx = (mx - PivotPxX) * s;
            float dy = -(my - PivotPxYFromTop) * s;
            return foot + new Vector2(dx, dy);
        }

        public static int AnimFrameCount(string json, string anim)
        {
            if (string.IsNullOrEmpty(json)) return 0;
            var m = Regex.Match(json, "\"" + anim + "\"\\s*:\\s*\\{[^}]*?\"frames\"\\s*:\\s*(\\d+)",
                RegexOptions.Singleline);
            return m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                ? n
                : 0;
        }

        static void ParseDirectionMap(string json, Dictionary<string, MapEntry> into)
        {
            into.Clear();
            var block = ExtractObject(json, "direction_map_8way");
            if (block == null) return;
            foreach (var dir in new[] { "S", "SE", "E", "NE", "N", "NW", "W", "SW" })
            {
                var sub = ExtractObject(block, "\"" + dir + "\"");
                if (sub == null) continue;
                var src = ParseString(sub, "source");
                bool flip = ParseBool(sub, "flipX");
                into[dir] = new MapEntry { Source = string.IsNullOrEmpty(src) ? dir : src, FlipX = flip };
            }
        }

        static bool TryGetMuzzlePx(string json, string dir, int frame, out float x, out float y)
        {
            x = y = 0f;
            var block = ExtractObject(json, "muzzle_px");
            if (block == null) return false;
            var dirBlock = ExtractObject(block, "\"" + dir + "\"");
            if (dirBlock == null) return false;
            var matches = Regex.Matches(dirBlock,
                "\\[\\s*([\\d.+-]+)\\s*,\\s*([\\d.+-]+)\\s*\\]",
                RegexOptions.CultureInvariant);
            if (matches.Count <= frame) return false;
            var m = matches[frame];
            if (!float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                || !float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                return false;
            return true;
        }

        static string ExtractObject(string json, string keyToken)
        {
            int i = json.IndexOf(keyToken, StringComparison.Ordinal);
            if (i < 0) return null;
            int start = json.IndexOf('{', i);
            if (start < 0) return null;
            int depth = 0;
            for (int j = start; j < json.Length; j++)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(start, j - start + 1);
                }
            }

            return null;
        }

        static string ParseString(string block, string key)
        {
            var m = Regex.Match(block, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        static bool ParseBool(string block, string key)
        {
            var m = Regex.Match(block, "\"" + key + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            return m.Success && m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}