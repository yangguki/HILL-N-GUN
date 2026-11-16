using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Combat;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.Proto.Frontline;
using HillNGun.Proto.TopDown;

namespace HillNGun.Tests.EditMode
{
    public class FrontlineTests
    {
        [Test]
        public void SideClamp_MoveAndRollStayOnOwnSide_OfTheLine()
        {
            AssertClampOnLayout(FrontlineRules.TopBottomDefault, Vector2.up);
            AssertClampOnLayout(FrontlineRules.LeftRightDefault, Vector2.right);
        }

        static void AssertClampOnLayout(FrontlineLayout layout, Vector2 towardEnemy)
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
                var world = new FreeMoveWorld(layout.WalkPolygon());
                world.StaticObstacles.Add(layout.Wall);
                ServiceLocator.Register(world);

                Vector2 start = layout.IsTopBottom
                    ? new Vector2(layout.MidX, layout.LineCoord - 1.1f)
                    : new Vector2(layout.LineCoord - 1.1f, layout.MidY);
                var blocked = world.Move(start, towardEnemy * 8f, 0.3f);
                Assert.IsTrue(layout.OnOwnSide(blocked, 0.3f, Team.Player), layout.SplitAxis + " world " + blocked);

                hero = new GameObject("front_clamp_" + layout.SplitAxis);
                var actor = hero.AddComponent<CharacterStateMachine>();
                actor.Initialize(data, Team.Player, 0, start, 0);
                actor.SetMoveMode(MoveMode.FreeMove);
                actor.OnMoveCommand(new MoveCommand(towardEnemy, 1f, 0f));
                for (int i = 0; i < 20; i++) actor.TickSimulation(0.05f);
                Assert.IsTrue(layout.OnOwnSide(actor.Position, actor.BodyRadius, Team.Player), layout.SplitAxis + " move " + actor.Position);

                actor.OnRollCommand(new RollCommand(towardEnemy, 1f));
                for (int i = 0; i < 16; i++) actor.TickSimulation(0.05f);
                Assert.IsTrue(layout.OnOwnSide(actor.Position, actor.BodyRadius, Team.Player), layout.SplitAxis + " roll " + actor.Position);

                var shoved = start + towardEnemy * 4f;
                var back = layout.ClampSide(shoved, actor.BodyRadius, Team.Player);
                actor.SetPosition(back);
                Assert.IsTrue(layout.OnOwnSide(actor.Position, actor.BodyRadius, Team.Player), layout.SplitAxis + " knockback " + actor.Position);
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
        public void Bullet_CrossesTheFrontLine()
        {
            var layout = FrontlineRules.TopBottomDefault;
            var world = new FreeMoveWorld(layout.WalkPolygon());
            world.StaticObstacles.Add(layout.Wall);
            var start = new Vector2(layout.MidX, layout.LineCoord - 2f);
            var blocked = world.Move(start, Vector2.up * 6f, 0.3f);
            Assert.Less(blocked.y, layout.LineCoord);

            var shot = new Projectile();
            shot.InitializeSimple(start, Vector2.up, 20f, 0.2f, 10, 30f, Team.Player, false);
            shot.Update(0.4f);
            Assert.IsTrue(shot.IsActive);
            Assert.Greater(shot.Position.y, layout.LineCoord);
        }

        [Test]
        public void Judge_WipeEndsImmediately()
        {
            var wipe = new List<FrontlineRules.Unit>
            {
                new FrontlineRules.Unit { Team = Team.Player, Alive = true, HpRatio = 0.2f },
                new FrontlineRules.Unit { Team = Team.Enemy, Alive = false, HpRatio = 0f }
            };
            Assert.AreEqual(FrontlineOutcome.Player, FrontlineRules.Judge(wipe, false));
            Assert.AreEqual(FrontlineOutcome.Enemy, FrontlineRules.Judge(new List<FrontlineRules.Unit>
            {
                new FrontlineRules.Unit { Team = Team.Player, Alive = false, HpRatio = 0f },
                new FrontlineRules.Unit { Team = Team.Enemy, Alive = true, HpRatio = 0.4f }
            }, false));
        }

        [Test]
        public void Judge_Timeout_SurvivorsCount()
        {
            var count = new List<FrontlineRules.Unit>
            {
                new FrontlineRules.Unit { Team = Team.Player, Alive = true, HpRatio = 0.1f },
                new FrontlineRules.Unit { Team = Team.Player, Alive = true, HpRatio = 0.1f },
                new FrontlineRules.Unit { Team = Team.Enemy, Alive = true, HpRatio = 1f }
            };
            Assert.AreEqual(FrontlineOutcome.Player, FrontlineRules.Judge(count, true));
            Assert.AreEqual(FrontlineOutcome.Playing, FrontlineRules.Judge(count, false));
        }

