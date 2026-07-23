using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(36000)]
    public sealed class BattleSkillDockV6 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private PeanutMobileCanvasPrototype sourceUi;
        private SkillManagementPrototype skills;
        private StageFlowController stageFlow;
        private FieldInfo mainPageField;
        private FieldInfo skillTextsField;
        private FieldInfo globalAutoTextField;
        private MethodInfo showPageMethod;
        private object skillsPageValue;
        private GameObject mainPage;
        private Font font;
        private Sprite roundedSprite;
        private readonly Sprite[] icons = new Sprite[8];
        private readonly Image[] iconImages = new Image[4];
        private readonly Text[] nameTexts = new Text[4];
        private readonly Text[] stateTexts = new Text[4];
        private Text autoText;
        private Image autoImage;
        private GameObject root;
        private float refreshTimer;

        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color muted = new Color(0.47f, 0.50f, 0.44f, 1f);
        private readonly Color dark = new Color(0.08f, 0.18f, 0.09f, 1f);

        public bool HidesLegacySkillBlocks => true;
        public bool UsesCircularBattleSkills => true;
        public bool AutoButtonIsTopLeft => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BattleSkillDockV6>() != null) return;
            GameObject go = new GameObject("PeanutWarriorBattleSkillDockV6");
            DontDestroyOnLoad(go);
            go.AddComponent<BattleSkillDockV6>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 24; i++)
            {
                sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
                skills = FindFirstObjectByType<SkillManagementPrototype>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                if (sourceUi != null && skills != null && stageFlow != null)
                {
                    BindSourceUi();
                    if (mainPage != null) break;
                }
                yield return null;
            }

            if (sourceUi == null || skills == null || stageFlow == null || mainPage == null)
            {
                enabled = false;
                yield break;
            }

            CreateAssets();
            RemoveLegacyDock();
            BuildDock();
            Refresh();
        }

        private void BindSourceUi()
        {
            Type type = typeof(PeanutMobileCanvasPrototype);
            mainPageField = type.GetField("mainPage", PrivateInstance);
            skillTextsField = type.GetField("skillTexts", PrivateInstance);
            globalAutoTextField = type.GetField("globalAutoText", PrivateInstance);
            showPageMethod = type.GetMethod("ShowPage", PrivateInstance);
            mainPage = mainPageField?.GetValue(sourceUi) as GameObject;
            if (showPageMethod != null)
            {
                Type pageType = showPageMethod.GetParameters()[0].ParameterType;
                skillsPageValue = Enum.Parse(pageType, "Skills");
            }
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 16);
            roundedSprite = CreateRoundedSprite();
            for (int i = 0; i < icons.Length; i++) icons[i] = SkillIconFactoryV6.Create(i);
        }

        private void RemoveLegacyDock()
        {
            if (mainPage == null) return;

            Transform namedDock = mainPage.transform.Find("Active Skills");
            if (namedDock != null && namedDock.gameObject != root)
                namedDock.gameObject.SetActive(false);

            Text[] oldSkillTexts = skillTextsField?.GetValue(sourceUi) as Text[];
            if (oldSkillTexts != null)
            {
                for (int i = 0; i < oldSkillTexts.Length; i++)
                {
                    Text oldText = oldSkillTexts[i];
                    if (oldText == null) continue;
                    Transform slot = oldText.transform.parent;
                    Transform dock = slot == null ? null : slot.parent;
                    if (dock != null && dock.gameObject != root) dock.gameObject.SetActive(false);
                }
            }

            Text oldAuto = globalAutoTextField?.GetValue(sourceUi) as Text;
            if (oldAuto != null && oldAuto.transform.parent != null && oldAuto.transform.parent.gameObject != root)
                oldAuto.transform.parent.gameObject.SetActive(false);
        }

        private void BuildDock()
        {
            Transform existing = mainPage.transform.Find("Battle Skill Dock V6");
            if (existing != null) Destroy(existing.gameObject);

            root = Rect(mainPage.transform, "Battle Skill Dock V6", 730f, 586f, 642f, 150f).gameObject;

            RectTransform autoRect = Rect(root.transform, "AUTO", 0f, 0f, 88f, 30f);
            autoImage = autoRect.gameObject.AddComponent<Image>();
            autoImage.sprite = roundedSprite;
            autoImage.type = Image.Type.Sliced;
            autoImage.color = green;
            Button autoButton = autoRect.gameObject.AddComponent<Button>();
            autoButton.targetGraphic = autoImage;
            autoButton.navigation = new Navigation { mode = Navigation.Mode.None };
            autoButton.onClick.AddListener(ToggleAuto);
            autoText = Label(autoRect, string.Empty, 2f, 1f, 84f, 28f, 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            for (int i = 0; i < 4; i++)
            {
                float x = 102f + i * 132f;
                GameObject iconObject = new GameObject("Battle Skill " + i,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform rect = iconObject.GetComponent<RectTransform>();
                rect.SetParent(root.transform, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -2f);
                rect.sizeDelta = new Vector2(96f, 96f);

                Image image = iconObject.GetComponent<Image>();
                image.preserveAspect = true;
                iconImages[i] = image;

                Button button = iconObject.GetComponent<Button>();
                button.targetGraphic = image;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(OpenSkillPage);

                stateTexts[i] = Label(iconObject.transform, string.Empty, 5f, 32f, 86f, 30f, 13,
                    Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                nameTexts[i] = Label(root.transform, string.Empty, x - 22f, 98f, 140f, 42f, 12,
                    dark, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            root.transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (sourceUi == null || skills == null || stageFlow == null) return;
            if (mainPage == null)
            {
                BindSourceUi();
                return;
            }

            RemoveLegacyDock();
            if (root == null)
            {
                BuildDock();
            }
            else
            {
                if (!root.activeSelf) root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.10f;
            Refresh();
        }

        private void LateUpdate()
        {
            RemoveLegacyDock();
            if (root != null) root.transform.SetAsLastSibling();
        }

        private void Refresh()
        {
            if (root == null || autoText == null || autoImage == null) return;
            bool auto = skills.GlobalAutoEnabled;
            autoText.text = auto ? "AUTO ON" : "AUTO OFF";
            autoImage.color = auto ? green : muted;

            int offset = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            int[] levels = skills.SkillLevels;
            float[] cooldowns = skills.Cooldowns;
            for (int i = 0; i < 4; i++)
            {
                int index = offset + i;
                int level = levels != null && index < levels.Length ? levels[index] : 1;
                float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                iconImages[i].sprite = icons[index];
                iconImages[i].color = SkillIconFactoryV6.ColorFor(index);
                nameTexts[i].text = skills.GetSkillName(index);
                stateTexts[i].text = cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "Lv." + level;
            }
        }

        private void ToggleAuto()
        {
            skills.ToggleGlobalAuto();
            Refresh();
        }

        private void OpenSkillPage()
        {
            if (showPageMethod != null && skillsPageValue != null)
                showPageMethod.Invoke(sourceUi, new[] { skillsPageValue });
        }

        private Text Label(Transform parent, string value, float x, float y, float width, float height,
            int size, Color color, TextAnchor anchor, FontStyle style)
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

        private static Sprite CreateRoundedSprite()
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
    }
}
