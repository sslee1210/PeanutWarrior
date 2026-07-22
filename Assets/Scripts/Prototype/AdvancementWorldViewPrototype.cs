using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(17800)]
    public sealed class AdvancementWorldViewPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private static readonly Color[] TierColors =
        {
            new Color(0.86f, 0.58f, 0.18f),
            new Color(0.77f, 0.43f, 0.14f),
            new Color(0.95f, 0.72f, 0.16f),
            new Color(0.94f, 0.30f, 0.14f),
            new Color(0.30f, 0.72f, 0.92f),
            new Color(0.55f, 0.38f, 0.94f),
            new Color(0.98f, 0.82f, 0.30f),
            new Color(0.75f, 0.47f, 0.96f)
        };

        private RuntimeWorldViewPrototype worldView;
        private AdvancementProgressionPrototype advancement;
        private FieldInfo playerViewField;
        private object playerView;
        private FieldInfo rootField;
        private FieldInfo bodyField;
        private FieldInfo labelField;
        private GameObject aura;
        private SpriteRenderer auraRenderer;
        private Sprite auraSprite;
        private int appliedTier = -1;

        public int AppliedTier => appliedTier;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<AdvancementWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorAdvancementWorldView");
            DontDestroyOnLoad(root);
            root.AddComponent<AdvancementWorldViewPrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            if (worldView == null || advancement == null)
            {
                enabled = false;
                yield break;
            }

            playerViewField = typeof(RuntimeWorldViewPrototype).GetField("playerView", PrivateInstance);
            playerView = playerViewField?.GetValue(worldView);
            if (playerView == null)
            {
                enabled = false;
                yield break;
            }

            Type viewType = playerView.GetType();
            rootField = viewType.GetField("Root", PublicInstance);
            bodyField = viewType.GetField("Body", PublicInstance);
            labelField = viewType.GetField("Label", PublicInstance);
            BuildAura();
            advancement.AdvancementChanged += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            if (advancement != null) advancement.AdvancementChanged -= Apply;
        }

        private void LateUpdate()
        {
            if (advancement == null) return;
            if (appliedTier != advancement.Tier) Apply();
            if (aura != null && aura.activeSelf)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.05f;
                aura.transform.localScale = Vector3.one * pulse;
                aura.transform.Rotate(0f, 0f, 18f * Time.deltaTime);
            }
        }

        private void BuildAura()
        {
            GameObject playerRoot = rootField?.GetValue(playerView) as GameObject;
            if (playerRoot == null) return;

            auraSprite = CreateAuraSprite();
            aura = new GameObject("Advancement Aura");
            aura.transform.SetParent(playerRoot.transform, false);
            aura.transform.localScale = Vector3.one;
            auraRenderer = aura.AddComponent<SpriteRenderer>();
            auraRenderer.sprite = auraSprite;
            auraRenderer.color = new Color(1f, 0.8f, 0.2f, 0.20f);
            auraRenderer.sortingOrder = 1;
            aura.SetActive(false);
        }

        private void Apply()
        {
            if (advancement == null || playerView == null) return;
            appliedTier = Mathf.Clamp(advancement.Tier, 0, TierColors.Length - 1);

            GameObject playerRoot = rootField?.GetValue(playerView) as GameObject;
            SpriteRenderer body = bodyField?.GetValue(playerView) as SpriteRenderer;
            TextMesh label = labelField?.GetValue(playerView) as TextMesh;
            if (body != null) body.color = TierColors[appliedTier];
            if (label != null)
            {
                label.text = $"땅콩전사\n{advancement.CurrentName}";
                label.color = appliedTier >= 6 ? new Color(1f, 0.90f, 0.35f) : Color.white;
            }
            if (playerRoot != null)
            {
                float scale = 0.95f + appliedTier * 0.055f;
                playerRoot.transform.localScale = Vector3.one * scale;
            }
            if (aura != null && auraRenderer != null)
            {
                aura.SetActive(appliedTier >= 2);
                Color color = TierColors[appliedTier];
                auraRenderer.color = new Color(color.r, color.g, color.b, 0.16f + appliedTier * 0.015f);
            }
        }

        private static Sprite CreateAuraSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "ProceduralAdvancementAura";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(distance - 0.72f) * 9f);
                    float rays = Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.Atan2(y - center.y, x - center.x) * 8f)), 8f);
                    float alpha = Mathf.Clamp01(ring * 0.75f + rays * Mathf.Clamp01(1f - distance) * 0.30f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 42f);
        }
    }
}
