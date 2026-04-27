using GTX.Flow;
using UnityEngine;
using UnityEngine.Events;

namespace GTX.Visuals
{
    public sealed class VisualIntensityMapper : MonoBehaviour
    {
        [SerializeField] private FlowState flowState;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float coldIntensity = 0.08f;
        [SerializeField] private float warmIntensity = 0.35f;
        [SerializeField] private float hotIntensity = 0.7f;
        [SerializeField] private float overdriveIntensity = 1f;
        [SerializeField] private UnityEvent<float> intensityChanged;

        public float CurrentIntensity { get; private set; }

        private void Reset()
        {
            flowState = GetComponentInParent<FlowState>();
        }

        private void OnEnable()
        {
            if (flowState == null)
            {
                flowState = GetComponentInParent<FlowState>();
            }

            if (flowState != null)
            {
                flowState.FlowChanged += HandleFlowChanged;
                HandleFlowChanged(flowState.Snapshot);
            }
        }

        private void OnDisable()
        {
            if (flowState != null)
            {
                flowState.FlowChanged -= HandleFlowChanged;
            }
        }

        public float Map(FlowSnapshot snapshot)
        {
            float tierFloor;
            switch (snapshot.Tier)
            {
                case FlowTier.Overdrive:
                    tierFloor = overdriveIntensity;
                    break;
                case FlowTier.Hot:
                    tierFloor = hotIntensity;
                    break;
                case FlowTier.Warm:
                    tierFloor = warmIntensity;
                    break;
                default:
                    tierFloor = coldIntensity;
                    break;
            }

            float curved = intensityCurve.Evaluate(snapshot.Normalized);
            return Mathf.Clamp01(Mathf.Max(tierFloor, curved));
        }

        private void HandleFlowChanged(FlowSnapshot snapshot)
        {
            CurrentIntensity = Map(snapshot);
            intensityChanged?.Invoke(CurrentIntensity);
        }
    }
}
