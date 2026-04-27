using System;
using UnityEngine;

namespace GTX.Flow
{
    public sealed class FlowState : MonoBehaviour
    {
        public event Action<FlowSnapshot> FlowChanged;
        public event Action<FlowTier, FlowTier> TierChanged;

        [SerializeField, Range(0f, 100f)] private float startingFlow;
        [SerializeField] private bool decayOverTime = true;
        [SerializeField, Min(0f)] private float decayPerSecond = 6f;
        [SerializeField] private float warmThreshold = 25f;
        [SerializeField] private float hotThreshold = 60f;
        [SerializeField] private float overdriveThreshold = 90f;

        private float value;
        private FlowTier tier;

        public float Value => value;
        public float Normalized => Mathf.InverseLerp(0f, 100f, value);
        public FlowTier Tier => tier;
        public FlowSnapshot Snapshot => new FlowSnapshot(value, tier, Normalized);

        private void Awake()
        {
            value = Mathf.Clamp(startingFlow, 0f, 100f);
            tier = CalculateTier(value);
            FlowChanged?.Invoke(Snapshot);
        }

        private void Update()
        {
            if (!decayOverTime || value <= 0f)
            {
                return;
            }

            AddFlow(-decayPerSecond * Time.deltaTime);
        }

        public void AddFlow(float amount)
        {
            SetFlow(value + amount);
        }

        public bool TrySpend(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (value < amount)
            {
                return false;
            }

            SetFlow(value - amount);
            return true;
        }

        public void SetFlow(float newValue)
        {
            float clamped = Mathf.Clamp(newValue, 0f, 100f);
            if (Mathf.Approximately(clamped, value))
            {
                return;
            }

            FlowTier previousTier = tier;
            value = clamped;
            tier = CalculateTier(value);

            FlowChanged?.Invoke(Snapshot);
            if (previousTier != tier)
            {
                TierChanged?.Invoke(previousTier, tier);
            }
        }

        public FlowTier CalculateTier(float flowValue)
        {
            if (flowValue >= overdriveThreshold)
            {
                return FlowTier.Overdrive;
            }

            if (flowValue >= hotThreshold)
            {
                return FlowTier.Hot;
            }

            if (flowValue >= warmThreshold)
            {
                return FlowTier.Warm;
            }

            return FlowTier.Cold;
        }
    }
}
