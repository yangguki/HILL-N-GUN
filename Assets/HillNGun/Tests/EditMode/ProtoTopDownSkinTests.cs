using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HillNGun.Battlefield;
using HillNGun.Characters;
using HillNGun.Core;
using HillNGun.Data;
using HillNGun.Input;
using HillNGun.PvE;
using HillNGun.Proto.TopDown;

namespace HillNGun.Tests.EditMode
{
    public class ProtoTopDownSkinTests
    {
        [Test]
        public void SkinToggle_DoesNotChangePlayerOrEnemyLogic()
        {
            var prevWorld = ServiceLocator.Get<FreeMoveWorld>();
            var prevFeel = ServiceLocator.Get<MovementFeelData>();
            GameObject hero = null;
            GameObject pixel = null;
            GameObject diorama = null;
            try
            {
                var feel = AssetDatabase.LoadAssetAtPath<MovementFeelData>(ProtoAssets.FeelMove);
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>(ProtoAssets.Buster);
                var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>(ProtoAssets.Enemy);
                Assert.IsNotNull(feel);
                Assert.IsNotNull(data);
                Assert.IsNotNull(enemyData);

                ServiceLocator.Register(feel);
                ServiceLocator.Register(new FreeMoveWorld(ProtoArena.WalkPolygon()));

                hero = new GameObject("proto_hero");
                var character = hero.AddComponent<CharacterStateMachine>();
                character.Initialize(data, Team.Player, 0, ProtoArena.PlayerSpawn, 0);
                character.SetMoveMode(MoveMode.FreeMove);
                character.OnMoveCommand(new MoveCommand(Vector2.up, 1f, 0f));
                character.TickSimulation(0.05f);

                var enemy = new EnemyInstance();
                enemy.InitializeFree(enemyData, ProtoArena.Door(), 0, 1f, 0.8f, new System.Random(1));
                var enemies = new[] { enemy };

                var before = ProtoLogicStamp.Capture(character, enemies);
                pixel = new GameObject("pixel");
                diorama = new GameObject("diorama");
                ProtoSkinSwitch.Apply(pixel, diorama, false);
                ProtoSkinSwitch.Apply(pixel, diorama, true);

                var after = ProtoLogicStamp.Capture(character, enemies);
                Assert.IsTrue(before.Same(after), before.Diff(after));
                Assert.IsTrue(pixel.activeSelf);
                Assert.IsFalse(diorama.activeSelf);
                Assert.AreEqual(before.Hp, character.CurrentHp);
                Assert.AreEqual(before.Position, character.Position);
                Assert.AreEqual(before.AmmoCells, character.AmmoGauge.CurrentCells);
                Assert.AreEqual(before.Grenades, character.GrenadeSystem.CurrentGrenades);
                Assert.AreEqual(before.EnemyHp[0], enemy.CurrentHp);
                Assert.AreEqual(before.EnemyPositions[0], enemy.Position);
            }
            finally
            {
                if (prevWorld != null) ServiceLocator.Register(prevWorld);
                else ServiceLocator.Unregister<FreeMoveWorld>();
                if (prevFeel != null) ServiceLocator.Register(prevFeel);
                else ServiceLocator.Unregister<MovementFeelData>();
                if (hero != null) Object.DestroyImmediate(hero);
                if (pixel != null) Object.DestroyImmediate(pixel);
                if (diorama != null) Object.DestroyImmediate(diorama);
            }
        }
    }
}
