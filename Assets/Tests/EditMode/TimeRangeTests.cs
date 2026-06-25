using Ankhora.Domain.Model;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class TimeRangeTests
    {
        [Test]
        public void DefaultSentinel_IsWholeChapter()
        {
            // (0,0) is the documented "visible for the whole chapter" sentinel.
            Assert.IsTrue(default(TimeRange).IsWholeChapter);
        }

        [Test]
        public void ExplicitWindow_IsNotWholeChapter()
        {
            Assert.IsFalse(new TimeRange { start = 0.1f, end = 1f }.IsWholeChapter);
        }
    }
}
