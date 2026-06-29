using Ankhora.Domain.Spatial;
using UnityEngine;

namespace Ankhora.Foundation.Replay
{
    /// <summary>
    /// Bakes the geometry-derived wrist-fade gradient (<see cref="WristFadeGradient"/>) into a hand mesh's
    /// vertex-colour alpha, which <c>GhostHands_URP</c> reads to fade the wrist toward transparent. RGB is
    /// left white; only the alpha channel carries the gradient. Used for both the live hand mesh and the
    /// replay ghost's instantiated copy so the two match.
    /// </summary>
    public static class WristFadeBake
    {
        public static void Apply(UnityEngine.Mesh mesh)
        {
            if (mesh == null)
                return;
            var verts = mesh.vertices;
            if (verts.Length == 0)
                return;

            float[] fade = WristFadeGradient.Compute(verts);
            var colors = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
                colors[i] = new Color(1f, 1f, 1f, fade[i]);
            mesh.colors = colors;
        }
    }
}
