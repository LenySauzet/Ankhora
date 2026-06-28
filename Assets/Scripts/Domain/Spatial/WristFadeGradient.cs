using UnityEngine;

namespace Ankhora.Domain.Spatial
{
    /// <summary>
    /// Per-vertex opacity gradient that fades a hand mesh toward its wrist/forearm stump, computed purely
    /// from geometry. The earlier approach (Meta's baked glow-mask alpha sampled by UV) could not be made
    /// to fade on device — the runtime hand mesh's UVs did not match what the mask assumed — so the wrist
    /// is found structurally instead. The Meta hand meshes are authored with the wrist/skeleton root at
    /// the mesh ORIGIN, fingers extending away (verified: OpenXR mesh wrist Y≈0 / fingers +0.19; OVRHand
    /// wrist X≈0.02 / fingers −0.19), so the wrist is the principal-axis end nearest origin. (Perpendicular
    /// cross-section spread is only a tiebreaker — it is unreliable as the primary cue because near the
    /// extreme fingertip a single finger reads as compact as the wrist disk.) Opacity ramps from 0 at the
    /// wrist to 1 across a band, as a fraction of hand length so it is scale-independent. Drives
    /// <c>GhostHands_URP</c> via vertex-colour alpha.
    /// </summary>
    public static class WristFadeGradient
    {
        /// <param name="bandStart">Fraction of the hand length over which it stays fully transparent (the very stump).</param>
        /// <param name="bandEnd">Fraction by which it reaches fully opaque.</param>
        public static float[] Compute(Vector3[] vertices, float bandStart = 0.02f, float bandEnd = 0.34f)
        {
            int n = vertices?.Length ?? 0;
            var fade = new float[n];
            if (n == 0)
                return fade;

            Vector3 min = vertices[0], max = vertices[0];
            for (int i = 1; i < n; i++)
            {
                min = Vector3.Min(min, vertices[i]);
                max = Vector3.Max(max, vertices[i]);
            }

            Vector3 size = max - min;
            int axis = (size.x >= size.y && size.x >= size.z) ? 0 : (size.y >= size.z ? 1 : 2);
            float lo = min[axis];
            float len = Mathf.Max(1e-6f, max[axis] - lo);
            int a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;

            // Wrist = the principal-axis end nearest the mesh origin (the skeleton root). Only when both
            // ends are near-equidistant from origin do we fall back to the compact-cross-section cue.
            float distLo = Mathf.Abs(lo), distHi = Mathf.Abs(max[axis]);
            bool wristAtLo;
            if (Mathf.Abs(distLo - distHi) > 0.05f * len)
            {
                wristAtLo = distLo <= distHi;
            }
            else
            {
                const float endFrac = 0.12f;
                wristAtLo = EndSpread(vertices, axis, a1, a2, lo, len, endFrac, true)
                         <= EndSpread(vertices, axis, a1, a2, lo, len, endFrac, false);
            }

            for (int i = 0; i < n; i++)
            {
                float t = (vertices[i][axis] - lo) / len;       // 0 at the lo end .. 1 at the hi end
                float d = wristAtLo ? t : 1f - t;               // distance from the wrist end
                fade[i] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bandStart, bandEnd, d));
            }
            return fade;
        }

        private static float EndSpread(Vector3[] v, int axis, int a1, int a2, float lo, float len, float frac, bool nearLo)
        {
            float m1 = 0f, m2 = 0f;
            int cnt = 0;
            for (int i = 0; i < v.Length; i++)
            {
                float d = DistFromEnd(v[i][axis], lo, len, nearLo);
                if (d <= frac) { m1 += v[i][a1]; m2 += v[i][a2]; cnt++; }
            }
            if (cnt == 0)
                return 0f;
            m1 /= cnt; m2 /= cnt;

            float dev = 0f;
            for (int i = 0; i < v.Length; i++)
            {
                float d = DistFromEnd(v[i][axis], lo, len, nearLo);
                if (d <= frac)
                    dev += Mathf.Abs(v[i][a1] - m1) + Mathf.Abs(v[i][a2] - m2);
            }
            return dev / cnt;
        }

        private static float DistFromEnd(float coord, float lo, float len, bool nearLo)
        {
            float t = (coord - lo) / len;
            return nearLo ? t : 1f - t;
        }
    }
}
