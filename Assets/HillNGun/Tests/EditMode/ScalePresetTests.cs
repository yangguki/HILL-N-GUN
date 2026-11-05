using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Combat;
using HillNGun.Cover;
using HillNGun.View3D;

namespace HillNGun.Tests.EditMode
{
    /// <summary>Scale presets (기획 비율 1/2/3안): data matches the spec, layouts keep 3 rows, protection rule identical.</summary>
    public class ScalePresetTests
    {
        private ScalePresetData _d;
        private static readonly string[] LayoutKeys = { "dusty_town_1_1", "pvp_town_street", "dusty_town_1_2", "dusty_town_1_3", "dusty_town_boss" };

        [SetUp]
        public void SetUp()
        {
            _d = Resources.Load<ScalePresetData>(ScalePresetData.ResourcePath);
            Assert.IsNotNull(_d, "Resources/" + ScalePresetData.ResourcePath + " missing");
        }

        [TearDown]
        public void TearDown() => ScalePresets.SetDataForTests(null, 0);

        private static Dictionary<string, CoverData> AllCovers()
        {
            var map = new Dictionary<string, CoverData>();
            foreach (var g in AssetDatabase.FindAssets("t:CoverData"))
            {
                var c = AssetDatabase.LoadAssetAtPath<CoverData>(AssetDatabase.GUIDToAssetPath(g));
                if (c != null && !map.ContainsKey(c.id)) map[c.id] = c;
            }
            return map;
        }

        [Test]
        public void Default_IsOption3_AndHeightsMatchSpec()
        {
            Assert.AreEqual(3, _d.defaultOption);
            Assert.AreEqual(0.10f, _d.GetOption(1).characterHeightFrac, 1e-4f);
            Assert.AreEqual(0.09f, _d.GetOption(2).characterHeightFrac, 1e-4f);
            Assert.AreEqual(0.08f, _d.GetOption(3).characterHeightFrac, 1e-4f);
            ScalePresets.SetDataForTests(_d, 0); // unknown option -> data default
            Assert.AreEqual(3, ScalePresets.Current);
        }

        [Test]
        public void CoverSizes_MatchSpec()
        {
            void Check(int opt, CoverSizeType t, float h, float w)
            {
                var e = _d.GetOption(opt).GetSize(t);
                Assert.IsNotNull(e, $"opt{opt} {t}");
                Assert.AreEqual(h, e.heightH, 1e-4f, $"opt{opt} {t} height");
                Assert.AreEqual(w, e.widthH, 1e-4f, $"opt{opt} {t} width");
            }
            foreach (int o in new[] { 1, 2 })
            {
                Check(o, CoverSizeType.Barrel, 0.6f, 0.45f);
                Check(o, CoverSizeType.Crate, 0.55f, 0.8f);
                Check(o, CoverSizeType.Wagon, 0.75f, 1.7f);
                Check(o, CoverSizeType.SwingDoor, 0.75f, 0.9f);
            }
            Check(3, CoverSizeType.Barrel, 0.5f, 0.4f);
            Check(3, CoverSizeType.Crate, 0.4f, 0.6f);
            Check(3, CoverSizeType.Wagon, 0.55f, 1.1f);
            Check(3, CoverSizeType.SwingDoor, 0.65f, 0.7f);
            Check(3, CoverSizeType.HayBale, 0.35f, 0.6f);
            Check(3, CoverSizeType.Trough, 0.3f, 0.8f);
        }

        [Test]
        public void Layouts_Keep3Rows_CountsPerSpec_AndIdsResolve()
        {
            var covers = AllCovers();
            foreach (var o in _d.options)
                foreach (var key in LayoutKeys)
                {
                    var l = o.GetLayout(key);
                    Assert.IsNotNull(l, $"opt{o.number} has no layout for {key}");
                    for (int row = 0; row < 3; row++)
                    {
                        int n = l.Count(p => p.rowIndex == row);
                        if (o.number == 1) Assert.That(n, Is.InRange(2, 3), $"opt1 {key} row{row}");
                        else Assert.AreEqual(3, n, $"opt{o.number} {key} row{row}");
                    }
                    Assert.IsTrue(l.All(p => p.rowIndex >= 0 && p.rowIndex < 3), $"opt{o.number} {key}: rows must stay 0..2");
                    foreach (var p in l)
                        Assert.IsTrue(covers.ContainsKey(p.coverId), $"opt{o.number} {key}: unknown cover {p.coverId}");
                }
            foreach (var extra in _d.extraCovers) Assert.IsNotNull(extra, "extraCovers has a missing reference");
        }

        [Test]
        public void CharacterMul_FollowsH_AndHitRadiusScales()
        {
            for (int n = 1; n <= 3; n++)
            {
                var o = _d.GetOption(n);
                float mul = ScalePresets.CharacterMulFor(_d, o);
                Assert.AreEqual(o.characterHeightFrac / _d.baselineCharacterHeightFrac, mul, 1e-5f);
                Assert.Less(mul, 1f, "every option is smaller than the previous build");
            }
            Assert.Greater(ScalePresets.CharacterMulFor(_d, _d.GetOption(1)), ScalePresets.CharacterMulFor(_d, _d.GetOption(2)));
            Assert.Greater(ScalePresets.CharacterMulFor(_d, _d.GetOption(2)), ScalePresets.CharacterMulFor(_d, _d.GetOption(3)));
        }

