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
        [SerializeField] private float launchAssist = 28f;
        [SerializeField] private float minimumLaunchSpeed = 7f;
        [SerializeField] private float stuckVelocityThreshold = 0.65f;
        [SerializeField] private float stuckAssistDelay = 0.18f;
        [SerializeField] private float maxSteerTorque = 16f;
        [SerializeField] private float rotationDegreesPerSecond = 190f;
        [SerializeField] private float turnSlowdown = 0.42f;
        [SerializeField] private float sideSlipDamping = 4.5f;
        [SerializeField] private float downforce = 20f;
        [SerializeField] private float uprightAssist = 10f;

        public float DistanceTravelled { get; private set; }
        public int LapCount { get; private set; }
        public float Progress01 { get; private set; }
        public int CurrentTargetIndex { get; private set; }
        public bool DrivingEnabled
        {
            get { return drivingEnabled; }
            set
            {
                if (drivingEnabled == value)
                {
                    return;
                }

                drivingEnabled = value;
                launchRampTimer = 0f;
                if (drivingEnabled)
                {
                    currentSpeed = 0f;
                }
            }
        }

        private Rigidbody body;
        private Vector3[] samples;
        private float[] cumulativeDistances;
        private float routeLength;
        private float cruiseSpeed;
        private float aggression;
        private float laneOffset;
        private float currentRouteDistance;
        private float currentSpeed;
        private float stuckTimer;
        private float routeDriveAssistTimer;
        private float launchRampTimer;
        private bool drivingEnabled = true;
        private bool configured;
        private bool suppressLapUpdate;

        public void Configure(Vector3[] routeSamples, float newRouteLength, float startDistance, float newCruiseSpeed, float newAggression, float newLaneOffset = 0f)
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
            laneOffset = newLaneOffset;

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
            Vector3 position = pose.position + pose.right * laneOffset + Vector3.up * routeHeightOffset;
            RoutePose aheadPose = PoseAtDistance(currentRouteDistance + Mathf.Max(6f, cornerLookAheadDistance));
            Vector3 forward = aheadPose.position - pose.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = pose.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

            body.position = position;
            body.rotation = rotation;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            body.detectCollisions = true;
            body.WakeUp();
            transform.SetPositionAndRotation(position, rotation);

            currentSpeed = 0f;
            stuckTimer = 0f;
            routeDriveAssistTimer = 0f;
            launchRampTimer = 0f;
            suppressLapUpdate = true;
            DrivingEnabled = false;
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

            if (!DrivingEnabled)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                return;
            }

            ApplyRouteLockedDrive();
        }

        private void ApplyRouteLockedDrive()
        {
            float lookAhead = Mathf.Lerp(cornerLookAheadDistance, lookAheadDistance, Mathf.Clamp01(currentSpeed / Mathf.Max(1f, cruiseSpeed)));
            lookAhead *= Mathf.Lerp(0.85f, 1.25f, aggression);

            RoutePose currentPose = PoseAtDistance(currentRouteDistance);
            RoutePose nearAheadPose = PoseAtDistance(currentRouteDistance + Mathf.Max(4f, cornerLookAheadDistance));
            RoutePose targetPose = PoseAtDistance(currentRouteDistance + lookAhead);
            CurrentTargetIndex = FindTargetSampleIndex(currentRouteDistance + lookAhead);

            float steerAngle = Vector3.SignedAngle(currentPose.forward, targetPose.forward, Vector3.up);
            float corner01 = Mathf.Clamp01(Mathf.Abs(steerAngle) / 78f);
            float targetSpeed = cruiseSpeed * Mathf.Lerp(1f, turnSlowdown, corner01) * Mathf.Lerp(0.92f, 1.18f, aggression);
            launchRampTimer += Time.fixedDeltaTime;
            float launch01 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(launchRampTimer / 2.4f));
            targetSpeed *= Mathf.Lerp(0.28f, 1f, launch01);
            float response = targetSpeed >= currentSpeed ? acceleration : braking;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, response * Time.fixedDeltaTime);
            currentSpeed = Mathf.Max(currentSpeed, Mathf.Min(minimumLaunchSpeed * launch01, targetSpeed));

            float previousDistance = currentRouteDistance;
            currentRouteDistance = Mathf.Repeat(currentRouteDistance + currentSpeed * Time.fixedDeltaTime, routeLength);
            if (!suppressLapUpdate && currentRouteDistance < previousDistance - routeLength * 0.5f)
            {
                LapCount++;
            }

            suppressLapUpdate = false;
            DistanceTravelled = Mathf.Max(0f, LapCount * routeLength + currentRouteDistance);
            Progress01 = currentRouteDistance / routeLength;

            RoutePose nextPose = PoseAtDistance(currentRouteDistance);
            Vector3 nextPosition = nextPose.position + nextPose.right * laneOffset + Vector3.up * routeHeightOffset;
            Vector3 desiredForward = nearAheadPose.position - currentPose.position;
            desiredForward.y = 0f;
            if (desiredForward.sqrMagnitude < 0.001f)
            {
                desiredForward = nextPose.forward;
            }

            Quaternion targetRotation = Quaternion.LookRotation(desiredForward.normalized, Vector3.up);
            float speed01 = Mathf.Clamp01(currentSpeed / Mathf.Max(1f, cruiseSpeed));
            float rotationRate = rotationDegreesPerSecond * Mathf.Lerp(0.8f, 1.35f, aggression) * Mathf.Lerp(1.2f, 0.85f, speed01);
            Quaternion nextRotation = Quaternion.RotateTowards(body.rotation, targetRotation, rotationRate * Time.fixedDeltaTime);

            body.MovePosition(nextPosition);
            body.MoveRotation(nextRotation);
            transform.SetPositionAndRotation(nextPosition, nextRotation);
            body.velocity = desiredForward.normalized * currentSpeed;
            body.angularVelocity = Vector3.zero;
        }

        private void ApplyDriveForces(RoutePose currentPose, Vector3 currentLanePosition, Vector3 desiredForward, float targetSpeed)
        {
            Vector3 flatVelocity = body.velocity;
            flatVelocity.y = 0f;

            float forwardSpeed = Vector3.Dot(flatVelocity, desiredForward);
            float speedError = targetSpeed - forwardSpeed;
            float response = speedError >= 0f ? acceleration : braking;
            if (targetSpeed > 1f && flatVelocity.magnitude < 1.2f)
            {
                body.AddForce(FlatForward(transform.forward) * launchAssist, ForceMode.Acceleration);
            }

            Vector3 lateralOffset = body.position - currentLanePosition;
            lateralOffset.y = 0f;
            float lateralError = Vector3.Dot(lateralOffset, currentPose.right);
            Vector3 correctionVelocity = -currentPose.right * lateralError * lateralCorrection;
            Vector3 targetVelocity = desiredForward * targetSpeed + Vector3.ClampMagnitude(correctionVelocity, 5.5f);
            Vector3 nextFlatVelocity = Vector3.MoveTowards(flatVelocity, targetVelocity, response * Time.fixedDeltaTime);

            Vector3 right = transform.right;
            right.y = 0f;
            if (right.sqrMagnitude > 0.001f)
            {
                right.Normalize();
                nextFlatVelocity -= right * Vector3.Dot(nextFlatVelocity, right) * Mathf.Clamp01(sideSlipDamping * Time.fixedDeltaTime);
            }

            body.velocity = new Vector3(nextFlatVelocity.x, body.velocity.y, nextFlatVelocity.z);
        }

        private void ApplyStuckLaunchAssist(Vector3 desiredForward, float targetSpeed)
        {
            Vector3 flatVelocity = body.velocity;
            flatVelocity.y = 0f;
            if (targetSpeed <= 1f || flatVelocity.magnitude > stuckVelocityThreshold)
            {
                stuckTimer = 0f;
                return;
            }

            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer < stuckAssistDelay)
            {
                return;
            }

            Vector3 launchVelocity = desiredForward * Mathf.Min(minimumLaunchSpeed, targetSpeed);
            launchVelocity.y = body.velocity.y;
            body.velocity = launchVelocity;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        private void ApplyRouteDriveAssist(RoutePose currentPose, RoutePose targetPose, float targetSpeed)
        {
            Vector3 flatVelocity = body.velocity;
            flatVelocity.y = 0f;
            if (targetSpeed <= 1f || flatVelocity.magnitude > 1.5f)
            {
                routeDriveAssistTimer = 0f;
                return;
            }

            routeDriveAssistTimer += Time.fixedDeltaTime;
            if (routeDriveAssistTimer < 0.35f)
            {
                return;
            }

            currentRouteDistance = Mathf.Repeat(currentRouteDistance + Mathf.Max(minimumLaunchSpeed, targetSpeed * 0.45f) * Time.fixedDeltaTime, routeLength);
            RoutePose pose = PoseAtDistance(currentRouteDistance);
            Vector3 nextPosition = pose.position + pose.right * laneOffset + Vector3.up * routeHeightOffset;
            Vector3 nextForward = targetPose.position - currentPose.position;
            nextForward.y = 0f;
            if (nextForward.sqrMagnitude < 0.001f)
            {
                nextForward = pose.forward;
            }

            body.MovePosition(nextPosition);
            body.MoveRotation(Quaternion.LookRotation(nextForward.normalized, Vector3.up));
            body.velocity = nextForward.normalized * Mathf.Min(targetSpeed, minimumLaunchSpeed);
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }

        private void ApplySteering(Vector3 desiredForward, float steer01)
        {
            float speed01 = Mathf.Clamp01(body.velocity.magnitude / Mathf.Max(1f, cruiseSpeed));
            if (desiredForward.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredForward, Vector3.up);
                float rotationRate = rotationDegreesPerSecond * Mathf.Lerp(0.68f, 1.25f, aggression) * Mathf.Lerp(1.15f, 0.72f, speed01);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, rotationRate * Time.fixedDeltaTime));
                transform.rotation = body.rotation;
            }

            float yawDamping = body.angularVelocity.y * Mathf.Lerp(2.5f, 5f, speed01);
            body.AddTorque(Vector3.up * ((steer01 * maxSteerTorque * 0.22f) - yawDamping), ForceMode.Acceleration);
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
