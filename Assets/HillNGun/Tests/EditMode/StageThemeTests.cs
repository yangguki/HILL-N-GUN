using NUnit.Framework;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Scenery;
using HillNGun.View3D;
using UnityEngine;

namespace HillNGun.Tests.EditMode
{
    /// <summary>Stage themes are view-only: deterministic look selection, library fallbacks, logic->view mapping parity,
    /// and themed cover meshes never exceed the cover's logical footprint width.</summary>
    [TestFixture]
    public class StageThemeTests
    {
        private static StageThemeData Theme(string id, params CoverVisualRule[] rules)
        {
            var t = ScriptableObject.CreateInstance<StageThemeData>();
            t.id = id;
            t.coverVisuals = rules;
            return t;
        }

        [Test]
        public void CoverVisual_IsDeterministic_AndHonoursChance()
        {
            var t = Theme("x",
                new CoverVisualRule { coverId = "cover_barrel", visual = CoverVisual.Rock, altVisual = CoverVisual.Barrel, altChance = 0f },
                new CoverVisualRule { coverId = "cover_trough", visual = CoverVisual.Trough, altVisual = CoverVisual.HayBale, altChance = 1f },
                new CoverVisualRule { coverId = "cover_wagon", visual = CoverVisual.Wagon, altVisual = CoverVisual.Crate, altChance = 0.5f });
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector2(i * 1.7f, 2f + i % 3);
                Assert.AreEqual(CoverVisual.Rock, t.ResolveCoverVisual("cover_barrel", p));
                Assert.AreEqual(CoverVisual.HayBale, t.ResolveCoverVisual("cover_trough", p));
                Assert.AreEqual(t.ResolveCoverVisual("cover_wagon", p), t.ResolveCoverVisual("cover_wagon", p));
            }
            Assert.AreEqual(CoverVisual.Default, t.ResolveCoverVisual("cover_unknown", Vector2.zero));
            int alt = 0;
            for (int i = 0; i < 200; i++) if (t.ResolveCoverVisual("cover_wagon", new Vector2(i * 0.5f, i % 7)) == CoverVisual.Crate) alt++;
            Assert.That(alt, Is.InRange(40, 160), "alt chance 0.5 should give a mix");
            Object.DestroyImmediate(t);
        }

        [Test]
        public void Library_FallsBackToDefault()
        {
            var a = Theme("town_street"); var b = Theme("canyon_pass");
            var lib = ScriptableObject.CreateInstance<StageThemeLibrary>();
            lib.themes = new[] { b, a };
            lib.defaultThemeId = "town_street";
            Assert.AreSame(b, lib.Get("canyon_pass"));
            Assert.AreSame(a, lib.Get("missing"));
            Assert.AreSame(a, lib.Get(null));
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(lib);
        }

        [Test]
        public void FieldMap_MatchesBattlefieldPerspective_AndRoundTrips()
        {
            var bf = ScriptableObject.CreateInstance<BattlefieldData>();
            var m = FieldMap.From(bf);
            for (float ly = 0f; ly <= 18f; ly += 1.5f)
            {
                Assert.AreEqual(bf.GetPerspectiveScale(ly), m.S(ly), 1e-4f);
                var p = new Vector2(3.3f + ly, ly);
                var back = m.ToLogic(m.W(p.x, p.y));
                Assert.AreEqual(p.x, back.x, 1e-3f);
                Assert.AreEqual(p.y, back.y, 1e-3f);
            }
            Object.DestroyImmediate(bf);
        }

        [Test]
        public void SandGrain_MeanMatchesColourCompensation()
        {
            var tex = BackdropBaker.GrainTexture(64, 0.55f, 1234);
            var px = tex.GetPixels();
            float sum = 0f;
            foreach (var c in px) sum += c.r;
            Assert.AreEqual(0.92f, sum / px.Length, 0.03f);
            Object.DestroyImmediate(tex);
        }

        // (visual, cover width) pairs used by the shipped themes for the new looks
        private static readonly (string key, float width)[] ThemedLooks =
        {
            ("v_Rock", 1.0f), ("v_Rock", 2.0f),
            ("v_HayBale", 1.0f), ("v_HayBale", 2.0f),
            ("v_Fence", 1.5f),
            ("v_CrateStack", 2.0f),
            ("v_Crate", 1.0f), ("v_Crate", 1.5f),
        };

        [Test]
        public void ThemedCoverMeshes_StayInsideFootprint()
        {
            foreach (var (key, w) in ThemedLooks)
                for (int s = 0; s <= 3; s++)
                {
                    var mesh = CoverProp3D.GetMesh(key, w, (CoverStage)s, new Color(0.7f, 0.5f, 0.4f));
                    Assert.IsNotNull(mesh, key);
                    Assert.Greater(mesh.vertexCount, 0, key);
                    var b = mesh.bounds;
                    float limit = w * 0.5f + (s >= 3 ? 0.45f : 0.12f);
                    Assert.LessOrEqual(Mathf.Max(-b.min.x, b.max.x), limit, $"{key} w={w} stage={s} half-width {Mathf.Max(-b.min.x, b.max.x):F2} > {limit:F2}");
                    // rubble (stage 3) may lie a few cm into the dirt; standing props must not sink
                    Assert.GreaterOrEqual(b.min.y, s >= 3 ? -0.2f : -0.05f, $"{key} stage={s} sinks below ground");
                    if (s == 0) Assert.Greater(b.max.y, 0.5f, $"{key} intact prop too flat to read as cover");
                }
        }

        [Test]
        public void VisualKey_DefaultKeepsCoverId()
        {
            Assert.AreEqual("cover_barrel", CoverProp3D.VisualKey("cover_barrel", CoverVisual.Default));
            Assert.AreEqual("v_HayBale", CoverProp3D.VisualKey("cover_barrel", CoverVisual.HayBale));
        }
    }
}
