using UnityEngine;

namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// Single home for the C#↔shader contract that drives the VR↔MR transition. The value is
    /// published as a global by <see cref="OvrPassthroughSurface"/> and read by the environment
    /// shaders (gradient sky reveal, floor grid fade) through <c>AnkhoraReveal.hlsl</c>. Any future
    /// shader that participates in the transition reads the same global from here.
    /// </summary>
    public static class PassthroughShaderProperties
    {
        /// <summary>
        /// Global shader float <c>_AnkhoraMrAmount</c>: 0 = full VR backdrop, 1 = full passthrough (MR).
        /// Cached id — declare the matching <c>float _AnkhoraMrAmount;</c> in shaders via AnkhoraReveal.hlsl.
        /// </summary>
        public static readonly int MrAmount = Shader.PropertyToID("_AnkhoraMrAmount");
    }
}
