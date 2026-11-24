using NUnit.Framework;

namespace HillNGun.Proto.Greybox.Tests.EditMode
{
    public class Proto_GreyboxViewModeTests
    {
        [SetUp]
        public void SetUp()
        {
            Proto_GreyboxViewModeState.ResetForTests();
            Proto_GreyboxViewPresenterRegistry.ResetForTests();
        }

        [Test]
        public void DefaultViewMode_Is2D()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.ResetForTests();
                Assert.AreEqual(Proto_GreyboxViewMode.View2DPerspective, Proto_GreyboxViewModeState.Current);
            }
        }

        [Test]
        public void ViewModePrefs_RoundTrip_InTestScope()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModePrefs.Save(Proto_GreyboxViewMode.View3D);
                Assert.AreEqual(Proto_GreyboxViewMode.View3D, Proto_GreyboxViewModePrefs.LoadOrDefault());
                Proto_GreyboxViewModePrefs.Save(Proto_GreyboxViewMode.View2D);
                Assert.AreEqual(Proto_GreyboxViewMode.View2D, Proto_GreyboxViewModePrefs.LoadOrDefault());
            }
        }

        [Test]
        public void SwitchViewMode_UpdatesState()
        {
            using (Proto_GreyboxViewModePrefs.EnterTestScope())
            {
                Proto_GreyboxViewModeState.ResetForTests();
                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View3D, persist: true);
                Assert.AreEqual(Proto_GreyboxViewMode.View3D, Proto_GreyboxViewModeState.Current);
                Proto_GreyboxViewModeState.SetMode(Proto_GreyboxViewMode.View2D, persist: true);
                Assert.IsFalse(Proto_GreyboxViewModeState.Is3D);
            }
        }
  

        [Test]
        public void PauseViewToggle_HitRect_IsDistinctFromOtherButtons()
        {
            Assert.IsTrue(Proto_GreyboxPauseUi.IsViewToggleHit(50f, 49.5f));
            Assert.IsFalse(Proto_GreyboxPauseUi.IsResumeHit(50f, 49.5f));
            Assert.IsFalse(Proto_GreyboxPauseUi.IsPauseSettingsHit(50f, 49.5f));
            Assert.IsFalse(Proto_GreyboxPauseUi.IsPauseRestartHit(50f, 49.5f));
            Assert.IsTrue(Proto_GreyboxPauseUi.IsResumeHit(50f, 59f));
            Assert.IsFalse(Proto_GreyboxPauseUi.IsViewToggleHit(50f, 59f));
        }

        [Test]
        public void SettingsViewToggle_HitRect_IsDistinctFromClose()
        {
            var settings = new Proto_GreyboxSettingsUi();
            Assert.IsTrue(settings.IsViewToggleHit(50f, 65.5f));
            Assert.IsFalse(Proto_GreyboxSettingsUi.IsCloseHit(50f, 65.5f));
            Assert.IsTrue(Proto_GreyboxSettingsUi.IsCloseHit(50f, 73f));
            Assert.IsFalse(settings.IsViewToggleHit(50f, 73f));
        }

        [Test]
        public void Registry_WithoutFactory_EnsureReturnsNull()
        {
            Proto_GreyboxViewPresenterRegistry.ClearFactoryForTests();
            Assert.IsFalse(Proto_GreyboxViewPresenterRegistry.HasFactory);
            Assert.IsNull(Proto_GreyboxViewPresenterRegistry.Ensure());
        }
    }
}
