using UnityEngine;

namespace GTX.Combat
{
    public sealed class DummyRivalTarget : MonoBehaviour, ICombatTarget
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private float health = 100f;
        [SerializeField] private float wobbleSeconds = 0.35f;
        [SerializeField] private Color normalColor = new Color(0.9f, 0.1f, 0.18f, 1f);
        [SerializeField] private Color hitColor = new Color(1f, 0.9f, 0.25f, 1f);

        private Renderer cachedRenderer;
        private Material materialInstance;
        private float wobbleUntil;

        public Transform TargetTransform => transform;
        public float Health => health;

        private void Reset()
        {
            body = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            cachedRenderer = GetComponentInChildren<Renderer>();
            if (cachedRenderer != null)
            {
                materialInstance = cachedRenderer.material;
                materialInstance.color = normalColor;
            }
        }

        private void Update()
        {
            if (Time.time < wobbleUntil)
            {
                float shake = Mathf.Sin(Time.time * 80f) * 2.5f;
                transform.localRotation = Quaternion.Euler(0f, shake, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 12f);
                if (materialInstance != null)
                {
                    materialInstance.color = Color.Lerp(materialInstance.color, normalColor, Time.deltaTime * 10f);
                }
            }
        }

        public void ReceiveHit(CombatHit hit)
        {
            health = Mathf.Max(0f, health - hit.Damage);
            wobbleUntil = Time.time + wobbleSeconds;

            if (body != null)
            {
                body.AddForceAtPosition(hit.Direction.normalized * hit.Impulse, hit.Point, ForceMode.Impulse);
            }

            if (materialInstance != null)
            {
                materialInstance.color = hitColor;
            }
        }
    }
}
