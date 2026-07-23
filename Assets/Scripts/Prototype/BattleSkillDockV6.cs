using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(34100)]
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
            for (int i = 0; i < 18; i++)
            {
                yield return null;
                sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
                skills = FindFirstObjectByType<SkillManagementPrototype>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                if (sourceUi != null && skills != null && stageFlow != null) break;
            }

            if (sourceUi == null || skills == null || stageFlow == null)
            {
                enabled = false;
                yield break;
            }

            BindSourceUi();
            if (mainPage == null)
            {
                enabled = false;
                yield break;
            }

            CreateAssets();
            HideLegacyDock();
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

        private void HideLegacyDock()
        {
            Text[] oldSkillTexts = skillTextsField?.GetValue(sourceUi) as Text[];
            if (oldSkillTexts != null && oldSkillTexts.Length > 0 && oldSkillTexts[0] != null)
            {
                Transform slot = oldSkillTexts[0].transform.parent;
                Transform dock = slot == null ? null : slot.parent;
                if (dock != null) dock.gameObject.SetActive(false);
            }

            Text oldAuto = globalAutoTextField?.GetValue(sourceUi) as Text;
            if (oldAuto != null && oldAuto.transform.parent != null)
                oldAuto.transform.parent.gameObject.SetActive(false);
        }

        private void BuildDock()
        {
            root = Rect(mainPage.transform, "Battle Skill Dock V6", 748f, 592f, 624f, 144f).gameObject;

            RectTransform autoRect = Rect(root.transform, "AUTO", 0f, 0f, 92f, 32f);
            autoImage = autoRect.gameObject.AddComponent<Image>();
            autoImage.sprite = roundedSprite;
            autoImage.type = Image.Type.Sliced;
            autoImage.color = green;
            Button autoButton = autoRect.gameObject.AddComponent<Button>();
            autoButton.targetGraphic = autoImage;
            autoButton.navigation = new Navigation { mode = Navigation.Mode.None };
            autoButton.onClick.AddListener(ToggleAuto);
            autoText = Label(autoRect, string.Empty, 2f, 1f, 88f, 30f, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            for (int i = 0; i < 4; i++)
            {
                int slotIndex = i;
                float x = 104f + i * 128f;
                GameObject iconObject = new GameObject("Battle Skill " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                RectTransform rect = iconObject.GetComponent<RectTransform>();
                rect.SetParent(root.transform, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, -4f);
                rect.sizeDelta = new Vector2(92f, 92f);

                Image image = iconObject.GetComponent<Image>();
                image.preserveAspect = true;
                iconImages[i] = image;

                Button button = iconObject.GetComponent<Button>();
                button.targetGraphic = image;
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                button.onClick.AddListener(OpenSkillPage);

                stateTexts[i] = Label(iconObject.transform, string.Empty, 4f, 30f, 84f, 30f, 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                nameTexts[i] = Label(root.transform, string.Empty, x - 18f, 96f, 128f, 38f, 12, dark, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private void Update()
        {
            if (root == null || skills == null || stageFlow == null) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.10f;
            Refresh();
        }

        private void Refresh()
        {
            if (root == null) return;
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
