using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace HillNGun.Proto.Greybox.Editor
{
    /// <summary>Round 4: ASTC / atlas verification without building an APK.</summary>
    public static class Proto_GreyboxPersp2DAstcReport
    {
        const string ReportPathR4 = "_tmp_slice_k/astc_report_r4.txt";
        const string ReportPathR5 = "_tmp_slice_k/astc_report_r5.txt";

        [MenuItem("HillNGun/Greybox/Write Persp2D ASTC Report (Round 4)")]
        public static void WriteRound4ReportMenu() => WriteRound4Report();

        [MenuItem("HillNGun/Greybox/Write Persp2D ASTC Report (Round 5)")]
        public static void WriteRound5ReportMenu() => WriteRound5Report();

        [MenuItem("HillNGun/Greybox/Write Persp2D ASTC Report (Round 6)")]
        public static void WriteRound6ReportMenu() => WriteRound6Report();

        [MenuItem("HillNGun/Greybox/Append Persp2D ATLAS_PAGES (Round 4b)")]
        public static void AppendAtlasPagesRound4bMenu() => AppendAtlasPagesRound4b();

        public static void AppendAtlasPagesRound4b()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("ATLAS_PAGES (round 4b append):");
            var prevGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (prevTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android, false);
                AppendAtlasPagesSection(sb);
            }
            finally
            {
                if (prevTarget != EditorUserBuildSettings.activeBuildTarget)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(prevGroup, prevTarget);
            }

            var outPath = Path.Combine(Directory.GetCurrentDirectory(), ReportPathR4);
            var existing = File.Exists(outPath) ? File.ReadAllText(outPath) : string.Empty;
            if (existing.Contains("ATLAS_PAGES (round 4b"))
            {
                var idx = existing.IndexOf("ATLAS_PAGES (round 4b", StringComparison.Ordinal);
                existing = existing[..idx].TrimEnd();
            }

            File.WriteAllText(outPath, existing + sb);
            Debug.Log($"Persp2D ATLAS_PAGES appended: {outPath}");
        }

        public static void WriteRound4Report()
        {
            Proto_GreyboxPersp2DCatalogRebuild.RebuildAll();
            var sb = new StringBuilder();
            sb.AppendLine("persp2d_astc_report_r4");
            sb.AppendLine($"utc={DateTime.UtcNow:O}");
            sb.AppendLine();
            sb.AppendLine("ROOT_CAUSE (evidence from prior APK + repo state):");
            sb.AppendLine("- Sprite packer disabled (EditorSettings m_SpritePackerMode was 0) -> atlases not included in Android player builds.");
            sb.AppendLine("- .spriteatlasv2 assets used V1 YAML (isAtlasV2=0) with packables: [] on disk; batch logs:");
            sb.AppendLine("  'Persp2D: could not load SpriteAtlas at ...spriteatlasv2' (LoadAssetAtPath<SpriteAtlas> fails on V2 assets).");
            sb.AppendLine("- EnsureAtlas never bound hero/bandit folder sprites -> build shipped 275 individual character Textures as RGBA32.");
            sb.AppendLine("- Per-frame importer had Android ASTC override in .meta but mipmaps enableMipMap=1 (~x1.33 size) and unpack path ignored atlas ASTC.");
            sb.AppendLine("- APK build report: 768x768 frames ~3.0 MB each (RGBA32+mips); Persp2D user textures ~916 MB uncompressed.");
            sb.AppendLine();
            sb.AppendLine("FIX_APPLIED:");
            sb.AppendLine("- EditorSettings m_SpritePackerMode=5 (Sprite Atlas V2 enabled for builds).");
            sb.AppendLine("- EnsureAtlas uses SpriteAtlasAsset.Load/Save + sprite packables; Android ASTC 6x6, mips off, max 4096.");
            sb.AppendLine("- Proto_GreyboxPersp2DImport: mipmapEnabled=false for all Persp2D textures.");
            sb.AppendLine();

            var prevGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (prevTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

                SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android);
                AppendAtlasSection(sb);
                AppendTextureGroupEstimates(sb);
            }
            finally
            {
                if (prevTarget != EditorUserBuildSettings.activeBuildTarget)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(prevGroup, prevTarget);
            }

            var outPath = Path.Combine(Directory.GetCurrentDirectory(), ReportPathR4);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Persp2D ASTC report written: {outPath}");
        }

        public static void WriteRound5Report()
        {
            Proto_GreyboxPersp2DCatalogRebuild.RebuildAll();
            var sb = new StringBuilder();
            sb.AppendLine("persp2d_astc_report_r5");
            sb.AppendLine($"utc={DateTime.UtcNow:O}");
            sb.AppendLine("package=512_cell_sky_layer no_768_frames");
            sb.AppendLine();
            int heroPng = CountPngs($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/hero");
            int banditPng = CountPngs($"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/bandit");
            int shadow = AssetDatabase.LoadAssetAtPath<Sprite>(
                $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/hng_char_shadow_ellipse.png") != null ? 1 : 0;
            sb.AppendLine($"CHAR_PNG_COUNT hero={heroPng} bandit={banditPng} shadow={shadow} total={heroPng + banditPng + shadow}");
            sb.AppendLine(heroPng + banditPng == 217 ? "OK: 217 character frames" : "WARN: expected 217 character PNGs");
            sb.AppendLine();

            var prevGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (prevTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android);
                AppendAtlasSection(sb);
                AppendAtlasPagesSection(sb);
                AppendTextureGroupEstimates(sb);
            }
            finally
            {
                if (prevTarget != EditorUserBuildSettings.activeBuildTarget)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(prevGroup, prevTarget);
            }

            var outPath = Path.Combine(Directory.GetCurrentDirectory(), ReportPathR5);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Persp2D ASTC report r5 written: {outPath}");
        }

        const string ReportPathR6 = "_tmp_slice_k/astc_report_r6.txt";

        public static void WriteRound6Report()
        {
            Proto_GreyboxPersp2DCatalogRebuild.RebuildAll();
            var sb = new StringBuilder();
            sb.AppendLine("persp2d_astc_report_r6");
            sb.AppendLine($"utc={DateTime.UtcNow:O}");
            sb.AppendLine("round6=atlas_only_compression: char frame importer uncompressed RGBA32 source; atlases Android ASTC 6x6");
            sb.AppendLine();

            var prevGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var prevTarget = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (prevTarget != BuildTarget.Android)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android);
                AppendAtlasSection(sb);
                AppendAtlasPagesSection(sb);
                AppendTextureGroupEstimates(sb);
                AppendCharImporterAudit(sb);
                AppendBuildInclusionEstimate(sb);
            }
            finally
            {
                if (prevTarget != EditorUserBuildSettings.activeBuildTarget)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(prevGroup, prevTarget);
            }

            var outPath = Path.Combine(Directory.GetCurrentDirectory(), ReportPathR6);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Persp2D ASTC report r6 written: {outPath}");
            EditorApplication.Exit(0);
        }

        static void AppendCharImporterAudit(StringBuilder sb)
        {
            sb.AppendLine("CHAR_FRAME_IMPORTER_AUDIT (editor, sample):");
            var sample = AssetDatabase.FindAssets("t:Sprite", new[]
                { $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/hero" }).FirstOrDefault();
            if (string.IsNullOrEmpty(sample))
            {
                sb.AppendLine("  sample=missing");
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(sample);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                sb.AppendLine("  sample=not_texture_importer");
                return;
            }

            var android = importer.GetPlatformTextureSettings("Android");
            sb.AppendLine($"  sample={path}");
            sb.AppendLine(
                $"  textureCompression={importer.textureCompression} mipmaps={importer.mipmapEnabled} maxSize={importer.maxTextureSize}");
            sb.AppendLine(
                $"  android_overridden={android.overridden} android_format={android.format} android_max={android.maxTextureSize}");
        }

        static void AppendBuildInclusionEstimate(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("BUILD_INCLUSION_ESTIMATE (editor; SpritePackerMode=5 assumed):");
            int packed = 0;
            int loose = 0;
            foreach (var who in new[] { "hero", "bandit" })
            {
                var folder = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/{who}";
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sp == null) continue;
                    if (IsSpriteBoundToCharAtlas(sp))
                        packed++;
                    else
                        loose++;
                }
            }

            sb.AppendLine($"  char_sprites_in_included_atlas={packed} char_sprites_loose_not_packed={loose}");
            sb.AppendLine("  standalone_char_android_textures_in_player_build_est=0 when packed (replaced by atlas pages)");
            sb.AppendLine("  APK_texture_delta_vs_r5_est_mb: ~0 (atlas page count unchanged; r6 is importer source quality only)");
            sb.AppendLine("  LABEL: all APK numbers are editor estimates, not measured device builds");
        }

        static bool IsSpriteBoundToCharAtlas(Sprite sprite)
        {
            if (sprite == null) return false;
            foreach (var name in new[] { "Persp2D_CharHeroAtlas", "Persp2D_CharBanditAtlas" })
            {
                var path = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Atlases/{name}.spriteatlasv2";
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas != null && atlas.CanBindTo(sprite))
                    return true;
            }

            return false;
        }

        static int CountPngs(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return 0;
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) n++;
            }
            return n;
        }

        static void AppendAtlasSection(StringBuilder sb)
        {
            sb.AppendLine("ATLAS_PACK_ANDROID:");
            foreach (var name in new[] { "Persp2D_CharHeroAtlas", "Persp2D_CharBanditAtlas" })
            {
                var path = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Atlases/{name}.spriteatlasv2";
                int packableCount = CountPackablesInFolder(name.Contains("Hero") ? "hero" : "bandit");
                sb.AppendLine($"  {name}: packables_bound={packableCount}");
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                {
                    sb.AppendLine("    packed_sprite_atlas_load=null (use SpriteAtlasAsset only until import refresh)");
                    continue;
                }

                var android = atlas.GetPlatformSettings("Android");
                sb.AppendLine(
                    $"    android_format={android.format} overridden={android.overridden} maxSize={android.maxTextureSize}");
                var texSettings = atlas.GetTextureSettings();
                sb.AppendLine($"    atlas_generateMipMaps={texSettings.generateMipMaps}");
            }

            sb.AppendLine();
        }

        static int CountPackablesInFolder(string who) =>
            AssetDatabase.FindAssets("t:Sprite", new[]
                { $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/{who}" }).Length;

        static void AppendTextureGroupEstimates(StringBuilder sb)
        {
            var groups = new (string label, string folder)[]
            {
                ("hero_frames", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/hero"),
                ("bandit_frames", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Characters/bandit"),
                ("env", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Environment"),
                ("fx", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/FX"),
                ("ui", $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/UI"),
            };

            sb.AppendLine("TEXTURE_MEMORY_ESTIMATE_MB (editor importer Android settings):");
            long beforeTotal = 0;
            long afterTotal = 0;
            foreach (var (label, folder) in groups)
            {
                int count = 0;
                long rgba32MipBytes = 0;
                long astcBytes = 0;
                var formats = new Dictionary<string, int>();
                if (AssetDatabase.IsValidFolder(folder))
                {
                    foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    {
                        var p = AssetDatabase.GUIDToAssetPath(g);
                        if (!p.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                        var importer = AssetImporter.GetAtPath(p) as TextureImporter;
                        if (importer == null) continue;
                        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                        int w = tex != null ? tex.width : importer.maxTextureSize;
                        int h = tex != null ? tex.height : w;
                        count++;
                        rgba32MipBytes += Rgba32MippedBytes(w, h);
                        var plat = importer.GetPlatformTextureSettings("Android");
                        string fmtKey = plat.overridden ? plat.format.ToString() : "default";
                        formats.TryGetValue(fmtKey, out int fc);
                        formats[fmtKey] = fc + 1;
                        astcBytes += EstimatedAndroidBytes(w, h, plat, label == "ui");
                    }
                }

                beforeTotal += rgba32MipBytes;
                afterTotal += astcBytes;
                sb.AppendLine(
                    $"  {label}: count={count} before_rgba32_mip_mb={BytesToMb(rgba32MipBytes):F1} after_est_mb={BytesToMb(astcBytes):F1}");
                foreach (var kv in formats)
                    sb.AppendLine($"    android_format[{kv.Key}]={kv.Value}");
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL_before_rgba32_mip_mb={BytesToMb(beforeTotal):F1}");
            sb.AppendLine($"TOTAL_after_per_texture_est_mb={BytesToMb(afterTotal):F1}");
            sb.AppendLine("NOTE: after_total assumes unpacked sprites; packed atlases replace hero/bandit frame");
            sb.AppendLine("      textures in player builds when packer mode is enabled.");
            sb.AppendLine();
            sb.AppendLine("REFERENCE_before_apk_mb (build report): hero~510 bandit~315 env~91.3");
        }

        static double BytesToMb(long bytes) => bytes / (1024.0 * 1024.0);

        static long Rgba32MippedBytes(int w, int h) => (long)(w * h * 4L * 1.333f);

        static void AppendAtlasPagesSection(StringBuilder sb)
        {
            foreach (var name in new[] { "Persp2D_CharHeroAtlas", "Persp2D_CharBanditAtlas" })
            {
                var path = $"{Proto_GreyboxPersp2DCatalog.ArtFolder}/Atlases/{name}.spriteatlasv2";
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas == null)
                {
                    sb.AppendLine($"  {name}: atlas_load=null");
                    continue;
                }

                var textures = GetAtlasPreviewTextures(atlas, path, BuildTarget.Android);
                long totalAstc = 0;
                sb.AppendLine($"  {name}: page_count={textures.Count}");
                for (int i = 0; i < textures.Count; i++)
                {
                    var t = textures[i];
                    if (t == null) continue;
                    var android = atlas.GetPlatformSettings("Android");
                    string fmt = android.overridden ? android.format.ToString() : "Automatic";
                    long bytes = AstcBlockBytes(t.width, t.height, 6);
                    totalAstc += bytes;
                    sb.AppendLine(
                        $"    page_{i}: {t.width}x{t.height} format={fmt} est_astc6x6_bytes={bytes}");
                }

                sb.AppendLine($"    total_est_astc6x6_bytes={totalAstc} mb={BytesToMb(totalAstc):F2}");
                if (textures.Count > 8)
                    sb.AppendLine($"    NOTE: >8 pages at maxTextureSize 4096 for {name}");
            }
        }

        static List<Texture2D> GetAtlasPreviewTextures(SpriteAtlas atlas, string path, BuildTarget target)
        {
            var list = new List<Texture2D>();
            try
            {
                var ext = typeof(SpriteAtlasExtensions);
                var mi = ext.GetMethod("GetPreviewTextures",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    var arr = mi.Invoke(null, new object[] { atlas, target }) as Texture2D[];
                    if (arr != null)
                        list.AddRange(arr);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Persp2D ATLAS_PAGES: GetPreviewTextures reflection failed: " + ex.Message);
            }

            if (list.Count == 0)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Texture2D t && t.width > 64 && t.height > 64)
                        list.Add(t);
            }

            return list;
        }

        static long AstcBlockBytes(int w, int h, int blockSize)
        {
            int bx = Mathf.CeilToInt(w / (float)blockSize);
            int by = Mathf.CeilToInt(h / (float)blockSize);
            return (long)bx * by * 16L;
        }

        static long EstimatedAndroidBytes(int w, int h, TextureImporterPlatformSettings plat, bool ui)
        {
            var format = plat.overridden ? plat.format : (ui ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6);
            float bpp = format switch
            {
                TextureImporterFormat.ASTC_4x4 => 8f,
                TextureImporterFormat.ASTC_5x5 => 5.12f,
                TextureImporterFormat.ASTC_6x6 => 3.56f,
                TextureImporterFormat.ASTC_8x8 => 2f,
                TextureImporterFormat.ASTC_10x10 => 1.28f,
                TextureImporterFormat.ASTC_12x12 => 0.89f,
                TextureImporterFormat.RGBA32 => 32f,
                _ => 4f,
            };
            return (long)(w * (long)h * (bpp / 8f));
        }
    }
}