        [Test]
        public void Judge_Timeout_HpRatioSum()
        {
            // Absolute HP would pick the player (100 > 80). The ratio sum picks the enemy.
            var ratio = new List<FrontlineRules.Unit>
            {
                new FrontlineRules.Unit { Team = Team.Player, Alive = true, HpRatio = 100f / 1000f },
                new FrontlineRules.Unit { Team = Team.Enemy, Alive = true, HpRatio = 80f / 100f }
            };
            Assert.AreEqual(FrontlineOutcome.Enemy, FrontlineRules.Judge(ratio, true));
            Assert.AreEqual(FrontlineOutcome.Playing, FrontlineRules.Judge(ratio, false));
        }

        [Test]
        public void Judge_Timeout_Draw()
        {
            var draw = new List<FrontlineRules.Unit>
            {
                new FrontlineRules.Unit { Team = Team.Player, Alive = true, HpRatio = 0.5f },
                new FrontlineRules.Unit { Team = Team.Enemy, Alive = true, HpRatio = 250f / 500f }
            };
            Assert.AreEqual(FrontlineOutcome.Draw, FrontlineRules.Judge(draw, true));
        }

        [Test]
        public void TopBottom_SpawnAndClamp_StayInDirtBand()
        {
            var layout = FrontlineRules.FromDirtUv(
                FrontlineSplitAxis.TopBottom, FrontlineRules.V21DirtUvMin, FrontlineRules.V21DirtUvMax);
            AssertV21DirtContainsSpawnsAndClamps(layout);
        }

        [Test]
        public void TopBottomDefault_UsesV21DirtUv()
        {
            var layout = FrontlineRules.TopBottomDefault;
            var expected = FrontlineRules.FromDirtUv(
                FrontlineSplitAxis.TopBottom, FrontlineRules.V21DirtUvMin, FrontlineRules.V21DirtUvMax);
            Assert.AreEqual(expected.MinX, layout.MinX, 1e-4f);
            Assert.AreEqual(expected.MaxX, layout.MaxX, 1e-4f);
            Assert.AreEqual(expected.MinY, layout.MinY, 1e-4f);
            Assert.AreEqual(expected.MaxY, layout.MaxY, 1e-4f);
            AssertV21DirtContainsSpawnsAndClamps(layout);
            // Player bottom / enemy top.
            Assert.Less(layout.Spawn(Team.Player, 0).y, layout.LineCoord);
            Assert.Greater(layout.Spawn(Team.Enemy, 0).y, layout.LineCoord);
        }

        static void AssertV21DirtContainsSpawnsAndClamps(FrontlineLayout layout)
        {
            const float radius = 0.3f;
            Assert.Greater(layout.MinX, layout.PlateMinX + 0.5f, "v2.1 dirt inset from plate L");
            Assert.Less(layout.MaxX, layout.PlateMaxX - 0.5f, "v2.1 dirt inset from plate R");
            for (int slot = 0; slot < 3; slot++)
            {
                var p = layout.Spawn(Team.Player, slot);
                var e = layout.Spawn(Team.Enemy, slot);
                Assert.Less(p.y, layout.LineCoord, "player below line");
                Assert.Greater(e.y, layout.LineCoord, "enemy above line");
                Assert.GreaterOrEqual(p.y, layout.MinY + radius - 1e-3f);
                Assert.LessOrEqual(e.y, layout.MaxY - radius + 1e-3f);
                Assert.GreaterOrEqual(p.x, layout.MinX + radius - 1e-3f);
                Assert.LessOrEqual(p.x, layout.MaxX - radius + 1e-3f);
                Assert.GreaterOrEqual(e.x, layout.MinX + radius - 1e-3f);
                Assert.LessOrEqual(e.x, layout.MaxX - radius + 1e-3f);

                var up = layout.ClampSide(new Vector2(p.x, layout.LineCoord + 3f), radius, Team.Player);
                Assert.IsTrue(layout.OnOwnSide(up, radius, Team.Player));
                Assert.LessOrEqual(up.y, layout.LineCoord - radius + 0.02f);
                Assert.GreaterOrEqual(up.x, layout.MinX + radius - 1e-3f);
                Assert.LessOrEqual(up.x, layout.MaxX - radius + 1e-3f);
            }
        }

        [Test]
        public void LeftRight_SpawnAndClamp_StillWorks()
        {
            var layout = FrontlineRules.LeftRightDefault;
            const float radius = 0.3f;
            for (int slot = 0; slot < 3; slot++)
            {
                var p = layout.Spawn(Team.Player, slot);
                var e = layout.Spawn(Team.Enemy, slot);
                Assert.Less(p.x, layout.LineCoord);
                Assert.Greater(e.x, layout.LineCoord);
                var right = layout.ClampSide(new Vector2(layout.LineCoord + 3f, p.y), radius, Team.Player);
                Assert.IsTrue(layout.OnOwnSide(right, radius, Team.Player));
            }
        }

