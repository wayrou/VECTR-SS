using GTX.Core;
using GTX.Flow;
using GTX.Visuals;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace GTX.Combat
{
    public sealed class SideSlamController : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private KeyCode slamLeftKey = KeyCode.Z;
        [SerializeField] private KeyCode slamRightKey = KeyCode.C;
        [SerializeField] private float slamRange = 1.7f;
        [SerializeField] private float slamRadius = 0.75f;
        [SerializeField] private float cooldownSeconds = 0.55f;
        [SerializeField] private float minimumSpeed = 3f;
        [SerializeField] private float damage = 18f;
        [SerializeField] private float impulse = 12f;
        [SerializeField] private float lateralSelfImpulse = 5.8f;
        [SerializeField] private float forwardCarryImpulse = 2.2f;
        [SerializeField] private float flowAward = 12f;
        [SerializeField] private UnityEvent sideSlamStarted;
        [SerializeField] private UnityEvent sideSlamConnected;

        private float nextAllowedTime;
        private float powerMultiplier = 1f;

        public event Action<string> FeedbackRaised;

        private void Reset()
        {
            body = GetComponentInParent<Rigidbody>();
            flowState = GetComponentInParent<FlowState>();
            effects = GetComponentInParent<RuntimeImpactEffects>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponentInParent<Rigidbody>();
            }

            if (flowState == null)
            {
                flowState = GetComponentInParent<FlowState>();
            }

            if (effects == null)
            {
                effects = GetComponentInParent<RuntimeImpactEffects>();
            }
        }

        public void Configure(Rigidbody newBody, FlowState newFlowState, RuntimeImpactEffects newEffects)
        {
            body = newBody;
            flowState = newFlowState;
            effects = newEffects;
        }

        private void Update()
        {
            if (Input.GetKeyDown(slamLeftKey) || GTXInput.AxisNegativePressedDown("GTX_RightStickX", 0.72f, 0))
            {
                TrySideSlam(-1f);
            }
            else if (Input.GetKeyDown(slamRightKey) || GTXInput.AxisPressedDown("GTX_RightStickX", 0.72f, 1))
            {
                TrySideSlam(1f);
            }
        }

        public bool TrySideSlam(float side)
        {
            if (Time.time < nextAllowedTime || CurrentSpeed() < minimumSpeed)
            {
                return false;
            }

            nextAllowedTime = Time.time + cooldownSeconds;
            sideSlamStarted?.Invoke();

            Vector3 direction = Mathf.Sign(side) >= 0f ? transform.right : -transform.right;
            Vector3 origin = transform.position + Vector3.up * 0.55f;
            if (body != null)
            {
                body.AddForce((direction * lateralSelfImpulse) + (transform.forward * forwardCarryImpulse), ForceMode.Impulse);
                body.AddTorque(Vector3.up * Mathf.Sign(side) * 1.8f, ForceMode.VelocityChange);
            }

            effects?.PlaySkid(transform.position - transform.forward * 0.45f, direction, 0.55f);

            if (!Physics.SphereCast(origin, slamRadius, direction, out RaycastHit hit, slamRange, targetMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.rigidbody == body)
            {
                return false;
            }

            ICombatTarget target = hit.collider.GetComponentInParent<ICombatTarget>();
            if (target == null)
            {
                return false;
            }

            CombatHit combatHit = new CombatHit(gameObject, hit.point, direction, damage * powerMultiplier, impulse * powerMultiplier, CombatHitType.SideSlam);
            target.ReceiveHit(combatHit);

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(direction * impulse * powerMultiplier, hit.point, ForceMode.Impulse);
            }

            flowState?.AddFlow(flowAward);
            effects?.PlayImpactBurst(hit.point, -direction, 0.75f);
            sideSlamConnected?.Invoke();
            FeedbackRaised?.Invoke("SIDE SLAM");
            return true;
        }

        public void SetPowerMultiplier(float multiplier)
        {
            powerMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private float CurrentSpeed()
        {
            return body != null ? body.velocity.magnitude : 0f;
        }
    }
}
