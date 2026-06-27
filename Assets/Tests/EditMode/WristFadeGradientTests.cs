using System.Collections.Generic;
using Ankhora.Domain.Spatial;
using NUnit.Framework;
using UnityEngine;

namespace Ankhora.Tests.EditMode
{
    /// <summary>
    /// The ghost/live hand wrist must fade to transparent toward the forearm stump. Two mask-texture
    /// attempts failed on device (the runtime mesh's UVs could not be trusted), so the fade is computed
    /// deterministically from mesh geometry instead: the wrist is the compact (low perpendicular spread)
    /// end of the hand's principal axis, and opacity ramps up from it. These tests pin that pure logic.
    /// </summary>
    public class WristFadeGradientTests
    {
        // A hand-like point cloud along Y, authored Meta-style with the WRIST AT THE ORIGIN (Y=0) and the
        // fingers extending away (+Y, or −Y to flip the orientation). First 40 verts = wrist, next 40 =
        // fingers, rest = palm.
        private static Vector3[] BuildHand(bool fingersTowardsPlusY)
        {
            var pts = new List<Vector3>();
            float wristY = 0f;
            float tipY = fingersTowardsPlusY ? 0.25f : -0.25f;

            for (int i = 0; i < 40; i++)                    // wrist: tight disk (radius 0.02)
            {
                float a = i / 40f * Mathf.PI * 2f;
                pts.Add(new Vector3(Mathf.Cos(a) * 0.02f, wristY, Mathf.Sin(a) * 0.02f));
            }
            for (int f = 0; f < 5; f++)                     // fingers: 5 columns spread wide on X
                for (int i = 0; i < 8; i++)
                    pts.Add(new Vector3((f - 2) * 0.03f, tipY, (i - 4) * 0.004f));
            for (int i = 0; i < 30; i++)                    // palm: mid, moderate spread
                pts.Add(new Vector3((i % 6 - 3) * 0.012f, Mathf.Lerp(wristY, tipY, 0.45f), 0f));

            return pts.ToArray();
        }

        [Test]
        public void WristEndFades_FingerEndOpaque()
        {
            float[] fade = WristFadeGradient.Compute(BuildHand(fingersTowardsPlusY: true));

            for (int i = 0; i < 40; i++)
                Assert.That(fade[i], Is.LessThan(0.2f), $"wrist vertex {i} should fade, got {fade[i]}");
            for (int i = 40; i < 80; i++)
                Assert.That(fade[i], Is.GreaterThan(0.8f), $"finger vertex {i} should be opaque, got {fade[i]}");
        }

        [Test]
        public void DetectsWristEnd_RegardlessOfOrientation()
        {
            // Fingers now extend toward −Y (wrist still at origin) — detection must not assume a direction.
            float[] fade = WristFadeGradient.Compute(BuildHand(fingersTowardsPlusY: false));

            for (int i = 0; i < 40; i++)
                Assert.That(fade[i], Is.LessThan(0.2f), $"wrist vertex {i} should fade, got {fade[i]}");
            for (int i = 40; i < 80; i++)
                Assert.That(fade[i], Is.GreaterThan(0.8f), $"finger vertex {i} should be opaque, got {fade[i]}");
        }

        [Test]
        public void FadeIsMonotonicAwayFromWrist()
        {
            // A straight ramp of points along Y with the wrist at the origin must give non-decreasing opacity.
            var pts = new List<Vector3>();
            for (int i = 0; i < 50; i++)
                pts.Add(new Vector3(0f, i * 0.005f, 0f));              // wrist at Y=0, ramping to +Y

            float[] fade = WristFadeGradient.Compute(pts.ToArray());
            for (int i = 1; i < 50; i++)
                Assert.That(fade[i], Is.GreaterThanOrEqualTo(fade[i - 1] - 1e-4f),
                    $"fade must not decrease away from wrist at {i}: {fade[i - 1]} -> {fade[i]}");
        }

        [Test]
        public void EmptyInput_ReturnsEmpty()
        {
            Assert.That(WristFadeGradient.Compute(new Vector3[0]).Length, Is.EqualTo(0));
            Assert.That(WristFadeGradient.Compute(null).Length, Is.EqualTo(0));
        }
    }
}
