using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(25000)]
    public sealed class PeanutMobileCanvasPrototype : MonoBehaviour
    {
        private enum Page
        {
            Main,
            Skills,
            Equipment,
            Growth,
            Advancement,
            Pets,
            Shop,
            StageSelect,
            Settings
        }

        private enum IconKind
        {
            Skill,
            Equipment,
            Growth,
            Advancement,
            Pet,
            Shop,
            Stage,
            Settings,
            Auto,
            Attack,
            Heart,
            Recovery,
            Mana,
            Crit,
            CritDamage,
            Experience,
            Gold,
            Material,
            Back
        }

        private const float ReferenceWidth = 1388f;
        private const float ReferenceHeight = 830f;
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };
        private static readonly Color[] ElementColors =
        {
            new Color(0.92f, 0.82f, 0.42f),
            new Color(0.94f, 0.35f, 0.20f),
            new Color(0.25f, 0.72f, 0.94f),
            new Color(0.68f, 0.42f, 0.94f)
        };

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private SkillManagementPrototype skillManager;
        private IdleSystemsPrototype idle;
        private PrototypeShopAndDaily shop;
        private PrototypeSaveBridge saveBridge;
        private SwordProgressionPrototype swords;

        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;
        private FieldInfo playerHpField;
        private FieldInfo playerMpField;
        private FieldInfo attackLevelField;
        private FieldInfo hpLevelField;
        private FieldInfo maxMpLevelField;
        private FieldInfo mpRegenLevelField;
        private FieldInfo advancementTierField;
        private FieldInfo miniSlotsUnlockedField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;
        private PropertyInfo mpRegenProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo combatPowerProperty;

        private FieldInfo critChanceLevelField;
        private FieldInfo critDamageLevelField;
        private FieldInfo goldGainLevelField;
        private FieldInfo hpRegenLevelField;
        private FieldInfo expGainLevelField;
        private FieldInfo equipmentGainLevelField;

        private FieldInfo miniAttackLevelField;
        private FieldInfo miniCritLevelField;
        private FieldInfo miniCritDamageLevelField;
        private FieldInfo eggsField;
        private FieldInfo hatchedMinisField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;
        private FieldInfo idleMessageField;

        private Canvas canvas;
        private RectTransform safeRoot;
        private GameObject mainPage;
        private GameObject menuPage;
        private GameObject contentHost;
        private GameObject bottomNavigation;
        private GameObject toastPanel;
        private Text toastText;
        private Text menuTitle;
        private Text menuResources;
        private Text playerTitle;
        private Text hpText;
        private Text mpText;
        private Text goldText;
        private Text diamondText;
        private Text fragmentText;
        private Text stageTitle;
        private Text stageProgressText;
        private Text autoBossText;
        private Text globalAutoText;
        private Text combatPowerText;
        private Image hpFill;
        private Image mpFill;
        private Image stageFill;
        private Button bossButton;
        private readonly Text[] skillTexts = new Text[4];
        private readonly Image[] navBackgrounds = new Image[6];
        private readonly List<Action> refreshers = new List<Action>();
        private readonly Dictionary<IconKind, Sprite> icons = new Dictionary<IconKind, Sprite>();

        private readonly Page[] navigationPages =
        {
            Page.Skills, Page.Equipment, Page.Growth,
            Page.Advancement, Page.Pets, Page.Shop
        };

        private Font font;
        private Sprite whiteSprite;
        private Sprite roundedSprite;
        private Page currentPage;
        private int purchaseAmount = 1;
        private int stageMapWorld = 1;
        private float refreshTimer;
        private float toastTimer;
        private Rect lastSafeArea;

        private readonly Color cream = new Color(0.98f, 0.94f, 0.78f, 0.98f);
        private readonly Color card = new Color(0.95f, 0.91f, 0.79f, 0.98f);
        private readonly Color cardAlt = new Color(0.84f, 0.93f, 0.78f, 0.98f);
        private readonly Color strongGreen = new Color(0.17f, 0.43f, 0.22f, 0.99f);
        private readonly Color darkGreen = new Color(0.07f, 0.25f, 0.11f, 1f);
        private readonly Color goldColor = new Color(0.96f, 0.65f, 0.13f, 1f);
        private readonly Color red = new Color(0.88f, 0.22f, 0.18f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.88f, 1f);
        private readonly Color brown = new Color(0.18f, 0.11f, 0.05f, 1f);
        private readonly Color locked = new Color(0.58f, 0.60f, 0.54f, 0.90f);
        private readonly Color pageBackground = new Color(0.92f, 0.94f, 0.84f, 1f);

        public int BottomMenuCount => 6;
        public bool UsesSimplifiedGrowthMenu => true;
        public bool UsesGlobalSkillAuto => true;
        public bool HasTopStageSelector => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutMobileCanvasPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMobileCanvasPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutMobileCanvasPrototype>();
        }

        private IEnumerator Start()
        {
            yield return null;
            BindSystems();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                yield break;
            }

            stageMapWorld = stageFlow.World;
            CreateAssets();
            EnsureEventSystem();
            BuildCanvas();
            ShowPage(Page.Main);
            RefreshAll();
        }

        private void BindSystems()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            skillManager = FindFirstObjectByType<SkillManagementPrototype>();
            idle = FindFirstObjectByType<IdleSystemsPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            saveBridge = FindFirstObjectByType<PrototypeSaveBridge>();
            swords = FindFirstObjectByType<SwordProgressionPrototype>();
            if (arena == null) return;

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            diamondsField = arenaType.GetField("diamonds", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            attackLevelField = arenaType.GetField("attackLevel", PrivateInstance);
            hpLevelField = arenaType.GetField("hpLevel", PrivateInstance);
            maxMpLevelField = arenaType.GetField("maxMpLevel", PrivateInstance);
            mpRegenLevelField = arenaType.GetField("mpRegenLevel", PrivateInstance);
            advancementTierField = arenaType.GetField("advancementTier", PrivateInstance);
            miniSlotsUnlockedField = arenaType.GetField("miniSlotsUnlocked", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            maxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            maxMpProperty = arenaType.GetProperty("PlayerMaxMp", PrivateInstance);
            mpRegenProperty = arenaType.GetProperty("PlayerMpRegen", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            combatPowerProperty = arenaType.GetProperty("CombatPower", PrivateInstance);

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                critChanceLevelField = growthType.GetField("critChanceLevel", PrivateInstance);
                critDamageLevelField = growthType.GetField("critDamageLevel", PrivateInstance);
                goldGainLevelField = growthType.GetField("goldGainLevel", PrivateInstance);
                hpRegenLevelField = growthType.GetField("hpRegenLevel", PrivateInstance);
                expGainLevelField = growthType.GetField("expGainLevel", PrivateInstance);
                equipmentGainLevelField = growthType.GetField("equipmentGainLevel", PrivateInstance);
            }

            if (idle != null)
            {
                Type idleType = typeof(IdleSystemsPrototype);
                miniAttackLevelField = idleType.GetField("miniAttackLevel", PrivateInstance);
                miniCritLevelField = idleType.GetField("miniCritLevel", PrivateInstance);
                miniCritDamageLevelField = idleType.GetField("miniCritDamageLevel", PrivateInstance);
                eggsField = idleType.GetField("eggs", PrivateInstance);
                hatchedMinisField = idleType.GetField("hatchedMinis", PrivateInstance);
                incubatingField = idleType.GetField("incubating", PrivateInstance);
                incubationRemainingField = idleType.GetField("incubationRemaining", PrivateInstance);
                idleMessageField = idleType.GetField("systemMessage", PrivateInstance);
            }
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);

            whiteSprite = SolidSprite();
            roundedSprite = CreateRoundedSprite();
            foreach (IconKind kind in Enum.GetValues(typeof(IconKind)))
                icons[kind] = CreateIconSprite(kind);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject root = new GameObject("PeanutWarriorEventSystem");
            DontDestroyOnLoad(root);
            root.AddComponent<EventSystem>();
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Peanut Warrior Mobile Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            safeRoot = CreateRect(canvasObject.transform, "Safe Area", 0f, 0f, ReferenceWidth, ReferenceHeight);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
            ApplySafeArea();

            mainPage = CreateRect(safeRoot, "Main HUD", 0f, 0f, ReferenceWidth, ReferenceHeight).gameObject;
            menuPage = CreateRect(safeRoot, "Menu Page", 0f, 0f, ReferenceWidth, ReferenceHeight).gameObject;
            BuildMainHud();
            BuildMenuFrame();
            BuildBottomNavigation();
            BuildToast();
        }

        private void BuildMainHud()
        {
            GameObject player = Panel(mainPage.transform, "Player", 16f, 16f, 326f, 112f, cream);
            IconImage(player.transform, IconKind.Growth, 14f, 14f, 52f, 52f, strongGreen);
            playerTitle = Label(player.transform, string.Empty, 76f, 6f, 232f, 34f, 22, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            hpFill = Bar(player.transform, 76f, 44f, 232f, 22f, red, out hpText);
            mpFill = Bar(player.transform, 76f, 76f, 232f, 22f, blue, out mpText);

            GameObject resources = Panel(mainPage.transform, "Resources", 382f, 16f, 568f, 58f, cream);
            goldText = ResourceCell(resources.transform, 0f, 190f, IconKind.Gold, goldColor);
            diamondText = ResourceCell(resources.transform, 190f, 188f, IconKind.Material, blue);
            fragmentText = ResourceCell(resources.transform, 378f, 190f, IconKind.Skill, strongGreen);

            IconButton(mainPage.transform, IconKind.Settings, string.Empty, 1304f, 16f, 68f, 58f,
                () => ShowPage(Page.Settings), cream, strongGreen);

            GameObject stage = Panel(mainPage.transform, "Stage", 400f, 84f, 650f, 92f, cream);
            IconImage(stage.transform, IconKind.Stage, 12f, 10f, 42f, 42f, strongGreen);
            stageTitle = Label(stage.transform, string.Empty, 62f, 4f, 296f, 38f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            IconButton(stage.transform, IconKind.Stage, "선택", 360f, 8f, 112f, 36f,
                () => ShowPage(Page.StageSelect), cardAlt, strongGreen, true);
            Button bossAuto = IconButton(stage.transform, IconKind.Auto, string.Empty, 478f, 8f, 94f, 36f,
                ToggleAutoBoss, cardAlt, strongGreen, true);
            autoBossText = bossAuto.GetComponentInChildren<Text>();
            bossButton = IconButton(stage.transform, IconKind.Attack, "균왕", 578f, 8f, 60f, 72f,
                TryStartBoss, red, Color.white, false);
            stageFill = Bar(stage.transform, 62f, 52f, 500f, 24f, goldColor, out stageProgressText);

            GameObject status = Panel(mainPage.transform, "Status", 16f, 742f, 218f, 72f, cream);
            combatPowerText = Label(status.transform, string.Empty, 12f, 5f, 194f, 62f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);

            BuildSkillDock();
            BuildGlobalAutoButton();
        }

        private void BuildSkillDock()
        {
            GameObject dock = Panel(mainPage.transform, "Active Skills", 780f, 600f, 438f, 136f, cream);
            Label(dock.transform, "사용 중인 SKILL", 12f, 4f, 414f, 25f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < 4; i++)
            {
                GameObject slot = Panel(dock.transform, "Skill Slot", 10f + i * 106f, 34f, 98f, 90f, cardAlt);
                IconImage(slot.transform, IconKind.Skill, 29f, 7f, 40f, 40f, strongGreen);
                skillTexts[i] = Label(slot.transform, string.Empty, 5f, 47f, 88f, 38f, 12, brown, TextAnchor.MiddleCenter, FontStyle.Bold);
                Button button = slot.AddComponent<Button>();
                button.onClick.AddListener(() => ShowPage(Page.Skills));
            }
        }

        private void BuildGlobalAutoButton()
        {
            Button button = IconButton(mainPage.transform, IconKind.Auto, string.Empty,
                1230f, 600f, 142f, 136f, skillManager != null ? skillManager.ToggleGlobalAuto : (Action)null,
                strongGreen, Color.white, false);
            globalAutoText = button.GetComponentInChildren<Text>();
        }

        private void BuildBottomNavigation()
        {
            bottomNavigation = Panel(safeRoot, "Bottom Navigation", 238f, 742f, 1134f, 72f,
                new Color(0.12f, 0.28f, 0.15f, 0.98f));
            string[] names = { "SKILL", "장비", "성장", "전직", "펫", "상점" };
            IconKind[] kinds =
            {
                IconKind.Skill, IconKind.Equipment, IconKind.Growth,
                IconKind.Advancement, IconKind.Pet, IconKind.Shop
            };

            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                Page target = navigationPages[i];
                Button button = NavButton(bottomNavigation.transform, kinds[i], names[i],
                    6f + i * 188f, 5f, 182f, 62f, () => ShowPage(target));
                navBackgrounds[index] = button.GetComponent<Image>();
            }
        }

        private void BuildMenuFrame()
        {
            Image background = menuPage.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.color = pageBackground;

            Panel(menuPage.transform, "Header", 0f, 0f, ReferenceWidth, 82f, strongGreen);
            IconButton(menuPage.transform, IconKind.Back, string.Empty, 16f, 14f, 58f, 52f,
                () => ShowPage(Page.Main), cream, strongGreen);
            menuTitle = Label(menuPage.transform, string.Empty, 92f, 10f, 560f, 60f, 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            menuResources = Label(menuPage.transform, string.Empty, 720f, 10f, 560f, 60f, 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            IconButton(menuPage.transform, IconKind.Settings, string.Empty, 1304f, 14f, 68f, 52f,
                () => ShowPage(Page.Settings), cream, strongGreen);
            contentHost = CreateRect(menuPage.transform, "Content", 0f, 82f, ReferenceWidth, 650f).gameObject;
        }

        private void BuildToast()
        {
            toastPanel = Panel(safeRoot, "Toast", 444f, 672f, 500f, 56f, strongGreen);
            toastText = Label(toastPanel.transform, string.Empty, 14f, 5f, 472f, 46f, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            toastPanel.SetActive(false);
        }

        private void Update()
        {
            if (canvas == null) return;
            if (lastSafeArea != Screen.safeArea) ApplySafeArea();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastPanel.SetActive(false);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.15f;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (arena == null || stageFlow == null || playerTitle == null) return;

            int playerLevel = growth == null ? Mathf.Max(1, CombatPower / 25) : growth.PlayerLevel;
            playerTitle.text = $"땅콩전사  Lv.{playerLevel:N0}";
            SetBar(hpFill, hpText, PlayerHp, MaxHp, $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            SetBar(mpFill, mpText, PlayerMp, MaxMp, $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
            goldText.text = $"골드  {Gold:N0}";
            diamondText.text = $"다이아  {Diamonds:N0}";
            fragmentText.text = $"조각  {Fragments:N0}";
            stageTitle.text = $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}";
            SetBar(stageFill, stageProgressText, stageFlow.MonsterKills, StageFlowController.RequiredKills,
                $"균왕 도전 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            autoBossText.text = stageFlow.AutoChallenge ? "보스 AUTO" : "보스 수동";
            bossButton.interactable = stageFlow.CanChallengeBoss;
            combatPowerText.text = $"전투력 {CombatPower:N0}\n처치 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}";

            int skillOffset = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            int[] levels = skillManager?.SkillLevels;
            float[] cooldowns = skillManager?.Cooldowns;
            for (int i = 0; i < skillTexts.Length; i++)
            {
                int index = skillOffset + i;
                int level = levels != null && index < levels.Length ? levels[index] : 1;
                float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                skillTexts[i].text = $"Lv.{level}\n{(cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "READY")}";
            }

            if (globalAutoText != null)
                globalAutoText.text = skillManager != null && skillManager.GlobalAutoEnabled
                    ? "SKILL\nAUTO ON"
                    : "SKILL\nAUTO OFF";

            if (menuResources != null)
                menuResources.text = $"골드 {Gold:N0}   다이아 {Diamonds:N0}   조각 {Fragments:N0}";

            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
            RefreshNavigation();
        }

        private void ShowPage(Page page)
        {
            currentPage = page;
            mainPage.SetActive(page == Page.Main);
            menuPage.SetActive(page != Page.Main);
            bottomNavigation.SetActive(page != Page.StageSelect && page != Page.Settings || true);

            if (page == Page.Main)
            {
                RefreshNavigation();
                return;
            }

            menuTitle.text = PageTitle(page);
            RebuildContent();
        }

        private void RebuildContent()
        {
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            switch (currentPage)
            {
                case Page.Skills: BuildSkillPage(); break;
                case Page.Equipment: BuildEquipmentPage(); break;
                case Page.Growth: BuildGrowthPage(); break;
                case Page.Advancement: BuildAdvancementPage(); break;
                case Page.Pets: BuildPetPage(); break;
                case Page.Shop: BuildShopPage(); break;
                case Page.StageSelect: BuildStageSelectPage(); break;
                case Page.Settings: BuildSettingsPage(); break;
            }
            RefreshAll();
        }

        private void BuildGrowthPage()
        {
            GameObject summary = Panel(contentHost.transform, "Growth Summary", 20f, 14f, 1348f, 86f, cream);
            IconImage(summary.transform, IconKind.Growth, 16f, 16f, 52f, 52f, strongGreen);
            Text summaryText = Label(summary.transform, string.Empty, 82f, 8f, 560f, 68f, 17, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            refreshers.Add(() =>
            {
                long need = growth == null ? 0L : growth.ExperienceToNextLevel;
                long current = growth == null ? 0L : growth.CurrentExperience;
                summaryText.text = growth == null
                    ? $"전투력 {CombatPower:N0}"
                    : $"Lv.{growth.PlayerLevel:N0} · EXP {current:N0}/{need:N0}\n전투력 {CombatPower:N0}";
            });

            Label(summary.transform, "한 번에 강화", 650f, 12f, 170f, 58f, 17, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                IconButton(summary.transform, IconKind.Growth, $"×{amount}", 820f + i * 166f, 14f, 152f, 56f,
                    () => { purchaseAmount = amount; RebuildContent(); },
                    purchaseAmount == amount ? goldColor : cardAlt,
                    purchaseAmount == amount ? Color.white : darkGreen, true);
            }

            Transform content = ScrollContent(contentHost.transform, 20f, 112f, 1348f, 528f);
            StatRow(content, IconKind.Attack, "공격력", () => $"현재 공격력 {AttackDamage:N1}", attackLevelField, arena, 20L);
            StatRow(content, IconKind.Heart, "HP", () => $"최대 HP {MaxHp:N0}", hpLevelField, arena, 25L);
            StatRow(content, IconKind.Recovery, "HP 회복량", () => $"초당 {growth?.HpRecoveryPerSecond ?? 0f:0.0}", hpRegenLevelField, growth, 40L);
            StatRow(content, IconKind.Mana, "MP", () => $"최대 MP {MaxMp:N0}", maxMpLevelField, arena, 30L);
            StatRow(content, IconKind.Recovery, "MP 회복량", () => $"초당 {PlayerMpRegen:0.0}", mpRegenLevelField, arena, 35L);
            StatRow(content, IconKind.Crit, "치명타", () => $"{(growth?.CriticalChance ?? 0f) * 100f:0}% · 최대 100%",
                critChanceLevelField, growth, 45L, 49);
            StatRow(content, IconKind.CritDamage, "치명타 피해량", () => $"{(growth?.CriticalDamageMultiplier ?? 1f) * 100f:0}%",
                critDamageLevelField, growth, 55L);
            StatRow(content, IconKind.Experience, "경험치 획득량 증가", () => $"+{((growth?.ExperienceMultiplier ?? 1f) - 1f) * 100f:0}%",
                expGainLevelField, growth, 70L);
            StatRow(content, IconKind.Gold, "골드 획득량 증가", () => $"+{((growth?.GoldMultiplier ?? 1f) - 1f) * 100f:0}%",
                goldGainLevelField, growth, 65L);
            StatRow(content, IconKind.Material, "장비 강화 획득량 증가", () => $"+{((growth?.EquipmentMaterialMultiplier ?? 1f) - 1f) * 100f:0}%",
                equipmentGainLevelField, growth, 80L);
        }

        private void BuildSkillPage()
        {
            GameObject autoCard = Panel(contentHost.transform, "Global Skill Auto", 20f, 14f, 1348f, 98f, cream);
            IconImage(autoCard.transform, IconKind.Auto, 18f, 18f, 62f, 62f, strongGreen);
            Label(autoCard.transform, "모든 스킬 자동 사용", 96f, 10f, 430f, 38f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(autoCard.transform, "개별 AUTO는 사용하지 않으며 이 버튼 하나로 전체 스킬을 제어합니다.",
                96f, 48f, 760f, 34f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Button autoButton = IconButton(autoCard.transform, IconKind.Auto, string.Empty, 1050f, 18f, 270f, 62f,
                ToggleGlobalSkillAuto, strongGreen, Color.white, true);
            Text autoLabel = autoButton.GetComponentInChildren<Text>();
            refreshers.Add(() => autoLabel.text = skillManager != null && skillManager.GlobalAutoEnabled ? "전체 AUTO ON" : "전체 AUTO OFF");

            Transform content = ScrollContent(contentHost.transform, 20f, 124f, 1348f, 516f);
            Section(content, "SKILL 구성", () => "스킬 종류와 장착 방식은 다음 설계 단계에서 다시 구성합니다. 현재는 기존 8개 스킬의 레벨만 유지합니다.");
            for (int i = 0; i < 8; i++) SkillRow(content, i);
        }

        private void BuildEquipmentPage()
        {
            GameObject header = Panel(contentHost.transform, "Equipment Header", 20f, 14f, 1348f, 98f, cream);
            IconImage(header.transform, IconKind.Equipment, 18f, 18f, 62f, 62f, strongGreen);
            Label(header.transform, "장비", 96f, 8f, 300f, 40f, 24, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text material = Label(header.transform, string.Empty, 96f, 48f, 720f, 34f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            refreshers.Add(() => material.text = $"장비 강화 재료 {growth?.EquipmentEnhancementMaterials ?? 0:N0} · 세부 장비 구성은 재설계 전 상태");
            Label(header.transform, "등급 순서  레어 → 에픽 → 유니크 → 레전드",
                800f, 20f, 510f, 54f, 17, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);

            Transform content = ScrollContent(contentHost.transform, 20f, 124f, 1348f, 516f);
            for (int i = 0; i < 4; i++) EquipmentRow(content, i);
        }

        private void BuildAdvancementPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 14f, 1348f, 626f);
            Section(content, "땅콩전사 전직", AdvancementStatus);
            Section(content, "다음 전직 조건", AdvancementRequirements);
            ActionRow(content, IconKind.Advancement, "전직 시도",
                () => "조건을 모두 충족하면 골드와 다이아를 소모합니다.", "전직", TryAdvance);
            Section(content, "전직 효과",
                () => "기본 능력치 증가 · 기본 공격 타수 증가 · 스킬 공격 횟수 증가 · 2차 전직 시 펫 3슬롯 해금");
        }

        private void BuildPetPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 14f, 1348f, 626f);
            Section(content, "펫", PetStatus);
            MiniRow(content, IconKind.Attack, "펫 공격력", miniAttackLevelField, 80L);
            MiniRow(content, IconKind.Crit, "펫 치명타 확률", miniCritLevelField, 100L);
            MiniRow(content, IconKind.CritDamage, "펫 치명타 피해", miniCritDamageLevelField, 120L);
            ActionRow(content, IconKind.Pet, "펫 알 구매", () => $"보유 알 {ReadInt(eggsField, idle)} · 다이아 3개",
                "구매", () => InvokePrivate(idle, "BuyEgg", null));
            ActionRow(content, IconKind.Pet, "알 부화", IncubationStatus,
                "부화", () => InvokePrivate(idle, "StartIncubation", null));
            Section(content, "최근 펫 알림", IdleMessage);
        }

        private void BuildShopPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 14f, 1348f, 626f);
            Section(content, "땅콩 상점", () => shop == null ? "상점 초기화 대기" : shop.ShopMessage);
            ActionRow(content, IconKind.Shop, "오늘의 보급", () => $"연속 접속 {(shop == null ? 0 : shop.DailyStreak)}일",
                "받기", () => InvokePrivate(shop, "ClaimDailyReward", null));
            ActionRow(content, IconKind.Equipment, "사냥 검 소환", () => "다이아 5개 · 레어/에픽/유니크/레전드",
                "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            ActionRow(content, IconKind.Equipment, "균왕 검 소환", () => "다이아 5개 · 균왕 장착 슬롯",
                "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }));
            ActionRow(content, IconKind.Pet, "펫 알", () => "다이아 3개 · 펫 화면에서 부화",
                "구매", () => InvokePrivate(shop, "BuyEgg", null));
        }

        private void BuildStageSelectPage()
        {
            GameObject worldBar = Panel(contentHost.transform, "World Selector", 20f, 14f, 1348f, 80f, cream);
            IconButton(worldBar.transform, IconKind.Back, "이전 월드", 16f, 12f, 180f, 56f,
                () => ChangeStageMapWorld(-1), cardAlt, darkGreen, true);
            Text worldTitle = Label(worldBar.transform, string.Empty, 220f, 8f, 908f, 64f, 24, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            IconButton(worldBar.transform, IconKind.Stage, "다음 월드", 1152f, 12f, 180f, 56f,
                () => ChangeStageMapWorld(1), cardAlt, darkGreen, true);
            refreshers.Add(() => worldTitle.text = $"월드 {stageMapWorld} · 최고 해금 {FormatStage(HighestGlobalStage)}");

            GameObject grid = Panel(contentHost.transform, "Stage Grid", 20f, 106f, 1348f, 534f, new Color(1f, 1f, 1f, 0.40f));
            const int columns = 6;
            const float cellWidth = 198f;
            const float cellHeight = 84f;
            for (int localStage = 1; localStage <= StageFlowController.StagesPerWorld; localStage++)
            {
                int stageNumber = localStage;
                int globalStage = (stageMapWorld - 1) * StageFlowController.StagesPerWorld + localStage;
                bool unlocked = globalStage <= HighestGlobalStage;
                bool selected = stageMapWorld == stageFlow.World && localStage == stageFlow.Stage;
                int column = (localStage - 1) % columns;
                int row = (localStage - 1) / columns;
                Color background = !unlocked ? locked : selected ? goldColor : cardAlt;
                Button button = IconButton(grid.transform, IconKind.Stage,
                    unlocked ? $"{stageMapWorld}-{localStage}" : "잠김",
                    24f + column * 216f, 24f + row * 98f, cellWidth, cellHeight,
                    () => SelectStage(stageMapWorld, stageNumber), background,
                    selected ? Color.white : darkGreen, true);
                button.interactable = unlocked && stageFlow.Phase != StageFlowPhase.BossBattle;
            }
        }

        private void BuildSettingsPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 14f, 1348f, 626f);
            Section(content, "설정", () => $"목표 {Application.targetFrameRate} FPS · 가로 화면 · 안전영역 자동 적용");
            ActionRow(content, IconKind.Settings, "60 FPS", () => "일반 플레이", "적용", () => SetPerformance(60));
            ActionRow(content, IconKind.Settings, "30 FPS 절전", () => "발열과 배터리 사용 감소", "적용", () => SetPerformance(30));
            ActionRow(content, IconKind.Settings, "즉시 저장", () => "스테이지·자원·성장·장비·펫 데이터", "저장",
                () => InvokePrivate(saveBridge, "Save", null));
        }

        private void StatRow(Transform content, IconKind icon, string title, Func<string> detail,
            FieldInfo field, object target, long baseCost, int maxLevel = 0)
        {
            GameObject row = Row(content, 88f, card);
            IconImage(row.transform, icon, 16f, 16f, 56f, 56f, strongGreen);
            Label(row.transform, title, 88f, 8f, 380f, 36f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text info = Label(row.transform, string.Empty, 88f, 42f, 610f, 34f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Text levelText = Label(row.transform, string.Empty, 720f, 15f, 170f, 54f, 17, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button upgrade = IconButton(row.transform, IconKind.Growth, string.Empty, 930f, 15f, 370f, 56f,
                () => UpgradeStat(field, target, baseCost, title, maxLevel), goldColor, Color.white, true);
            Text upgradeText = upgrade.GetComponentInChildren<Text>();

            Action refresh = () =>
            {
                int level = ReadLevel(field, target);
                int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - level)) : purchaseAmount;
                info.text = detail == null ? string.Empty : detail();
                levelText.text = maxLevel > 0 && level >= maxLevel ? "MAX" : $"Lv.{level:N0}";
                if (amount <= 0)
                {
                    upgrade.interactable = false;
                    upgradeText.text = "최대치";
                }
                else
                {
                    long cost = UpgradeCost(baseCost, level, amount);
                    upgrade.interactable = true;
                    upgradeText.text = $"+{amount} 강화  {cost:N0}G";
                }
            };
            refreshers.Add(refresh);
            refresh();
        }

        private void SkillRow(Transform content, int index)
        {
            GameObject row = Row(content, 84f, index % 2 == 0 ? card : cardAlt);
            IconImage(row.transform, IconKind.Skill, 16f, 14f, 56f, 56f, strongGreen);
            Text info = Label(row.transform, string.Empty, 88f, 8f, 720f, 68f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button upgrade = IconButton(row.transform, IconKind.Growth, string.Empty, 970f, 15f, 330f, 54f,
                () => UpgradeSkill(index), goldColor, Color.white, true);
            Text upgradeText = upgrade.GetComponentInChildren<Text>();
            Action refresh = () =>
            {
                int[] levels = skillManager?.SkillLevels;
                float[] cooldowns = skillManager?.Cooldowns;
                int level = levels != null && index < levels.Length ? levels[index] : 1;
                float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                string category = index < 4 ? "사냥" : "균왕";
                info.text = $"{skillManager?.GetSkillName(index) ?? "SKILL"} · Lv.{level}\n{category}용 · 쿨타임 {cooldown:0.0}초";
                upgradeText.text = $"강화  {skillManager?.GetUpgradeCost(index) ?? 0:N0}조각";
            };
            refreshers.Add(refresh);
            refresh();
        }

        private void EquipmentRow(Transform content, int element)
        {
            GameObject row = Row(content, 126f, element % 2 == 0 ? card : cardAlt);
            IconImage(row.transform, IconKind.Equipment, 18f, 25f, 72f, 72f, ElementColors[element]);
            Text info = Label(row.transform, string.Empty, 108f, 10f, 610f, 62f, 19, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text copies = Label(row.transform, string.Empty, 108f, 70f, 780f, 42f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            IconButton(row.transform, IconKind.Equipment, "사냥 장착", 930f, 18f, 170f, 46f,
                () => Equip(huntingElementField, element, "사냥 장비"), cream, darkGreen, true);
            IconButton(row.transform, IconKind.Equipment, "균왕 장착", 1110f, 18f, 190f, 46f,
                () => Equip(bossElementField, element, "균왕 장비"), cream, darkGreen, true);

            Action refresh = () =>
            {
                SwordProgressionPrototype.SwordRarity rarity = swords == null
                    ? SwordProgressionPrototype.SwordRarity.None
                    : swords.GetHighestRarity(element);
                int level = swords == null ? 1 : swords.GetLevel(element);
                info.text = $"{ElementNames[element]} 검 · {SwordProgressionPrototype.RarityName(rarity)} · Lv.{level}\n전투 피해 ×{(swords == null ? 1f : swords.GetDamageMultiplier(element)):0.000}";
                copies.text = swords == null
                    ? "장비 시스템 초기화 대기"
                    : $"레어 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Rare)}  ·  에픽 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Epic)}  ·  유니크 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Unique)}  ·  레전드 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Legend)}";
            };
            refreshers.Add(refresh);
            refresh();
        }

        private void MiniRow(Transform content, IconKind icon, string title, FieldInfo field, long baseCost)
        {
            ActionRow(content, icon, title, () =>
            {
                int level = ReadLevel(field, idle);
                return $"Lv.{level} · 다음 비용 {baseCost * level:N0}G";
            }, "강화", () =>
            {
                int level = ReadLevel(field, idle);
                long cost = baseCost * level;
                if (!SpendGold(cost))
                {
                    Toast($"골드 부족 · {cost:N0}G 필요");
                    return;
                }
                field?.SetValue(idle, level + 1);
                Toast($"{title} Lv.{level + 1}");
            });
        }

        private void Section(Transform content, string title, Func<string> detail)
        {
            GameObject row = Row(content, 104f, cardAlt);
            Label(row.transform, title, 20f, 8f, 330f, 34f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, string.Empty, 370f, 8f, 930f, 84f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Action refresh = () => body.text = detail == null ? string.Empty : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void ActionRow(Transform content, IconKind icon, string title, Func<string> detail,
            string actionName, Action action)
        {
            GameObject row = Row(content, 84f, card);
            IconImage(row.transform, icon, 16f, 14f, 56f, 56f, strongGreen);
            Label(row.transform, title, 88f, 8f, 350f, 68f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, string.Empty, 450f, 8f, 570f, 68f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            IconButton(row.transform, icon, actionName, 1080f, 15f, 220f, 54f,
                () => { action?.Invoke(); RefreshAll(); }, goldColor, Color.white, true);
            Action refresh = () => body.text = detail == null ? string.Empty : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void UpgradeStat(FieldInfo field, object target, long baseCost, string title, int maxLevel)
        {
            if (field == null || target == null)
            {
                Toast($"{title} 연결 대기");
                return;
            }

            int level = ReadLevel(field, target);
            int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - level)) : purchaseAmount;
            if (amount <= 0)
            {
                Toast($"{title} 최대치");
                return;
            }

            long cost = UpgradeCost(baseCost, level, amount);
            if (!SpendGold(cost))
            {
                Toast($"골드 부족 · {cost:N0}G 필요");
                return;
            }

            field.SetValue(target, level + amount);
            if (field == hpLevelField || field == maxMpLevelField)
                InvokePrivate(arena, "FullRestore", null);
            if (ReferenceEquals(target, growth)) growth.SaveNow();
            Toast($"{title} +{amount} 강화 완료");
        }

        private void UpgradeSkill(int index)
        {
            if (skillManager == null)
            {
                Toast("SKILL 시스템 초기화 대기");
                return;
            }
            skillManager.UpgradeSkill(index);
            Toast(skillManager.LastMessage);
        }

        private void ToggleGlobalSkillAuto()
        {
            if (skillManager == null) return;
            skillManager.ToggleGlobalAuto();
            Toast(skillManager.LastMessage);
        }

        private void TryAdvance()
        {
            int before = ReadInt(advancementTierField, arena);
            InvokePrivate(arena, "TryAdvance", null);
            int after = ReadInt(advancementTierField, arena);
            Toast(after > before ? $"전직 성공 · {after}단계" : "전직 조건을 확인하십시오");
        }

        private void Equip(FieldInfo field, int element, string slot)
        {
            if (field == null) return;
            field.SetValue(arena, Enum.ToObject(field.FieldType, element));
            Toast($"{slot}에 {ElementNames[element]} 검 장착");
        }

        private void ToggleAutoBoss()
        {
            stageFlow.SetAutoChallenge(!stageFlow.AutoChallenge);
            Toast(stageFlow.AutoChallenge ? "균왕 자동 도전 ON" : "균왕 자동 도전 OFF");
        }

        private void TryStartBoss()
        {
            if (!stageFlow.TryStartBossBattle())
                Toast($"일반 몬스터 {StageFlowController.RequiredKills}마리를 먼저 처치해야 합니다");
        }

        private void ChangeStageMapWorld(int direction)
        {
            int highestWorld = (HighestGlobalStage - 1) / StageFlowController.StagesPerWorld + 1;
            stageMapWorld = Mathf.Clamp(stageMapWorld + direction, 1, highestWorld);
            RebuildContent();
        }

        private void SelectStage(int world, int stage)
        {
            if (stageFlow.Phase == StageFlowPhase.BossBattle)
            {
                Toast("균왕전 중에는 스테이지를 변경할 수 없습니다");
                return;
            }

            int global = (world - 1) * StageFlowController.StagesPerWorld + stage;
            if (global > HighestGlobalStage)
            {
                Toast("아직 해금되지 않은 스테이지입니다");
                return;
            }

            stageFlow.SelectStage(world, stage);
            stageMapWorld = world;
            Toast($"{world}-{stage} 스테이지로 이동");
            ShowPage(Page.Main);
        }

        private void SetPerformance(int fps)
        {
            Application.targetFrameRate = fps;
            QualitySettings.antiAliasing = fps >= 60 ? 2 : 0;
            Toast($"{fps} FPS 모드 적용");
        }

        private void InvokePrivate(object target, string methodName, object[] arguments)
        {
            if (target == null)
            {
                Toast("해당 시스템 초기화 대기");
                return;
            }

            object[] args = arguments ?? Array.Empty<object>();
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++) types[i] = args[i].GetType();
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance, null, types, null);
            if (method == null)
            {
                Toast($"기능 연결 실패 · {methodName}");
                return;
            }

            try
            {
                method.Invoke(target, args.Length == 0 ? null : args);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception, target as UnityEngine.Object);
                Toast("기능 실행 오류");
            }
        }

        private void Toast(string message)
        {
            if (toastPanel == null) return;
            toastText.text = message;
            toastTimer = 2.4f;
            toastPanel.SetActive(true);
        }

        private string AdvancementStatus()
        {
            int tier = ReadInt(advancementTierField, arena);
            bool unlocked = miniSlotsUnlockedField != null && Convert.ToBoolean(miniSlotsUnlockedField.GetValue(arena));
            return $"전직 {tier}단계 · 기본 공격 {tier + 1}타 · 펫 {(unlocked ? "3/3 해금" : "잠김")}";
        }

        private string AdvancementRequirements()
        {
            int tier = ReadInt(advancementTierField, arena);
            if (tier >= 2) return "현재 프로토타입 최고 전직 단계";
            return tier == 0
                ? "전체 스테이지 2 · 전투력 180 · 150G · 다이아 5"
                : "전체 스테이지 4 · 전투력 420 · 500G · 다이아 15";
        }

        private string PetStatus()
        {
            bool unlocked = miniSlotsUnlockedField != null && Convert.ToBoolean(miniSlotsUnlockedField.GetValue(arena));
            return unlocked
                ? $"펫 3/3 활동 · 부화 도감 {ReadInt(hatchedMinisField, idle)}"
                : "2차 전직 후 펫 3슬롯 해금";
        }

        private string IncubationStatus()
        {
            bool active = incubatingField != null && idle != null && Convert.ToBoolean(incubatingField.GetValue(idle));
            return active
                ? $"부화 중 · {Mathf.CeilToInt(ReadFloat(incubationRemainingField, idle))}초"
                : $"대기 · 보유 알 {ReadInt(eggsField, idle)}";
        }

        private string IdleMessage()
        {
            if (idle == null || idleMessageField == null) return "펫 시스템 초기화 대기";
            return idleMessageField.GetValue(idle) as string ?? "최근 알림 없음";
        }

        private void RefreshNavigation()
        {
            for (int i = 0; i < navBackgrounds.Length; i++)
            {
                if (navBackgrounds[i] == null) continue;
                navBackgrounds[i].color = currentPage == navigationPages[i] ? goldColor : cardAlt;
            }
        }

        private GameObject Row(Transform content, float height, Color color)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(content, false);
            Image image = row.GetComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return row;
        }

        private Transform ScrollContent(Transform parent, float x, float y, float width, float height)
        {
            GameObject scrollObject = Panel(parent, "Scroll", x, y, width, height, new Color(1f, 1f, 1f, 0.32f));
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 36f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect(scrollObject.transform, "Viewport", 8f, 8f, width - 16f, height - 16f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = roundedSprite;
            viewportImage.type = Image.Type.Sliced;
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup group = contentObject.GetComponent<VerticalLayoutGroup>();
            group.spacing = 10f;
            group.padding = new RectOffset(8, 8, 8, 8);
            group.childControlHeight = true;
            group.childControlWidth = true;
            group.childForceExpandHeight = false;
            group.childForceExpandWidth = true;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private GameObject Panel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect.gameObject;
        }

        private RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
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

        private Text Label(Transform parent, string value, float x, float y, float width, float height,
            int size, Color color, TextAnchor alignment, FontStyle style)
        {
            RectTransform rect = CreateRect(parent, "Text", x, y, width, height);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private Image IconImage(Transform parent, IconKind kind, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateRect(parent, "Icon", x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = icons[kind];
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Button IconButton(Transform parent, IconKind icon, string value, float x, float y, float width, float height,
            Action action, Color color, Color textColor, bool horizontal = false)
        {
            GameObject go = Panel(parent, "Button", x, y, width, height, color);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
            colors.pressedColor = new Color(0.76f, 0.86f, 0.68f, 1f);
            colors.disabledColor = new Color(0.60f, 0.60f, 0.60f, 0.72f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());

            if (horizontal)
            {
                float iconSize = Mathf.Min(height - 16f, 38f);
                IconImage(go.transform, icon, 10f, (height - iconSize) * 0.5f, iconSize, iconSize, textColor);
                Label(go.transform, value, 54f, 3f, width - 60f, height - 6f,
                    14, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else
            {
                float iconSize = Mathf.Min(width - 22f, height * 0.46f);
                IconImage(go.transform, icon, (width - iconSize) * 0.5f, 9f, iconSize, iconSize, textColor);
                Label(go.transform, value, 5f, iconSize + 12f, width - 10f, height - iconSize - 15f,
                    14, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            return button;
        }

        private Button NavButton(Transform parent, IconKind icon, string value, float x, float y, float width, float height, Action action)
        {
            Button button = IconButton(parent, icon, value, x, y, width, height, action, cardAlt, darkGreen, true);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        private Image Bar(Transform parent, float x, float y, float width, float height, Color color, out Text text)
        {
            GameObject back = Panel(parent, "Bar Back", x, y, width, height, new Color(0.16f, 0.18f, 0.13f, 0.88f));
            RectTransform fillRect = CreateRect(back.transform, "Fill", 2f, 2f, width - 4f, height - 4f);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = roundedSprite;
            fill.type = Image.Type.Sliced;
            fill.color = color;
            text = Label(back.transform, string.Empty, 0f, 0f, width, height, 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return fill;
        }

        private Text ResourceCell(Transform parent, float x, float width, IconKind icon, Color iconColor)
        {
            IconImage(parent, icon, x + 14f, 12f, 34f, 34f, iconColor);
            return Label(parent, string.Empty, x + 52f, 0f, width - 58f, 58f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        private static void SetBar(Image fill, Text text, float current, float maximum, string label)
        {
            if (fill == null || text == null) return;
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            RectTransform rect = fill.rectTransform;
            RectTransform parent = rect.parent as RectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, (parent == null ? 0f : parent.rect.width - 4f) * ratio);
            rect.sizeDelta = size;
            text.text = label;
        }

        private void ApplySafeArea()
        {
            if (safeRoot == null) return;
            Rect area = Screen.safeArea;
            lastSafeArea = area;
            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Mathf.Max(1f, Screen.width);
            min.y /= Mathf.Max(1f, Screen.height);
            max.x /= Mathf.Max(1f, Screen.width);
            max.y /= Mathf.Max(1f, Screen.height);
            safeRoot.anchorMin = min;
            safeRoot.anchorMax = max;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private static long UpgradeCost(long baseCost, int startLevel, int amount)
        {
            long total = 0L;
            for (int i = 0; i < amount; i++)
            {
                long next = baseCost * Math.Max(1, startLevel + i);
                if (long.MaxValue - total < next) return long.MaxValue;
                total += next;
            }
            return total;
        }

        private bool SpendGold(long amount)
        {
            if (goldField == null || Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private string PageTitle(Page page)
        {
            return page switch
            {
                Page.Skills => "SKILL",
                Page.Equipment => "장비",
                Page.Growth => "성장",
                Page.Advancement => "전직",
                Page.Pets => "펫",
                Page.Shop => "상점",
                Page.StageSelect => "스테이지 선택",
                Page.Settings => "설정",
                _ => "땅콩전사 키우기"
            };
        }

        private long Gold => ReadLong(goldField, arena);
        private long Fragments => ReadLong(fragmentsField, arena);
        private int Diamonds => ReadInt(diamondsField, arena);
        private float PlayerHp => ReadFloat(playerHpField, arena);
        private float PlayerMp => ReadFloat(playerMpField, arena);
        private float MaxHp => ReadProperty(maxHpProperty, arena, 1f);
        private float MaxMp => ReadProperty(maxMpProperty, arena, 1f);
        private float PlayerMpRegen => ReadProperty(mpRegenProperty, arena, 0f);
        private float AttackDamage => ReadProperty(attackDamageProperty, arena, 0f);
        private int CombatPower => combatPowerProperty == null
            ? Mathf.RoundToInt(AttackDamage * 10f)
            : Convert.ToInt32(combatPowerProperty.GetValue(arena));
        private int CurrentGlobalStage => (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
        private int HighestGlobalStage => Mathf.Max(CurrentGlobalStage, PlayerPrefs.GetInt(HighestStageKey, CurrentGlobalStage));

        private static int ReadLevel(FieldInfo field, object target)
        {
            if (field == null || target == null) return 1;
            return Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        }

        private static int ReadInt(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0;
            return Convert.ToInt32(field.GetValue(target));
        }

        private static long ReadLong(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0L;
            return Convert.ToInt64(field.GetValue(target));
        }

        private static float ReadFloat(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0f;
            return Convert.ToSingle(field.GetValue(target));
        }

        private static float ReadProperty(PropertyInfo property, object target, float fallback)
        {
            if (property == null || target == null) return fallback;
            return Convert.ToSingle(property.GetValue(target));
        }

        private static string FormatStage(int globalStage)
        {
            int world = (globalStage - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (globalStage - 1) % StageFlowController.StagesPerWorld + 1;
            return $"{world}-{stage}";
        }

        private static Sprite SolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "PeanutUiWhite";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 32;
            const float radius = 9f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PeanutRoundedPanel";
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, radius - Mathf.Min(x, size - 1 - x));
                    float dy = Mathf.Max(0f, radius - Mathf.Min(y, size - 1 - y));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.6f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }

        private static Sprite CreateIconSprite(IconKind kind)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PeanutIcon_" + kind;
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color white = Color.white;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    texture.SetPixel(x, y, clear);

            switch (kind)
            {
                case IconKind.Skill:
                    DrawLine(texture, 12, 32, 52, 32, 5, white);
                    DrawLine(texture, 32, 12, 32, 52, 5, white);
                    DrawLine(texture, 18, 18, 46, 46, 4, white);
                    DrawLine(texture, 46, 18, 18, 46, 4, white);
                    FillCircle(texture, 32, 32, 7, white);
                    break;
                case IconKind.Equipment:
                case IconKind.Attack:
                    DrawLine(texture, 15, 49, 47, 17, 7, white);
                    DrawLine(texture, 42, 13, 51, 22, 5, white);
                    DrawLine(texture, 17, 42, 27, 52, 5, white);
                    DrawLine(texture, 12, 52, 22, 42, 4, white);
                    break;
                case IconKind.Growth:
                    FillRect(texture, 10, 42, 18, 54, white);
                    FillRect(texture, 24, 32, 32, 54, white);
                    FillRect(texture, 38, 20, 46, 54, white);
                    DrawLine(texture, 12, 36, 50, 12, 4, white);
                    DrawLine(texture, 50, 12, 43, 13, 4, white);
                    DrawLine(texture, 50, 12, 47, 20, 4, white);
                    break;
                case IconKind.Advancement:
                    FillRect(texture, 12, 35, 52, 51, white);
                    FillTriangle(texture, new Vector2(12, 35), new Vector2(18, 14), new Vector2(28, 35), white);
                    FillTriangle(texture, new Vector2(24, 35), new Vector2(32, 10), new Vector2(40, 35), white);
                    FillTriangle(texture, new Vector2(36, 35), new Vector2(48, 14), new Vector2(52, 35), white);
                    break;
                case IconKind.Pet:
                    FillCircle(texture, 32, 38, 12, white);
                    FillCircle(texture, 17, 24, 7, white);
                    FillCircle(texture, 28, 18, 7, white);
                    FillCircle(texture, 40, 18, 7, white);
                    FillCircle(texture, 50, 26, 7, white);
                    break;
                case IconKind.Shop:
                    FillRect(texture, 14, 25, 50, 53, white);
                    DrawCircle(texture, 32, 25, 13, 4, white);
                    break;
                case IconKind.Stage:
                    DrawLine(texture, 17, 10, 17, 54, 5, white);
                    FillTriangle(texture, new Vector2(19, 12), new Vector2(50, 21), new Vector2(19, 32), white);
                    FillRect(texture, 10, 51, 27, 56, white);
                    break;
                case IconKind.Settings:
                    DrawCircle(texture, 32, 32, 15, 6, white);
                    FillCircle(texture, 32, 32, 6, clear);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        DrawLine(texture,
                            Mathf.RoundToInt(32 + Mathf.Cos(angle) * 18), Mathf.RoundToInt(32 + Mathf.Sin(angle) * 18),
                            Mathf.RoundToInt(32 + Mathf.Cos(angle) * 26), Mathf.RoundToInt(32 + Mathf.Sin(angle) * 26),
                            5, white);
                    }
                    break;
                case IconKind.Auto:
                    DrawLine(texture, 14, 23, 46, 23, 5, white);
                    DrawLine(texture, 46, 23, 39, 16, 5, white);
                    DrawLine(texture, 46, 23, 39, 30, 5, white);
                    DrawLine(texture, 50, 41, 18, 41, 5, white);
                    DrawLine(texture, 18, 41, 25, 34, 5, white);
                    DrawLine(texture, 18, 41, 25, 48, 5, white);
                    break;
                case IconKind.Heart:
                    FillCircle(texture, 23, 25, 12, white);
                    FillCircle(texture, 41, 25, 12, white);
                    FillTriangle(texture, new Vector2(12, 28), new Vector2(52, 28), new Vector2(32, 54), white);
                    break;
                case IconKind.Recovery:
                    DrawLine(texture, 32, 12, 32, 52, 9, white);
                    DrawLine(texture, 12, 32, 52, 32, 9, white);
                    break;
                case IconKind.Mana:
                    FillCircle(texture, 32, 37, 15, white);
                    FillTriangle(texture, new Vector2(20, 34), new Vector2(32, 8), new Vector2(44, 34), white);
                    break;
                case IconKind.Crit:
                    DrawCircle(texture, 32, 32, 22, 4, white);
                    DrawCircle(texture, 32, 32, 11, 4, white);
                    FillCircle(texture, 32, 32, 4, white);
                    break;
                case IconKind.CritDamage:
                    FillCircle(texture, 32, 32, 8, white);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        DrawLine(texture, 32, 32,
                            Mathf.RoundToInt(32 + Mathf.Cos(angle) * 27), Mathf.RoundToInt(32 + Mathf.Sin(angle) * 27),
                            5, white);
                    }
                    break;
                case IconKind.Experience:
                    FillTriangle(texture, new Vector2(32, 7), new Vector2(39, 25), new Vector2(58, 25), white);
                    FillTriangle(texture, new Vector2(58, 25), new Vector2(43, 37), new Vector2(49, 56), white);
                    FillTriangle(texture, new Vector2(49, 56), new Vector2(32, 45), new Vector2(15, 56), white);
                    FillTriangle(texture, new Vector2(15, 56), new Vector2(21, 37), new Vector2(6, 25), white);
                    FillTriangle(texture, new Vector2(6, 25), new Vector2(25, 25), new Vector2(32, 7), white);
                    break;
                case IconKind.Gold:
                    FillCircle(texture, 32, 32, 24, white);
                    DrawCircle(texture, 32, 32, 15, 3, clear);
                    break;
                case IconKind.Material:
                    FillTriangle(texture, new Vector2(32, 7), new Vector2(56, 25), new Vector2(32, 57), white);
                    FillTriangle(texture, new Vector2(32, 7), new Vector2(8, 25), new Vector2(32, 57), white);
                    break;
                case IconKind.Back:
                    DrawLine(texture, 50, 15, 18, 32, 7, white);
                    DrawLine(texture, 18, 32, 50, 49, 7, white);
                    DrawLine(texture, 18, 32, 56, 32, 6, white);
                    break;
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void FillRect(Texture2D texture, int minX, int minY, int maxX, int maxY, Color color)
        {
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    SetPixel(texture, x, y, color);
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int radiusSquared = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
                for (int x = centerX - radius; x <= centerX + radius; x++)
                    if ((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY) <= radiusSquared)
                        SetPixel(texture, x, y, color);
        }

        private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, int thickness, Color color)
        {
            int outer = radius * radius;
            int innerRadius = Mathf.Max(0, radius - thickness);
            int inner = innerRadius * innerRadius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int distance = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);
                    if (distance <= outer && distance >= inner) SetPixel(texture, x, y, color);
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            if (steps <= 0)
            {
                FillCircle(texture, x0, y0, Mathf.Max(1, thickness / 2), color);
                return;
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                FillCircle(texture, x, y, Mathf.Max(1, thickness / 2), color);
            }
        }

        private static void FillTriangle(Texture2D texture, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int minX = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
            int maxX = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
            int minY = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int maxY = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            float area = Edge(a, b, c);
            if (Mathf.Abs(area) < 0.001f) return;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float w0 = Edge(b, c, p);
                    float w1 = Edge(c, a, p);
                    float w2 = Edge(a, b, p);
                    bool inside = area > 0f
                        ? w0 >= 0f && w1 >= 0f && w2 >= 0f
                        : w0 <= 0f && w1 <= 0f && w2 <= 0f;
                    if (inside) SetPixel(texture, x, y, color);
                }
            }
        }

        private static float Edge(Vector2 a, Vector2 b, Vector2 c)
        {
            return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= texture.width || y >= texture.height) return;
            texture.SetPixel(x, y, color);
        }
    }
}
