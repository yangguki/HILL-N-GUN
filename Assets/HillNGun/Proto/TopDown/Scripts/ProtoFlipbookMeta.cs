using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HillNGun.Proto.TopDown
{
    /// <summary>Parses stage1 *_meta.json without a full JSON library (EditMode-friendly).</summary>
    public static class ProtoFlipbookMeta
    {
        public const float PivotX = 0.5f;
        public const float PivotY = 4f / 128f; // (128-124)/128 = 0.03125 foot pivot
        public const float DefaultPpu = 100f;

        public struct AnimMeta
        {
            public string Key;
            public ProtoFacingDir Facing;
            public ProtoAnimKind Anim;
            public string[] Frames;
            public float Fps;
            public bool Loop;
            public bool FlipXForLeft;
            public Vector2 PivotPx;
        }

        public struct StageMeta
        {
            public string Character;
            public int Stage;
            public List<AnimMeta> Anims;
        }

        static readonly string[] Facings = { "down", "up", "side" };
        static readonly string[] Anims = { "idle", "run", "shoot" };

        public static bool TryParse(string json, out StageMeta stage)
        {
            stage = new StageMeta { Anims = new List<AnimMeta>() };
            if (string.IsNullOrEmpty(json)) return false;

            stage.Character = MatchGroup(json, "\"character\"\\s*:\\s*\"([^\"]+)\"");
            if (int.TryParse(MatchGroup(json, "\"stage\"\\s*:\\s*(\\d+)"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int st))
                stage.Stage = st;
            else
                stage.Stage = 1;

            for (int f = 0; f < Facings.Length; f++)
            {
                for (int a = 0; a < Anims.Length; a++)
                {
                    string key = Facings[f] + "/" + Anims[a];
                    if (!TryExtractObject(json, key, out string body)) continue;
                    if (!ParseFacing(Facings[f], out var facing)) continue;
                    if (!ParseAnim(Anims[a], out var anim)) continue;

                    var frames = new List<string>();
                    var frameMatches = Regex.Matches(body, "\"(frames/[^\"]+\\.png)\"");
                    for (int i = 0; i < frameMatches.Count; i++)
                        frames.Add(frameMatches[i].Groups[1].Value);

                    float fps = 12f;
                    if (float.TryParse(MatchGroup(body, "\"fps\"\\s*:\\s*([0-9.]+)"), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFps))
                        fps = parsedFps;

                    string loopTok = MatchGroup(body, "\"loop\"\\s*:\\s*(true|false)");
                    bool loop = loopTok != "false";
                    bool flip = MatchGroup(body, "\"flip_x_for_left\"\\s*:\\s*(true|false)") == "true";

                    Vector2 pivot = new Vector2(64f, 124f);
                    var pivotMatch = Regex.Match(body, "\"pivot\"\\s*:\\s*\\[\\s*([0-9.]+)\\s*,\\s*([0-9.]+)\\s*\\]");
                    if (pivotMatch.Success)
                    {
                        float.TryParse(pivotMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out pivot.x);
                        float.TryParse(pivotMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out pivot.y);
                    }

                    stage.Anims.Add(new AnimMeta
                    {
                        Key = key,
                        Facing = facing,
                        Anim = anim,
                        Frames = frames.ToArray(),
                        Fps = fps,
                        Loop = loop,
                        FlipXForLeft = flip,
                        PivotPx = pivot
                    });
                }
            }

            return !string.IsNullOrEmpty(stage.Character) && stage.Anims.Count > 0;
        }

        public static int ExpectedFrameCount(ProtoAnimKind anim)
        {
            switch (anim)
            {
                case ProtoAnimKind.Idle: return 4;
                case ProtoAnimKind.Run: return 8;
                case ProtoAnimKind.Shoot: return 3;
                default: return 0;
            }
        }

        static bool TryExtractObject(string json, string key, out string body)
        {
            body = null;
            string needle = "\"" + key + "\"";
            int keyIdx = json.IndexOf(needle, System.StringComparison.Ordinal);
            if (keyIdx < 0) return false;
            int brace = json.IndexOf('{', keyIdx + needle.Length);
            if (brace < 0) return false;
            int depth = 0;
            for (int i = brace; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        body = json.Substring(brace + 1, i - brace - 1);
                        return true;
                    }
                }
            }
            return false;
        }

        static bool ParseFacing(string s, out ProtoFacingDir facing)
        {
            switch (s)
            {
                case "down": facing = ProtoFacingDir.Down; return true;
                case "up": facing = ProtoFacingDir.Up; return true;
                case "side": facing = ProtoFacingDir.Side; return true;
                default: facing = ProtoFacingDir.Down; return false;
            }
        }

        static bool ParseAnim(string s, out ProtoAnimKind anim)
        {
            switch (s)
            {
                case "idle": anim = ProtoAnimKind.Idle; return true;
                case "run": anim = ProtoAnimKind.Run; return true;
                case "shoot": anim = ProtoAnimKind.Shoot; return true;
                default: anim = ProtoAnimKind.Idle; return false;
            }
        }

        static string MatchGroup(string text, string pattern)
        {
            var m = Regex.Match(text, pattern);
            return m.Success ? m.Groups[1].Value : "";
        }
    }
}
