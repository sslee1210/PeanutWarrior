using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Runtime-built uGUI interface for the playable prototype. It keeps the
    /// battlefield visible, uses a safe-area aware landscape layout, and exposes
    /// Peanut Warrior's actual progression instead of legacy debug panels.
    /// </summary>
    [DefaultExecutionOrder(25000)]
    public sealed class PeanutMobileCanvasPrototype : MonoBehaviour
    {
        private enum Page
        {
            Main,
            Growth,
            Swords,
            Skills,
            Minis,
            Adventure,
            Missions,
            Shop,
            Settings
        }

        private const float ReferenceWidth = 1388f;
        private const float ReferenceHeight = 830f;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";

        private static readonly string[] SkillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단",
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };

        private readonly List<Action> menuRefreshers = new List<Action>();
        private readonly Text[] skillDockTexts = new Text[4];
        private readonly Image[] skillDockCooldownFills = new Image[4];

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private SkillManagementPrototype skills;
        private IdleSystemsPrototype idle;
        private PrototypeShopAndDaily shop;
        private PrototypeSaveBridge saveBridge;
        private MetaProgressionPrototype meta;
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
        private FieldInfo skillLevelsField;
        private FieldInfo skillCooldownsField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private FieldInfo advancementTierField;
        private FieldInfo miniSlotsUnlockedField;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo combatPowerProperty;

        private FieldInfo critChanceLevelField;
        private FieldInfo critDamageLevelField;
        private FieldInfo goldGainLevelField;
        private FieldInfo hpRegenLevelField;
        private FieldInfo skillAutoField;
        private FieldInfo miniAttackLevelField;
        private FieldInfo miniCritLevelField;
        private FieldInfo miniCritDamageLevelField;
        private FieldInfo eggsField;
        private FieldInfo hatchedMinisField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;

        private Canvas canvas;
        private RectTransform safeRoot;
        private GameObject mainPage;
        private GameObject menuPage;
        private GameObject quickMenu;
        private GameObject contentHost;
        private Text menuTitle;
        private Text menuResources;
        private Text playerTitle;
        private Text hpText;
        private Text mpText;
        private Image hpFill;
        private Image mpFill;
        private Text goldText;
        private Text diamondText;
        private Text fragmentText;
        private Text stageTitle;
        private Text stageProgressText;
        private Image stageProgressFill;
        private Text autoBossText;
        private Button bossButton;
        private Text combatPowerText;
        private Text toastText;
        private GameObject toastPanel;

        private Font font;
        private Sprite whiteSprite;
        private Page currentPage;
        private int growthTab;
        private int purchaseAmount = 1;
        private float refreshTimer;
        private float toastTimer;
        private Rect lastSafeArea;

        private readonly Color cream = new Color(0.98f, 0.93f, 0.72f, 0.97f);
        private readonly Color paleGreen = new Color(0.82f, 0.93f, 0.76f, 0.97f);
        private readonly Color strongGreen = new Color(0.20f, 0.45f, 0.23f, 0.98f);
        private readonly Color darkGreen = new Color(0.08f, 0.28f, 0.12f, 0.98f);
        private readonly Color gold = new Color(0.96f, 0.66f, 0.14f, 1f);
        private readonly Color red = new Color(0.88f, 0.22f, 0.18f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.88f, 1f);
        private readonly Color brown = new Color(0.19f, 0.11f, 0.05f, 1f);

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

            CreateFontAndSprite();
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
            skills = FindFirstObjectByType<SkillManagementPrototype>();
            idle = FindFirstObjectByType<IdleSystemsPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            saveBridge = FindFirstObjectByType<PrototypeSaveBridge>();
            meta = FindFirstObjectByType<MetaProgressionPrototype>();
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
            skillLevelsField = arenaType.GetField("skillLevels", PrivateInstance);
            skillCooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            advancementTierField = arenaType.GetField("advancementTier", PrivateInstance);
            miniSlotsUnlockedField = arenaType.GetField("miniSlotsUnlocked", PrivateInstance);
            maxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            maxMpProperty = arenaType.GetProperty("PlayerMaxMp", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            combatPowerProperty = arenaType.GetProperty("CombatPower", PrivateInstance);

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                critChanceLevelField = growthType.GetField("critChanceLevel", PrivateInstance);
                critDamageLevelField = growthType.GetField("critDamageLevel", PrivateInstance);
                goldGainLevelField = growthType.GetField("goldGainLevel", PrivateInstance);
                hpRegenLevelField = growthType.GetField("hpRegenLevel", PrivateInstance);
            }

            if (skills != null)
                skillAutoField = typeof(SkillManagementPrototype).GetField("autoEnabled", PrivateInstance);

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
            }
        }

        private void CreateFontAndSprite()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                string[] preferredFonts = { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" };
                font = Font.CreateDynamicFontFromOSFont(preferredFonts, 18);
            }

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "PeanutUiWhiteTexture";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            GameObject eventObject = new GameObject("PeanutWarriorEventSystem");
            DontDestroyOnLoad(eventObject);
            eventObject.AddComponent<EventSystem>();
            InputSystemUIInputModule module = eventObject.AddComponent<InputSystemUIInputModule>();
            MethodInfo assignDefaults = typeof(InputSystemUIInputModule).GetMethod(
                "AssignDefaultActions",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            assignDefaults?.Invoke(module, null);
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
            BuildToast();
        }

        private void BuildMainHud()
        {
            GameObject playerPanel = CreatePanel(mainPage.transform, "Player Status", 16f, 16f, 320f, 110f, cream);
            playerTitle = CreateText(playerPanel.transform, "땅콩전사", 14f, 5f, 292f, 34f, 25, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            hpFill = CreateBar(playerPanel.transform, 14f, 44f, 292f, 22f, red, out hpText);
            mpFill = CreateBar(playerPanel.transform, 14f, 75f, 292f, 22f, blue, out mpText);

            GameObject resources = CreatePanel(mainPage.transform, "Resources", 480f, 16f, 510f, 56f, cream);
            goldText = CreateResourceCell(resources.transform, 0f, 0f, 166f, 56f);
            diamondText = CreateResourceCell(resources.transform, 172f, 0f, 166f, 56f);
            fragmentText = CreateResourceCell(resources.transform, 344f, 0f, 166f, 56f);

            GameObject stagePanel = CreatePanel(mainPage.transform, "Stage", 430f, 82f, 600f, 84f, cream);
            stageTitle = CreateText(stagePanel.transform, string.Empty, 14f, 4f, 360f, 30f, 17, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            stageProgressFill = CreateBar(stagePanel.transform, 14f, 42f, 352f, 24f, gold, out stageProgressText);
            Button autoButton = CreateButton(stagePanel.transform, "자동 도전", 378f, 37f, 104f, 38f, ToggleAutoBoss, paleGreen);
            autoBossText = autoButton.GetComponentInChildren<Text>();
            bossButton = CreateButton(stagePanel.transform, "균왕 도전", 492f, 35f, 94f, 42f, TryStartBoss, red, Color.white);

            GameObject statusPanel = CreatePanel(mainPage.transform, "Combat Power", 16f, 744f, 216f, 62f, cream);
            combatPowerText = CreateText(statusPanel.transform, string.Empty, 10f, 3f, 196f, 56f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);

            Button menuButton = CreateButton(mainPage.transform, "땅콩 메뉴", 1245f, 18f, 127f, 48f,
                () => quickMenu.SetActive(!quickMenu.activeSelf), paleGreen);
            menuButton.name = "Peanut Quick Menu Button";
            BuildQuickMenu();
            BuildSkillDock();
            BuildBottomNavigation();
        }

        private void BuildQuickMenu()
        {
            quickMenu = CreatePanel(mainPage.transform, "Quick Menu", 1090f, 78f, 282f, 360f, cream);
            CreateText(quickMenu.transform, "땅콩월드 바로가기", 12f, 8f, 258f, 30f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);

            string[] names =
            {
                "전직", "검 보관함", "알 부화",
                "스테이지", "방치 연구", "수호 임무",
                "검술 연구", "업적", "소환 상점",
                "설정", "도움말", "저장"
            };
            Page[] pages =
            {
                Page.Growth, Page.Swords, Page.Minis,
                Page.Adventure, Page.Growth, Page.Missions,
                Page.Skills, Page.Missions, Page.Shop,
                Page.Settings, Page.Settings, Page.Settings
            };

            for (int i = 0; i < names.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                int captured = i;
                CreateButton(quickMenu.transform, names[i], 10f + column * 90f, 46f + row * 74f, 84f, 64f,
                    () =>
                    {
                        quickMenu.SetActive(false);
                        if (captured == 4) growthTab = 4;
                        if (captured == 0) growthTab = 2;
                        if (captured == 11) InvokePrivate(saveBridge, "Save", null);
                        ShowPage(pages[captured]);
                    }, paleGreen);
            }
            quickMenu.SetActive(false);
        }

        private void BuildSkillDock()
        {
            GameObject dock = CreatePanel(mainPage.transform, "Skill Dock", 936f, 600f, 436f, 138f, cream);
            CreateText(dock.transform, "자동 검술", 10f, 4f, 416f, 26f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                Button button = CreateButton(dock.transform, string.Empty, 10f + i * 104f, 36f, 96f, 88f,
                    () => ShowPage(Page.Skills), paleGreen);
                skillDockTexts[i] = button.GetComponentInChildren<Text>();
                GameObject cooldown = CreatePanel(button.transform, "Cooldown Fill", 0f, 82f, 96f, 6f, gold);
                skillDockCooldownFills[i] = cooldown.GetComponent<Image>();
                skillDockCooldownFills[i].type = Image.Type.Filled;
                skillDockCooldownFills[i].fillMethod = Image.FillMethod.Horizontal;
                skillDockCooldownFills[i].fillOrigin = 0;
                skillDockCooldownFills[i].fillAmount = 0f;
            }
        }

        private void BuildBottomNavigation()
        {
            string[] names = { "전사 성장", "검", "검술", "미니 땅콩", "모험", "수호 임무", "땅콩 상점" };
            Page[] pages = { Page.Growth, Page.Swords, Page.Skills, Page.Minis, Page.Adventure, Page.Missions, Page.Shop };
            const float startX = 238f;
            const float width = 132f;
            for (int i = 0; i < names.Length; i++)
            {
                Page captured = pages[i];
                CreateButton(mainPage.transform, names[i], startX + i * width, 744f, width - 5f, 62f,
                    () => ShowPage(captured), paleGreen);
            }
        }

        private void BuildMenuFrame()
        {
            Image background = menuPage.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.color = new Color(0.90f, 0.95f, 0.82f, 1f);
            CreatePanel(menuPage.transform, "Header", 0f, 0f, ReferenceWidth, 78f, strongGreen);
            CreateButton(menuPage.transform, "뒤로", 16f, 14f, 62f, 50f, () => ShowPage(Page.Main), cream);
            menuTitle = CreateText(menuPage.transform, string.Empty, 94f, 10f, 520f, 56f, 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            menuResources = CreateText(menuPage.transform, string.Empty, 900f, 10f, 466f, 56f, 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            contentHost = CreateRect(menuPage.transform, "Content Host", 0f, 78f, ReferenceWidth, ReferenceHeight - 78f).gameObject;
        }

        private void BuildToast()
        {
            toastPanel = CreatePanel(safeRoot, "Toast", 454f, 680f, 480f, 52f, darkGreen);
            toastText = CreateText(toastPanel.transform, string.Empty, 12f, 4f, 456f, 44f, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            if (arena == null || stageFlow == null) return;
            RefreshMainHud();
            menuResources.text = $"골드 {Gold:N0}   다이아 {Diamonds:N0}   조각 {Fragments:N0}";
            for (int i = 0; i < menuRefreshers.Count; i++) menuRefreshers[i]?.Invoke();
        }

        private void RefreshMainHud()
        {
            int advancement = ReadInt(advancementTierField, arena);
            playerTitle.text = $"땅콩전사   Lv.{Mathf.Max(1, CombatPower / 25)}   ·   전직 {advancement}단계";
            SetBar(hpFill, hpText, PlayerHp, MaxHp, $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            SetBar(mpFill, mpText, PlayerMp, MaxMp, $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
            goldText.text = $"골드\n{Gold:N0}";
            diamondText.text = $"다이아\n{Diamonds:N0}";
            fragmentText.text = $"조각\n{Fragments:N0}";
            stageTitle.text = $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}";
            SetBar(stageProgressFill, stageProgressText, stageFlow.MonsterKills, StageFlowController.RequiredKills,
                $"균왕 자격 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            autoBossText.text = stageFlow.AutoChallenge ? "자동 도전 ON" : "자동 도전 OFF";
            bossButton.interactable = stageFlow.CanChallengeBoss;
            combatPowerText.text = $"전투력 {CombatPower:N0}\n처치 {stageFlow.MonsterKills}/100";

            int offset = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            int[] levels = SkillLevels;
            float[] cooldowns = SkillCooldowns;
            for (int i = 0; i < 4; i++)
            {
                int index = offset + i;
                int level = levels != null && index < levels.Length ? levels[index] : 1;
                float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                skillDockTexts[i].text = $"{SkillNames[index]}\nLv.{level}\n{(cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "AUTO")}";
                float maximum = 5f + (index % 4) * 1.5f;
                skillDockCooldownFills[i].fillAmount = maximum <= 0f ? 0f : Mathf.Clamp01(cooldown / maximum);
            }
        }

        private void ShowPage(Page page)
        {
            currentPage = page;
            mainPage.SetActive(page == Page.Main);
            menuPage.SetActive(page != Page.Main);
            if (page == Page.Main) return;

            menuTitle.text = PageTitle(page);
            RebuildMenuContent();
        }

        private void RebuildMenuContent()
        {
            menuRefreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            switch (currentPage)
            {
                case Page.Growth: BuildGrowthPage(); break;
                case Page.Swords: BuildSwordPage(); break;
                case Page.Skills: BuildSkillPage(); break;
                case Page.Minis: BuildMiniPage(); break;
                case Page.Adventure: BuildAdventurePage(); break;
                case Page.Missions: BuildMissionPage(); break;
                case Page.Shop: BuildShopPage(); break;
                case Page.Settings: BuildSettingsPage(); break;
            }
            RefreshAll();
        }

        private void BuildGrowthPage()
        {
            string[] tabs = { "기초 능력", "껍질 단련", "전직", "속성 연구", "방치 효율" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int captured = i;
                CreateButton(contentHost.transform, tabs[i], 20f + i * 184f, 12f, 174f, 50f,
                    () => { growthTab = captured; RebuildMenuContent(); }, growthTab == i ? gold : paleGreen);
            }

            Transform content = CreateScrollContent(contentHost.transform, 20f, 78f, 1348f, 646f);
            switch (growthTab)
            {
                case 0: BuildBaseGrowth(content); break;
                case 1: BuildShellGrowth(content); break;
                case 2: BuildAdvancement(content); break;
                case 3: BuildElementResearch(content); break;
                case 4: BuildIdleGrowth(content); break;
            }
        }

        private void BuildBaseGrowth(Transform content)
        {
            AddSection(content, "현재 전투 능력",
                () => $"검 공격력 {AttackDamage:N1}   ·   전투력 {CombatPower:N0}   ·   HP {MaxHp:N0}   ·   MP {MaxMp:N0}");
            AddPurchaseSelector(content);
            AddUpgradeRow(content, "검 공격력", attackLevelField, arena, 20L);
            AddUpgradeRow(content, "껍질 생명력", hpLevelField, arena, 25L);
            AddUpgradeRow(content, "최대 마력", maxMpLevelField, arena, 30L);
            AddUpgradeRow(content, "껍질 재생", hpRegenLevelField, growth, 40L);
            AddUpgradeRow(content, "마력 회복", mpRegenLevelField, arena, 35L);
            AddUpgradeRow(content, "정밀 베기", critChanceLevelField, growth, 45L);
            AddUpgradeRow(content, "치명 일격", critDamageLevelField, growth, 55L);
            AddUpgradeRow(content, "땅콩 수확량", goldGainLevelField, growth, 65L);
        }

        private void AddPurchaseSelector(Transform content)
        {
            GameObject row = AddRow(content, 70f, paleGreen);
            CreateText(row.transform, "한 번에 강화", 18f, 8f, 520f, 52f, 19, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            int[] values = { 1, 10, 100 };
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                CreateButton(row.transform, $"×{value}", 850f + i * 128f, 10f, 116f, 50f,
                    () => { purchaseAmount = value; RebuildMenuContent(); }, purchaseAmount == value ? gold : cream);
            }
        }

        private void AddUpgradeRow(Transform content, string label, FieldInfo field, object target, long baseCost)
        {
            GameObject row = AddRow(content, 82f, cream);
            Text info = CreateText(row.transform, string.Empty, 20f, 8f, 780f, 66f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text buttonText;
            Button button = CreateButton(row.transform, string.Empty, 1050f, 14f, 260f, 54f,
                () => UpgradeField(field, target, baseCost, label), paleGreen);
            buttonText = button.GetComponentInChildren<Text>();
            Action refresh = () =>
            {
                int level = ReadLevel(field, target);
                long cost = SumUpgradeCost(baseCost, level, purchaseAmount);
                info.text = $"{label}\nLv.{level:N0} → Lv.{level + purchaseAmount:N0}";
                buttonText.text = $"강화 {cost:N0}G";
            };
            menuRefreshers.Add(refresh);
            refresh();
        }

        private void BuildShellGrowth(Transform content)
        {
            AddSection(content, "껍질 단련",
                () => "방어구를 따로 입는 대신 땅콩 껍질 자체를 단련합니다. 단련 수치는 실제 HP와 재생 성장에 연결됩니다.");
            AddActionRow(content, "껍질 생명 단련",
                () => $"Lv.{(meta != null ? meta.ShellVitalityLevel : 1)} · 다음 비용 {(meta != null ? meta.ShellVitalityCost : 0):N0}G",
                "단련", () => RunMetaUpgrade(meta?.UpgradeShellVitality));
            AddActionRow(content, "껍질 재생 단련",
                () => $"Lv.{(meta != null ? meta.ShellRecoveryLevel : 1)} · 다음 비용 {(meta != null ? meta.ShellRecoveryCost : 0):N0}G",
                "단련", () => RunMetaUpgrade(meta?.UpgradeShellRecovery));
            AddSection(content, "전직과 외형",
                () => "전직 단계가 오르면 껍질 명칭과 기본 공격 타수가 바뀌며, 2차 전직에서 미니 땅콩 3슬롯이 해금됩니다.");
        }

        private void BuildAdvancement(Transform content)
        {
            AddSection(content, "땅콩전사 전직", AdvancementStatusText);
            AddActionRow(content, "다음 껍질로 전직",
                AdvancementRequirementText, "전직 시도", TryAdvance);
            AddSection(content, "전직 효과",
                () => "기본 능력치 증가 · 기본 공격 타수 증가 · 검술 공격 횟수 증가 · 2차 전직 시 미니 땅콩 3슬롯 해금");
        }

        private void BuildElementResearch(Transform content)
        {
            AddSection(content, "속성 연구",
                () => "장착한 사냥 검 또는 균왕 검의 속성 피해를 강화합니다. 검 등급·검 레벨과 곱연산으로 실제 전투 보너스가 적용됩니다.");
            for (int i = 0; i < 4; i++)
            {
                int element = i;
                AddActionRow(content, ElementNames[element],
                    () =>
                    {
                        int level = meta != null ? meta.GetElementResearchLevel(element) : 1;
                        long cost = meta != null ? meta.ElementResearchCost(element) : 0L;
                        float multiplier = meta != null ? meta.GetElementDamageMultiplier(element) : 1f;
                        return $"연구 Lv.{level} · 피해 ×{multiplier:0.000} · 비용 {cost:N0}조각";
                    }, "연구", () => RunMetaUpgrade(() => meta != null && meta.UpgradeElementResearch(element)));
            }
        }

        private void BuildIdleGrowth(Transform content)
        {
            AddSection(content, "방치 효율",
                () => $"현재 최대 방치 시간 {(meta != null ? meta.MaximumOfflineHours : 8)}시간 · 기본 방치 보상에 연구 보너스를 추가합니다.");
            AddActionRow(content, "방치 골드 연구",
                () => $"Lv.{(meta != null ? meta.IdleGoldLevel : 1)} · ×{(meta != null ? meta.OfflineGoldMultiplier : 1f):0.00} · {(meta != null ? meta.IdleGoldCost : 0):N0}G",
                "연구", () => RunMetaUpgrade(meta?.UpgradeIdleGold));
            AddActionRow(content, "방치 조각 연구",
                () => $"Lv.{(meta != null ? meta.IdleFragmentLevel : 1)} · ×{(meta != null ? meta.OfflineFragmentMultiplier : 1f):0.00} · {(meta != null ? meta.IdleFragmentCost : 0):N0}G",
                "연구", () => RunMetaUpgrade(meta?.UpgradeIdleFragments));
            AddActionRow(content, "최대 방치 시간",
                () => $"Lv.{(meta != null ? meta.IdleHourLevel : 1)} · 최대 {(meta != null ? meta.MaximumOfflineHours : 8)}시간 · {(meta != null ? meta.IdleHourCost : 0):N0}G",
                "확장", () => RunMetaUpgrade(meta?.UpgradeIdleHours));
        }

        private void BuildSwordPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "검 보관함",
                () => $"사냥 검 {ElementNames[ReadElement(huntingElementField)]} · 균왕 검 {ElementNames[ReadElement(bossElementField)]}\n검은 일반 몬스터에게 드롭되지 않고 소환·합성·강화로 성장합니다.");

            for (int i = 0; i < 4; i++)
            {
                int element = i;
                GameObject row = AddRow(content, 132f, i % 2 == 0 ? cream : paleGreen);
                Text info = CreateText(row.transform, string.Empty, 18f, 10f, 610f, 108f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                CreateButton(row.transform, "사냥 장착", 650f, 18f, 145f, 48f, () => EquipElement(huntingElementField, element, "사냥 검"), cream);
                CreateButton(row.transform, "균왕 장착", 810f, 18f, 145f, 48f, () => EquipElement(bossElementField, element, "균왕 검"), cream);
                CreateButton(row.transform, "검 강화", 970f, 18f, 145f, 48f, () => UpgradeSword(element), gold);
                CreateButton(row.transform, "자동 합성", 1130f, 18f, 170f, 48f, () => SynthesizeLowest(element), paleGreen);
                Text copies = CreateText(row.transform, string.Empty, 650f, 74f, 650f, 40f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
                Action refresh = () =>
                {
                    SwordProgressionPrototype.SwordRarity rarity = swords != null
                        ? swords.GetHighestRarity(element)
                        : SwordProgressionPrototype.SwordRarity.None;
                    int level = swords != null ? swords.GetLevel(element) : 1;
                    float multiplier = swords != null ? swords.GetDamageMultiplier(element) : 1f;
                    long cost = swords != null ? swords.GetUpgradeCost(element) : 0L;
                    info.text = $"{ElementNames[element]} 검 · {SwordProgressionPrototype.RarityName(rarity)} · Lv.{level}\n전투 피해 ×{multiplier:0.000} · 강화 비용 {cost:N0}G";
                    copies.text = swords == null
                        ? "검 시스템 초기화 대기"
                        : $"일반 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Common)}   희귀 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Rare)}   전설 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Legendary)}   신화 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Mythic)}";
                };
                menuRefreshers.Add(refresh);
                refresh();
            }

            AddActionRow(content, "사냥 검 소환", () => "다이아 5개 · 획득 즉시 사냥 검 슬롯에 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            AddActionRow(content, "균왕 검 소환", () => "다이아 5개 · 획득 즉시 균왕 검 슬롯에 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }));
        }

        private void BuildSkillPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "땅콩 검술",
                () => $"보유 강화 조각 {Fragments:N0} · 사냥 검술 4개와 균왕 검술 4개를 별도로 자동 운용합니다.");
            int[] levels = SkillLevels;
            bool[] auto = SkillAuto;
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                GameObject row = AddRow(content, 88f, i % 2 == 0 ? cream : paleGreen);
                Text info = CreateText(row.transform, string.Empty, 18f, 8f, 800f, 70f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Button upgrade = CreateButton(row.transform, string.Empty, 920f, 16f, 180f, 54f, () => UpgradeSkill(index), gold);
                Button toggle = CreateButton(row.transform, string.Empty, 1115f, 16f, 185f, 54f, () => ToggleSkillAuto(index), paleGreen);
                Text upgradeText = upgrade.GetComponentInChildren<Text>();
                Text toggleText = toggle.GetComponentInChildren<Text>();
                Action refresh = () =>
                {
                    int level = levels != null && index < levels.Length ? levels[index] : 1;
                    float cooldown = SkillCooldowns != null && index < SkillCooldowns.Length ? Mathf.Max(0f, SkillCooldowns[index]) : 0f;
                    bool enabledForAuto = auto == null || index >= auto.Length || auto[index];
                    info.text = $"{SkillNames[index]} · Lv.{level} · {(index < 4 ? "사냥" : "균왕")}용 · 현재 쿨 {cooldown:0.0}초";
                    upgradeText.text = $"강화 {SkillCost(index, level)}조각";
                    toggleText.text = enabledForAuto ? "자동 사용 ON" : "자동 사용 OFF";
                };
                menuRefreshers.Add(refresh);
                refresh();
            }
        }

        private void BuildMiniPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "미니 땅콩 원정대", MiniStatusText);
            AddActionRow(content, "미니 공격력", () => MiniUpgradeText(miniAttackLevelField, 80L), "강화", () => UpgradeMini(miniAttackLevelField, 80L, "미니 공격력"));
            AddActionRow(content, "미니 치명타 확률", () => MiniUpgradeText(miniCritLevelField, 100L), "강화", () => UpgradeMini(miniCritLevelField, 100L, "미니 치명타 확률"));
            AddActionRow(content, "미니 치명타 피해", () => MiniUpgradeText(miniCritDamageLevelField, 120L), "강화", () => UpgradeMini(miniCritDamageLevelField, 120L, "미니 치명타 피해"));
            AddActionRow(content, "미니 알 구매", () => $"보유 알 {ReadInt(eggsField, idle)} · 다이아 3개", "구매", () => InvokePrivate(idle, "BuyEgg", null));
            AddActionRow(content, "알 부화", IncubationText, "부화 시작", () => InvokePrivate(idle, "StartIncubation", null));
            AddSection(content, "편성 규칙",
                () => "사냥 편성과 균왕 편성을 별도로 저장합니다. 미니는 검을 장착하지 않고 화염·냉기·번개 고유 속성으로 공격하며 사망하지 않습니다.");
        }

        private void BuildAdventurePage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "현재 모험", AdventureStatusText);
            AddActionRow(content, "이전 해금 스테이지", () => "현재 스테이지보다 한 단계 이전으로 이동", "이동", () => MoveStage(-1));
            AddActionRow(content, "다음 해금 스테이지", () => $"최고 해금 {FormatGlobalStage(HighestGlobalStage)}", "이동", () => MoveStage(1));
            AddActionRow(content, "자동 균왕 도전", () => stageFlow.AutoChallenge ? "현재 ON · 100/100 달성 즉시 입장" : "현재 OFF · 100/100 이후에도 계속 방치 사냥", "전환", ToggleAutoBoss);
            AddActionRow(content, "균왕 도전", () => stageFlow.CanChallengeBoss ? "도전 가능 · 입장 시 HP·MP·모든 쿨타임 초기화" : $"현재 {stageFlow.MonsterKills}/100", "도전", TryStartBoss);
            AddSection(content, "확정 모험 규칙",
                () => "100마리는 균왕 도전 자격만 해금합니다. 도전하지 않으면 몬스터가 계속 등장합니다. 사냥 중 사망하면 이전 스테이지, 균왕전 사망이면 현재 스테이지 0/100으로 복귀합니다.");
            AddSection(content, "월드 순환",
                () => "땅콩밭 침공 → 곰팡이 창고 → 포식자의 숲 → 얼어붙은 저장고 → 불타는 이세계 → 차원 균열 중심부. 이후 강화된 월드가 반복됩니다.");
        }

        private void BuildMissionPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "수호 임무와 업적", () => "처치·스테이지·성장 진행에 따라 다이아와 검술 조각을 수령합니다.");
            AddActionRow(content, "몬스터 정리 임무", () => "누적 50마리 단위 보상", "보상 수령", () => InvokePrivate(idle, "ClaimKillMission", null));
            AddActionRow(content, "지역 개척 임무", () => "2개 스테이지 진행 단위 보상", "보상 수령", () => InvokePrivate(idle, "ClaimStageMission", null));
            AddActionRow(content, "전사 성장 업적", () => "전직과 미니 강화 진행 보상", "보상 수령", () => InvokePrivate(idle, "ClaimGrowthAchievement", null));
        }

        private void BuildShopPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "땅콩 상점", () => shop != null ? shop.ShopMessage : "상점 초기화 대기");
            AddActionRow(content, "오늘의 보급", () => $"연속 접속 {(shop != null ? shop.DailyStreak : 0)}일", "받기", () => InvokePrivate(shop, "ClaimDailyReward", null));
            AddActionRow(content, "사냥 검 소환", () => "다이아 5개 · 일반/희귀/전설/신화", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            AddActionRow(content, "균왕 검 소환", () => "다이아 5개 · 균왕 전용 슬롯에 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }));
            AddActionRow(content, "미니 알", () => "다이아 3개 · 부화 후 미니 도감 증가", "구매", () => InvokePrivate(shop, "BuyEgg", null));
        }

        private void BuildSettingsPage()
        {
            Transform content = CreateScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            AddSection(content, "실행 설정", () => $"현재 목표 프레임 {Application.targetFrameRate} FPS · 화면 방향 가로 고정 · 안전영역 자동 적용");
            AddActionRow(content, "60 FPS", () => "일반 플레이 모드", "적용", () => { Application.targetFrameRate = 60; QualitySettings.antiAliasing = 2; ShowToast("60 FPS 모드 적용"); });
            AddActionRow(content, "30 FPS 절전", () => "발열과 배터리 사용 감소", "적용", () => { Application.targetFrameRate = 30; QualitySettings.antiAliasing = 0; ShowToast("30 FPS 절전 모드 적용"); });
            AddActionRow(content, "즉시 저장", () => "스테이지·자원·성장·검·미니 데이터를 즉시 기록", "저장", () => { InvokePrivate(saveBridge, "Save", null); ShowToast("저장 요청 완료"); });
            AddSection(content, "개발 상태",
                () => "콘솔의 [PeanutWarrior Runtime Audit]가 PASS인지 확인하십시오. 실제 캐릭터·몬스터 이미지와 애니메이션은 별도 리소스 제작 후 교체해야 합니다.");
        }

        private void AddSection(Transform content, string title, Func<string> bodyProvider)
        {
            GameObject row = AddRow(content, 112f, paleGreen);
            CreateText(row.transform, title, 18f, 8f, 340f, 36f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = CreateText(row.transform, string.Empty, 370f, 8f, 930f, 92f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Action refresh = () => body.text = bodyProvider != null ? bodyProvider() : string.Empty;
            menuRefreshers.Add(refresh);
            refresh();
        }

        private void AddActionRow(Transform content, string title, Func<string> detailProvider, string buttonLabel, Action action)
        {
            GameObject row = AddRow(content, 84f, cream);
            CreateText(row.transform, title, 18f, 8f, 360f, 68f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text detail = CreateText(row.transform, string.Empty, 390f, 8f, 650f, 68f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            CreateButton(row.transform, buttonLabel, 1080f, 15f, 220f, 54f,
                () => { action?.Invoke(); RefreshAll(); }, gold);
            Action refresh = () => detail.text = detailProvider != null ? detailProvider() : string.Empty;
            menuRefreshers.Add(refresh);
            refresh();
        }

        private GameObject AddRow(Transform content, float height, Color color)
        {
            GameObject row = new GameObject("Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(content, false);
            row.GetComponent<Image>().sprite = whiteSprite;
            row.GetComponent<Image>().color = color;
            LayoutElement layout = row.GetComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.minHeight = height;
            return row;
        }

        private Transform CreateScrollContent(Transform parent, float x, float y, float width, float height)
        {
            GameObject scrollObject = CreatePanel(parent, "Scroll", x, y, width, height, new Color(1f, 1f, 1f, 0.20f));
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 36f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect(scrollObject.transform, "Viewport", 8f, 8f, width - 16f, height - 16f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = whiteSprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private void UpgradeField(FieldInfo field, object target, long baseCost, string label)
        {
            if (field == null || target == null)
            {
                ShowToast($"{label} 데이터 연결 대기");
                return;
            }
            int level = ReadLevel(field, target);
            long cost = SumUpgradeCost(baseCost, level, purchaseAmount);
            if (!SpendGold(cost))
            {
                ShowToast($"골드 부족 · {cost:N0}G 필요");
                return;
            }
            field.SetValue(target, level + purchaseAmount);
            if (field == hpLevelField || field == maxMpLevelField) InvokePrivate(arena, "FullRestore", null);
            ShowToast($"{label} ×{purchaseAmount} 강화 완료");
        }

        private void UpgradeSkill(int index)
        {
            int[] levels = SkillLevels;
            if (levels == null || index < 0 || index >= levels.Length) return;
            long cost = SkillCost(index, levels[index]);
            if (Fragments < cost)
            {
                ShowToast($"조각 부족 · {cost}개 필요");
                return;
            }
            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            ShowToast($"{SkillNames[index]} Lv.{levels[index]} 강화");
        }

        private void ToggleSkillAuto(int index)
        {
            bool[] auto = SkillAuto;
            if (auto == null || index < 0 || index >= auto.Length)
            {
                ShowToast("자동 검술 설정 연결 대기");
                return;
            }
            auto[index] = !auto[index];
            PlayerPrefs.SetInt("PeanutWarrior.SkillAuto." + index, auto[index] ? 1 : 0);
            PlayerPrefs.Save();
            ShowToast($"{SkillNames[index]} 자동 사용 {(auto[index] ? "ON" : "OFF")}");
        }

        private void UpgradeMini(FieldInfo field, long baseCost, string label)
        {
            if (field == null || idle == null) return;
            int level = ReadLevel(field, idle);
            long cost = baseCost * level;
            if (!SpendGold(cost))
            {
                ShowToast($"골드 부족 · {cost:N0}G 필요");
                return;
            }
            field.SetValue(idle, level + 1);
            ShowToast($"{label} Lv.{level + 1} 강화");
        }

        private void RunMetaUpgrade(Func<bool> upgrade)
        {
            if (upgrade == null)
            {
                ShowToast("전용 성장 시스템 초기화 대기");
                return;
            }
            bool success = upgrade();
            ShowToast(meta != null ? meta.LastMessage : success ? "성장 완료" : "성장 실패");
        }

        private void TryAdvance()
        {
            int before = ReadInt(advancementTierField, arena);
            InvokePrivate(arena, "TryAdvance", null);
            int after = ReadInt(advancementTierField, arena);
            ShowToast(after > before ? $"전직 성공 · {after}단계" : "전직 조건을 확인하십시오");
        }

        private void UpgradeSword(int element)
        {
            if (swords == null)
            {
                ShowToast("검 성장 시스템 초기화 대기");
                return;
            }
            swords.UpgradeSword(element);
            ShowToast(swords.LastMessage);
        }

        private void SynthesizeLowest(int element)
        {
            if (swords == null) return;
            bool success = swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Common) ||
                           swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Rare) ||
                           swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Legendary);
            ShowToast(swords.LastMessage + (success ? string.Empty : ""));
        }

        private void EquipElement(FieldInfo field, int element, string slot)
        {
            if (field == null) return;
            field.SetValue(arena, Enum.ToObject(field.FieldType, element));
            ShowToast($"{slot}에 {ElementNames[element]} 장착");
        }

        private void ToggleAutoBoss()
        {
            stageFlow.SetAutoChallenge(!stageFlow.AutoChallenge);
            ShowToast(stageFlow.AutoChallenge ? "자동 균왕 도전 ON" : "자동 균왕 도전 OFF");
        }

        private void TryStartBoss()
        {
            if (!stageFlow.TryStartBossBattle())
                ShowToast("일반 몬스터 100마리를 먼저 처치해야 합니다");
        }

        private void MoveStage(int direction)
        {
            int target = CurrentGlobalStage + direction;
            if (target < 1)
            {
                ShowToast("1-1보다 이전으로 이동할 수 없습니다");
                return;
            }
            if (target > HighestGlobalStage)
            {
                ShowToast($"미해금 스테이지 · 최고 {FormatGlobalStage(HighestGlobalStage)}");
                return;
            }
            int world = (target - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (target - 1) % StageFlowController.StagesPerWorld + 1;
            stageFlow.SelectStage(world, stage);
            ShowToast($"{world}-{stage}로 이동");
        }

        private string AdvancementStatusText()
        {
            int tier = ReadInt(advancementTierField, arena);
            bool minisUnlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            return $"현재 전직 {tier}단계 · 기본 공격 {tier + 1}타 · 미니 슬롯 {(minisUnlocked ? "3/3 해금" : "잠김")}";
        }

        private string AdvancementRequirementText()
        {
            int tier = ReadInt(advancementTierField, arena);
            if (tier >= 2) return "현재 프로토타입 최고 전직 단계 달성";
            int requiredStage = tier == 0 ? 2 : 4;
            int requiredPower = tier == 0 ? 180 : 420;
            long requiredGold = tier == 0 ? 150L : 500L;
            int requiredDiamonds = tier == 0 ? 5 : 15;
            return $"조건: 전체 스테이지 {requiredStage} · 전투력 {requiredPower} · {requiredGold:N0}G · 다이아 {requiredDiamonds}";
        }

        private string MiniStatusText()
        {
            bool unlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            return unlocked
                ? $"미니 슬롯 3/3 활동 중 · 부화 도감 {ReadInt(hatchedMinisField, idle)} · 주인공보다 한 단계 낮은 외형"
                : "2차 전직을 달성하면 미니 땅콩 3슬롯이 동시에 해금됩니다.";
        }

        private string MiniUpgradeText(FieldInfo field, long baseCost)
        {
            int level = ReadLevel(field, idle);
            return $"Lv.{level} · 다음 비용 {baseCost * level:N0}G";
        }

        private string IncubationText()
        {
            bool incubating = incubatingField != null && idle != null && (bool)incubatingField.GetValue(idle);
            return incubating
                ? $"부화 진행 중 · {Mathf.CeilToInt(ReadFloat(incubationRemainingField, idle))}초 남음"
                : $"대기 중 · 보유 알 {ReadInt(eggsField, idle)}";
        }

        private string AdventureStatusText()
        {
            return $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage} · 처치 {stageFlow.MonsterKills}/100 · 최고 해금 {FormatGlobalStage(HighestGlobalStage)}";
        }

        private void InvokePrivate(object target, string methodName, object[] arguments)
        {
            if (target == null)
            {
                ShowToast("해당 시스템 초기화 대기");
                return;
            }
            arguments = arguments ?? Array.Empty<object>();
            Type[] argumentTypes = new Type[arguments.Length];
            for (int i = 0; i < arguments.Length; i++) argumentTypes[i] = arguments[i].GetType();
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance, null, argumentTypes, null);
            if (method == null)
            {
                ShowToast($"기능 연결 실패 · {methodName}");
                return;
            }
            try
            {
                method.Invoke(target, arguments.Length == 0 ? null : arguments);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception, target as UnityEngine.Object);
                ShowToast("기능 실행 중 오류 발생");
            }
        }

        private void ShowToast(string message)
        {
            if (toastPanel == null) return;
            toastText.text = message;
            toastTimer = 2.4f;
            toastPanel.SetActive(true);
        }

        private void ApplySafeArea()
        {
            if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            Vector2 minimum = safe.position;
            Vector2 maximum = safe.position + safe.size;
            minimum.x /= Screen.width;
            minimum.y /= Screen.height;
            maximum.x /= Screen.width;
            maximum.y /= Screen.height;
            safeRoot.anchorMin = minimum;
            safeRoot.anchorMax = maximum;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        private GameObject CreatePanel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = CreateRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = whiteSprite;
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

        private Text CreateText(Transform parent, string text, float x, float y, float width, float height,
            int size, Color color, TextAnchor anchor, FontStyle style)
        {
            RectTransform rect = CreateRect(parent, "Text", x, y, width, height);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private Button CreateButton(Transform parent, string text, float x, float y, float width, float height,
            Action action, Color color, Color? textColor = null)
        {
            GameObject go = CreatePanel(parent, "Button", x, y, width, height, color);
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.90f, 0.55f, 1f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.65f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Text label = CreateText(go.transform, text, 4f, 3f, width - 8f, height - 6f, 15,
                textColor ?? brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.raycastTarget = false;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        private Text CreateResourceCell(Transform parent, float x, float y, float width, float height)
        {
            GameObject cell = CreatePanel(parent, "Resource", x, y, width, height, paleGreen);
            return CreateText(cell.transform, string.Empty, 4f, 2f, width - 8f, height - 4f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private Image CreateBar(Transform parent, float x, float y, float width, float height, Color fillColor, out Text label)
        {
            GameObject background = CreatePanel(parent, "Bar", x, y, width, height, new Color(0.16f, 0.20f, 0.13f, 0.92f));
            GameObject fillObject = CreatePanel(background.transform, "Fill", 2f, 2f, width - 4f, height - 4f, fillColor);
            Image fill = fillObject.GetComponent<Image>();
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(width - 4f, -4f);
            label = CreateText(background.transform, string.Empty, 0f, 0f, width, height, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return fill;
        }

        private static void SetBar(Image fill, Text label, float current, float maximum, string text)
        {
            if (fill != null)
            {
                float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
                RectTransform rect = fill.rectTransform;
                float fullWidth = rect.parent.GetComponent<RectTransform>().rect.width - 4f;
                rect.sizeDelta = new Vector2(fullWidth * ratio, -4f);
            }
            if (label != null) label.text = text;
        }

        private long Gold => ReadLong(goldField, arena);
        private long Fragments => ReadLong(fragmentsField, arena);
        private int Diamonds => ReadInt(diamondsField, arena);
        private float PlayerHp => ReadFloat(playerHpField, arena);
        private float PlayerMp => ReadFloat(playerMpField, arena);
        private float MaxHp => ReadPropertyFloat(maxHpProperty, arena, 1f);
        private float MaxMp => ReadPropertyFloat(maxMpProperty, arena, 1f);
        private float AttackDamage => ReadPropertyFloat(attackDamageProperty, arena, 0f);
        private int CombatPower => combatPowerProperty == null ? Mathf.RoundToInt(AttackDamage * 10f) : Convert.ToInt32(combatPowerProperty.GetValue(arena));
        private int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        private float[] SkillCooldowns => skillCooldownsField?.GetValue(arena) as float[];
        private bool[] SkillAuto => skillAutoField?.GetValue(skills) as bool[];
        private int CurrentGlobalStage => (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
        private int HighestGlobalStage => Mathf.Max(CurrentGlobalStage, PlayerPrefs.GetInt(HighestStageKey, CurrentGlobalStage));

        private bool SpendGold(long amount)
        {
            if (goldField == null || Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private int ReadElement(FieldInfo field)
        {
            return field == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private static long SumUpgradeCost(long baseCost, int startingLevel, int count)
        {
            long n = Mathf.Max(1, count);
            long first = Mathf.Max(1, startingLevel);
            return baseCost * n * (2L * first + n - 1L) / 2L;
        }

        private static long SkillCost(int index, int level)
        {
            return 2L + Mathf.Max(1, level) * 2L + index / 4;
        }

        private static int ReadLevel(FieldInfo field, object target)
        {
            return field == null || target == null ? 1 : Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        }

        private static int ReadInt(FieldInfo field, object target)
        {
            return field == null || target == null ? 0 : Convert.ToInt32(field.GetValue(target));
        }

        private static long ReadLong(FieldInfo field, object target)
        {
            return field == null || target == null ? 0L : Convert.ToInt64(field.GetValue(target));
        }

        private static float ReadFloat(FieldInfo field, object target)
        {
            return field == null || target == null ? 0f : Convert.ToSingle(field.GetValue(target));
        }

        private static float ReadPropertyFloat(PropertyInfo property, object target, float fallback)
        {
            return property == null || target == null ? fallback : Convert.ToSingle(property.GetValue(target));
        }

        private static string FormatGlobalStage(int globalStage)
        {
            int world = (globalStage - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (globalStage - 1) % StageFlowController.StagesPerWorld + 1;
            return $"{world}-{stage}";
        }

        private static string PageTitle(Page page)
        {
            return page switch
            {
                Page.Growth => "땅콩전사 성장",
                Page.Swords => "검 보관함",
                Page.Skills => "땅콩 검술",
                Page.Minis => "미니 땅콩",
                Page.Adventure => "땅콩월드 모험",
                Page.Missions => "수호 임무",
                Page.Shop => "땅콩 상점",
                Page.Settings => "설정과 진단",
                _ => "땅콩전사 키우기"
            };
        }
    }
}
