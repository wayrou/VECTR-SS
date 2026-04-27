using UnityEngine;

namespace GTX.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SimpleRouteRivalAI : MonoBehaviour
    {
        [Header("Route Following")]
        [SerializeField] private float lookAheadDistance = 18f;
        [SerializeField] private float cornerLookAheadDistance = 9f;
        [SerializeField] private float lateralCorrection = 0.75f;
        [SerializeField] private float routeHeightOffset = 0.15f;

        [Header("Arcade Physics")]
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float braking = 24f;
        [SerializeField] private float maxSteerTorque = 16f;
        [SerializeField] private float turnSlowdown = 0.42f;
        [SerializeField] private float sideSlipDamping = 4.5f;
        [SerializeField] private float downforce = 20f;
        [SerializeField] private float uprightAssist = 10f;

        public float DistanceTravelled { get; private set; }
        public int LapCount { get; private set; }
        public float Progress01 { get; private set; }
        public int CurrentTargetIndex { get; private set; }

        private Rigidbody body;
        private Vector3[] samples;
        private float[] cumulativeDistances;
        private float routeLength;
        private float cruiseSpeed;
        private float aggression;
        private float currentRouteDistance;
        private bool configured;
        private bool suppressLapUpdate;

        public void Configure(Vector3[] routeSamples, float newRouteLength, float startDistance, float newCruiseSpeed, float newAggression)
        {
            body = GetComponent<Rigidbody>();
            configured = false;

            if (routeSamples == null || routeSamples.Length < 2)
            {
                samples = null;
                cumulativeDistances = null;
                routeLength = 0f;
                DistanceTravelled = 0f;
                LapCount = 0;
                Progress01 = 0f;
                CurrentTargetIndex = 0;
                return;
            }

            samples = new Vector3[routeSamples.Length];
            for (int i = 0; i < routeSamples.Length; i++)
            {
                samples[i] = routeSamples[i];
            }

            cumulativeDistances = BuildCumulativeDistances(samples);
            float measuredLength = cumulativeDistances[cumulativeDistances.Length - 1];
            routeLength = newRouteLength > 0.01f ? newRouteLength : measuredLength;
            routeLength = Mathf.Max(0.01f, routeLength);
            cruiseSpeed = Mathf.Max(0f, newCruiseSpeed);
            aggression = Mathf.Clamp01(newAggression);

            LapCount = Mathf.Max(0, Mathf.FloorToInt(Mathf.Max(0f, startDistance) / routeLength));
            currentRouteDistance = Mathf.Repeat(startDistance, routeLength);
            DistanceTravelled = LapCount * routeLength + currentRouteDistance;
            Progress01 = currentRouteDistance / routeLength;
            CurrentTargetIndex = FindTargetSampleIndex(currentRouteDistance);
            configured = true;

            ResetToRoute();
        }

        public void ResetToRoute()
        {
            if (!configured || body == null)
            {
                return;
            }

            RoutePose pose = PoseAtDistance(currentRouteDistance);
            Vector3 position = pose.position + Vector3.up * routeHeightOffset;
            Quaternion rotation = Quaternion.LookRotation(pose.forward, Vector3.up);

            body.position = position;
            body.rotation = rotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(position, rotation);

            suppressLapUpdate = true;
        }

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!configured || body == null || samples == null || samples.Length < 2)
            {
                return;
            }

            float projectedDistance = DistanceAlongRoute(body.position);
            UpdateProgress(projectedDistance);

            float speed = body.velocity.magnitude;
            float lookAhead = Mathf.Lerp(cornerLookAheadDistance, lookAheadDistance, Mathf.Clamp01(speed / Mathf.Max(1f, cruiseSpeed)));
            lookAhead *= Mathf.Lerp(0.85f, 1.25f, aggression);

            RoutePose currentPose = PoseAtDistance(currentRouteDistance);
            RoutePose targetPose = PoseAtDistance(currentRouteDistance + lookAhead);
            CurrentTargetIndex = FindTargetSampleIndex(currentRouteDistance + lookAhead);
            Vector3 toTarget = targetPose.position - body.position;
            toTarget.y = 0f;

            Vector3 desiredForward = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : targetPose.forward;
            float steerAngle = Vector3.SignedAngle(FlatForward(transform.forward), desiredForward, Vector3.up);
            float steer01 = Mathf.Clamp(steerAngle / 55f, -1f, 1f);

            float corner01 = Mathf.Clamp01(Mathf.Abs(steerAngle) / 70f);
            float targetSpeed = cruiseSpeed * Mathf.Lerp(1f, turnSlowdown, corner01);
            targetSpeed *= Mathf.Lerp(0.92f, 1.18f, aggression);

            ApplyDriveForces(currentPose, desiredForward, targetSpeed);
            ApplySteering(steer01);
            ApplyStability();
        }

        private void ApplyDriveForces(RoutePose currentPose, Vector3 desiredForward, float targetSpeed)
        {
            Vector3 flatVelocity = body.velocity;
            flatVelocity.y = 0f;

            float forwardSpeed = Vector3.Dot(flatVelocity, desiredForward);
            float speedError = targetSpeed - forwardSpeed;
            float force = speedError >= 0f ? acceleration : braking;
            body.AddForce(desiredForward * Mathf.Clamp(speedError, -1f, 1f) * force, ForceMode.Acceleration);

            Vector3 lateralOffset = body.position - currentPose.position;
            lateralOffset.y = 0f;
            float lateralError = Vector3.Dot(lateralOffset, currentPose.right);
            Vector3 correction = -currentPose.right * lateralError * lateralCorrection;
            body.AddForce(correction, ForceMode.Acceleration);

            Vector3 right = transform.right;
            right.y = 0f;
            if (right.sqrMagnitude > 0.001f)
            {
                right.Normalize();
                body.AddForce(-right * Vector3.Dot(flatVelocity, right) * sideSlipDamping, ForceMode.Acceleration);
            }
        }

        private void ApplySteering(float steer01)
        {
            float speed01 = Mathf.Clamp01(body.velocity.magnitude / Mathf.Max(1f, cruiseSpeed));
            float yawDamping = body.angularVelocity.y * Mathf.Lerp(2.5f, 5f, speed01);
            body.AddTorque(Vector3.up * ((steer01 * maxSteerTorque) - yawDamping), ForceMode.Acceleration);
        }

        private void ApplyStability()
        {
            float speed = body.velocity.magnitude;
            body.AddForce(Vector3.down * downforce * speed, ForceMode.Acceleration);

            Vector3 tiltAxis = Vector3.Cross(transform.up, Vector3.up);
            if (tiltAxis.sqrMagnitude > 0.001f)
            {
                body.AddTorque(tiltAxis * uprightAssist, ForceMode.Acceleration);
            }
        }

        private void UpdateProgress(float projectedDistance)
        {
            if (suppressLapUpdate)
            {
                suppressLapUpdate = false;
                currentRouteDistance = projectedDistance;
                DistanceTravelled = LapCount * routeLength + currentRouteDistance;
                Progress01 = currentRouteDistance / routeLength;
                CurrentTargetIndex = FindTargetSampleIndex(currentRouteDistance);
                return;
            }

            float delta = projectedDistance - currentRouteDistance;
            if (delta < -routeLength * 0.5f)
            {
                LapCount++;
            }
            else if (delta > routeLength * 0.5f && LapCount > 0)
            {
                LapCount--;
            }

            currentRouteDistance = projectedDistance;
            DistanceTravelled = Mathf.Max(0f, LapCount * routeLength + currentRouteDistance);
            Progress01 = currentRouteDistance / routeLength;
            CurrentTargetIndex = FindTargetSampleIndex(currentRouteDistance);
        }

        private float DistanceAlongRoute(Vector3 worldPosition)
        {
            float bestSqrDistance = float.MaxValue;
            float bestRouteDistance = currentRouteDistance;
            Vector2 point = new Vector2(worldPosition.x, worldPosition.z);

            for (int i = 0; i < samples.Length - 1; i++)
            {
                Vector2 start = new Vector2(samples[i].x, samples[i].z);
                Vector2 end = new Vector2(samples[i + 1].x, samples[i + 1].z);
                Vector2 segment = end - start;
                float segmentSqrLength = segment.sqrMagnitude;
                if (segmentSqrLength < 0.001f)
                {
                    continue;
                }

                float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentSqrLength);
                Vector2 closest = start + segment * t;
                float sqrDistance = Vector2.SqrMagnitude(point - closest);
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    float segmentLength = cumulativeDistances[i + 1] - cumulativeDistances[i];
                    bestRouteDistance = cumulativeDistances[i] + segmentLength * t;
                }
            }

            return Mathf.Repeat(bestRouteDistance, routeLength);
        }

        private RoutePose PoseAtDistance(float distance)
        {
            float wrappedDistance = Mathf.Repeat(distance, routeLength);
            int segmentIndex = FindSegmentIndex(wrappedDistance);
            int nextIndex = Mathf.Min(samples.Length - 1, segmentIndex + 1);
            float segmentLength = Mathf.Max(0.001f, cumulativeDistances[nextIndex] - cumulativeDistances[segmentIndex]);
            float t = Mathf.Clamp01((wrappedDistance - cumulativeDistances[segmentIndex]) / segmentLength);

            Vector3 position = Vector3.Lerp(samples[segmentIndex], samples[nextIndex], t);
            Vector3 forward = samples[nextIndex] - samples[segmentIndex];
            forward = FlatForward(forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            return new RoutePose(position, forward, right);
        }

        private int FindSegmentIndex(float distance)
        {
            if (cumulativeDistances == null || cumulativeDistances.Length < 2)
            {
                return 0;
            }

            float wrappedDistance = Mathf.Repeat(distance, routeLength);
            for (int i = 0; i < cumulativeDistances.Length - 1; i++)
            {
                if (cumulativeDistances[i + 1] >= wrappedDistance)
                {
                    return i;
                }
            }

            return Mathf.Max(0, cumulativeDistances.Length - 2);
        }

        private int FindTargetSampleIndex(float distance)
        {
            return Mathf.Min(samples.Length - 1, FindSegmentIndex(distance) + 1);
        }

        private static float[] BuildCumulativeDistances(Vector3[] routeSamples)
        {
            float[] distances = new float[routeSamples.Length];
            for (int i = 1; i < routeSamples.Length; i++)
            {
                distances[i] = distances[i - 1] + Vector3.Distance(routeSamples[i - 1], routeSamples[i]);
            }

            return distances;
        }

        private static Vector3 FlatForward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return Vector3.forward;
            }

            return direction.normalized;
        }

        private readonly struct RoutePose
        {
            public readonly Vector3 position;
            public readonly Vector3 forward;
            public readonly Vector3 right;

            public RoutePose(Vector3 position, Vector3 forward, Vector3 right)
            {
                this.position = position;
                this.forward = forward;
                this.right = right;
            }
        }
    }
}
