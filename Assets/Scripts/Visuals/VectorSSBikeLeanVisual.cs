using GTX.Vehicle;
using UnityEngine;

namespace GTX.Visuals
{
    public sealed class VectorSSBikeLeanVisual : MonoBehaviour
    {
        [SerializeField] private Transform leanRoot;
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private Rigidbody body;
        [SerializeField] private float maxLeanDegrees = 28f;
        [SerializeField] private float response = 8f;

        private Quaternion currentLean = Quaternion.identity;

        public void Configure(Transform newLeanRoot, VehicleController newVehicle, Rigidbody newBody, float newResponse)
        {
            leanRoot = newLeanRoot;
            vehicle = newVehicle;
            body = newBody;
            response = Mathf.Max(1f, newResponse);
        }

        private void Awake()
        {
            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
        }

        private void LateUpdate()
        {
            if (leanRoot == null || vehicle == null)
            {
                return;
            }

            float speed01 = body != null ? Mathf.InverseLerp(3f, 32f, body.velocity.magnitude) : 0.5f;
            float steer = vehicle.CurrentInput.steer;
            float targetLean = -steer * Mathf.Lerp(maxLeanDegrees * 0.35f, maxLeanDegrees, speed01);
            Quaternion target = Quaternion.Euler(0f, 0f, targetLean);
            currentLean = Quaternion.Slerp(currentLean, target, Time.deltaTime * response);
            leanRoot.localRotation = currentLean;
        }
    }
}
