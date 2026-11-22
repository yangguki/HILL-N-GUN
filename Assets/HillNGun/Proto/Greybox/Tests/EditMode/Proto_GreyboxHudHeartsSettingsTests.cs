using HillNGun.Proto.Greybox;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    [TestFixture]
    public class Proto_GreyboxHudHeartsSettingsTests
    {
        [Test]
        public void HeartsRoot_InBottomBand_Centered_NotTopBar()
        {
            float cx = (Proto_GreyboxHudBuilder.HeartsRootX0 + Proto_GreyboxHudBuilder.HeartsRootX1) * 0.5f;
            Assert.Greater(Proto_GreyboxHudBuilder.HeartsRootY0, 90f);
            Assert.LessOrEqual(Proto_GreyboxHudBuilder.HeartsRootY1, 98f);
            Assert.Greater(cx, 45f);
            Assert.Less(cx, 55f);
            Assert.Greater(Proto_GreyboxHudBuilder.HeartsRootY0, Proto_GreyboxHudBuilder.HeartsLegacyTopY1);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_NoOverlapJoystickOrCluster_At16x9()
        {
            Assert.IsTrue(Proto_GreyboxHudLayoutMath.HeartsClearHudChromeAt(1920f, 1080f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_NoOverlapJoystickOrCluster_At4x3Tablet()
        {
            Assert.IsTrue(Proto_GreyboxHudLayoutMath.HeartsClearHudChromeAt(2048f, 1536f));
            Assert.IsTrue(Proto_GreyboxHudLayoutMath.HeartsClearHudChromeAt(1024f, 768f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Hearts_NoOverlapJoystickOrCluster_At195x9Landscape()
        {
            Assert.IsTrue(Proto_GreyboxHudLayoutMath.HeartsClearHudChromeAt(2532f, 1170f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void HudBuild_CreatesEventSystemWithInputModule()
        {
            EditorSceneManager.OpenScene(Proto_GreyboxStageBuilder.ScenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            Assert.NotNull(cam);

            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (Application.isPlaying)
                    Object.Destroy(es.gameObject);
                else
                    Object.DestroyImmediate(es.gameObject);
            }

            Proto_GreyboxHudBuilder.Build(cam, null);

            var go = GameObject.Find(Proto_GreyboxUiEventSystem.EventSystemName);
            Assert.NotNull(go);
            Assert.NotNull(go.GetComponent<EventSystem>());
#if ENABLE_INPUT_SYSTEM
            Assert.NotNull(go.GetComponent<InputSystemUIInputModule>());
#endif
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Settings_NudgeAllyEnemy_ClampsAndPersists()
        {
            using (Proto_GreyboxBulletSpeedPrefs.EnterTestScope())
            {
                var tuning = new Proto_GreyboxRuntimeTuning();
                var canvasGo = new GameObject("TestCanvas", typeof(RectTransform));
                var settings = new Proto_GreyboxSettingsUi();
                settings.Ensure(canvasGo.transform, tuning, () => { });

                settings.NudgeAlly(0.5f);
                Assert.AreEqual(4.7f, tuning.AllyBulletSpeed, 0.01f);
                settings.NudgeAlly(20f);
                Assert.AreEqual(Proto_GreyboxSettingsUi.BulletSpeedMax, tuning.AllyBulletSpeed, 0.01f);
                settings.NudgeAlly(-30f);
                Assert.AreEqual(Proto_GreyboxSettingsUi.BulletSpeedMin, tuning.AllyBulletSpeed, 0.01f);

                settings.SetEnemyBulletSpeed(9.1f);
                Assert.AreEqual(9.1f, tuning.EnemyBulletSpeed, 0.01f);
                settings.NudgeEnemy(-0.5f);
                Assert.AreEqual(8.6f, tuning.EnemyBulletSpeed, 0.01f);

                var load = new Proto_GreyboxRuntimeTuning();
                Proto_GreyboxBulletSpeedPrefs.LoadInto(load);
                Assert.AreEqual(Proto_GreyboxSettingsUi.BulletSpeedMin, load.AllyBulletSpeed, 0.01f);
                Assert.AreEqual(8.6f, load.EnemyBulletSpeed, 0.01f);

                settings.Clear();
                Object.DestroyImmediate(canvasGo);
            }

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Settings_SetAllySliderValue_UpdatesTuning()
        {
            using (Proto_GreyboxBulletSpeedPrefs.EnterTestScope())
            {
                var tuning = new Proto_GreyboxRuntimeTuning();
                var canvasGo = new GameObject("TestCanvas", typeof(RectTransform));
                var settings = new Proto_GreyboxSettingsUi();
                settings.Ensure(canvasGo.transform, tuning, () => { });
                settings.SetAllyBulletSpeed(7.2f);
                Assert.AreEqual(7.2f, tuning.AllyBulletSpeed, 0.01f);
                settings.Clear();
                Object.DestroyImmediate(canvasGo);
            }

            LogAssert.NoUnexpectedReceived();
        }
    }
}