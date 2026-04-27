using GTX.Combat;
using GTX.Flow;
using GTX.Vehicle;
using GTX.Visuals;
using UnityEngine;

namespace GTX.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class GTXDrivingFlowBridge : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;
        [SerializeField] private BoostRamDetector boostRam;
        [SerializeField] private Transform resetPoint;
        [SerializeField] private float highSpeedFlowKph = 145f;
        [SerializeField] private float fallResetY = -12f;

        private Rigidbody body;
        private float previousShiftAge = 99f;
        private float nextSkidTime;
        private bool wasGrounded = true;
        private float airborneTime;

        public void Configure(VehicleController newVehicle, FlowState newFlowState, RuntimeImpactEffects newEffects, BoostRamDetector newBoostRam, Transform newResetPoint)
        {
            vehicle = newVehicle;
            flowState = newFlowState;
            effects = newEffects;
            boostRam = newBoostRam;
            resetPoint = newResetPoint;
            body = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (vehicle == null)
            {
                vehicle = GetComponent<VehicleController>();
            }

            if (flowState == null)
            {
                flowState = GetComponent<FlowState>();
            }

            if (effects == null)
            {
                effects = GetComponent<RuntimeImpactEffects>();
            }

            if (boostRam == null)
            {
                boostRam = GetComponent<BoostRamDetector>();
            }
        }

        private void Update()
        {
            if (vehicle == null || flowState == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R) || transform.position.y < fallResetY)
            {
                ResetVehicle();
                return;
            }

            if (boostRam != null)
            {
                boostRam.BoostActive = vehicle.IsBoosting;
            }

            AwardShiftFlow();
            AwardDrivingFlow();
            AwardLandingFlow();
        }

        private void AwardShiftFlow()
        {
            GearboxController gearbox = vehicle.Gearbox;
            if (gearbox.LastShiftAge < previousShiftAge && gearbox.LastShiftAge < 0.08f)
            {
                if (gearbox.LastShiftQuality == ShiftQuality.Perfect)
                {
                    flowState.AddFlow(10f);
                    effects?.PlayBoostFlash(transform, 0.65f + flowState.Normalized * 0.35f);
                }
                else if (gearbox.LastShiftQuality == ShiftQuality.Bad)
                {
                    flowState.AddFlow(-8f);
                    effects?.PlaySkid(transform.position, transform.forward, 0.55f);
                }
            }

            previousShiftAge = gearbox.LastShiftAge;
        }

        private void AwardDrivingFlow()
        {
            if (vehicle.SpeedKph >= highSpeedFlowKph)
            {
                flowState.AddFlow(Time.deltaTime * 1.5f);
            }

            if (vehicle.IsDrifting && vehicle.SpeedKph > 45f)
            {
                flowState.AddFlow(Time.deltaTime * 4.8f);
                if (effects != null && Time.time >= nextSkidTime)
                {
                    nextSkidTime = Time.time + 0.12f;
                    effects.PlaySkid(transform.position - transform.right * 0.9f, transform.forward, 0.75f);
                    effects.PlaySkid(transform.position + transform.right * 0.9f, transform.forward, 0.75f);
                }
            }
        }

        private void AwardLandingFlow()
        {
            bool grounded = IsGrounded();
            if (!grounded)
            {
                airborneTime += Time.deltaTime;
            }

            if (grounded && !wasGrounded && airborneTime > 0.35f && vehicle.SpeedKph > 55f)
            {
                flowState.AddFlow(Mathf.Clamp(airborneTime * 7f, 3f, 12f));
                effects?.PlayImpactBurst(transform.position - Vector3.up * 0.35f, Vector3.up, 0.45f);
            }

            if (grounded)
            {
                airborneTime = 0f;
            }

            wasGrounded = grounded;
        }

        private bool IsGrounded()
        {
            Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1.8f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider != null && hitCollider.attachedRigidbody != body)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (flowState == null || collision.collider.GetComponentInParent<ICombatTarget>() != null)
            {
                return;
            }

            if (collision.contactCount <= 0)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            bool wallLikeImpact = Mathf.Abs(contact.normal.y) < 0.55f;
            if (wallLikeImpact && collision.relativeVelocity.magnitude > 8f)
            {
                flowState.AddFlow(-Mathf.Clamp(collision.relativeVelocity.magnitude, 6f, 22f));
                effects?.PlayImpactBurst(contact.point, contact.normal, 0.65f);
            }
        }

        private void ResetVehicle()
        {
            if (resetPoint != null)
            {
                transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);
            }
            else
            {
                transform.SetPositionAndRotation(new Vector3(0f, 0.05f, 4f), Quaternion.identity);
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            flowState?.AddFlow(-18f);
        }
    }
}
