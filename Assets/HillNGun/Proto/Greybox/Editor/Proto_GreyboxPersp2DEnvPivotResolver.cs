using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HillNGun.Proto.Greybox.Editor
{
    /// <summary>Disk JSON → filename pivot map for OnPreprocessTexture (no AssetDatabase).</summary>
    public static class Proto_GreyboxPersp2DEnvPivotResolver
    {
        static Dictionary<string, Vector2> _map;
        static long _jsonTicks;

        static string JsonPath =>
            $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Environment/hng_env_layout_2d.json";

        static readonly string[] BuildingIds = { "sheriff_R", "store_L", "hotel_R", "saloon_L" };
        static readonly string[] SignNames = { "saloon", "store", "hotel", "sheriff" };
        static readonly string[] PropNames =
        {
            "barrel", "crate", "crate_small", "cactus_saguaro_tall", "cactus_small", "agave",
            "hitching_post", "rock_large", "rock_cluster"
        };

        public static void InvalidateCache() => _map = null;

        public static Vector2 Resolve(string fileLower)
        {
            var map = GetMap();
            if (TryLookup(map, fileLower, out var p)) return p;
            if (fileLower.Contains("shadow_ellipse")) return new Vector2(0.5f, 0.5f);
            if (fileLower.Contains("road")) return new Vector2(0.5f, 1f);
            if (fileLower.Contains("sky_sunset")) return new Vector2(0f, 1f);
            return new Vector2(0.5f, 0.5f);
        }

        public static IReadOnlyDictionary<string, Vector2> GetMap()
        {
            if (!File.Exists(JsonPath))
                return _map ??= new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);

            var ticks = File.GetLastWriteTimeUtc(JsonPath).Ticks;
            if (_map != null && ticks == _jsonTicks) return _map;

            _jsonTicks = ticks;
            _map = BuildMap(File.ReadAllText(JsonPath));
            return _map;
        }

        static bool TryLookup(IReadOnlyDictionary<string, Vector2> map, string fileLower, out Vector2 pivot)
        {
            if (map.TryGetValue(fileLower, out pivot)) return true;
            foreach (var kv in map)
            {
                if (fileLower.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                {
                    pivot = kv.Value;
                    return true;
                }
            }
            pivot = default;
            return false;
        }

        public static Dictionary<string, Vector2> BuildMap(string json)
        {
            var map = new Dictionary<string, Vector2>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in BuildingIds)
            {
                var block = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(json, $"\"{id}\"");
                if (block == null) continue;
                var piv = Proto_GreyboxPersp2DEnvLayoutBake.ParsePivotNormPublic(block);
                map[$"hng_env_bld_{id}_back"] = piv;
                map[$"hng_env_bld_{id}_front"] = piv;
            }

            var signsRoot = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(json, "\"signs\"");
            if (signsRoot != null)
            {
                foreach (var name in SignNames)
                {
                    var sb = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(signsRoot, $"\"{name}\"");
                    if (sb == null) continue;
                    map[$"hng_env_sign_{name}"] = Proto_GreyboxPersp2DEnvLayoutBake.ParsePivotNormPublic(sb);
                }
            }

            var propsRoot = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(json, "\"props\"");
            if (propsRoot != null)
            {
                foreach (var name in PropNames)
                {
                    var pb = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(propsRoot, $"\"{name}\"");
                    if (pb == null) continue;
                    map[$"hng_env_prop_{name}"] = Proto_GreyboxPersp2DEnvLayoutBake.ParsePivotNormPublic(pb);
                }
            }

            map["hng_env_road_perspective"] = new Vector2(0.5f, 1f);
            var skyBlock = Proto_GreyboxPersp2DEnvLayoutBake.ExtractObjectBlockPublic(json, "\"sky\"");
            if (skyBlock != null)
                map["hng_env_sky_sunset_2d"] = Proto_GreyboxPersp2DEnvLayoutBake.ParsePivotNormPublic(skyBlock);
            else
                map["hng_env_sky_sunset_2d"] = new Vector2(0f, 1f);
            map["hng_env_prop_shadow_ellipse"] = new Vector2(0.5f, 0.5f);
            return map;
        }
    }
}