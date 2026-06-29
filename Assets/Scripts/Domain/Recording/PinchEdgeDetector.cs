using UnityEngine;

namespace Ankhora.Domain.Recording
{
    /// <summary>
    /// Pure debounced rising-edge detector over a boolean pinch signal. Fires once per deliberate
    /// pinch — only after the signal has been held continuously for the debounce window (rejecting
    /// hand-tracking jitter), and only once per hold. Releasing re-arms it for the next pinch. Kept
    /// pure (no OVR types, no Unity time) so the pinch toggle is fully EditMode-testable.
    /// </summary>
    public class PinchEdgeDetector
    {
        private readonly float _debounce;
        private float _heldFor;
        private bool _wasPinching;
        private bool _firedThisHold;

        public PinchEdgeDetector(float debounceSeconds = 0.05f)
        {
            _debounce = Mathf.Max(0f, debounceSeconds);
        }

        public bool Tick(bool isPinching, float deltaSeconds)
        {
            if (!isPinching)
            {
                _heldFor = 0f;
                _wasPinching = false;
                _firedThisHold = false;
                return false;
            }

            _heldFor = _wasPinching ? _heldFor + deltaSeconds : deltaSeconds;
            _wasPinching = true;

            if (!_firedThisHold && _heldFor >= _debounce)
            {
                _firedThisHold = true;
                return true;
            }
            return false;
        }
    }
}
