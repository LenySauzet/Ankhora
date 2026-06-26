using Ankhora.Foundation.Passthrough;
using NUnit.Framework;

namespace Ankhora.Tests.EditMode
{
    public class PassthroughFadeTests
    {
        [Test]
        public void Step_TowardOne_ReachesTargetAfterFullDuration()
        {
            var fade = new PassthroughFade();

            fade.Step(1f, deltaTime: 0.4f, transitionSeconds: 0.4f);

            Assert.That(fade.Current, Is.EqualTo(1f).Within(1e-4f));
            Assert.IsTrue(fade.HasReached(1f));
        }

        [Test]
        public void Step_HalfDuration_IsHalfway()
        {
            var fade = new PassthroughFade();

            fade.Step(1f, deltaTime: 0.2f, transitionSeconds: 0.4f);

            Assert.That(fade.Current, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Step_ZeroDuration_SnapsInstantly()
        {
            var fade = new PassthroughFade();

            fade.Step(1f, deltaTime: 0.0001f, transitionSeconds: 0f);

            Assert.That(fade.Current, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Step_BackTowardZero_ReturnsToVr()
        {
            var fade = new PassthroughFade { Current = 1f };

            fade.Step(0f, deltaTime: 0.4f, transitionSeconds: 0.4f);

            Assert.That(fade.Current, Is.EqualTo(0f).Within(1e-4f));
            Assert.IsTrue(fade.HasReached(0f));
        }

        [Test]
        public void Step_DoesNotOvershootTarget()
        {
            var fade = new PassthroughFade();

            fade.Step(1f, deltaTime: 10f, transitionSeconds: 0.4f); // huge dt

            Assert.That(fade.Current, Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void Opacity_IsEasedAndMonotonicWithFixedEndpoints()
        {
            var atZero = new PassthroughFade { Current = 0f };
            var atQuarter = new PassthroughFade { Current = 0.25f };
            var atHalf = new PassthroughFade { Current = 0.5f };
            var atOne = new PassthroughFade { Current = 1f };

            Assert.That(atZero.Opacity, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(atOne.Opacity, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(atHalf.Opacity, Is.EqualTo(0.5f).Within(1e-4f)); // SmoothStep symmetric at midpoint
            // Eased: slow start means the quarter point sits below its linear value.
            Assert.That(atQuarter.Opacity, Is.LessThan(0.25f));
            Assert.That(atQuarter.Opacity, Is.LessThan(atHalf.Opacity));
        }
    }
}
