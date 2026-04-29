using GTX.Core;
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
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3.8f, -8.4f);
        [SerializeField] private float followSharpness = 10.5f;
        [SerializeField] private float lookAhead = 6.2f;
        [SerializeField] private float baseFov = 67f;
        [SerializeField] private float orbitYawSpeed = 92f;
        [SerializeField] private float orbitPitchSpeed = 54f;
        [SerializeField] private float orbitPitchMin = -18f;
        [SerializeField] private float orbitPitchMax = 24f;
        [SerializeField] private KeyCode cameraDistanceCycleKey = KeyCode.F5;
        [SerializeField] private Vector3[] cameraDistanceOptions =
        {
            new Vector3(0f, 1.55f, -3.25f),
            new Vector3(0f, 2.35f, -5.2f),
            new Vector3(0f, 3.8f, -8.4f),
            new Vector3(0f, 4.7f, -11.2f),
            new Vector3(0f, 6.0f, -15.0f),
            new Vector3(0f, 7.4f, -19.0f)
        };

        private Camera cameraComponent;
        private float manualYaw;
        private float manualPitch;
        private const int DefaultCameraDistanceIndex = 1;
        private int cameraDistanceIndex = DefaultCameraDistanceIndex;

        public void Configure(Transform newTarget, Rigidbody newTargetBody, FlowState newFlowState)
        {
            target = newTarget;
            targetBody = newTargetBody;
            flowState = newFlowState;
            cameraDistanceIndex = DefaultCameraDistanceIndex;
            ApplyCameraDistanceOption();
            if (cameraComponent == null)
            {
                cameraComponent = GetComponent<Camera>();
            }

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView = baseFov;
            }
        }

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            ApplyCameraDistanceOption();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            UpdateManualOrbit();
            UpdateCameraDistanceCycle();

            Vector3 dynamicOffset = followOffset;
            Quaternion orbit = Quaternion.Euler(manualPitch, manualYaw, 0f);
            Vector3 desiredPosition = target.position + target.TransformDirection(orbit * dynamicOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Vector3 lookPoint = target.position + target.forward * lookAhead + Vector3.up;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 1f - Mathf.Exp(-12f * Time.deltaTime));

            if (cameraComponent != null)
            {
                cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, baseFov, 1f - Mathf.Exp(-6f * Time.deltaTime));
            }
        }

        private void UpdateManualOrbit()
        {
            float yawInput = GTXInput.Axis("GTX_RightStickX");
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                yawInput -= 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                yawInput += 1f;
            }

            float pitchInput = -GTXInput.Axis("GTX_RightStickY");
            if (Input.GetKey(KeyCode.UpArrow))
            {
                pitchInput += 1f;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                pitchInput -= 1f;
            }

            manualYaw = Mathf.Repeat(manualYaw + yawInput * orbitYawSpeed * Time.deltaTime + 180f, 360f) - 180f;
            manualPitch = Mathf.Clamp(manualPitch + pitchInput * orbitPitchSpeed * Time.deltaTime, orbitPitchMin, orbitPitchMax);
        }

        private void UpdateCameraDistanceCycle()
        {
            if (!Input.GetKeyDown(cameraDistanceCycleKey) || cameraDistanceOptions == null || cameraDistanceOptions.Length == 0)
            {
                return;
            }

            cameraDistanceIndex = (cameraDistanceIndex + 1) % cameraDistanceOptions.Length;
            ApplyCameraDistanceOption();
        }

        private void ApplyCameraDistanceOption()
        {
            if (cameraDistanceOptions == null || cameraDistanceOptions.Length == 0)
            {
                return;
            }

            cameraDistanceIndex = Mathf.Clamp(cameraDistanceIndex, 0, cameraDistanceOptions.Length - 1);
            followOffset = cameraDistanceOptions[cameraDistanceIndex];
        }
    }
}
