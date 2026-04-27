using UnityEngine;

namespace GTX.Flow
{
    public sealed class FlowAwardTrigger : MonoBehaviour
    {
        [SerializeField] private FlowState flowState;
        [SerializeField] private float sideSlamAward = 12f;
        [SerializeField] private float boostRamAward = 18f;
        [SerializeField] private float nearMissAward = 5f;

        private void Reset()
        {
            flowState = GetComponentInParent<FlowState>();
        }

        public void AwardSideSlam()
        {
            Award(sideSlamAward);
        }

        public void AwardBoostRam()
        {
            Award(boostRamAward);
        }

        public void AwardNearMiss()
        {
            Award(nearMissAward);
        }

        public void Award(float amount)
        {
            if (flowState == null)
            {
                flowState = GetComponentInParent<FlowState>();
            }

            flowState?.AddFlow(amount);
        }
    }
}
