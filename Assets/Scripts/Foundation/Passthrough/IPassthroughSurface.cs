namespace Ankhora.Foundation.Passthrough
{
    /// <summary>
    /// The visual surface a passthrough toggle drives. Enabling it composites the real world
    /// behind the virtual content (MR); disabling it restores the opaque VR background.
    /// Behind an interface so the toggle logic is EditMode-testable without the Meta SDK or a
    /// headset — the real implementation (<see cref="OvrPassthroughSurface"/>) binds the
    /// <c>OVRPassthroughLayer</c> and the center-eye camera.
    /// </summary>
    public interface IPassthroughSurface
    {
        /// <summary>Show passthrough (the real-world feed) when <paramref name="enabled"/> is true.</summary>
        void SetEnabled(bool enabled);
    }
}
