using GTX.Vehicle;
using UnityEngine;

namespace GTX.Visuals
{
    public sealed class VectorSSLaunchSmoke : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private Rigidbody body;
        [SerializeField] private float lowSpeedCutoff = 18f;
        [SerializeField] private float fadeOutSpeed = 26f;
        [SerializeField] private float puffInterval = 0.045f;

        private const int MaxPuffs = 32;
        private SmokePuff[] puffs = new SmokePuff[0];
        private Vector3[] localEmitterPositions = new Vector3[0];
        private Material smokeMaterial;
        private float nextPuffTime;
        private int nextPuffIndex;

        public void Configure(VehicleController newVehicle, Rigidbody newBody, Vector3[] newLocalEmitterPositions, Material newSmokeMaterial)
        {
            vehicle = newVehicle;
            body = newBody;
            smokeMaterial = newSmokeMaterial;
            localEmitterPositions = newLocalEmitterPositions != null && newLocalEmitterPositions.Length > 0
                ? newLocalEmitterPositions
                : new[] { new Vector3(0f, 0.22f, -1.35f) };
            BuildPuffPool();
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

        private void Update()
        {
            UpdatePuffs();
            if (vehicle == null || puffs == null || puffs.Length == 0)
            {
                return;
            }

            float throttle = Mathf.Clamp01(vehicle.CurrentInput.throttle);
            float speed = vehicle.SpeedMetersPerSecond;
            float launch01 = Mathf.InverseLerp(fadeOutSpeed, lowSpeedCutoff, speed);
            float throttle01 = Mathf.InverseLerp(0.25f, 0.9f, throttle);
            float burnout01 = Mathf.Clamp01(launch01 * throttle01);
            if (vehicle.IsBoosting)
            {
                burnout01 = Mathf.Max(burnout01, Mathf.InverseLerp(24f, 6f, speed) * 0.45f);
            }

            if (burnout01 <= 0.02f || Time.time < nextPuffTime)
            {
                return;
            }

            float interval = Mathf.Lerp(puffInterval * 1.8f, puffInterval, burnout01);
            nextPuffTime = Time.time + interval;
            for (int i = 0; i < localEmitterPositions.Length; i++)
            {
                SpawnPuff(localEmitterPositions[i], burnout01);
            }
        }

        private void BuildPuffPool()
        {
            puffs = new SmokePuff[MaxPuffs];
            for (int i = 0; i < puffs.Length; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Quad);
                puff.name = "Launch Smoke Puff " + i;
                puff.transform.SetParent(transform, false);
                puff.SetActive(false);

                Collider collider = puff.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = puff.GetComponent<Renderer>();
                if (renderer != null && smokeMaterial != null)
                {
                    renderer.material = smokeMaterial;
                }

                puffs[i] = new SmokePuff
                {
                    transform = puff.transform,
                    renderer = renderer,
                    startColor = new Color(0.58f, 0.54f, 0.44f, 0.46f)
                };
            }
        }

        private void SpawnPuff(Vector3 localEmitterPosition, float intensity)
        {
            if (puffs.Length == 0)
            {
                return;
            }

            SmokePuff puff = puffs[nextPuffIndex];
            nextPuffIndex = (nextPuffIndex + 1) % puffs.Length;

            Vector3 worldPosition = transform.TransformPoint(localEmitterPosition + new Vector3(0f, -0.28f, -0.22f));
            Vector3 rearward = -transform.forward;
            Vector3 sideways = transform.right * Random.Range(-0.42f, 0.42f);
            Vector3 upward = Vector3.up * Random.Range(0.18f, 0.58f);
            Vector3 bodyCarry = body != null ? body.velocity * 0.08f : Vector3.zero;

            puff.age = 0f;
            puff.lifetime = Random.Range(0.55f, 1.05f);
            puff.startSize = Random.Range(0.34f, 0.58f) * Mathf.Lerp(0.8f, 1.45f, intensity);
            puff.endSize = puff.startSize * Random.Range(2.5f, 4.1f);
            puff.velocity = rearward * Random.Range(1.8f, 3.4f) * Mathf.Lerp(0.65f, 1.25f, intensity) + sideways + upward + bodyCarry;
            puff.spin = Random.Range(-90f, 90f);
            puff.startColor = Color.Lerp(new Color(0.68f, 0.62f, 0.48f, 0.5f), new Color(0.22f, 0.23f, 0.22f, 0.38f), intensity);
            puff.transform.position = worldPosition;
            puff.transform.rotation = Quaternion.LookRotation(Camera.main != null ? Camera.main.transform.forward : transform.forward, Vector3.up);
            puff.transform.localScale = Vector3.one * puff.startSize;
            puff.transform.gameObject.SetActive(true);
            ApplyPuffColor(puff, puff.startColor);

            puffs[(nextPuffIndex + puffs.Length - 1) % puffs.Length] = puff;
        }

        private void UpdatePuffs()
        {
            if (puffs == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < puffs.Length; i++)
            {
                SmokePuff puff = puffs[i];
                if (puff.transform == null || !puff.transform.gameObject.activeSelf)
                {
                    continue;
                }

                puff.age += deltaTime;
                float life01 = puff.lifetime <= 0.001f ? 1f : Mathf.Clamp01(puff.age / puff.lifetime);
                if (life01 >= 1f)
                {
                    puff.transform.gameObject.SetActive(false);
                    puffs[i] = puff;
                    continue;
                }

                puff.velocity += Vector3.up * deltaTime * 0.22f;
                puff.transform.position += puff.velocity * deltaTime;
                puff.transform.Rotate(0f, 0f, puff.spin * deltaTime, Space.Self);
                if (Camera.main != null)
                {
                    puff.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up) * Quaternion.Euler(0f, 0f, puff.spin * puff.age);
                }

                float size = Mathf.Lerp(puff.startSize, puff.endSize, life01);
                puff.transform.localScale = Vector3.one * size;
                Color color = puff.startColor;
                color.a *= Mathf.Sin(life01 * Mathf.PI);
                ApplyPuffColor(puff, color);
                puffs[i] = puff;
            }
        }

        private static void ApplyPuffColor(SmokePuff puff, Color color)
        {
            if (puff.renderer != null && puff.renderer.material != null)
            {
                puff.renderer.material.color = color;
            }
        }

        private struct SmokePuff
        {
            public Transform transform;
            public Renderer renderer;
            public Vector3 velocity;
            public Color startColor;
            public float age;
            public float lifetime;
            public float startSize;
            public float endSize;
            public float spin;
        }
    }
}
