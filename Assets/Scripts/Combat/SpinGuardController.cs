using GTX.Core;
using GTX.Flow;
using GTX.Visuals;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace GTX.Combat
{
    public sealed class SpinGuardController : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private FlowState flowState;
        [SerializeField] private RuntimeImpactEffects effects;
        [SerializeField] private KeyCode guardKey = KeyCode.N;
        [SerializeField] private float activeSeconds = 0.42f;
        [SerializeField] private float cooldownSeconds = 1.15f;
        [SerializeField] private float minimumSpeed = 3f;
        [SerializeField] private float deflectDamage = 10f;
        [SerializeField] private float deflectImpulse = 18f;
        [SerializeField] private float spinTorque = 6.5f;
        [SerializeField] private float flowAward = 14f;
        [SerializeField] private UnityEvent guardStarted;
        [SerializeField] private UnityEvent guardParried;

        private float activeUntil;
        private float nextAllowedTime;
        private float nextSkidTime;

        public event Action<string> FeedbackRaised;

        public bool IsGuarding => Time.time <= activeUntil;

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
            if (Input.GetKeyDown(guardKey) || GTXInput.AxisNegativePressedDown("GTX_DPadY", 0.5f, 5))
            {
                TrySpinGuard();
            }

            if (IsGuarding && body != null)
            {
                body.AddTorque(Vector3.up * spinTorque, ForceMode.Acceleration);
                if (effects != null && Time.time >= nextSkidTime)
                {
                    nextSkidTime = Time.time + 0.1f;
                    effects.PlaySkid(transform.position - transform.right * 0.8f, transform.forward, 0.48f);
                    effects.PlaySkid(transform.position + transform.right * 0.8f, transform.forward, 0.48f);
                }
            }
        }

        public bool TrySpinGuard()
        {
            if (Time.time < nextAllowedTime || CurrentSpeed() < minimumSpeed)
            {
                return false;
            }

            activeUntil = Time.time + activeSeconds;
            nextAllowedTime = Time.time + cooldownSeconds;
            guardStarted?.Invoke();
            FeedbackRaised?.Invoke("SPIN GUARD");

            if (body != null)
            {
                body.AddTorque(Vector3.up * spinTorque, ForceMode.VelocityChange);
                body.AddForce(transform.forward * 1.8f, ForceMode.Impulse);
            }

            effects?.PlayBoostFlash(transform, FlowIntensity());
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsGuarding || collision.collider == null)
            {
                return;
            }

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default(ContactPoint);
            TryParry(collision.collider, contact.point, contact.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsGuarding || other == null)
            {
                return;
            }

            TryParry(other, other.ClosestPoint(transform.position), -transform.forward);
        }

        private bool TryParry(Collider other, Vector3 point, Vector3 normal)
        {
            if (other.attachedRigidbody == body)
            {
                return false;
            }

            ICombatTarget target = other.GetComponentInParent<ICombatTarget>();
            if (target == null)
            {
                if (body != null)
                {
                    body.AddForce(normal.normalized * 2.5f, ForceMode.Impulse);
                }

                effects?.PlayImpactBurst(point, normal, 0.35f);
                return false;
            }

            Vector3 direction = (target.TargetTransform.position - transform.position).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
            }

            CombatHit hit = new CombatHit(gameObject, point, direction, deflectDamage, deflectImpulse, CombatHitType.SpinGuard);
            target.ReceiveHit(hit);

            Rigidbody targetBody = other.attachedRigidbody;
            if (targetBody != null)
            {
                targetBody.AddForceAtPosition(direction * deflectImpulse, point, ForceMode.Impulse);
            }

            flowState?.AddFlow(flowAward);
            effects?.PlayImpactBurst(point, normal, 0.85f);
            guardParried?.Invoke();
            FeedbackRaised?.Invoke("PARRY");
            activeUntil = 0f;
            return true;
        }

        private float CurrentSpeed()
        {
            return body != null ? body.velocity.magnitude : 0f;
        }

        private float FlowIntensity()
        {
            return flowState != null ? Mathf.Clamp01(0.35f + flowState.Normalized * 0.65f) : 0.65f;
        }
    }
}
