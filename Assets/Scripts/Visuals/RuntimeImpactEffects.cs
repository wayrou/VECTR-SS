using System.Collections;
using UnityEngine;

namespace GTX.Visuals
{
    public sealed class RuntimeImpactEffects : MonoBehaviour
    {
        [SerializeField] private Material effectMaterial;
        [SerializeField] private Color impactColor = new Color(1f, 0.48f, 0.08f, 1f);
        [SerializeField] private Color boostColor = new Color(0.08f, 0.78f, 1f, 1f);
        [SerializeField] private Color skidColor = new Color(0.03f, 0.035f, 0.05f, 0.68f);
        [SerializeField] private Color speedLineColor = new Color(0.95f, 0.98f, 1f, 0.58f);

        private Material runtimeMaterial;

        public void PlayImpactBurst(Vector3 position, Vector3 normal, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Cube);
            burst.name = "Runtime Impact Burst";
            burst.transform.position = position + normal.normalized * 0.08f;
            burst.transform.localRotation = Random.rotation;
            burst.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.62f, intensity);
            DestroyCollider(burst);
            ApplyMaterial(burst, impactColor);
            SpawnImpactPanels(position, normal, intensity);
            StartCoroutine(ScaleAndFade(burst.transform, 0.18f, 1.8f));
        }

        public void PlayBoostFlash(Transform target, float intensity = 1f)
        {
            if (target == null)
            {
                return;
            }

            intensity = Mathf.Clamp01(intensity);
            GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flash.name = "Runtime Boost Flash";
            flash.transform.SetParent(target, false);
            flash.transform.localPosition = Vector3.back * 1.3f;
            flash.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            flash.transform.localScale = new Vector3(0.45f, Mathf.Lerp(0.7f, 1.75f, intensity), 0.45f);
            DestroyCollider(flash);
            ApplyMaterial(flash, boostColor);
            StartCoroutine(ScaleAndFade(flash.transform, 0.25f, 2.25f));
        }

        public void PlaySkid(Vector3 position, Vector3 direction, float intensity = 1f)
        {
            SpawnLine("Runtime Skid", position, position - direction.normalized * Mathf.Lerp(1.3f, 3.2f, Mathf.Clamp01(intensity)), skidColor, Mathf.Lerp(0.05f, 0.14f, intensity), 0.45f);
        }

        public void PlaySpeedLines(Transform target, float intensity = 1f)
        {
            if (target == null)
            {
                return;
            }

            intensity = Mathf.Clamp01(intensity);
            int lineCount = Mathf.RoundToInt(Mathf.Lerp(3f, 9f, intensity));
            for (int i = 0; i < lineCount; i++)
            {
                Vector3 offset = target.right * Random.Range(-1.4f, 1.4f) + target.up * Random.Range(0.1f, 1.2f);
                Vector3 start = target.position + offset - target.forward * Random.Range(1.3f, 2.2f);
                Color color = i % 3 == 0 ? new Color(1f, 0.48f, 0.08f, 0.5f) : speedLineColor;
                SpawnLine("Runtime Speed Line", start, start - target.forward * Random.Range(1.5f, 3.8f), color, 0.035f, 0.2f);
            }
        }

        private void SpawnImpactPanels(Vector3 position, Vector3 normal, float intensity)
        {
            Vector3 faceNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            Vector3 tangent = Vector3.Cross(faceNormal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = Vector3.Cross(faceNormal, Vector3.right);
            }

            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(faceNormal, tangent).normalized;
            int count = Mathf.RoundToInt(Mathf.Lerp(5f, 9f, intensity));
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count + Random.Range(-0.12f, 0.12f);
                Vector3 direction = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle) + faceNormal * 0.18f).normalized;
                GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = "Runtime Comic Impact Panel";
                panel.transform.position = position + direction * Mathf.Lerp(0.28f, 0.9f, intensity);
                panel.transform.rotation = Quaternion.LookRotation(direction, faceNormal);
                panel.transform.localScale = new Vector3(Mathf.Lerp(0.045f, 0.095f, intensity), 0.035f, Mathf.Lerp(0.62f, 1.7f, intensity));
                DestroyCollider(panel);
                ApplyMaterial(panel, i % 2 == 0 ? impactColor : Color.white);
                StartCoroutine(ScaleAndFade(panel.transform, 0.16f, 1.4f));
            }
        }

        private void SpawnLine(string lineName, Vector3 start, Vector3 end, Color color, float width, float lifetime)
        {
            GameObject lineObject = new GameObject(lineName);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = width;
            line.endWidth = 0f;
            line.material = GetMaterial(color);
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            StartCoroutine(DestroyAfter(lineObject, lifetime));
        }

        private IEnumerator ScaleAndFade(Transform effect, float lifetime, float scaleMultiplier)
        {
            Vector3 startScale = effect.localScale;
            float elapsed = 0f;
            while (effect != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                effect.localScale = Vector3.Lerp(startScale, startScale * scaleMultiplier, t);
                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        private IEnumerator DestroyAfter(GameObject target, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (target != null)
            {
                Destroy(target);
            }
        }

        private void ApplyMaterial(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = GetMaterial(color);
            }
        }

        private Material GetMaterial(Color color)
        {
            if (effectMaterial != null)
            {
                Material material = new Material(effectMaterial);
                material.color = color;
                return material;
            }

            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                runtimeMaterial = new Material(shader != null ? shader : Shader.Find("Standard"));
            }

            Material instance = new Material(runtimeMaterial);
            instance.color = color;
            return instance;
        }

        private static void DestroyCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
