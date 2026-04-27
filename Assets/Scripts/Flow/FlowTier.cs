using System;

namespace GTX.Flow
{
    public enum FlowTier
    {
        Cold = 0,
        Warm = 1,
        Hot = 2,
        Overdrive = 3
    }

    [Serializable]
    public struct FlowSnapshot
    {
        public FlowSnapshot(float value, FlowTier tier, float normalized)
        {
            Value = value;
            Tier = tier;
            Normalized = normalized;
        }

        public float Value { get; }
        public FlowTier Tier { get; }
        public float Normalized { get; }
    }
}