        [Test]
        public void TopBottom_CoverRows_TwoPerSide()
        {
            var layout = FrontlineRules.TopBottomDefault;
            var pieces = layout.Pieces();
            int playerFront = 0, playerBack = 0, enemyFront = 0, enemyBack = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                var p = pieces[i];
                float d = Mathf.Abs(p.Center.y - layout.LineCoord);
                float sideSpan = p.Team == Team.Player
                    ? (layout.LineCoord - layout.MinY)
                    : (layout.MaxY - layout.LineCoord);
                bool front = d <= sideSpan * 0.45f;
                if (p.Team == Team.Player)
                {
                    if (front) playerFront++; else playerBack++;
                    Assert.Less(p.Center.y, layout.LineCoord);
                    AssertInDirt(layout, p.Center);
                }
                else
                {
                    if (front) enemyFront++; else enemyBack++;
                    Assert.Greater(p.Center.y, layout.LineCoord);
                    AssertInDirt(layout, p.Center);
                }
            }
            Assert.GreaterOrEqual(playerFront, 2);
            Assert.GreaterOrEqual(playerBack, 2);
            Assert.GreaterOrEqual(enemyFront, 2);
            Assert.GreaterOrEqual(enemyBack, 2);
            Assert.GreaterOrEqual(pieces.Length, 8, "preview 4/side + extras for 3v3 slots");
        }

        static void AssertInDirt(FrontlineLayout layout, Vector2 p)
        {
            Assert.GreaterOrEqual(p.x, layout.MinX - 0.05f);
            Assert.LessOrEqual(p.x, layout.MaxX + 0.05f);
            Assert.GreaterOrEqual(p.y, layout.MinY - 0.05f);
            Assert.LessOrEqual(p.y, layout.MaxY + 0.05f);
        }

        [Test]
        public void TopBottom_ArchPosts_BlockAsStaticObstacles()
        {
            var layout = FrontlineRules.TopBottomDefault;
            var posts = layout.ArchPosts();
            Assert.AreEqual(2, posts.Length, "two arch posts");
            var world = new FreeMoveWorld(layout.WalkPolygon());
            world.StaticObstacles.Add(layout.Wall);
            for (int i = 0; i < posts.Length; i++)
                world.StaticObstacles.Add(posts[i]);

            // Step from below a post column into the post — movement must stop short of the post interior.
            for (int i = 0; i < posts.Length; i++)
            {
                var post = posts[i];
                float cx = post.xMin + post.width * 0.5f;
                float startY = Mathf.Max(layout.LineCoord + 0.6f, post.yMin - 1.2f);
                var start = new Vector2(cx, startY);
                Assert.IsTrue(layout.OnOwnSide(start, 0.3f, Team.Enemy), "start on enemy half toward arch");
                Assert.IsTrue(world.IsFree(start, 0.3f), "start clear of post");
                var blocked = world.Move(start, Vector2.up * 4f, 0.3f);
                Assert.Less(blocked.y, post.yMin + post.height * 0.5f,
                    "arch post blocks upward move at x=" + cx);
                Assert.LessOrEqual(blocked.y, post.yMin + 0.35f + 1e-2f,
                    "stopped near post bottom edge");
            }
        }

        [Test]
        public void TopBottom_Fence_DisplayOnly_NotAnExtraCollider()
        {
            var layout = FrontlineRules.TopBottomDefault;
            // Fence span is shorter than dirt and centered on the split; not added as an obstacle.
            Assert.Greater(layout.FenceMinX, layout.MinX + 0.5f);
            Assert.Less(layout.FenceMaxX, layout.MaxX - 0.5f);
            float fenceLen = layout.FenceMaxX - layout.FenceMinX;
            float dirtLen = layout.MaxX - layout.MinX;
            Assert.Less(fenceLen, dirtLen * 0.9f, "fence shorter than dirt width");

            var posts = layout.ArchPosts();
            var world = new FreeMoveWorld(layout.WalkPolygon());
            world.StaticObstacles.Add(layout.Wall);
            for (int i = 0; i < posts.Length; i++)
                world.StaticObstacles.Add(posts[i]);

            // Along the player side near the line but outside WallHalf — lateral slide must not hit a fence collider.
            float y = layout.LineCoord - 0.55f;
            var a = new Vector2(layout.FenceMinX + 0.5f, y);
            var b = world.Move(a, Vector2.right * (fenceLen - 1f), 0.25f);
            Assert.Greater(b.x, a.x + fenceLen * 0.4f, "no fence collider blocking lateral walk");
        }

        [Test]
        public void TopBottom_CameraFit195_ContainsDirt()
        {
            const float aspect = 19.5f / 9f;
            const float pad = 0.35f;
            var layout = FrontlineRules.TopBottomDefault;
            float fMinX = layout.PlateMinX - 1.2f;
            float fMaxX = layout.PlateMaxX + 1.2f;
            float fMinY = layout.PlateMinY - 0.9f;
            float fMaxY = layout.PlateMaxY + 1.0f;
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(aspect, fMinX, fMaxX, fMinY, fMaxY, pad));
            Assert.IsTrue(ProtoSafeLayout.PixelShowsArena(
                aspect, layout.MinX, layout.MaxX, layout.MinY, layout.MaxY, pad), "dirt in camera");
        }
    }
}
