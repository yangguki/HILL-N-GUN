using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.Proto.Frontline;
using HillNGun.Proto.TopDown;

namespace HillNGun.Tests.EditMode
{
    public class ProtoTopDownIosTests
    {
        [Test]
        public void Restart_RestoresSpawnHpAmmoAndGrenades()
        {
            var prevWorld = ServiceLocator.Get<FreeMoveWorld>();
            var prevFeel = ServiceLocator.Get<MovementFeelData>();
            GameObject hero = null;
            try
            {
                var feel = AssetDatabase.LoadAssetAtPath<MovementFeelData>(ProtoAssets.FeelMove);
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>(ProtoAssets.Buster);
                Assert.IsNotNull(feel);
                Assert.IsNotNull(data);
                ServiceLocator.Register(feel);
                ServiceLocator.Register(new FreeMoveWorld(ProtoArena.WalkPolygon()));

                hero = new GameObject("proto_restart");
                var character = hero.AddComponent<CharacterStateMachine>();
                ProtoTopDownMatch.ResetPlayer(character, data);
                int hp = character.CurrentHp;
                int ammo = character.AmmoGauge.CurrentCells;
                int grenades = character.GrenadeSystem.CurrentGrenades;

                character.OnMoveCommand(new MoveCommand(Vector2.up, 1f, 0f));
                character.TickSimulation(0.45f);
                character.TakeDamage(400, Vector2.down);
                character.AmmoGauge.ConsumeShot();
                Assert.IsTrue(character.GrenadeSystem.TryUseGrenade());

                Assert.Less(character.CurrentHp, hp);
                Assert.AreNotEqual(ProtoArena.PlayerSpawn, character.Position);
                Assert.Less(character.AmmoGauge.CurrentCells, ammo);
                Assert.Less(character.GrenadeSystem.CurrentGrenades, grenades);

                ProtoTopDownMatch.ResetPlayer(character, data);
                Assert.AreEqual(ProtoArena.PlayerSpawn, character.Position);
                Assert.AreEqual(hp, character.CurrentHp);
                Assert.AreEqual(CharacterOverallState.Active, character.OverallState);
                Assert.AreEqual(ammo, character.AmmoGauge.CurrentCells);
                Assert.AreEqual(grenades, character.GrenadeSystem.CurrentGrenades);
            }
            finally
            {
                if (prevWorld != null) ServiceLocator.Register(prevWorld);
                else ServiceLocator.Unregister<FreeMoveWorld>();
                if (prevFeel != null) ServiceLocator.Register(prevFeel);
                else ServiceLocator.Unregister<MovementFeelData>();
                if (hero != null) Object.DestroyImmediate(hero);
            }
        }

