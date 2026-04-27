using GTX.Flow;
using UnityEngine;

namespace GTX.Visuals
{
    [RequireComponent(typeof(Camera))]
    public sealed class GTXCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Rigidbody targetBody;
        [SerializeField] private FlowState flowState;
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 4.1f, -9.2f);
        [SerializeField] private float followSharpness = 9f;
        [SerializeField] private float lookAhead = 5.4f;
        [SerializeField] private float baseFov = 64f;
        [SerializeField] private float speedFovKick = 10f;
        [SerializeField] private float flowFovKick = 6f;

        private Camera cameraComponent;

        public void Configure(Transform newTarget, Rigidbody newTargetBody, FlowState newFlowState)
        {
            target = newTarget;
            targetBody = newTargetBody;
            flowState = newFlowState;
        }

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float speed01 = targetBody != null ? Mathf.InverseLerp(0f, 72f, targetBody.velocity.magnitude) : 0f;
            float flow01 = flowState != null ? flowState.Normalized : 0f;
            Vector3 desiredPosition = target.TransformPoint(followOffset + Vector3.back * speed01 * 1.8f);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Vector3 lookPoint = target.position + target.forward * (lookAhead + speed01 * 5f) + Vector3.up * 1.1f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 1f - Mathf.Exp(-12f * Time.deltaTime));

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, baseFov + speed01 * speedFovKick + flow01 * flowFovKick, 1f - Mathf.Exp(-6f * Time.deltaTime));
            }
        }
    }
}
