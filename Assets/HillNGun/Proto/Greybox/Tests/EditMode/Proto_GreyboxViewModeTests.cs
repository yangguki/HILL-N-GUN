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
                Assert.AreEqual(Proto_GreyboxViewMode.View2D, Proto_GreyboxViewModeState.Current);
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
    }
}