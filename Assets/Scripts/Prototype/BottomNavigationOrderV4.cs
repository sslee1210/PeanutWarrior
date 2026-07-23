using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Final bottom navigation presentation. The reference is used only for the broad
    /// idea of large readable icons; all badges, colors and selection treatments are
    /// original to Peanut Warrior.
    /// </summary>
    [DefaultExecutionOrder(40000)]
    public sealed class BottomNavigationOrderV4 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] VisualOrder = { 2, 1, 0, 4, 3, 5 };
        private static readonly string[] Labels = { "성장", "장비", "스킬", "펫", "전직", "상점" };
        private static readonly string[] PageNames = { "Growth", "Equipment", "Skills", "Pets", "Advancement", "Shop" };
        private static readonly Color[] AccentColors =
        {
            new Color(0.96f, 0.57f, 0.17f, 1f),
            new Color(0.16f, 0.64f, 0.84f, 1f),
            new Color(0.50f, 0.36f, 0.92f, 1f),
            new Color(0.93f, 0.38f, 0.43f, 1f),
            new Color(0.16f, 0.67f, 0.56f, 1f),
            new Color(0.83f, 0.31f, 0.70f, 1f)
        };

        private PeanutMobileCanvasPrototype ui;
        private Image[] backgrounds;
        private FieldInfo currentPageField;
        private FieldInfo bottomNavigationField;
        private GameObject bottomNavigation;
        private Sprite badgeSprite;
        private Sprite circleSprite;
        private bool assetsReady;

        public string BottomMenuOrder => "성장 → 장비 → 스킬 → 펫 → 전직 → 상점";
        public bool AppliesFinalNavigationStateEveryFrame => true;
        public bool UsesBrightIconDock => true;
        public bool UsesPerMenuAccentColors => true;
        public bool UsesOriginalPeanutBadgeDesign => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BottomNavigationOrderV4>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBottomNavigationOrderV4");
            DontDestroyOnLoad(root);
            root.AddComponent<BottomNavigationOrderV4>();
        }

        private IEnumerator Start()
        {
            CreateAssets();
            for (int i = 0; i < 24; i++)
            {
                TryRebind();
                if (backgrounds != null && backgrounds.Length >= 6 && bottomNavigation != null) break;
                yield return null;
            }
            Apply();
        }

        private void LateUpdate()
        {
            if (ui == null || backgrounds == null || backgrounds.Length < 6 || bottomNavigation == null)
            {
                TryRebind();
                return;
            }
            Apply();
        }

        private void TryRebind()
        {
            ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (ui == null) return;
            Type uiType = typeof(PeanutMobileCanvasPrototype);
            FieldInfo backgroundsField = uiType.GetField("navBackgrounds", PrivateInstance);
            backgrounds = backgroundsField?.GetValue(ui) as Image[];
            currentPageField = uiType.GetField("currentPage", PrivateInstance);
            bottomNavigationField = uiType.GetField("bottomNavigation", PrivateInstance);
            bottomNavigation = bottomNavigationField?.GetValue(ui) as GameObject;
        }

        private void CreateAssets()
        {
            if (assetsReady) return;
            badgeSprite = CreatePeanutBadgeSprite();
            circleSprite = CreateCircleSprite();
            assetsReady = true;
        }

        private void Apply()
        {
            if (ui == null || backgrounds == null || backgrounds.Length < 6 || bottomNavigation == null) return;
            if (!assetsReady) CreateAssets();

            StyleDock();
            string currentPage = currentPageField?.GetValue(ui)?.ToString() ?? "Main";

            const float startX = 6f;
            const float spacing = 188f;
            for (int visualIndex = 0; visualIndex < VisualOrder.Length; visualIndex++)
            {
                int sourceIndex = VisualOrder[visualIndex];
                Image background = backgrounds[sourceIndex];
                if (background == null) continue;

                bool active = currentPage == PageNames[visualIndex];
                Color accent = AccentColors[visualIndex];
                StyleButton(background, Labels[visualIndex], accent, active,
                    startX + visualIndex * spacing);
            }
        }

        private void StyleDock()
        {
            RectTransform rect = bottomNavigation.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(238f, -712f);
                rect.sizeDelta = new Vector2(1134f, 104f);
            }

            Image image = bottomNavigation.GetComponent<Image>();
            if (image != null)
                image.color = new Color(0.99f, 0.96f, 0.85f, 0.96f);

            Outline outline = bottomNavigation.GetComponent<Outline>();
            if (outline == null) outline = bottomNavigation.AddComponent<Outline>();
            outline.effectColor = new Color(0.42f, 0.31f, 0.16f, 0.34f);
            outline.effectDistance = new Vector2(0f, 2f);
            outline.useGraphicAlpha = false;

            Image topLine = EnsureImage(bottomNavigation.transform, "Navigation Top Highlight");
            RectTransform lineRect = topLine.rectTransform;
            SetTopLeft(lineRect, 18f, 2f, 1098f, 3f);
            topLine.sprite = null;
            topLine.color = new Color(1f, 0.76f, 0.24f, 0.72f);
            topLine.raycastTarget = false;
            topLine.transform.SetAsLastSibling();
        }

        private void StyleButton(Image background, string labelValue, Color accent, bool active, float x)
        {
            RectTransform rect = background.rectTransform;
            rect.anchoredPosition = new Vector2(x, -2f);
            rect.sizeDelta = new Vector2(182f, 98f);
            rect.localScale = Vector3.one;
            background.color = active
                ? new Color(1f, 0.94f, 0.72f, 0.42f)
                : new Color(1f, 1f, 1f, 0.015f);

            Button button = background.GetComponent<Button>();
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.98f, 0.90f, 1f);
                colors.pressedColor = new Color(0.94f, 0.90f, 0.80f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                colors.colorMultiplier = 1f;
                button.colors = colors;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
            }

            Image shadow = EnsureImage(background.transform, "Peanut Badge Shadow");
            SetTopLeft(shadow.rectTransform, 56f, 9f, 70f, 62f);
            shadow.sprite = badgeSprite;
            shadow.color = new Color(0.12f, 0.10f, 0.18f, 0.24f);
            shadow.raycastTarget = false;
            shadow.transform.SetAsFirstSibling();

            Image rim = EnsureImage(background.transform, "Peanut Badge Rim");
            SetTopLeft(rim.rectTransform, 54f, 4f, 74f, 64f);
            rim.sprite = badgeSprite;
            rim.color = active ? new Color(1f, 0.72f, 0.18f, 1f) : Lighten(accent, 0.32f);
            rim.raycastTarget = false;
            rim.transform.SetSiblingIndex(1);

            Image badge = EnsureImage(background.transform, "Peanut Badge Fill");
            SetTopLeft(badge.rectTransform, 58f, 8f, 66f, 56f);
            badge.sprite = badgeSprite;
            badge.color = active ? Lighten(accent, 0.08f) : accent;
            badge.raycastTarget = false;
            badge.transform.SetSiblingIndex(2);

            Transform iconTransform = background.transform.Find("Icon");
            if (iconTransform != null)
            {
                RectTransform iconRect = iconTransform as RectTransform;
                SetTopLeft(iconRect, 68f, 15f, 46f, 42f);
                Image icon = iconTransform.GetComponent<Image>();
                if (icon != null)
                {
                    icon.color = Color.white;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    Outline iconOutline = icon.GetComponent<Outline>();
                    if (iconOutline == null) iconOutline = icon.gameObject.AddComponent<Outline>();
                    iconOutline.effectColor = Darken(accent, 0.30f);
                    iconOutline.effectDistance = new Vector2(1.2f, -1.2f);
                    iconOutline.useGraphicAlpha = false;
                }
                iconTransform.SetAsLastSibling();
            }

            Text label = FindNavigationLabel(background.transform);
            if (label != null)
            {
                label.text = labelValue;
                SetTopLeft(label.rectTransform, 6f, 68f, 170f, 26f);
                label.fontSize = active ? 19 : 18;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = active ? Darken(accent, 0.32f) : new Color(0.18f, 0.20f, 0.29f, 1f);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.verticalOverflow = VerticalWrapMode.Overflow;
                Outline labelOutline = label.GetComponent<Outline>();
                if (labelOutline == null) labelOutline = label.gameObject.AddComponent<Outline>();
                labelOutline.effectColor = new Color(1f, 0.98f, 0.88f, 0.96f);
                labelOutline.effectDistance = new Vector2(1f, -1f);
                labelOutline.useGraphicAlpha = false;
                label.transform.SetAsLastSibling();
            }

            Image spark = EnsureImage(background.transform, "Navigation Active Spark");
            SetTopLeft(spark.rectTransform, 119f, 3f, 18f, 18f);
            spark.sprite = circleSprite;
            spark.color = new Color(1f, 0.36f, 0.12f, 1f);
            spark.raycastTarget = false;
            spark.gameObject.SetActive(active);
            spark.transform.SetAsLastSibling();

            Transform oldLine = background.transform.Find("Navigation Active Line");
            if (oldLine != null) oldLine.gameObject.SetActive(false);
        }

        private static Text FindNavigationLabel(Transform parent)
        {
            Text[] texts = parent.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                string value = texts[i].text ?? string.Empty;
                if (value.Contains("SKILL") || value == "스킬" || value == "장비" || value == "성장" ||
                    value == "전직" || value == "펫" || value == "상점")
                    return texts[i];
            }
            return texts.Length > 0 ? texts[0] : null;
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Image existingImage = existing.GetComponent<Image>();
                if (existingImage != null) return existingImage;
            }

            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            return root.GetComponent<Image>();
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, Mathf.Clamp01(amount));
        }

        private static Color Darken(Color color, float amount)
        {
            return Color.Lerp(color, new Color(0.05f, 0.06f, 0.12f, 1f), Mathf.Clamp01(amount));
        }

        private static Sprite CreatePeanutBadgeSprite()
        {
            const int width = 80;
            const int height = 68;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "PeanutNavigationBadge";
            texture.filterMode = FilterMode.Bilinear;

            Vector2 upper = new Vector2(width * 0.38f, height * 0.40f);
            Vector2 lower = new Vector2(width * 0.62f, height * 0.60f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float first = Mathf.Sqrt(
                        Mathf.Pow((point.x - upper.x) / 28f, 2f) +
                        Mathf.Pow((point.y - upper.y) / 25f, 2f));
                    float second = Mathf.Sqrt(
                        Mathf.Pow((point.x - lower.x) / 28f, 2f) +
                        Mathf.Pow((point.y - lower.y) / 25f, 2f));
                    float body = Mathf.Min(first, second);
                    float cornerCut = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    float alpha = Mathf.Clamp01((1f - body) * 5f) * Mathf.Clamp01(cornerCut / 3f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PeanutNavigationSpark";
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 0.8f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
