using NUnit.Framework;
using HillNGun.PvE;

namespace HillNGun.Tests.EditMode
{
    public class WantedBountyFormatTests
    {
        [Test]
        public void FormatBounty_UsesAsciiCommaThousands()
        {
            Assert.AreEqual("$1,000", StagePresentation.FormatBounty(1000));
            Assert.AreEqual("$10,000", StagePresentation.FormatBounty(10000));
            Assert.AreEqual("$999", StagePresentation.FormatBounty(999));
            Assert.AreEqual("$1,234,567", StagePresentation.FormatBounty(1234567));
            // Must not look like ko-KR grouping ($1.000)
            Assert.IsFalse(StagePresentation.FormatBounty(1000).Contains("."));
            Assert.IsTrue(StagePresentation.FormatBounty(1000).Contains(","));
        }
    }
}
