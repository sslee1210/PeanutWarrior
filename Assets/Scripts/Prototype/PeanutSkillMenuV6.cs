using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(34200)]
    public sealed class PeanutSkillMenuV6 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Width = 1388f;
        private const float Height = 650f;
        private const int IconTextureSize = 128;

        private static readonly Color[] SkillColors =
        {
            new Color(0.15f, 0.58f, 0.34f),
            new Color(0.14f, 0.48f, 0.72f),
            new Color(0.22f, 0.64f, 0.62f),
            new Color(0.74f, 0.56f, 0.12f),
            new Color(0.82f, 0.24f, 0.16f),
            new Color(0.72f, 0.18f, 0.24f),
            new Color(0.52f, 0.30f, 0.80f),
            new Color(0.28f, 0.18f, 0.48f)
        };

        private PeanutMobileCanvasPrototype sourceUi;
        private SkillManagementPrototype skills;
        private PeanutSaveGameService saveService;
        private FieldInfo currentPageField;
        private FieldInfo contentHostField;
        private FieldInfo menuTitleField;
        private FieldInfo sourceRefreshersField;
        private MethodInfo toastMethod;
        private GameObject contentHost;
        private Text menuTitle;
        private IList sourceRefreshers;
        private Font font;
        private Sprite backgroundSprite;
        private Sprite autoSprite;
        private readonly Sprite[] skillSprites = new Sprite[8];
        private readonly List<Action> refreshers = new List<Action>();
        private GameObject root;
        private string activePage = string.Empty;
        private float refreshTimer;

        private GameObject detailOverlay;
        private Image detailIcon;
        private Text detailType;
        private Text detailName;
        private Text detailDescription;
        private Text detailStats;
        private Text detailLevel;
        private Text detailCost;
        private Button detailUpgradeButton;
        private Text detailUpgradeText;
        private int selectedSkill = -1;

        private readonly Color background = new Color(0.94f, 0.96f, 0.89f, 1f);
        private readonly Color darkGreen = new Color(0.06f, 0.23f, 0.10f, 1f);
        private readonly Color brown = new Color(0.20f, 0.12f, 0.06f, 1f);
        private readonly Color muted = new Color(0.48f, 0.51f, 0.45f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color red = new Color(0.72f, 0.20f, 0.15f, 1f);
        private readonly Color cream = new Color(0.98f, 0.95f, 0.82f, 1f);
        private readonly Color gold = new Color(0.94f, 0.61f, 0.10f, 1f);

        public int SkillIconCount => 8;
        public bool UsesCardlessSkillLayout => true;
        public bool UsesNamedSkillSilhouettes => true;
        public bool AutoButtonIsTopLeft => true;
        public bool UsesSkillDetailWindow => true;
        public bool ShowsAccurateDamageDetails => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutSkillMenuV6>() != null) return;
            GameObject go = new GameObject("PeanutWarriorSkillMenuV6");
            DontDestroyOnLoad(go);
            go.AddComponent<PeanutSkillMenuV6>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 18; i++)
            {
                yield return null;
                if (Bind()) break;
            }
            if (sourceUi == null || contentHost == null || skills == null)
            {
                enabled = false;
                yield break;
            }
            CreateAssets();
        }

        private bool Bind()
        {
            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            skills = FindFirstObjectByType<SkillManagementPrototype>();
            saveService = FindFirstObjectByType<PeanutSaveGameService>();
            if (sourceUi == null || skills == null) return false;

            Type type = typeof(PeanutMobileCanvasPrototype);
            currentPageField = type.GetField("currentPage", PrivateInstance);
            contentHostField = type.GetField("contentHost", PrivateInstance);
            menuTitleField = type.GetField("menuTitle", PrivateInstance);
            sourceRefreshersField = type.GetField("refreshers", PrivateInstance);
            toastMethod = type.GetMethod("Toast", PrivateInstance);
            contentHost = contentHostField?.GetValue(sourceUi) as GameObject;
            menuTitle = menuTitleField?.GetValue(sourceUi) as Text;
            sourceRefreshers = sourceRefreshersField?.GetValue(sourceUi) as IList;
            return contentHost != null;
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            backgroundSprite = SolidSprite();
            autoSprite = RoundedSprite();
            for (int i = 0; i < skillSprites.Length; i++) skillSprites[i] = CreateSkillSprite(i);
        }

        private void LateUpdate()
        {
            if (sourceUi == null || contentHost == null || skills == null) return;
            string page = CurrentPage;
            if (page != "Skills")
            {
                activePage = page;
                return;
            }

            if (activePage != page || root == null || root.transform.parent != contentHost.transform)
            {
                activePage = page;
                BuildPage();
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.12f;
            for (int i = 0; i < refreshers.Count; i++)
            {
                try { refreshers[i]?.Invoke(); }
                catch (MissingReferenceException) { }
            }
        }

        private string CurrentPage => currentPageField?.GetValue(sourceUi)?.ToString() ?? "Main";

        private void BuildPage()
        {
            sourceRefreshers?.Clear();
            refreshers.Clear();
            selectedSkill = -1;
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            root = Rect(contentHost.transform, "Peanut Skill Menu V6", 0f, 0f, Width, Height).gameObject;
            Image pageBackground = root.AddComponent<Image>();
            pageBackground.sprite = backgroundSprite;
            pageBackground.color = background;
            pageBackground.raycastTarget = false;
            if (menuTitle != null) menuTitle.text = "스킬";

            Button auto = SmallButton(root.transform, string.Empty, 18f, 12f, 94f, 34f, ToggleAuto);
            Text autoText = auto.GetComponentInChildren<Text>();
            refreshers.Add(() =>
            {
                bool enabledNow = skills.GlobalAutoEnabled;
                autoText.text = enabledNow ? "AUTO ON" : "AUTO OFF";
                auto.GetComponent<Image>().color = enabledNow ? green : muted;
            });

            Label(root.transform, "사냥 스킬", 150f, 8f, 470f, 44f, 24, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(root.transform, "보스 스킬", 768f, 8f, 470f, 44f, 24, new Color(0.52f, 0.20f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Image divider = Rect(root.transform, "Skill Divider", 693f, 58f, 2f, 548f).gameObject.AddComponent<Image>();
            divider.sprite = backgroundSprite;
            divider.color = new Color(0.40f, 0.48f, 0.37f, 0.36f);
            divider.raycastTarget = false;

            for (int i = 0; i < 8; i++) BuildSkill(i);
            Label(root.transform, "스킬 문양을 누르면 상세 능력과 실제 피해량을 확인할 수 있습니다.",
                18f, 610f, 1352f, 26f, 13, muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            BuildDetailWindow();
        }

        private void BuildSkill(int index)
        {
            bool boss = index >= 4;
            int local = index % 4;
            int column = local % 2;
            int row = local / 2;
            float groupX = boss ? 770f : 82f;
            float x = groupX + column * 300f;
            float y = 78f + row * 268f;
            int captured = index;

            GameObject iconObject = new GameObject("Skill Icon " + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(root.transform, false);
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(x, -y);
            iconRect.sizeDelta = new Vector2(176f, 176f);

            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = skillSprites[index];
            icon.preserveAspect = true;
            icon.color = SkillColors[index];

            Button button = iconObject.GetComponent<Button>();
            button.targetGraphic = icon;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.86f);
            colors.pressedColor = new Color(0.74f, 0.78f, 0.70f, 1f);
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.52f);
            button.colors = colors;
            button.onClick.AddListener(() => OpenSkillDetails(captured));

            Text name = Label(root.transform, string.Empty, x - 42f, y + 172f, 260f, 32f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text state = Label(root.transform, string.Empty, x - 42f, y + 204f, 260f, 28f, 14, brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text hint = Label(root.transform, "상세 보기", x - 42f, y + 232f, 260f, 26f, 13, muted, TextAnchor.MiddleCenter, FontStyle.Normal);

            refreshers.Add(() =>
            {
                float[] cooldowns = skills.Cooldowns;
                float cooldown = cooldowns != null && captured < cooldowns.Length ? Mathf.Max(0f, cooldowns[captured]) : 0f;
                name.text = skills.GetSkillName(captured);
                state.text = $"Lv.{skills.GetSkillLevel(captured)}  ·  {(cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "READY")}";
                hint.color = selectedSkill == captured ? SkillColors[captured] : muted;
                icon.color = SkillColors[captured];
            });
        }

        private void BuildDetailWindow()
        {
            detailOverlay = Rect(root.transform, "Skill Detail Overlay", 0f, 0f, Width, Height).gameObject;
            Image dim = detailOverlay.AddComponent<Image>();
            dim.sprite = backgroundSprite;
            dim.color = new Color(0.03f, 0.04f, 0.05f, 0.70f);

            GameObject panel = Panel(detailOverlay.transform, "Skill Detail Window", 304f, 72f, 780f, 506f, cream, gold);
            SmallButton(panel.transform, "닫기", 674f, 16f, 82f, 36f, CloseSkillDetails);

            GameObject iconObject = Rect(panel.transform, "Detail Skill Icon", 34f, 56f, 188f, 188f).gameObject;
            detailIcon = iconObject.AddComponent<Image>();
            detailIcon.preserveAspect = true;
            detailType = Label(panel.transform, string.Empty, 250f, 54f, 470f, 30f, 15, muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            detailName = Label(panel.transform, string.Empty, 250f, 84f, 470f, 50f, 28, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            detailDescription = Label(panel.transform, string.Empty, 250f, 142f, 470f, 92f, 16, brown, TextAnchor.UpperLeft, FontStyle.Normal);

            detailLevel = Label(panel.transform, string.Empty, 34f, 266f, 712f, 38f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            detailStats = Label(panel.transform, string.Empty, 34f, 312f, 712f, 84f, 18, brown, TextAnchor.UpperLeft, FontStyle.Bold);
            detailCost = Label(panel.transform, string.Empty, 34f, 406f, 430f, 54f, 15, muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            detailUpgradeButton = SmallButton(panel.transform, string.Empty, 500f, 408f, 246f, 58f, UpgradeSelectedSkill);
            detailUpgradeText = detailUpgradeButton.GetComponentInChildren<Text>();

            detailOverlay.SetActive(false);
            detailOverlay.transform.SetAsLastSibling();
            refreshers.Add(RefreshDetailWindow);
        }

        private void OpenSkillDetails(int index)
        {
            selectedSkill = Mathf.Clamp(index, 0, 7);
            if (detailOverlay != null)
            {
                detailOverlay.SetActive(true);
                detailOverlay.transform.SetAsLastSibling();
            }
            RefreshDetailWindow();
        }

        private void CloseSkillDetails()
        {
            selectedSkill = -1;
            if (detailOverlay != null) detailOverlay.SetActive(false);
        }

        private void RefreshDetailWindow()
        {
            if (detailOverlay == null || !detailOverlay.activeSelf || selectedSkill < 0 || skills == null) return;
            int index = selectedSkill;
            long cost = skills.GetUpgradeCost(index);
            detailIcon.sprite = skillSprites[index];
            detailIcon.color = SkillColors[index];
            detailType.text = skills.IsBossSkill(index) ? "보스 스킬" : "사냥 스킬";
            detailName.text = skills.GetSkillName(index);
            detailDescription.text = skills.GetSkillDescription(index);
            detailLevel.text = $"현재 Lv.{skills.GetSkillLevel(index)} · 피해 배율 ×{skills.GetSkillDamageMultiplier(index):0.00}";
            detailStats.text = skills.GetSkillCombatSummary(index);
            detailCost.text = $"보유 조각 {skills.Fragments:N0}\n다음 강화 비용 {cost:N0} 조각";
            detailUpgradeButton.interactable = skills.Fragments >= cost;
            detailUpgradeButton.GetComponent<Image>().color = detailUpgradeButton.interactable ? SkillColors[index] : muted;
            detailUpgradeText.text = detailUpgradeButton.interactable ? "스킬 강화" : "조각 부족";
        }

        private void ToggleAuto()
        {
            skills.ToggleGlobalAuto();
            saveService?.SaveNow();
            Toast(skills.LastMessage);
        }

        private void UpgradeSelectedSkill()
        {
            if (selectedSkill < 0 || skills == null) return;
            skills.UpgradeSkill(selectedSkill);
            saveService?.SaveNow();
            Toast(skills.LastMessage);
            RefreshDetailWindow();
        }

        private void Toast(string message)
        {
            if (sourceUi != null && toastMethod != null)
                toastMethod.Invoke(sourceUi, new object[] { message });
            else
                Debug.Log("[PeanutWarrior] " + message);
        }

        private GameObject Panel(Transform parent, string name, float x, float y, float width, float height, Color color, Color border)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = autoSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.50f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
            return rect.gameObject;
        }

        private Button SmallButton(Transform parent, string text, float x, float y, float width, float height, Action action)
        {
            RectTransform rect = Rect(parent, "Skill Button", x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = autoSprite;
            image.type = Image.Type.Sliced;
            image.color = green;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Label(rect, text, 3f, 2f, width - 6f, height - 4f, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            RectTransform rect = Rect(parent, "Text", x, y, width, height);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Sprite SolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite RoundedSprite()
        {
            const int size = 24;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, 7f - Mathf.Min(x, size - 1 - x));
                    float dy = Mathf.Max(0f, 7f - Mathf.Min(y, size - 1 - y));
                    float alpha = Mathf.Clamp01(7.5f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(8f, 8f, 8f, 8f));
        }

        private static Sprite CreateSkillSprite(int index)
        {
            Texture2D texture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false);
            texture.name = "SkillSilhouette" + index;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < IconTextureSize; y++)
                for (int x = 0; x < IconTextureSize; x++)
                    texture.SetPixel(x, y, clear);

            DrawRing(texture, 64, 64, 54, 3, 0.42f);
            switch (index)
            {
                case 0: DrawWhirlwind(texture); break;
                case 1: DrawBarrage(texture); break;
                case 2: DrawTrackingDance(texture); break;
                case 3: DrawHeavenEarthCut(texture); break;
                case 4: DrawComboSlash(texture); break;
                case 5: DrawVitalCut(texture); break;
                case 6: DrawElementMark(texture); break;
                default: DrawDimensionEnd(texture); break;
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, IconTextureSize, IconTextureSize), new Vector2(0.5f, 0.5f), 128f);
        }

        private static void DrawWhirlwind(Texture2D texture)
        {
            for (int arm = 0; arm < 3; arm++)
            {
                float offset = arm * Mathf.PI * 2f / 3f;
                Vector2 previous = Vector2.zero;
                for (int step = 0; step <= 34; step++)
                {
                    float t = step / 34f;
                    float angle = offset + t * Mathf.PI * 1.55f;
                    float radius = 8f + t * 38f;
                    Vector2 point = new Vector2(64f + Mathf.Cos(angle) * radius, 64f + Mathf.Sin(angle) * radius);
                    if (step > 0) DrawLine(texture, previous, point, 3, 1f);
                    previous = point;
                }
            }
            DrawFilledCircle(texture, 64, 64, 6, 1f);
        }

        private static void DrawBarrage(Texture2D texture)
        {
            for (int i = 0; i < 5; i++)
            {
                float shift = (i - 2) * 14f;
                DrawLine(texture, new Vector2(30f + shift, 92f), new Vector2(76f + shift, 36f), 4, 1f);
                DrawLine(texture, new Vector2(71f + shift, 36f), new Vector2(83f + shift, 31f), 2, 0.85f);
            }
        }

        private static void DrawTrackingDance(Texture2D texture)
        {
            DrawRing(texture, 64, 64, 25, 3, 0.95f);
            DrawRing(texture, 64, 64, 10, 3, 0.95f);
            DrawLine(texture, new Vector2(24f, 90f), new Vector2(91f, 34f), 5, 1f);
            DrawLine(texture, new Vector2(91f, 34f), new Vector2(82f, 35f), 3, 1f);
            DrawLine(texture, new Vector2(91f, 34f), new Vector2(89f, 44f), 3, 1f);
        }

        private static void DrawHeavenEarthCut(Texture2D texture)
        {
            DrawLine(texture, new Vector2(64f, 22f), new Vector2(64f, 106f), 6, 1f);
            DrawLine(texture, new Vector2(24f, 64f), new Vector2(104f, 64f), 6, 1f);
            DrawLine(texture, new Vector2(42f, 28f), new Vector2(86f, 100f), 2, 0.65f);
            DrawLine(texture, new Vector2(86f, 28f), new Vector2(42f, 100f), 2, 0.65f);
        }

        private static void DrawComboSlash(Texture2D texture)
        {
            for (int i = 0; i < 4; i++)
            {
                float shift = i * 13f;
                DrawLine(texture, new Vector2(26f + shift, 102f), new Vector2(66f + shift, 28f), 5, 1f);
            }
            DrawLine(texture, new Vector2(30f, 44f), new Vector2(97f, 84f), 3, 0.72f);
        }

        private static void DrawVitalCut(Texture2D texture)
        {
            DrawRing(texture, 64, 64, 30, 3, 0.95f);
            DrawRing(texture, 64, 64, 12, 3, 0.95f);
            DrawLine(texture, new Vector2(64f, 20f), new Vector2(64f, 42f), 3, 0.9f);
            DrawLine(texture, new Vector2(64f, 86f), new Vector2(64f, 108f), 3, 0.9f);
            DrawLine(texture, new Vector2(20f, 64f), new Vector2(42f, 64f), 3, 0.9f);
            DrawLine(texture, new Vector2(86f, 64f), new Vector2(108f, 64f), 3, 0.9f);
            DrawLine(texture, new Vector2(31f, 96f), new Vector2(97f, 31f), 5, 1f);
        }

        private static void DrawElementMark(Texture2D texture)
        {
            DrawLine(texture, new Vector2(64f, 22f), new Vector2(102f, 64f), 5, 1f);
            DrawLine(texture, new Vector2(102f, 64f), new Vector2(64f, 106f), 5, 1f);
            DrawLine(texture, new Vector2(64f, 106f), new Vector2(26f, 64f), 5, 1f);
            DrawLine(texture, new Vector2(26f, 64f), new Vector2(64f, 22f), 5, 1f);
            DrawFilledCircle(texture, 64, 38, 7, 1f);
            DrawFilledCircle(texture, 88, 64, 7, 1f);
            DrawFilledCircle(texture, 64, 90, 7, 1f);
            DrawFilledCircle(texture, 40, 64, 7, 1f);
            DrawRing(texture, 64, 64, 13, 3, 1f);
        }

        private static void DrawDimensionEnd(Texture2D texture)
        {
            DrawRing(texture, 64, 64, 37, 5, 1f);
            DrawRing(texture, 64, 64, 21, 3, 0.82f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                Vector2 inner = new Vector2(64f + Mathf.Cos(angle) * 12f, 64f + Mathf.Sin(angle) * 12f);
                Vector2 outer = new Vector2(64f + Mathf.Cos(angle + 0.18f) * 48f, 64f + Mathf.Sin(angle + 0.18f) * 48f);
                DrawLine(texture, inner, outer, 3, 1f);
            }
            DrawFilledCircle(texture, 64, 64, 7, 1f);
        }

        private static void DrawRing(Texture2D texture, int centerX, int centerY, int radius, int thickness, float alpha)
        {
            int min = radius - thickness;
            int max = radius + thickness;
            for (int y = centerY - max; y <= centerY + max; y++)
            {
                for (int x = centerX - max; x <= centerX + max; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance >= min && distance <= max) SetPixel(texture, x, y, alpha);
                }
            }
        }

        private static void DrawFilledCircle(Texture2D texture, int centerX, int centerY, int radius, float alpha)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
                for (int x = centerX - radius; x <= centerX + radius; x++)
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radiusSquared)
                        SetPixel(texture, x, y, alpha);
        }

        private static void DrawLine(Texture2D texture, Vector2 from, Vector2 to, int thickness, float alpha)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)steps);
                DrawFilledCircle(texture, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), thickness, alpha);
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, float alpha)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
            Color current = texture.GetPixel(x, y);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(current.a, alpha)));
        }
    }
}
