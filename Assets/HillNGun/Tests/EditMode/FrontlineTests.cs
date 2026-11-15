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
            var layout = FrontlineRules.TopBottomDefault;
            const float radius = 0.3f;
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

                var up = layout.ClampSide(new Vector2(p.x, layout.LineCoord + 3f), radius, Team.Player);
                Assert.IsTrue(layout.OnOwnSide(up, radius, Team.Player));
                Assert.LessOrEqual(up.y, layout.LineCoord - radius + 0.02f);
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
                }
                else
                {
                    if (front) enemyFront++; else enemyBack++;
                    Assert.Greater(p.Center.y, layout.LineCoord);
                }
            }
            Assert.GreaterOrEqual(playerFront, 2);
            Assert.GreaterOrEqual(playerBack, 2);
            Assert.GreaterOrEqual(enemyFront, 2);
            Assert.GreaterOrEqual(enemyBack, 2);
        }
    }
}
