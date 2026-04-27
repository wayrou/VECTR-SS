using GTX.Flow;
using GTX.Visuals;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace GTX.Combat
{
    public sealed class BoostRamDetector : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private bool boostActive;
        [SerializeField] private float minimumForwardSpeed = 10f;
        [SerializeField] private float damage = 28f;
        [SerializeField] private float impulse = 22f;
        [SerializeField] private float flowAward = 18f;
        [SerializeField] private float repeatHitLockout = 0.35f;
        [SerializeField] private UnityEvent boostRamConnected;

        private float nextHitTime;
        private float nextSpeedLineTime;
        private float powerMultiplier = 1f;

        public event Action<string> FeedbackRaised;

        public bool BoostActive
        {
            get => boostActive;
            set
            {
                bool wasActive = boostActive;
                boostActive = value;
                if (boostActive && !wasActive)
                {
                    effects?.PlayBoostFlash(transform, FlowIntensity());
                }
            }
        }

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
            if (boostActive && Time.time >= nextSpeedLineTime)
            {
                nextSpeedLineTime = Time.time + 0.18f;
                effects?.PlaySpeedLines(transform, FlowIntensity());
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default(ContactPoint);
            TryRegisterRam(collision.collider, contact.point, contact.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            Vector3 closest = other.ClosestPoint(transform.position);
            TryRegisterRam(other, closest, -transform.forward);
        }

        public bool TryRegisterRam(Collider other, Vector3 point, Vector3 normal)
        {
            if (other == null || Time.time < nextHitTime || !boostActive || !IsInTargetMask(other.gameObject.layer))
            {
                return false;
            }

            float forwardSpeed = Vector3.Dot(body != null ? body.velocity : transform.forward * minimumForwardSpeed, transform.forward);
            if (forwardSpeed < minimumForwardSpeed)
            {
                return false;
            }

            ICombatTarget target = other.GetComponentInParent<ICombatTarget>();
            if (target == null)
            {
                return false;
            }

            nextHitTime = Time.time + repeatHitLockout;
            CombatHit hit = new CombatHit(gameObject, point, transform.forward, damage * powerMultiplier, impulse * powerMultiplier, CombatHitType.BoostRam);
            target.ReceiveHit(hit);

            Rigidbody targetBody = other.attachedRigidbody;
            if (targetBody != null)
            {
                targetBody.AddForceAtPosition(transform.forward * impulse * powerMultiplier, point, ForceMode.Impulse);
            }

            flowState?.AddFlow(flowAward);
            effects?.PlayImpactBurst(point, normal, 1f);
            effects?.PlayBoostFlash(transform, FlowIntensity());
            boostRamConnected?.Invoke();
            FeedbackRaised?.Invoke("BOOST RAM");
            return true;
        }

        public void SetPowerMultiplier(float multiplier)
        {
            powerMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private bool IsInTargetMask(int layer)
        {
            return (targetMask.value & (1 << layer)) != 0;
        }

        private float FlowIntensity()
        {
            return flowState != null ? Mathf.Clamp01(0.35f + flowState.Normalized * 0.65f) : 0.65f;
        }
    }
}
