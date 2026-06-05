using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// Smoke test whose only job is to make CI compile the whole project and the
    /// EditMode test assembly, and to give the test runner at least one test to
    /// execute. Replace / extend with real coverage as gameplay code lands
    /// (e.g. the PassthroughController described in the Foundation plan).
    /// </summary>
    public class SmokeTests
    {
        [Test]
        public void Project_Compiles_AndEditModeTestsRun()
        {
            Assert.Pass("EditMode test assembly compiled and executed.");
        }
    }
}
