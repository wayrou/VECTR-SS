using System.Collections;
using UnityEngine;

namespace GTX.Visuals
{
    public sealed class RuntimeImpactEffects : MonoBehaviour
    {
        [SerializeField] private Material effectMaterial;
        [SerializeField] private Color impactColor = new Color(0.78f, 0.32f, 0.08f, 1f);
        [SerializeField] private Color boostColor = new Color(0.13f, 0.36f, 0.48f, 1f);
        [SerializeField] private Color skidColor = new Color(0.04f, 0.04f, 0.036f, 0.68f);
        [SerializeField] private Color speedLineColor = new Color(0.76f, 0.78f, 0.70f, 0.48f);

        private static readonly Color InkColor = new Color(0.018f, 0.018f, 0.016f, 0.9f);
        private static readonly Color PaperColor = new Color(0.90f, 0.86f, 0.72f, 0.86f);
        private static readonly Color SparkColor = new Color(0.82f, 0.56f, 0.16f, 0.95f);
        private static readonly Color SmokeColor = new Color(0.18f, 0.19f, 0.22f, 0.34f);

        private Material runtimeMaterial;

        public void PlayImpactBurst(Vector3 position, Vector3 normal, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            normal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;

            GameObject inkCore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inkCore.name = "Runtime Impact Ink Core";
            inkCore.transform.position = position + normal * 0.075f;
            inkCore.transform.localRotation = Random.rotation;
            inkCore.transform.localScale = Vector3.one * Mathf.Lerp(0.32f, 0.84f, intensity);
            DestroyCollider(inkCore);
            ApplyMaterial(inkCore, InkColor);
            StartCoroutine(ScaleAndFade(inkCore.transform, 0.2f, 1.9f));

            GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Cube);
            burst.name = "Runtime Impact Burst";
            burst.transform.position = position + normal * 0.11f;
            burst.transform.localRotation = Random.rotation;
            burst.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.58f, intensity);
            DestroyCollider(burst);
            ApplyMaterial(burst, impactColor);
            SpawnImpactPanels(position, normal, intensity);
            SpawnImpactSparks(position, normal, intensity);
            SpawnSmokePuffs(position, normal, Mathf.Lerp(2f, 5f, intensity), 0.32f);
            StartCoroutine(ScaleAndFade(burst.transform, 0.16f, 1.72f));
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
            SpawnBoostStrokes(target, intensity);
            SpawnSmokePuffs(target.position - target.forward * 1.35f, -target.forward, Mathf.Lerp(2f, 4f, intensity), 0.22f);
            StartCoroutine(ScaleAndFade(flash.transform, 0.25f, 2.25f));
        }

        public void PlaySkid(Vector3 position, Vector3 direction, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            Vector3 travel = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, travel);
            if (side.sqrMagnitude < 0.001f)
            {
                side = transform.right;
            }

            side.Normalize();
            float length = Mathf.Lerp(1.55f, 3.9f, intensity);
            float width = Mathf.Lerp(0.065f, 0.17f, intensity);
            Vector3 lifted = position + Vector3.up * 0.018f;
            SpawnLine("Runtime Ink Skid Core", lifted, lifted - travel * length, InkColor, width * 1.35f, width * 0.34f, 0.64f);

            int streakCount = Mathf.RoundToInt(Mathf.Lerp(2f, 5f, intensity));
            for (int i = 0; i < streakCount; i++)
            {
                float offset = Random.Range(-0.19f, 0.19f) * Mathf.Lerp(0.7f, 1.45f, intensity);
                float startPush = Random.Range(0.02f, 0.38f);
                float streakLength = length * Random.Range(0.58f, 1.08f);
                Vector3 start = lifted + side * offset - travel * startPush;
                Color stroke = i % 2 == 0 ? skidColor : MakeColorAlpha(InkColor, 0.58f);
                SpawnLine("Runtime Ink Skid Streak", start, start - travel * streakLength + side * Random.Range(-0.05f, 0.05f), stroke, width * Random.Range(0.42f, 0.9f), 0f, 0.5f);
            }

            int blotCount = Mathf.RoundToInt(Mathf.Lerp(2f, 6f, intensity));
            for (int i = 0; i < blotCount; i++)
            {
                Vector3 blotPosition = lifted - travel * Random.Range(0.2f, length) + side * Random.Range(-0.2f, 0.2f);
                SpawnInkBlot(blotPosition, travel, Mathf.Lerp(0.12f, 0.28f, intensity) * Random.Range(0.65f, 1.25f));
            }
        }

        public void PlaySpeedLines(Transform target, float intensity = 1f)
        {
            if (target == null)
            {
                return;
            }

            intensity = Mathf.Clamp01(intensity);
            int lineCount = Mathf.RoundToInt(Mathf.Lerp(3f, 7f, intensity));
            for (int i = 0; i < lineCount; i++)
            {
                float lane = (i - (lineCount - 1f) * 0.5f) / Mathf.Max(1f, lineCount - 1f);
                Vector3 offset = target.right * (lane * Random.Range(1.15f, 2.05f)) + target.up * Random.Range(0.18f, 1.2f);
                Vector3 start = target.position + offset - target.forward * Random.Range(1.15f, 2.25f);
                Vector3 end = start - target.forward * Random.Range(1.75f, Mathf.Lerp(3f, 5.1f, intensity));
                Color color = i % 4 == 0 ? MakeColorAlpha(boostColor, 0.58f) : speedLineColor;
                SpawnLine("Runtime Speed Line Ink", start + target.right * 0.025f, end + target.right * 0.025f, MakeColorAlpha(InkColor, 0.42f), 0.055f, 0f, 0.16f);
                SpawnLine("Runtime Speed Line Neon", start, end, color, Mathf.Lerp(0.026f, 0.052f, intensity), 0f, Mathf.Lerp(0.15f, 0.24f, intensity));
            }
        }

        public void PlayJumpBurst(Vector3 position, Vector3 forward, float intensity = 1f)
        {
            PlayComicPopBurst(position, forward, "POP!", new Color(0.82f, 0.56f, 0.16f, 0.95f), MakeColorAlpha(boostColor, 0.86f), intensity);
        }

        public void PlayNicePanel(Transform target)
        {
            if (target == null)
            {
                return;
            }

            PlayComicPopBurst(target.position - Vector3.up * 0.28f, target.forward, "NICE", MakeColorAlpha(boostColor, 0.96f), new Color(0.50f, 0.62f, 0.64f, 0.95f), 1f);
        }

        private void PlayComicPopBurst(Vector3 position, Vector3 forward, string text, Color primaryColor, Color accentColor, float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            Vector3 travel = forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
            travel.y = 0f;
            if (travel.sqrMagnitude < 0.001f)
            {
                travel = Vector3.forward;
            }

            travel.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, travel).normalized;
            Vector3 center = position + Vector3.up * 0.08f;

            SpawnJumpRing(center, travel, right, intensity, accentColor);

            int burstCount = Mathf.RoundToInt(Mathf.Lerp(8f, 14f, intensity));
            for (int i = 0; i < burstCount; i++)
            {
                float angle = i * Mathf.PI * 2f / burstCount + Random.Range(-0.16f, 0.16f);
                Vector3 radial = (right * Mathf.Cos(angle) + travel * Mathf.Sin(angle)).normalized;
                Vector3 start = center + radial * Random.Range(0.36f, 0.72f) + Vector3.up * Random.Range(0.02f, 0.22f);
                Vector3 end = start + radial * Random.Range(0.65f, Mathf.Lerp(1.15f, 1.95f, intensity)) + Vector3.up * Random.Range(0.18f, 0.64f);
                SpawnLine("Runtime Comic Pop Burst Ink", start + Vector3.up * 0.015f, end + Vector3.up * 0.015f, MakeColorAlpha(InkColor, 0.62f), 0.074f, 0f, 0.28f);
                SpawnLine("Runtime Comic Pop Burst", start, end, i % 3 == 0 ? accentColor : primaryColor, Mathf.Lerp(0.028f, 0.062f, intensity), 0f, 0.32f);
            }

            int panelCount = Mathf.RoundToInt(Mathf.Lerp(4f, 7f, intensity));
            for (int i = 0; i < panelCount; i++)
            {
                float sideSign = i % 2 == 0 ? -1f : 1f;
                Vector3 lateral = right * sideSign;
                Vector3 panelPosition = center + lateral * Random.Range(0.35f, 1.05f) - travel * Random.Range(0.2f, 0.7f) + Vector3.up * Random.Range(0.18f, 0.62f);
                Quaternion panelRotation = Quaternion.LookRotation((lateral + Vector3.up * 0.25f).normalized, Vector3.up) * Quaternion.Euler(0f, 0f, Random.Range(-18f, 18f));
                Vector3 panelScale = new Vector3(Random.Range(0.05f, 0.1f), Random.Range(0.03f, 0.06f), Random.Range(0.55f, 1.05f));
                SpawnPanelSlab("Runtime Comic Pop Panel Ink", panelPosition, panelRotation, panelScale * 1.28f, InkColor, 0.24f, 1.45f);
                SpawnPanelSlab("Runtime Comic Pop Panel", panelPosition + lateral * 0.018f, panelRotation, panelScale, i % 2 == 0 ? primaryColor : accentColor, 0.22f, 1.55f);
            }

            SpawnSmokePuffs(center - travel * 0.24f, -travel + Vector3.up * 0.55f, Mathf.Lerp(5f, 9f, intensity), 0.42f);
            SpawnJumpText(position + Vector3.up * 1.25f + right * 0.55f - travel * 0.35f, travel, text, primaryColor);
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
                Vector3 panelPosition = position + direction * Mathf.Lerp(0.3f, 0.96f, intensity);
                Quaternion rotation = Quaternion.LookRotation(direction, faceNormal);
                Vector3 panelScale = new Vector3(Mathf.Lerp(0.055f, 0.12f, intensity), 0.032f, Mathf.Lerp(0.7f, 1.86f, intensity));
                SpawnPanelSlab("Runtime Comic Impact Blackline", panelPosition, rotation, panelScale * 1.24f, InkColor, 0.2f, 1.26f);
                SpawnPanelSlab("Runtime Comic Impact Panel", panelPosition + direction * 0.014f, rotation, panelScale, i % 2 == 0 ? impactColor : PaperColor, 0.16f, 1.38f);
                SpawnLine("Runtime Impact Slash", position + direction * 0.08f, position + direction * Mathf.Lerp(0.76f, 1.52f, intensity), i % 2 == 0 ? SparkColor : PaperColor, Mathf.Lerp(0.025f, 0.055f, intensity), 0f, 0.18f);
            }
        }

        private void SpawnImpactSparks(Vector3 position, Vector3 normal, float intensity)
        {
            Vector3 faceNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : Vector3.up;
            Vector3 tangent = Vector3.Cross(faceNormal, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
            {
                tangent = Vector3.Cross(faceNormal, Vector3.right);
            }

            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(faceNormal, tangent).normalized;
            int sparkCount = Mathf.RoundToInt(Mathf.Lerp(4f, 10f, intensity));
            for (int i = 0; i < sparkCount; i++)
            {
                Vector3 spray = (faceNormal * Random.Range(0.18f, 0.45f) + tangent * Random.Range(-1f, 1f) + bitangent * Random.Range(-1f, 1f)).normalized;
                Vector3 start = position + faceNormal * 0.08f + spray * 0.08f;
                Vector3 end = start + spray * Random.Range(0.42f, Mathf.Lerp(0.78f, 1.45f, intensity));
                SpawnLine("Runtime Impact Spark", start, end, SparkColor, Random.Range(0.016f, 0.04f), 0f, 0.2f);
            }
        }

        private void SpawnBoostStrokes(Transform target, float intensity)
        {
            int strokeCount = Mathf.RoundToInt(Mathf.Lerp(4f, 8f, intensity));
            for (int i = 0; i < strokeCount; i++)
            {
                float sideSign = i % 2 == 0 ? -1f : 1f;
                float sideOffset = sideSign * Random.Range(0.28f, 0.9f);
                Vector3 start = target.position - target.forward * Random.Range(0.65f, 1.65f) + target.right * sideOffset + target.up * Random.Range(0.12f, 0.62f);
                Vector3 end = start - target.forward * Random.Range(1.1f, Mathf.Lerp(2f, 3.4f, intensity)) + target.right * sideSign * Random.Range(0.08f, 0.28f);
                SpawnLine("Runtime Boost Blackline", start + target.up * 0.015f, end + target.up * 0.015f, MakeColorAlpha(InkColor, 0.5f), 0.072f, 0f, 0.2f);
                SpawnLine("Runtime Boost Neon Stroke", start, end, MakeColorAlpha(boostColor, 0.82f), Mathf.Lerp(0.035f, 0.075f, intensity), 0f, 0.22f);
            }
        }

        private void SpawnJumpRing(Vector3 center, Vector3 travel, Vector3 right, float intensity, Color accentColor)
        {
            int segments = 10;
            float radius = Mathf.Lerp(0.68f, 1.12f, intensity);
            for (int i = 0; i < segments; i++)
            {
                float startAngle = i * Mathf.PI * 2f / segments;
                float endAngle = (i + 0.54f) * Mathf.PI * 2f / segments;
                Vector3 start = center + (right * Mathf.Cos(startAngle) + travel * Mathf.Sin(startAngle)) * radius;
                Vector3 end = center + (right * Mathf.Cos(endAngle) + travel * Mathf.Sin(endAngle)) * radius;
                Color color = i % 2 == 0 ? MakeColorAlpha(InkColor, 0.62f) : MakeColorAlpha(accentColor, 0.9f);
                SpawnLine("Runtime Jump Ground Ring", start, end, color, Mathf.Lerp(0.04f, 0.075f, intensity), 0f, 0.34f);
            }
        }

        private void SpawnJumpText(Vector3 position, Vector3 forward, string textValue, Color color)
        {
            TextMesh text = new GameObject("Runtime Comic Pop Text").AddComponent<TextMesh>();
            text.transform.position = position;
            text.transform.rotation = Quaternion.LookRotation(Camera.main != null ? Camera.main.transform.forward : forward, Vector3.up) * Quaternion.Euler(0f, 0f, -8f);
            text.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            text.text = textValue;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.color = color;
            StartCoroutine(ScaleAndFadeText(text, 0.42f, 1.34f));
        }

        private void SpawnSmokePuffs(Vector3 position, Vector3 direction, float count, float lifetime)
        {
            Vector3 drift = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.up;
            int puffCount = Mathf.RoundToInt(count);
            for (int i = 0; i < puffCount; i++)
            {
                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Runtime Smoke Puff";
                puff.transform.position = position + drift * Random.Range(0.03f, 0.42f) + Random.insideUnitSphere * 0.18f;
                puff.transform.localScale = Vector3.one * Random.Range(0.12f, 0.32f);
                DestroyCollider(puff);
                ApplyMaterial(puff, SmokeColor);
                StartCoroutine(DriftScaleAndFade(puff.transform, drift + Random.insideUnitSphere * 0.35f, lifetime * Random.Range(0.85f, 1.35f), Random.Range(1.75f, 2.8f)));
            }
        }

        private void SpawnNiceLetter(Transform parent, string letter, Vector3 localPosition, float zRotation, float scale = 0.18f)
        {
            TextMesh text = new GameObject("Runtime NICE Letter " + letter).AddComponent<TextMesh>();
            text.transform.SetParent(parent, false);
            text.transform.localPosition = localPosition;
            text.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
            text.transform.localScale = Vector3.one * scale;
            text.text = letter;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 86;
            text.color = boostColor;
        }

        private void SpawnComicBurstLines(Vector3 position, Quaternion rotation)
        {
            Vector3 right = rotation * Vector3.right;
            Vector3 up = rotation * Vector3.up;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                Vector3 start = position + direction * 1.65f;
                Vector3 end = position + direction * 2.35f;
                SpawnLine("Runtime NICE Burst Line", start, end, i % 2 == 0 ? boostColor : SparkColor, 0.045f, 0f, 0.32f);
            }
        }

        private void SpawnComicBurstLines(Transform parent, float startRadius, float endRadius)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 start = direction * startRadius;
                Vector3 end = direction * endRadius;
                SpawnLocalLine(parent, "Runtime NICE Burst Line", start, end, i % 2 == 0 ? boostColor : SparkColor, 0.045f, 0f, 0.32f);
            }
        }

        private void SpawnInkBlot(Vector3 position, Vector3 direction, float size)
        {
            GameObject blot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blot.name = "Runtime Ink Skid Blot";
            blot.transform.position = position;
            blot.transform.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward, Vector3.up);
            blot.transform.localScale = new Vector3(size * Random.Range(0.7f, 1.45f), 0.018f, size * Random.Range(0.35f, 0.8f));
            DestroyCollider(blot);
            ApplyMaterial(blot, MakeColorAlpha(InkColor, Random.Range(0.42f, 0.74f)));
            StartCoroutine(ScaleAndFade(blot.transform, Random.Range(0.46f, 0.78f), 1.04f));
        }

        private void SpawnPanelSlab(string name, Vector3 position, Quaternion rotation, Vector3 scale, Color color, float lifetime, float scaleMultiplier)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = name;
            panel.transform.position = position;
            panel.transform.rotation = rotation;
            panel.transform.localScale = scale;
            DestroyCollider(panel);
            ApplyMaterial(panel, color);
            StartCoroutine(ScaleAndFade(panel.transform, lifetime, scaleMultiplier));
        }

        private void SpawnLine(string lineName, Vector3 start, Vector3 end, Color color, float width, float lifetime)
        {
            SpawnLine(lineName, start, end, color, width, 0f, lifetime);
        }

        private void SpawnLine(string lineName, Vector3 start, Vector3 end, Color color, float startWidth, float endWidth, float lifetime)
        {
            GameObject lineObject = new GameObject(lineName);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.numCapVertices = 2;
            line.material = GetMaterial(color);
            line.startColor = color;
            line.endColor = MakeColorAlpha(color, 0f);
            StartCoroutine(FadeLineAndDestroy(line, color, lifetime));
        }

        private void SpawnLocalLine(Transform parent, string lineName, Vector3 start, Vector3 end, Color color, float startWidth, float endWidth, float lifetime)
        {
            GameObject lineObject = new GameObject(lineName);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            line.numCapVertices = 2;
            line.material = GetMaterial(color);
            line.startColor = color;
            line.endColor = MakeColorAlpha(color, 0f);
            StartCoroutine(FadeLineAndDestroy(line, color, lifetime));
        }

        private IEnumerator ScaleAndFade(Transform effect, float lifetime, float scaleMultiplier)
        {
            Vector3 startScale = effect.localScale;
            Renderer renderer = effect.GetComponent<Renderer>();
            Color startColor = renderer != null ? renderer.material.color : Color.white;
            float elapsed = 0f;
            while (effect != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                effect.localScale = Vector3.Lerp(startScale, startScale * scaleMultiplier, t);
                if (renderer != null)
                {
                    renderer.material.color = MakeColorAlpha(startColor, startColor.a * (1f - t));
                }

                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        private IEnumerator ScaleAndFadeText(TextMesh text, float lifetime, float scaleMultiplier)
        {
            if (text == null)
            {
                yield break;
            }

            Transform effect = text.transform;
            Vector3 startScale = effect.localScale;
            Color startColor = text.color;
            float elapsed = 0f;
            while (text != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                effect.localScale = Vector3.Lerp(startScale, startScale * scaleMultiplier, t);
                text.color = MakeColorAlpha(startColor, startColor.a * (1f - t));
                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        private IEnumerator ScaleAndFadeCameraAnchored(Transform anchor, float lifetime, float scaleMultiplier)
        {
            if (anchor == null)
            {
                yield break;
            }

            Vector3 startScale = anchor.localScale;
            Renderer[] renderers = anchor.GetComponentsInChildren<Renderer>(true);
            Color[] colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                colors[i] = renderers[i] != null ? renderers[i].material.color : Color.white;
            }

            float elapsed = 0f;
            while (anchor != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                anchor.localScale = Vector3.Lerp(startScale, startScale * scaleMultiplier, t);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].material.color = MakeColorAlpha(colors[i], colors[i].a * (1f - t));
                    }
                }

                yield return null;
            }

            if (anchor != null)
            {
                Destroy(anchor.gameObject);
            }
        }

        private IEnumerator DriftScaleAndFade(Transform effect, Vector3 drift, float lifetime, float scaleMultiplier)
        {
            Vector3 startPosition = effect.position;
            Vector3 targetPosition = startPosition + drift * 0.32f;
            Vector3 startScale = effect.localScale;
            Renderer renderer = effect.GetComponent<Renderer>();
            Color startColor = renderer != null ? renderer.material.color : SmokeColor;
            float elapsed = 0f;
            while (effect != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                effect.position = Vector3.Lerp(startPosition, targetPosition, t);
                effect.localScale = Vector3.Lerp(startScale, startScale * scaleMultiplier, t);
                if (renderer != null)
                {
                    renderer.material.color = MakeColorAlpha(startColor, startColor.a * (1f - t));
                }

                yield return null;
            }

            if (effect != null)
            {
                Destroy(effect.gameObject);
            }
        }

        private IEnumerator FadeLineAndDestroy(LineRenderer line, Color startColor, float lifetime)
        {
            float elapsed = 0f;
            float startWidth = line.startWidth;
            float endWidth = line.endWidth;
            while (line != null && elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                Color fadeStart = MakeColorAlpha(startColor, startColor.a * (1f - t));
                line.startColor = fadeStart;
                line.endColor = MakeColorAlpha(startColor, 0f);
                line.startWidth = Mathf.Lerp(startWidth, startWidth * 0.24f, t);
                line.endWidth = Mathf.Lerp(endWidth, 0f, t);
                yield return null;
            }

            if (line != null)
            {
                Destroy(line.gameObject);
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

        private static Color MakeColorAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
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
