using Ankhora.Foundation.Passthrough;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    public class PassthroughControllerTests
    {
        private GameObject _go;

        private sealed class FakeSurface : IPassthroughSurface
        {
            public bool Enabled { get; private set; }
            public int Calls { get; private set; }

            public void SetEnabled(bool enabled)
            {
                Enabled = enabled;
                Calls++;
            }
        }

        private PassthroughController NewController()
        {
            _go = new GameObject(nameof(PassthroughController));
            return _go.AddComponent<PassthroughController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void Initialize_StartInVr_StateOffAndSurfaceDisabled()
        {
            var fake = new FakeSurface();
            PassthroughController controller = NewController();

            controller.Initialize(fake, startInPassthrough: false);

            Assert.IsFalse(controller.IsPassthroughOn);
            Assert.IsFalse(fake.Enabled);
        }

        [Test]
        public void Initialize_StartInPassthrough_StateOnAndSurfaceEnabled()
        {
            var fake = new FakeSurface();
            PassthroughController controller = NewController();

            controller.Initialize(fake, startInPassthrough: true);

            Assert.IsTrue(controller.IsPassthroughOn);
            Assert.IsTrue(fake.Enabled);
        }

        [Test]
        public void Toggle_FlipsStateAndDrivesSurfaceEachTime()
        {
            var fake = new FakeSurface();
            PassthroughController controller = NewController();
            controller.Initialize(fake, startInPassthrough: false);

            controller.Toggle();
            Assert.IsTrue(controller.IsPassthroughOn);
            Assert.IsTrue(fake.Enabled);

            controller.Toggle();
            Assert.IsFalse(controller.IsPassthroughOn);
            Assert.IsFalse(fake.Enabled);
        }
    }
}