        [Test]
        public void SafeLayout_IphoneAndOtherAspects_KeepControlsInsideAndArenaVisible()
        {
            AssertPhone(2556f, 1179f, 460f, new Rect(177f, 63f, 2556f - 177f - 141f, 1179f - 63f));
            AssertPhone(2556f, 1179f, 0f, new Rect(177f, 63f, 2556f - 177f - 141f, 1179f - 63f));
            AssertPhone(1920f, 1080f, 110f, new Rect(0f, 0f, 1920f, 1080f));
            AssertPhone(1024f, 768f, 132f, new Rect(0f, 0f, 1024f, 768f));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(2556f / 1179f));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(19.5f / 9f));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(16f / 9f));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(4f / 3f));
        }

        [Test]
        public void PixelFit_Aspect195to9_ContainsTopDownAndFrontlineArenaFrames()
        {
            const float aspect = 19.5f / 9f;
            const float pad = 0.35f;

            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(aspect, -1.6f, 19.6f, 0f, 14.6f, pad));

            var layout = FrontlineRules.TopBottomDefault;
            // Camera frames the full BG plate; dirt walk stays inside that plate.
            float fMinX = layout.PlateMinX - 1.2f;
            float fMaxX = layout.PlateMaxX + 1.2f;
            float fMinY = layout.PlateMinY - 0.9f;
            float fMaxY = layout.PlateMaxY + 1.0f;
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(aspect, fMinX, fMaxX, fMinY, fMaxY, pad));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(
                aspect, layout.MinX, layout.MaxX, layout.MinY, layout.MaxY, pad), "dirt walk in camera");

            AssertBackgroundFillsCameraKeepsDirt(ProtoAssets.PixelArtTopDown, aspect, -1.6f, 19.6f, 0f, 14.6f, pad);
            AssertBackgroundFillsCameraKeepsDirt(ProtoAssets.PixelArtFrontline, aspect, fMinX, fMaxX, fMinY, fMaxY, pad);
        }

        [Test]
        public void CoverKind_HayMapsToHayNotSandbag()
        {
            var hay = AssetDatabase.LoadAssetAtPath<CoverData>(ProtoAssets.Hay);
            Assert.IsNotNull(hay);
            Assert.AreEqual("cover_hay", hay.id);
            // ProtoPresentation.CoverKind is private; CoverSprite("hay") must resolve to the hay plate.
            var art = AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(ProtoAssets.PixelArtTopDown);
            Assert.IsNotNull(art);
            Assert.IsNotNull(art.hay, "hay sprite assigned");
            Assert.AreSame(art.hay, art.CoverSprite("hay"));
            if (art.sandbag != null)
                Assert.AreNotSame(art.hay, art.CoverSprite("sandbag"));
            if (art.hayDamaged != null)
                Assert.AreSame(art.hayDamaged, art.CoverSprite("hay", 0.4f));
            if (art.hayDestroyed != null)
                Assert.AreSame(art.hayDestroyed, art.CoverSprite("hay", 0f));
            if (art.crateDamaged != null)
                Assert.AreSame(art.crateDamaged, art.CoverSprite("crate", 0.4f));
        }

        [Test]
        public void FrontlineSpawn_StaysInsideWalkBounds()
        {
            const float radius = 0.3f;
            var layout = FrontlineRules.TopBottomDefault;
            for (int slot = 0; slot < 3; slot++)
            {
                var p = layout.Spawn(Team.Player, slot);
                var e = layout.Spawn(Team.Enemy, slot);
                Assert.GreaterOrEqual(p.x, layout.MinX + radius - 1e-3f);
                Assert.LessOrEqual(p.x, layout.MaxX - radius + 1e-3f);
                Assert.GreaterOrEqual(p.y, layout.MinY + radius - 1e-3f);
                Assert.LessOrEqual(p.y, layout.LineCoord - radius + 1e-3f);
                Assert.LessOrEqual(e.y, layout.MaxY - radius + 1e-3f);
                Assert.GreaterOrEqual(e.y, layout.LineCoord + radius - 1e-3f);
                var cp = layout.ClampSide(new Vector2(layout.MinX - 2f, p.y), radius, Team.Player);
                Assert.GreaterOrEqual(cp.x, layout.MinX + radius - 1e-4f);
                Assert.LessOrEqual(cp.y, layout.LineCoord - radius + 0.02f);
            }
        }

        static void AssertBackgroundFillsCameraKeepsDirt(
            string artPath, float aspect, float minX, float maxX, float minY, float maxY, float pad)
        {
            var art = AssetDatabase.LoadAssetAtPath<ProtoPixelArt>(artPath);
            Assert.IsNotNull(art, artPath);
            Assert.IsNotNull(art.background, artPath + " background");
            float unitsW = art.background.rect.width / art.background.pixelsPerUnit;
            float unitsH = art.background.rect.height / art.background.pixelsPerUnit;
            var cam = ProtoSafeLayout.PixelFit(aspect, minX - pad, maxX + pad, minY - pad, maxY + pad);
            float camW = cam.OrthoSize * 2f * aspect;
            float camH = cam.OrthoSize * 2f;
            float cover = ProtoSafeLayout.BackgroundCoverScale(camW, camH, unitsW, unitsH);
            float plateW = unitsW * cover;
            float plateH = unitsH * cover;
            // Cover-scale fills the camera (no letterbox / blue bars).
            Assert.GreaterOrEqual(plateW, camW - 0.02f, artPath + " fill width");
            Assert.GreaterOrEqual(plateH, camH - 0.02f, artPath + " fill height");
            // Plate is centered on the camera that already contains the arena, so the combat/dirt
            // center stays in view; only plate edges may crop.
            float arenaW = maxX - minX;
            float arenaH = Mathf.Abs(ProtoArena.ToPixel(new Vector2(0f, maxY)).y - ProtoArena.ToPixel(new Vector2(0f, minY)).y);
            Assert.GreaterOrEqual(plateW, arenaW - 0.02f, artPath + " plate covers arena width");
            Assert.GreaterOrEqual(plateH, arenaH - 0.02f, artPath + " plate covers arena height");
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(aspect, minX, maxX, minY, maxY, pad), artPath + " camera");
        }

        static void AssertPhone(float screenW, float screenH, float dpi, Rect safe)
        {
            float scale = ProtoSafeLayout.CanvasScale(screenW, screenH);
            float min = ProtoSafeLayout.MinButtonCanvas(screenW, screenH, dpi);
            float panelW = safe.width / scale;
            float panelH = safe.height / scale;
            var slots = ProtoSafeLayout.Place(panelW, panelH, min);
            var panel = new Rect(0f, 0f, panelW, panelH);
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Move), "move");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Aim), "aim");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Fire), "fire");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Bomb), "bomb");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Roll), "roll");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Skin), "skin");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Hud), "hud");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Result), "result");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Restart), "restart");
            Assert.IsTrue(ProtoSafeLayout.Contains(panel, slots.Menu), "menu");
            float need = 44f * ProtoSafeLayout.PixelsPerPoint(dpi, screenW, screenH) - 0.6f;
            Assert.GreaterOrEqual(slots.Fire.width * scale, need);
            Assert.GreaterOrEqual(slots.Fire.height * scale, need);
            Assert.GreaterOrEqual(slots.Bomb.height * scale, need);
            Assert.GreaterOrEqual(slots.Roll.height * scale, need);
            Assert.GreaterOrEqual(slots.Skin.width * scale, need);
            Assert.GreaterOrEqual(slots.Skin.height * scale, need);
            Assert.GreaterOrEqual(slots.Restart.height * scale, need);
            Assert.GreaterOrEqual(slots.Menu.height * scale, need);
            Assert.GreaterOrEqual(Mathf.Min(slots.Move.width, slots.Move.height) * scale, need);
        }
    }
}
