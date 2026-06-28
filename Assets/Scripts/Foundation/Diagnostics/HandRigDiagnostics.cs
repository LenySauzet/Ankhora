using System.Text;
using UnityEngine;

namespace Ankhora.Foundation.Diagnostics
{
    /// <summary>
    /// TEMPORARY device diagnostic for the replay positional offset (fingertips that touched live don't
    /// meet on replay). The replay ghost rebuilds each hand by forward kinematics from the captured
    /// <c>BindPoses</c> bone offsets; if those rest offsets don't match the LIVE hand's actual bone offsets
    /// (e.g. Meta scales each user's hand at runtime), the FK reproduces a differently-sized hand and the
    /// fingertips land off. This logs, once per second per hand, the bind-vs-live local bone offset lengths,
    /// their ratio, and the runtime hand scale — the decisive evidence. Remove once the offset is fixed.
    /// <para>Reads over adb: <c>logcat -s Unity | grep HandRigDiag</c>.</para>
    /// </summary>
    public class HandRigDiagnostics : MonoBehaviour
    {
        [SerializeField] private OVRSkeleton _leftSkeleton;
        [SerializeField] private OVRSkeleton _rightSkeleton;
        [SerializeField] private float _logIntervalSeconds = 1f;

        private float _nextLog;

        private void Update()
        {
            if (Time.unscaledTime < _nextLog)
                return;
            _nextLog = Time.unscaledTime + _logIntervalSeconds;

            Report("L", _leftSkeleton);
            Report("R", _rightSkeleton);
        }

        private void Report(string tag, OVRSkeleton skeleton)
        {
            if (skeleton == null || !skeleton.IsInitialized)
            {
                Debug.Log($"[HandRigDiag] {tag}: skeleton not initialised");
                return;
            }

            var live = skeleton.Bones;          // current runtime bones
            var bind = skeleton.BindPoses;      // rest bones the ghost rebuilds from
            if (live == null || bind == null || live.Count == 0 || bind.Count != live.Count)
            {
                Debug.Log($"[HandRigDiag] {tag}: bones live={live?.Count} bind={bind?.Count} (mismatch/empty)");
                return;
            }

            int n = live.Count;
            float bindSum = 0f, liveSum = 0f;
            int compared = 0;
            var sb = new StringBuilder();
            for (int i = 1; i < n; i++)          // skip wrist (no parent offset)
            {
                Transform lb = live[i].Transform;
                Transform bb = bind[i].Transform;
                if (lb == null || bb == null)
                    continue;
                float liveLen = lb.localPosition.magnitude;
                float bindLen = bb.localPosition.magnitude;
                bindSum += bindLen;
                liveSum += liveLen;
                compared++;
                if (i <= 4)                       // a few sample bones for detail
                    sb.Append($" b{i}:live={liveLen * 1000f:0.0}mm/bind={bindLen * 1000f:0.0}mm");
            }

            float ratio = bindSum > 1e-6f ? liveSum / bindSum : 0f;
            Vector3 wristScale = live[0].Transform != null ? live[0].Transform.lossyScale : Vector3.one;
            Debug.Log($"[HandRigDiag] {tag}: bones={n} sumLive={liveSum * 1000f:0.0}mm sumBind={bindSum * 1000f:0.0}mm " +
                      $"live/bind={ratio:0.000} wristLossyScale={wristScale.x:0.000} valid={skeleton.IsDataValid} conf={skeleton.IsDataHighConfidence} |{sb}");
        }
    }
}
