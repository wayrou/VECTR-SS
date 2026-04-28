using UnityEngine;

namespace GTX.Visuals
{
    public sealed class VectorSSWheelVisualSync : MonoBehaviour
    {
        [SerializeField] private WheelCollider[] wheelColliders;
        [SerializeField] private Transform[] wheelVisuals;
        [SerializeField] private Vector3 visualRotationOffset = new Vector3(0f, 90f, 0f);

        public void Configure(WheelCollider[] colliders, Transform[] visuals, Quaternion rotationOffset)
        {
            wheelColliders = colliders;
            wheelVisuals = visuals;
            visualRotationOffset = rotationOffset.eulerAngles;
            SyncNow();
        }

        private void LateUpdate()
        {
            SyncNow();
        }

        private void SyncNow()
        {
            if (wheelColliders == null || wheelVisuals == null)
            {
                return;
            }

            int count = Mathf.Min(wheelColliders.Length, wheelVisuals.Length);
            Quaternion offset = Quaternion.Euler(visualRotationOffset);
            for (int i = 0; i < count; i++)
            {
                WheelCollider wheel = wheelColliders[i];
                Transform visual = wheelVisuals[i];
                if (wheel == null || visual == null)
                {
                    continue;
                }

                wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
                visual.SetPositionAndRotation(position, rotation * offset);
            }
        }
    }
}
