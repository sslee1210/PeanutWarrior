using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Mirrors BossPatternPrototype warnings into world-space renderers so boss
    /// attacks remain readable after legacy IMGUI panels are suppressed.
    /// </summary>
    [DefaultExecutionOrder(18000)]
    public sealed class BossPatternWorldViewPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float SourceLeft = 55f;
        private const float SourceRight = 705f;
        private const float SourceTop = 155f;
        private const float SourceBottom = 425f;
        private const float WorldHalfWidth = 8.2f;
        private const float WorldHalfHeight = 4.2f;

        private BossPatternPrototype pattern;
        private RuntimeWorldViewPrototype worldView;
        private FieldInfo encounterActiveField;
        private FieldInfo warningTimerField;
        private FieldInfo warningDurationField;
        private FieldInfo warningCenterField;
        private FieldInfo warningRadiusField;
        private FieldInfo remainingTimeField;
        private FieldInfo patternNameField;
        private FieldInfo enragedField;
        private FieldInfo worldRootField;

        private GameObject root;
        private SpriteRenderer warningRenderer;
        private TextMesh warningLabel;
        private TextMesh timerLabel;
        private Sprite circleSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BossPatternWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBossPatternWorldViewPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<BossPatternWorldViewPrototype>();
        }

        private System.Collections.IEnumerator Start()
        {
            yield return null;
            pattern = FindFirstObjectByType<BossPatternPrototype>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (pattern == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            Type patternType = typeof(BossPatternPrototype);
            encounterActiveField = patternType.GetField("encounterActive", PrivateInstance);
            warningTimerField = patternType.GetField("warningTimer", PrivateInstance);
            warningDurationField = patternType.GetField("warningDuration", PrivateInstance);
            warningCenterField = patternType.GetField("warningCenter", PrivateInstance);
            warningRadiusField = patternType.GetField("warningRadius", PrivateInstance);
            remainingTimeField = patternType.GetField("remainingTime", PrivateInstance);
            patternNameField = patternType.GetField("patternName", PrivateInstance);
            enragedField = patternType.GetField("enraged", PrivateInstance);
            worldRootField = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance);

            BuildVisuals();
        }

        private void BuildVisuals()
        {
            CreateCircleSprite();
            GameObject worldRoot = worldRootField?.GetValue(worldView) as GameObject;
            root = new GameObject("Boss Pattern World UI");
            root.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);

            GameObject warning = new GameObject("Boss Warning Zone");
            warning.transform.SetParent(root.transform, false);
            warningRenderer = warning.AddComponent<SpriteRenderer>();
            warningRenderer.sprite = circleSprite;
            warningRenderer.color = new Color(1f, 0.18f, 0.08f, 0.34f);
            warningRenderer.sortingOrder = 14;

            GameObject labelObject = new GameObject("Boss Warning Label");
            labelObject.transform.SetParent(warning.transform, false);
            warningLabel = labelObject.AddComponent<TextMesh>();
            warningLabel.anchor = TextAnchor.MiddleCenter;
            warningLabel.alignment = TextAlignment.Center;
            warningLabel.fontSize = 44;
            warningLabel.characterSize = 0.07f;
            warningLabel.color = Color.white;
            warningLabel.GetComponent<MeshRenderer>().sortingOrder = 18;

            GameObject timerObject = new GameObject("Boss Timer Label");
            timerObject.transform.SetParent(root.transform, false);
            timerObject.transform.localPosition = new Vector3(0f, WorldHalfHeight + 0.35f, 0f);
            timerLabel = timerObject.AddComponent<TextMesh>();
            timerLabel.anchor = TextAnchor.MiddleCenter;
            timerLabel.alignment = TextAlignment.Center;
            timerLabel.fontSize = 46;
            timerLabel.characterSize = 0.075f;
            timerLabel.color = new Color(1f, 0.93f, 0.52f);
            timerLabel.GetComponent<MeshRenderer>().sortingOrder = 25;
            root.SetActive(false);
        }

        private void CreateCircleSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "BossWarningCircle";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float fill = normalized <= 1f ? 0.22f : 0f;
                    float ring = normalized > 0.82f && normalized <= 1f ? 0.78f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(fill, ring)));
                }
            }
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private void LateUpdate()
        {
            if (root == null || pattern == null) return;
            bool active = ReadBool(encounterActiveField);
            root.SetActive(active);
            if (!active) return;

            float remaining = ReadFloat(remainingTimeField);
            bool enraged = ReadBool(enragedField);
            string patternName = patternNameField?.GetValue(pattern) as string ?? "균왕 패턴";
            timerLabel.text = $"균왕 제한 {Mathf.CeilToInt(remaining)}초{(enraged ? " · 광폭화" : string.Empty)}\n{patternName}";
            timerLabel.color = enraged ? new Color(1f, 0.34f, 0.18f) : new Color(1f, 0.93f, 0.52f);

            float warningTime = ReadFloat(warningTimerField);
            warningRenderer.gameObject.SetActive(warningTime > 0f);
            if (warningTime <= 0f) return;

            Vector2 center = warningCenterField == null ? Vector2.zero : (Vector2)warningCenterField.GetValue(pattern);
            float radius = ReadFloat(warningRadiusField);
            float duration = Mathf.Max(0.01f, ReadFloat(warningDurationField));
            float progress = Mathf.Clamp01(1f - warningTime / duration);
            float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.05f;
            float worldRadius = SourceRadiusToWorld(radius) * pulse;

            warningRenderer.transform.position = SourceToWorld(center);
            warningRenderer.transform.localScale = Vector3.one * worldRadius * 2f;
            warningRenderer.color = Color.Lerp(
                new Color(1f, 0.72f, 0.08f, 0.30f),
                new Color(1f, 0.04f, 0.02f, 0.62f),
                progress);
            warningLabel.text = $"위험\n{warningTime:0.0}";
            warningLabel.transform.localScale = Vector3.one / Mathf.Max(0.4f, worldRadius * 2f);
        }

        private bool ReadBool(FieldInfo field)
        {
            return field != null && Convert.ToBoolean(field.GetValue(pattern));
        }

        private float ReadFloat(FieldInfo field)
        {
            return field == null ? 0f : Convert.ToSingle(field.GetValue(pattern));
        }

        private static Vector3 SourceToWorld(Vector2 source)
        {
            float x = Mathf.Lerp(-WorldHalfWidth, WorldHalfWidth, Mathf.InverseLerp(SourceLeft, SourceRight, source.x));
            float y = Mathf.Lerp(WorldHalfHeight, -WorldHalfHeight, Mathf.InverseLerp(SourceTop, SourceBottom, source.y));
            return new Vector3(x, y, 0f);
        }

        private static float SourceRadiusToWorld(float sourceRadius)
        {
            float xScale = WorldHalfWidth * 2f / (SourceRight - SourceLeft);
            float yScale = WorldHalfHeight * 2f / (SourceBottom - SourceTop);
            return sourceRadius * (xScale + yScale) * 0.5f;
        }
    }
}