        /// <summary>The H the existing hit radii were tuned for = buster's drawn height in the previous build.</summary>
        [Test]
        public void Baseline_MatchesPreviousBuildCharacterHeight()
        {
            var a3 = AssetDatabase.LoadAssetAtPath<Actor3DData>("Assets/HillNGun/Data/Feel/actor3d.asset");
            Assert.IsNotNull(a3);
            var def = a3.GetModel("buster") ?? Actor3DDefaults.Buster();
            var mat = new Material(Shader.Find("Sprites/Default"));
            var rig = new Actor3DRig(def, mat, 0, Color.white, 0f, null, "scale_test_rig");
            float drawn = rig.Height * a3.modelScale * Mathf.Cos(a3.viewTiltDeg * Mathf.Deg2Rad);
            float frac = drawn / _d.screenHeightWorld;
            Object.DestroyImmediate(rig.Root.gameObject);
            Object.DestroyImmediate(mat);
            Debug.Log($"[ScalePresetTests] previous-build H = {frac:P1} (data baseline {_d.baselineCharacterHeightFrac:P1})");
            Assert.AreEqual(_d.baselineCharacterHeightFrac, frac, 0.006f);
        }

        /// <summary>Rule 4: tucked behind any cover = protected, in every option, whatever the drawn cover height.</summary>
        [Test]
        public void TuckedBehindCover_IsProtected_InEveryOption_ForEveryCoverType()
        {
            var covers = AllCovers();
            var field = AssetDatabase.LoadAssetAtPath<BattlefieldData>("Assets/HillNGun/Data/Battlefield/battlefield_main.asset");
            Assert.IsNotNull(field);
            float offset = (field.coverOffsetMin + field.coverOffsetMax) * 0.5f;
            float[] bulletRadii = AssetDatabase.FindAssets("t:EnemyData").Select(g => AssetDatabase.LoadAssetAtPath<EnemyData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e != null).Select(e => e.bulletRadius).Distinct().ToArray();
            float[] hitRadii = AssetDatabase.FindAssets("t:CharacterData").Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null).Select(c => c.hitRadius).Distinct().ToArray();
            Assert.IsNotEmpty(bulletRadii); Assert.IsNotEmpty(hitRadii);
            int checks = 0;
            for (int n = 1; n <= 3; n++)
            {
                var o = _d.GetOption(n);
                float mul = ScalePresets.CharacterMulFor(_d, o);
                foreach (var cd in covers.Values)
                {
                    var size = ScalePresets.CoverSizeFor(_d, o, cd);
                    for (int row = 0; row < 3; row++)
                    {
                        float rowY = field.GetOurRowY(row);
                        var inst = new CoverInstance();
                        inst.Initialize(cd, new Vector2(12f, rowY + offset), row, Team.Player, size);
                        var rect = inst.GetBounds();
                        foreach (float hr in hitRadii)
                            foreach (float br in bulletRadii)
                            {
                                float hurt = hr * mul;
                                foreach (float dx in new[] { 0f, -hurt, hurt })
                                {
                                    bool ok = CoverProtection.ProtectsStraightShot(rect, rowY, field.GetEnemyRowY(0), dx, br, hurt);
                                    Assert.IsTrue(ok, $"opt{n} {cd.id} row{row} dx={dx:F2} hurt={hurt:F2} bullet={br:F2} logicW={size.logicWidth:F2}");
                                    checks++;
                                }
                            }
                    }
                }
            }
            Debug.Log($"[ScalePresetTests] cover protection: {checks} tucked-behind shots blocked across options 1/2/3");
        }

        [Test]
        public void CoverLogicWidth_DoesNotDependOnDrawnHeight()
        {
            var covers = AllCovers();
            var o = Object.Instantiate(_d);
            try
            {
                foreach (var opt in o.options)
                    foreach (var cd in covers.Values)
                    {
                        float before = ScalePresets.CoverSizeFor(o, opt, cd).logicWidth;
                        var e = opt.GetSize(o.TypeOf(cd.id));
                        if (e == null) continue;
                        float h = e.heightH;
                        e.heightH = h * 0.1f;
                        Assert.AreEqual(before, ScalePresets.CoverSizeFor(o, opt, cd).logicWidth, 1e-6f, $"opt{opt.number} {cd.id}");
                        e.heightH = h;
                    }
            }
            finally { Object.DestroyImmediate(o); }
        }

        [Test]
        public void CoverLogicWidth_NeverBelowHurtDiameter()
        {
            foreach (var opt in _d.options)
                foreach (var cd in AllCovers().Values)
                {
                    var s = ScalePresets.CoverSizeFor(_d, opt, cd);
                    Assert.GreaterOrEqual(s.logicWidth + 1e-5f, ScalePresets.HurtDiameterFor(_d, opt) * _d.coverMinWidthVsHurtDiameter, $"opt{opt.number} {cd.id}");
                    Assert.GreaterOrEqual(s.logicWidth + 1e-5f, s.visualWidth);
                }
        }

        [Test]
        public void PropSizeMul_HitsTargetDrawnSize()
        {
            var mesh = new Vector3(0.92f, 1.25f, 0.92f);
            float tilt = 28f, tw = 0.64f, th = 0.8f;
            var m = CoverProp3D.ComputeSizeMul(mesh, 1f, tw, th, tilt, 0.1f);
            float t = tilt * Mathf.Deg2Rad;
            Assert.AreEqual(tw, mesh.x * m.x, 1e-4f);
            Assert.AreEqual(th, mesh.y * m.y * Mathf.Cos(t) + mesh.z * m.z * Mathf.Sin(t), 1e-4f);
            var u = CoverProp3D.ComputeSizeMul(mesh, 2f, 1f, -1f, tilt, 0.1f);
            Assert.AreEqual(0.5f, u.x, 1e-5f); Assert.AreEqual(u.x, u.y, 1e-6f);
        }
    }
}
