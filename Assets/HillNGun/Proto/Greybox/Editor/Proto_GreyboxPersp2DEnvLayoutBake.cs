using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    public static class Proto_GreyboxPersp2DEnvLayoutBake
    {
        const string LayoutRel = "Environment/hng_env_layout_2d.json";
        static readonly string[] BuildingIds = { "sheriff_R", "store_L", "hotel_R", "saloon_L" };

        static readonly Dictionary<string, string> PropFiles = new()
        {
            { "barrel", "hng_env_prop_barrel.png" },
            { "crate", "hng_env_prop_crate.png" },
            { "crate_small", "hng_env_prop_crate_small.png" },
            { "cactus_saguaro_tall", "hng_env_prop_cactus_saguaro_tall.png" },
            { "cactus_small", "hng_env_prop_cactus_small.png" },
            { "agave", "hng_env_prop_agave.png" },
            { "hitching_post", "hng_env_prop_hitching_post.png" },
            { "rock_large", "hng_env_prop_rock_large.png" },
            { "rock_cluster", "hng_env_prop_rock_cluster.png" },
        };

        public static void BakeIntoCatalog(Proto_GreyboxPersp2DCatalog c)
        {
            var path = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/{LayoutRel}";
            if (!File.Exists(path))
            {
                Debug.LogWarning("Persp2D: missing env layout JSON — skip bake.");
                return;
            }

            var json = File.ReadAllText(path);
            c.envLayoutJson = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            c.roadPlane = ParseRoad(json);
            c.skyPlane = ParseSky(json);

            var buildings = new List<Proto_GreyboxPersp2DCatalog.BakedBuilding>();
            foreach (var id in BuildingIds)
            {
                var block = ExtractObjectBlock(json, $"\"{id}\"");
                if (block == null) continue;
                var signBlock = ExtractNestedObject(block, "\"sign\"");
                buildings.Add(new Proto_GreyboxPersp2DCatalog.BakedBuilding
                {
                    id = id,
                    nativeW = ParseSizeW(block),
                    nativeH = ParseSizeH(block),
                    pivotX = ParsePairFirst(block, "recommended_pivot_screen_2x"),
                    pivotY = ParsePairSecond(block, "recommended_pivot_screen_2x"),
                    signOffsetX = signBlock != null ? ParsePairFirst(signBlock, "anchor_offset_from_pivot_px") : 0f,
                    signOffsetY = signBlock != null ? ParsePairSecond(signBlock, "anchor_offset_from_pivot_px") : 0f,
                    signScale = signBlock != null ? ParseFloatKey(signBlock, "scale") : 1f,
                    signNativeW = signBlock != null
                        ? ParsePairFirst(signBlock, "size_px_at_scale") / Mathf.Max(ParseFloatKey(signBlock, "scale"), 0.001f)
                        : 0f,
                    porchEdgeX = ParseWorldFloat(block, "X_porch_edge"),
                    nearPorchZ = ParseWorldFloat(block, "Z_near"),
                });
            }
            c.bakedBuildings = buildings.ToArray();

            var propDefs = new List<Proto_GreyboxPersp2DCatalog.BakedPropDef>();
            var propsRoot = ExtractObjectBlock(json, "\"props\"");
            if (propsRoot != null)
            {
                foreach (var kv in PropFiles)
                {
                    var pb = ExtractObjectBlock(propsRoot, $"\"{kv.Key}\"");
                    if (pb == null) continue;
                    propDefs.Add(new Proto_GreyboxPersp2DCatalog.BakedPropDef
                    {
                        name = kv.Key,
                        nativeW = ParseSizeW(pb),
                        nativeH = ParseSizeH(pb),
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                            $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Environment/Props/{kv.Value}")
                    });
                }
            }
            c.propDefs = propDefs.ToArray();
            c.scenePropPlacements = ParseSceneProps(json);

            var pivots = new List<Proto_GreyboxPersp2DCatalog.BakedSpritePivot>();
            if (propsRoot != null)
            {
                foreach (var kv in PropFiles)
                {
                    var pb = ExtractObjectBlock(propsRoot, $"\"{kv.Key}\"");
                    if (pb == null) continue;
                    pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot
                    {
                        fileContains = kv.Value.Replace(".png", ""),
                        pivot = ParsePivotNorm(pb)
                    });
                }
            }

            foreach (var id in BuildingIds)
            {
                var block = ExtractObjectBlock(json, $"\"{id}\"");
                if (block == null) continue;
                var piv = ParsePivotNorm(block);
                pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot { fileContains = $"bld_{id}_back", pivot = piv });
                pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot { fileContains = $"bld_{id}_front", pivot = piv });
                var signBlock = ExtractNestedObject(block, "\"sign\"");
                if (signBlock != null)
                {
                    var signFile = ParseString(signBlock, "\"file\"");
                    if (!string.IsNullOrEmpty(signFile))
                    {
                        var signName = Path.GetFileNameWithoutExtension(signFile);
                        pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot
                        {
                            fileContains = signName,
                            pivot = ParsePivotNorm(signBlock)
                        });
                    }
                }
            }

            pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot
                { fileContains = "road_perspective", pivot = new Vector2(0.5f, 1f) });
            pivots.Add(new Proto_GreyboxPersp2DCatalog.BakedSpritePivot
                { fileContains = "backdrop_sunset", pivot = new Vector2(0f, 1f) });
            c.spritePivots = pivots.ToArray();

            foreach (var baked in c.bakedBuildings)
            {
                if (baked == null) continue;
                foreach (var b in c.sceneBuildings)
                {
                    if (b == null || b.id != baked.id) continue;
                    b.porchEdgeX = baked.porchEdgeX;
                    b.nearPorchZ = baked.nearPorchZ;
                }
            }
        }

        static Proto_GreyboxPersp2DCatalog.BakedPlane ParseRoad(string json)
        {
            var block = ExtractObjectBlock(json, "\"road\"");
            if (block == null) return new Proto_GreyboxPersp2DCatalog.BakedPlane { nativeW = 3840f, nativeH = 1253f, topLeftY = 907f };
            return new Proto_GreyboxPersp2DCatalog.BakedPlane
            {
                nativeW = ParseSizeW(block),
                nativeH = ParseSizeH(block),
                topLeftX = ParsePairFirst(block, "topleft_screen_2x"),
                topLeftY = ParsePairSecond(block, "topleft_screen_2x"),
            };
        }

        static Proto_GreyboxPersp2DCatalog.BakedPlane ParseSky(string json)
        {
            var block = ExtractObjectBlock(json, "\"sky\"");
            if (block == null)
                return new Proto_GreyboxPersp2DCatalog.BakedPlane { nativeW = 3840f, nativeH = 907f };
            return new Proto_GreyboxPersp2DCatalog.BakedPlane
            {
                nativeW = ParseSizeW(block),
                nativeH = ParseSizeH(block),
                topLeftX = ParsePairFirst(block, "topleft_screen_2x"),
                topLeftY = ParsePairSecond(block, "topleft_screen_2x"),
            };
        }

        static Proto_GreyboxPersp2DCatalog.BakedPropPlacement[] ParseSceneProps(string json)
        {
            var scene = ExtractObjectBlock(json, "\"scene\"");
            if (scene == null) return Array.Empty<Proto_GreyboxPersp2DCatalog.BakedPropPlacement>();
            int propsIdx = scene.IndexOf("\"props\"", StringComparison.Ordinal);
            if (propsIdx < 0) return Array.Empty<Proto_GreyboxPersp2DCatalog.BakedPropPlacement>();
            int arrStart = scene.IndexOf('[', propsIdx);
            if (arrStart < 0) return Array.Empty<Proto_GreyboxPersp2DCatalog.BakedPropPlacement>();
            var list = new List<Proto_GreyboxPersp2DCatalog.BakedPropPlacement>();
            int i = arrStart + 1;
            while (i < scene.Length)
            {
                int objStart = scene.IndexOf('{', i);
                if (objStart < 0) break;
                int depth = 0;
                int j = objStart;
                for (; j < scene.Length; j++)
                {
                    if (scene[j] == '{') depth++;
                    else if (scene[j] == '}')
                    {
                        depth--;
                        if (depth == 0) break;
                    }
                }
                if (j >= scene.Length) break;
                var obj = scene.Substring(objStart, j - objStart + 1);
                var name = ParseString(obj, "\"name\"");
                if (!string.IsNullOrEmpty(name))
                {
                    list.Add(new Proto_GreyboxPersp2DCatalog.BakedPropPlacement
                    {
                        name = name,
                        pivotX = ParsePairFirst(obj, "pivot_screen_2x"),
                        pivotY = ParsePairSecond(obj, "pivot_screen_2x"),
                        jsonScale = ParseFloat(obj, "\"scale\""),
                        flipX = ParseBool(obj, "flip_x")
                    });
                }
                i = j + 1;
                if (scene[i] == ']') break;
            }
            return list.ToArray();
        }

        public static string ExtractObjectBlockPublic(string json, string key) => ExtractObjectBlock(json, key);

        public static Vector2 ParsePivotNormPublic(string block) => ParsePivotNorm(block);

        static string ExtractObjectBlock(string json, string key)
        {
            var pattern = Regex.Escape(key) + @"\s*:\s*\{";
            var m = Regex.Match(json, pattern, RegexOptions.CultureInvariant);
            if (!m.Success) return null;
            int brace = m.Index + m.Value.LastIndexOf('{');
            int depth = 0;
            for (int i = brace; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(brace, i - brace + 1);
                }
            }
            return null;
        }

        static string ExtractNestedObject(string block, string key) => ExtractObjectBlock(block, key);

        static float ParseSizeW(string block)
        {
            var m = Regex.Match(block, "\"size\"\\s*:\\s*\\[\\s*([0-9.]+)", RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static float ParseSizeH(string block)
        {
            var m = Regex.Match(block, "\"size\"\\s*:\\s*\\[\\s*[0-9.]+\\s*,\\s*([0-9.]+)", RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static float ParsePairFirst(string block, string key)
        {
            var m = Regex.Match(block, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[\\s*([-.0-9eE+]+)",
                RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static float ParsePairSecond(string block, string key)
        {
            var m = Regex.Match(block, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[[^\\]]*,\\s*([-.0-9eE+]+)",
                RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static float ParseFloatKey(string block, string key)
        {
            var m = Regex.Match(block, "\"" + Regex.Escape(key) + "\"\\s*:\\s*([-.0-9eE+]+)", RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static float ParseWorldFloat(string block, string innerKey)
        {
            var world = ExtractNestedObject(block, "\"world\"");
            return world != null ? ParseFloatNested(world, innerKey) : 0f;
        }

        static float ParseFloat(string block, string key)
        {
            return ParseFloatKey(block, key.Trim('"'));
        }

        static float ParseFloatNested(string block, string innerKey)
        {
            var m = Regex.Match(block, "\"" + Regex.Escape(innerKey) + "\"\\s*:\\s*([-.0-9eE+]+)",
                RegexOptions.CultureInvariant);
            return m.Success ? ParseFloat(m.Groups[1].Value) : 0f;
        }

        static string ParseString(string block, string key)
        {
            var m = Regex.Match(block, key + @"\s*:\s*""([^""]+)""", RegexOptions.CultureInvariant);
            return m.Success ? m.Groups[1].Value : null;
        }

        static bool ParseBool(string block, string key)
        {
            var m = Regex.Match(block, "\"" + key + @"\""\s*:\s*(true|false)", RegexOptions.CultureInvariant);
            return m.Success && m.Groups[1].Value == "true";
        }

        static Vector2 ParsePivotNorm(string block)
        {
            return new Vector2(
                ParsePairFirst(block, "pivot_norm_unity"),
                ParsePairSecond(block, "pivot_norm_unity"));
        }

        static float ParseFloat(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        public static Vector2 PivotForFile(string fileLower, Proto_GreyboxPersp2DCatalog cat)
        {
            if (cat?.spritePivots != null)
            {
                foreach (var p in cat.spritePivots)
                {
                    if (p != null && !string.IsNullOrEmpty(p.fileContains) &&
                        fileLower.Contains(p.fileContains, StringComparison.OrdinalIgnoreCase))
                        return p.pivot;
                }
            }
            if (fileLower.Contains("shadow_ellipse"))
                return new Vector2(0.5f, 0.5f);
            if (fileLower.Contains("road"))
                return new Vector2(0.5f, 1f);
            if (fileLower.Contains("backdrop"))
                return new Vector2(0f, 1f);
            return new Vector2(0.5f, 0.5f);
        }
    }
}