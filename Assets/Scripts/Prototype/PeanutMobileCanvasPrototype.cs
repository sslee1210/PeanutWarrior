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
            Growth,
            Advancement,
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
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] SkillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단",
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };

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
        private FieldInfo idleMessageField;

        private Canvas canvas;
        private RectTransform safeRoot;
        private GameObject mainPage;
        private GameObject menuPage;
        private GameObject quickMenu;
        private GameObject contentHost;
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
        private Text combatPowerText;
        private Image hpFill;
        private Image mpFill;
        private Image stageFill;
        private Button bossButton;
        private readonly Text[] skillTexts = new Text[4];
        private readonly List<Action> refreshers = new List<Action>();

        private Font font;
        private Sprite whiteSprite;
        private Page currentPage;
        private int purchaseAmount = 1;
        private int stageMapWorld = 1;
        private float refreshTimer;
        private float toastTimer;
        private Rect lastSafeArea;

        private readonly Color cream = new Color(0.98f, 0.93f, 0.72f, 0.97f);
        private readonly Color paleGreen = new Color(0.82f, 0.93f, 0.76f, 0.97f);
        private readonly Color strongGreen = new Color(0.20f, 0.45f, 0.23f, 0.98f);
        private readonly Color darkGreen = new Color(0.08f, 0.28f, 0.12f, 1f);
        private readonly Color goldColor = new Color(0.96f, 0.66f, 0.14f, 1f);
        private readonly Color red = new Color(0.88f, 0.22f, 0.18f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.88f, 1f);
        private readonly Color brown = new Color(0.19f, 0.11f, 0.05f, 1f);
        private readonly Color locked = new Color(0.62f, 0.64f, 0.57f, 0.88f);

        public int BottomMenuCount => 7;
        public bool UsesSimplifiedGrowthMenu => true;

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

            if (skillManager != null)
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
                idleMessageField = idleType.GetField("systemMessage", PrivateInstance);
            }
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);

            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "PeanutUiWhite";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
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
            BuildToast();
        }

        private void BuildMainHud()
        {
            GameObject player = Panel(mainPage.transform, "Player", 16f, 16f, 320f, 110f, cream);
            playerTitle = Label(player.transform, string.Empty, 14f, 5f, 292f, 34f, 23, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            hpFill = Bar(player.transform, 14f, 44f, 292f, 22f, red, out hpText);
            mpFill = Bar(player.transform, 14f, 75f, 292f, 22f, blue, out mpText);

            GameObject resources = Panel(mainPage.transform, "Resources", 470f, 16f, 520f, 56f, cream);
            goldText = ResourceCell(resources.transform, 0f, 174f);
            diamondText = ResourceCell(resources.transform, 174f, 172f);
            fragmentText = ResourceCell(resources.transform, 346f, 174f);

            GameObject stage = Panel(mainPage.transform, "Stage", 420f, 82f, 620f, 84f, cream);
            stageTitle = Label(stage.transform, string.Empty, 14f, 4f, 380f, 30f, 17, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            stageFill = Bar(stage.transform, 14f, 42f, 372f, 24f, goldColor, out stageProgressText);
            Button autoButton = UiButton(stage.transform, string.Empty, 398f, 37f, 108f, 38f, ToggleAutoBoss, paleGreen);
            autoBossText = autoButton.GetComponentInChildren<Text>();
            bossButton = UiButton(stage.transform, "균왕 도전", 516f, 35f, 90f, 42f, TryStartBoss, red, Color.white);

            GameObject status = Panel(mainPage.transform, "Status", 16f, 744f, 216f, 62f, cream);
            combatPowerText = Label(status.transform, string.Empty, 10f, 3f, 196f, 56f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);

            UiButton(mainPage.transform, "땅콩 메뉴", 1245f, 18f, 127f, 48f,
                () => quickMenu.SetActive(!quickMenu.activeSelf), paleGreen);
            BuildQuickMenu();
            BuildSkillDock();
            BuildBottomNavigation();
        }

        private void BuildQuickMenu()
        {
            quickMenu = Panel(mainPage.transform, "Quick Menu", 1090f, 78f, 282f, 292f, cream);
            Label(quickMenu.transform, "땅콩월드 바로가기", 12f, 8f, 258f, 30f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);

            string[] names = { "전사 성장", "전직", "검", "검술", "미니", "모험", "임무", "상점", "설정" };
            Page[] pages = { Page.Growth, Page.Advancement, Page.Swords, Page.Skills, Page.Minis, Page.Adventure, Page.Missions, Page.Shop, Page.Settings };
            for (int i = 0; i < names.Length; i++)
            {
                Page target = pages[i];
                UiButton(quickMenu.transform, names[i], 10f + (i % 3) * 90f, 46f + (i / 3) * 76f, 84f, 66f,
                    () =>
                    {
                        quickMenu.SetActive(false);
                        ShowPage(target);
                    }, paleGreen);
            }
            quickMenu.SetActive(false);
        }

        private void BuildSkillDock()
        {
            GameObject dock = Panel(mainPage.transform, "Skills", 936f, 600f, 436f, 138f, cream);
            Label(dock.transform, "자동 검술", 10f, 4f, 416f, 26f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < 4; i++)
            {
                Button button = UiButton(dock.transform, string.Empty, 10f + i * 104f, 36f, 96f, 88f,
                    () => ShowPage(Page.Skills), paleGreen);
                skillTexts[i] = button.GetComponentInChildren<Text>();
            }
        }

        private void BuildBottomNavigation()
        {
            string[] names = { "전사 성장", "검", "검술", "미니 땅콩", "모험", "수호 임무", "땅콩 상점" };
            Page[] pages = { Page.Growth, Page.Swords, Page.Skills, Page.Minis, Page.Adventure, Page.Missions, Page.Shop };
            for (int i = 0; i < names.Length; i++)
            {
                Page target = pages[i];
                UiButton(mainPage.transform, names[i], 238f + i * 132f, 744f, 127f, 62f,
                    () => ShowPage(target), paleGreen);
            }
        }

        private void BuildMenuFrame()
        {
            Image background = menuPage.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.color = new Color(0.90f, 0.95f, 0.82f, 1f);
            Panel(menuPage.transform, "Header", 0f, 0f, ReferenceWidth, 78f, strongGreen);
            UiButton(menuPage.transform, "뒤로", 16f, 14f, 62f, 50f, () => ShowPage(Page.Main), cream);
            menuTitle = Label(menuPage.transform, string.Empty, 94f, 10f, 520f, 56f, 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            menuResources = Label(menuPage.transform, string.Empty, 820f, 10f, 546f, 56f, 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            contentHost = CreateRect(menuPage.transform, "Content", 0f, 78f, ReferenceWidth, ReferenceHeight - 78f).gameObject;
        }

        private void BuildToast()
        {
            toastPanel = Panel(safeRoot, "Toast", 454f, 680f, 480f, 52f, darkGreen);
            toastText = Label(toastPanel.transform, string.Empty, 12f, 4f, 456f, 44f, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            int tier = ReadInt(advancementTierField, arena);
            playerTitle.text = $"땅콩전사   Lv.{Mathf.Max(1, CombatPower / 25)}   ·   전직 {tier}단계";
            SetBar(hpFill, hpText, PlayerHp, MaxHp, $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            SetBar(mpFill, mpText, PlayerMp, MaxMp, $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
            goldText.text = $"골드\n{Gold:N0}";
            diamondText.text = $"다이아\n{Diamonds:N0}";
            fragmentText.text = $"조각\n{Fragments:N0}";
            stageTitle.text = $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}";
            SetBar(stageFill, stageProgressText, stageFlow.MonsterKills, 100f, $"균왕 자격 {stageFlow.MonsterKills}/100");
            autoBossText.text = stageFlow.AutoChallenge ? "자동 도전 ON" : "자동 도전 OFF";
            bossButton.interactable = stageFlow.CanChallengeBoss;
            combatPowerText.text = $"전투력 {CombatPower:N0}\n처치 {stageFlow.MonsterKills}/100";

            int offset = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int i = 0; i < skillTexts.Length; i++)
            {
                int index = offset + i;
                int level = SkillLevels != null && index < SkillLevels.Length ? SkillLevels[index] : 1;
                float cooldown = SkillCooldowns != null && index < SkillCooldowns.Length ? Mathf.Max(0f, SkillCooldowns[index]) : 0f;
                skillTexts[i].text = $"{SkillNames[index]}\nLv.{level}\n{(cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "AUTO")}";
            }

            if (menuResources != null)
                menuResources.text = $"골드 {Gold:N0}   다이아 {Diamonds:N0}   조각 {Fragments:N0}";
            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
        }

        private void ShowPage(Page page)
        {
            currentPage = page;
            mainPage.SetActive(page == Page.Main);
            menuPage.SetActive(page != Page.Main);
            if (page == Page.Main) return;
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

            if (currentPage == Page.Growth) BuildGrowthPage();
            else if (currentPage == Page.Advancement) BuildAdvancementPage();
            else if (currentPage == Page.Swords) BuildSwordPage();
            else if (currentPage == Page.Skills) BuildSkillPage();
            else if (currentPage == Page.Minis) BuildMiniPage();
            else if (currentPage == Page.Adventure) BuildAdventurePage();
            else if (currentPage == Page.Missions) BuildMissionPage();
            else if (currentPage == Page.Shop) BuildShopPage();
            else if (currentPage == Page.Settings) BuildSettingsPage();
            RefreshAll();
        }

        private void BuildGrowthPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "현재 전투 능력",
                () => $"검 공격력 {AttackDamage:N1} · 전투력 {CombatPower:N0} · HP {MaxHp:N0} · MP {MaxMp:N0}");

            GameObject selector = Row(content, 70f, paleGreen);
            Label(selector.transform, "한 번에 강화", 18f, 8f, 520f, 52f, 19, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                UiButton(selector.transform, $"×{amount}", 850f + i * 128f, 10f, 116f, 50f,
                    () =>
                    {
                        purchaseAmount = amount;
                        RebuildContent();
                    }, purchaseAmount == amount ? goldColor : cream);
            }

            UpgradeRow(content, "검 공격력", attackLevelField, arena, 20L);
            UpgradeRow(content, "최대 생명력", hpLevelField, arena, 25L);
            UpgradeRow(content, "최대 마력", maxMpLevelField, arena, 30L);
            UpgradeRow(content, "생명력 회복", hpRegenLevelField, growth, 40L);
            UpgradeRow(content, "마력 회복", mpRegenLevelField, arena, 35L);
            UpgradeRow(content, "치명타 확률", critChanceLevelField, growth, 45L);
            UpgradeRow(content, "치명타 피해", critDamageLevelField, growth, 55L);
            UpgradeRow(content, "골드 획득", goldGainLevelField, growth, 65L);
            ActionRow(content, "전직", AdvancementStatus, "전직 화면", () => ShowPage(Page.Advancement));
        }

        private void BuildAdvancementPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "땅콩전사 전직", AdvancementStatus);
            Section(content, "다음 전직 조건", AdvancementRequirements);
            ActionRow(content, "전직 시도", () => "조건을 모두 충족하면 골드와 다이아를 소모합니다.", "전직", TryAdvance);
            Section(content, "전직 효과",
                () => "기본 능력치 증가 · 기본 공격 타수 증가 · 검술 공격 횟수 증가 · 2차 전직 시 미니 땅콩 3슬롯 해금");
        }

        private void BuildSwordPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "검 보관함",
                () => $"사냥 검 {ElementNames[ReadElement(huntingElementField)]} · 균왕 검 {ElementNames[ReadElement(bossElementField)]}\n검은 소환·3대1 합성·골드 강화로 성장합니다.");

            for (int i = 0; i < 4; i++)
            {
                int element = i;
                GameObject row = Row(content, 132f, i % 2 == 0 ? cream : paleGreen);
                Text info = Label(row.transform, string.Empty, 18f, 10f, 610f, 108f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text copies = Label(row.transform, string.Empty, 650f, 74f, 650f, 40f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiButton(row.transform, "사냥 장착", 650f, 18f, 145f, 48f, () => Equip(huntingElementField, element, "사냥 검"), cream);
                UiButton(row.transform, "균왕 장착", 810f, 18f, 145f, 48f, () => Equip(bossElementField, element, "균왕 검"), cream);
                UiButton(row.transform, "검 강화", 970f, 18f, 145f, 48f, () => UpgradeSword(element), goldColor);
                UiButton(row.transform, "합성", 1130f, 18f, 170f, 48f, () => Synthesize(element), paleGreen);

                Action refresh = () =>
                {
                    SwordProgressionPrototype.SwordRarity rarity = swords == null
                        ? SwordProgressionPrototype.SwordRarity.None
                        : swords.GetHighestRarity(element);
                    int level = swords == null ? 1 : swords.GetLevel(element);
                    float multiplier = swords == null ? 1f : swords.GetDamageMultiplier(element);
                    long cost = swords == null ? 0L : swords.GetUpgradeCost(element);
                    info.text = $"{ElementNames[element]} 검 · {SwordProgressionPrototype.RarityName(rarity)} · Lv.{level}\n전투 피해 ×{multiplier:0.000} · 강화 {cost:N0}G";
                    copies.text = swords == null
                        ? "검 시스템 초기화 대기"
                        : $"일반 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Common)} · 희귀 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Rare)} · 전설 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Legendary)} · 신화 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Mythic)}";
                };
                refreshers.Add(refresh);
                refresh();
            }

            ActionRow(content, "사냥 검 소환", () => "다이아 5개 · 획득 검을 사냥 슬롯에 장착", "소환",
                () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            ActionRow(content, "균왕 검 소환", () => "다이아 5개 · 획득 검을 균왕 슬롯에 장착", "소환",
                () => InvokePrivate(shop, "SummonSword", new object[] { true }));
        }

        private void BuildSkillPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "땅콩 검술", () => $"강화 조각 {Fragments:N0} · 사냥 4개와 균왕 4개를 별도로 자동 운용합니다.");

            for (int i = 0; i < 8; i++)
            {
                int index = i;
                GameObject row = Row(content, 88f, i % 2 == 0 ? cream : paleGreen);
                Text info = Label(row.transform, string.Empty, 18f, 8f, 800f, 70f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Button upgrade = UiButton(row.transform, string.Empty, 920f, 16f, 180f, 54f, () => UpgradeSkill(index), goldColor);
                Button toggle = UiButton(row.transform, string.Empty, 1115f, 16f, 185f, 54f, () => ToggleSkill(index), paleGreen);
                Text upgradeText = upgrade.GetComponentInChildren<Text>();
                Text toggleText = toggle.GetComponentInChildren<Text>();

                Action refresh = () =>
                {
                    int level = SkillLevels != null && index < SkillLevels.Length ? SkillLevels[index] : 1;
                    float cooldown = SkillCooldowns != null && index < SkillCooldowns.Length ? Mathf.Max(0f, SkillCooldowns[index]) : 0f;
                    bool enabledForAuto = SkillAuto == null || index >= SkillAuto.Length || SkillAuto[index];
                    info.text = $"{SkillNames[index]} · Lv.{level} · {(index < 4 ? "사냥" : "균왕")}용 · 쿨 {cooldown:0.0}초";
                    upgradeText.text = $"강화 {SkillCost(index, level)}조각";
                    toggleText.text = enabledForAuto ? "자동 사용 ON" : "자동 사용 OFF";
                };
                refreshers.Add(refresh);
                refresh();
            }
        }

        private void BuildMiniPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "미니 땅콩 원정대", MiniStatus);
            MiniRow(content, "미니 공격력", miniAttackLevelField, 80L);
            MiniRow(content, "미니 치명타 확률", miniCritLevelField, 100L);
            MiniRow(content, "미니 치명타 피해", miniCritDamageLevelField, 120L);
            ActionRow(content, "미니 알 구매", () => $"보유 알 {ReadInt(eggsField, idle)} · 다이아 3개", "구매",
                () => InvokePrivate(idle, "BuyEgg", null));
            ActionRow(content, "알 부화", IncubationStatus, "부화 시작",
                () => InvokePrivate(idle, "StartIncubation", null));
            Section(content, "편성 규칙",
                () => "사냥과 균왕 편성을 별도 저장하며 미니는 화염·냉기·번개 속성으로 독립 공격하고 사망하지 않습니다.");
        }

        private void BuildAdventurePage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "현재 모험", AdventureStatus);
            ActionRow(content, "자동 균왕 도전",
                () => stageFlow.AutoChallenge ? "ON · 100/100 즉시 입장" : "OFF · 100/100 이후에도 계속 방치 사냥",
                "전환", ToggleAutoBoss);
            ActionRow(content, "균왕 도전",
                () => stageFlow.CanChallengeBoss ? "도전 가능 · HP·MP·모든 쿨타임 초기화" : $"현재 {stageFlow.MonsterKills}/100",
                "도전", TryStartBoss);
            Section(content, "최근 방치·전투 알림", IdleMessage);
            BuildStageSelector(content);
            Section(content, "확정 규칙",
                () => "100마리는 도전 자격만 해금합니다. 도전하지 않으면 몬스터가 계속 등장합니다. 사냥 사망은 이전 스테이지, 균왕 사망은 현재 스테이지 0/100으로 복귀합니다.");
        }

        private void BuildStageSelector(Transform content)
        {
            GameObject header = Row(content, 72f, paleGreen);
            Text worldText = Label(header.transform, string.Empty, 18f, 8f, 760f, 54f, 20, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiButton(header.transform, "이전 월드", 900f, 10f, 170f, 50f, () => ChangeStageMapWorld(-1), cream);
            UiButton(header.transform, "다음 월드", 1090f, 10f, 190f, 50f, () => ChangeStageMapWorld(1), cream);
            Action headerRefresh = () => worldText.text = $"스테이지 선택 · 월드 {stageMapWorld} · 최고 해금 {FormatStage(HighestGlobalStage)}";
            refreshers.Add(headerRefresh);
            headerRefresh();

            GameObject grid = Row(content, 270f, cream);
            int highest = HighestGlobalStage;
            for (int stage = 1; stage <= StageFlowController.StagesPerWorld; stage++)
            {
                int stageValue = stage;
                int global = (stageMapWorld - 1) * StageFlowController.StagesPerWorld + stageValue;
                bool unlocked = global <= highest;
                bool current = stageMapWorld == stageFlow.World && stageValue == stageFlow.Stage;
                int column = (stageValue - 1) % 10;
                int row = (stageValue - 1) / 10;
                Color color = current ? goldColor : unlocked ? paleGreen : locked;
                Button button = UiButton(grid.transform, $"{stageMapWorld}-{stageValue}", 18f + column * 128f, 18f + row * 82f, 112f, 64f,
                    () => SelectStageFromMap(stageMapWorld, stageValue), color);
                button.interactable = unlocked;
            }
        }

        private void BuildMissionPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "수호 임무와 업적", () => "처치·스테이지·성장 진행에 따라 다이아와 검술 조각을 획득합니다.");
            ActionRow(content, "몬스터 정리", () => "누적 50마리 단위", "보상 수령",
                () => InvokePrivate(idle, "ClaimKillMission", null));
            ActionRow(content, "지역 개척", () => "2개 스테이지 진행 단위", "보상 수령",
                () => InvokePrivate(idle, "ClaimStageMission", null));
            ActionRow(content, "전사 성장", () => "전직과 미니 성장 단계", "보상 수령",
                () => InvokePrivate(idle, "ClaimGrowthAchievement", null));
        }

        private void BuildShopPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "땅콩 상점", () => shop == null ? "상점 초기화 대기" : shop.ShopMessage);
            ActionRow(content, "오늘의 보급", () => $"연속 접속 {(shop == null ? 0 : shop.DailyStreak)}일", "받기",
                () => InvokePrivate(shop, "ClaimDailyReward", null));
            ActionRow(content, "사냥 검 소환", () => "다이아 5개 · 일반/희귀/전설/신화", "소환",
                () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            ActionRow(content, "균왕 검 소환", () => "다이아 5개 · 균왕 슬롯 장착", "소환",
                () => InvokePrivate(shop, "SummonSword", new object[] { true }));
            ActionRow(content, "미니 알", () => "다이아 3개 · 부화 도감", "구매",
                () => InvokePrivate(shop, "BuyEgg", null));
        }

        private void BuildSettingsPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "실행 설정", () => $"목표 {Application.targetFrameRate} FPS · 가로 화면 · 안전영역 자동 적용");
            ActionRow(content, "60 FPS", () => "일반 플레이", "적용", () => SetPerformance(60));
            ActionRow(content, "30 FPS 절전", () => "발열과 배터리 사용 감소", "적용", () => SetPerformance(30));
            ActionRow(content, "즉시 저장", () => "스테이지·자원·성장·검·미니 데이터", "저장",
                () => InvokePrivate(saveBridge, "Save", null));
            Section(content, "검증", () => "Console에서 Runtime Audit와 Feature Audit의 PASS 여부를 확인하십시오.");
        }

        private void UpgradeRow(Transform content, string name, FieldInfo field, object target, long baseCost)
        {
            GameObject row = Row(content, 82f, cream);
            Text info = Label(row.transform, string.Empty, 20f, 8f, 780f, 66f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button button = UiButton(row.transform, string.Empty, 1050f, 14f, 260f, 54f,
                () => Upgrade(field, target, baseCost, name), paleGreen);
            Text buttonText = button.GetComponentInChildren<Text>();

            Action refresh = () =>
            {
                int level = ReadLevel(field, target);
                long cost = UpgradeCost(baseCost, level, purchaseAmount);
                info.text = $"{name}\nLv.{level:N0} → Lv.{level + purchaseAmount:N0}";
                buttonText.text = $"강화 {cost:N0}G";
            };
            refreshers.Add(refresh);
            refresh();
        }

        private void MiniRow(Transform content, string name, FieldInfo field, long baseCost)
        {
            ActionRow(content, name,
                () =>
                {
                    int level = ReadLevel(field, idle);
                    return $"Lv.{level} · 다음 비용 {baseCost * level:N0}G";
                },
                "강화",
                () =>
                {
                    int level = ReadLevel(field, idle);
                    long cost = baseCost * level;
                    if (!SpendGold(cost))
                    {
                        Toast($"골드 부족 · {cost:N0}G 필요");
                        return;
                    }
                    field?.SetValue(idle, level + 1);
                    Toast($"{name} Lv.{level + 1}");
                });
        }

        private void Section(Transform content, string title, Func<string> detail)
        {
            GameObject row = Row(content, 112f, paleGreen);
            Label(row.transform, title, 18f, 8f, 340f, 36f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, string.Empty, 370f, 8f, 930f, 92f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Action refresh = () => body.text = detail == null ? string.Empty : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void ActionRow(Transform content, string title, Func<string> detail, string actionName, Action action)
        {
            GameObject row = Row(content, 84f, cream);
            Label(row.transform, title, 18f, 8f, 360f, 68f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, string.Empty, 390f, 8f, 650f, 68f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiButton(row.transform, actionName, 1080f, 15f, 220f, 54f,
                () =>
                {
                    action?.Invoke();
                    RefreshAll();
                }, goldColor);
            Action refresh = () => body.text = detail == null ? string.Empty : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void Upgrade(FieldInfo field, object target, long baseCost, string name)
        {
            if (field == null || target == null)
            {
                Toast($"{name} 연결 대기");
                return;
            }

            int level = ReadLevel(field, target);
            long cost = UpgradeCost(baseCost, level, purchaseAmount);
            if (!SpendGold(cost))
            {
                Toast($"골드 부족 · {cost:N0}G 필요");
                return;
            }

            field.SetValue(target, level + purchaseAmount);
            if (field == hpLevelField || field == maxMpLevelField)
                InvokePrivate(arena, "FullRestore", null);
            Toast($"{name} ×{purchaseAmount} 강화 완료");
        }

        private void TryAdvance()
        {
            int before = ReadInt(advancementTierField, arena);
            InvokePrivate(arena, "TryAdvance", null);
            int after = ReadInt(advancementTierField, arena);
            Toast(after > before ? $"전직 성공 · {after}단계" : "전직 조건을 확인하십시오");
        }

        private void UpgradeSword(int element)
        {
            if (swords == null)
            {
                Toast("검 성장 초기화 대기");
                return;
            }
            swords.UpgradeSword(element);
            Toast(swords.LastMessage);
        }

        private void Synthesize(int element)
        {
            if (swords == null) return;
            bool success = swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Common);
            if (!success) success = swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Rare);
            if (!success) swords.ManualSynthesize(element, SwordProgressionPrototype.SwordRarity.Legendary);
            Toast(swords.LastMessage);
        }

        private void Equip(FieldInfo field, int element, string slot)
        {
            if (field == null) return;
            field.SetValue(arena, Enum.ToObject(field.FieldType, element));
            Toast($"{slot}에 {ElementNames[element]} 장착");
        }

        private void UpgradeSkill(int index)
        {
            int[] levels = SkillLevels;
            if (levels == null || index < 0 || index >= levels.Length) return;
            long cost = SkillCost(index, levels[index]);
            if (Fragments < cost)
            {
                Toast($"조각 부족 · {cost}개 필요");
                return;
            }
            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            Toast($"{SkillNames[index]} Lv.{levels[index]}");
        }

        private void ToggleSkill(int index)
        {
            bool[] auto = SkillAuto;
            if (auto == null || index < 0 || index >= auto.Length)
            {
                Toast("자동 검술 연결 대기");
                return;
            }
            auto[index] = !auto[index];
            PlayerPrefs.SetInt("PeanutWarrior.SkillAuto." + index, auto[index] ? 1 : 0);
            PlayerPrefs.Save();
            Toast($"{SkillNames[index]} 자동 {(auto[index] ? "ON" : "OFF")}");
        }

        private void ToggleAutoBoss()
        {
            stageFlow.SetAutoChallenge(!stageFlow.AutoChallenge);
            Toast(stageFlow.AutoChallenge ? "자동 균왕 도전 ON" : "자동 균왕 도전 OFF");
        }

        private void TryStartBoss()
        {
            if (!stageFlow.TryStartBossBattle())
                Toast("일반 몬스터 100마리를 먼저 처치해야 합니다");
        }

        private void ChangeStageMapWorld(int direction)
        {
            int highestWorld = Mathf.Max(1, (HighestGlobalStage - 1) / StageFlowController.StagesPerWorld + 1);
            int target = Mathf.Clamp(stageMapWorld + direction, 1, highestWorld);
            if (target == stageMapWorld)
            {
                Toast(direction < 0 ? "첫 번째 월드입니다" : "아직 다음 월드가 해금되지 않았습니다");
                return;
            }
            stageMapWorld = target;
            RebuildContent();
        }

        private void SelectStageFromMap(int world, int stage)
        {
            int global = (world - 1) * StageFlowController.StagesPerWorld + stage;
            if (global > HighestGlobalStage)
            {
                Toast("아직 해금되지 않은 스테이지입니다");
                return;
            }
            stageFlow.SelectStage(world, stage);
            Toast($"{world}-{stage}로 이동");
            RebuildContent();
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
            bool unlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            return $"전직 {tier}단계 · 기본 공격 {tier + 1}타 · 미니 {(unlocked ? "3/3 해금" : "잠김")}";
        }

        private string AdvancementRequirements()
        {
            int tier = ReadInt(advancementTierField, arena);
            if (tier >= 2) return "현재 프로토타입 최고 전직 단계";
            return tier == 0
                ? "전체 스테이지 2 · 전투력 180 · 골드 150 · 다이아 5"
                : "전체 스테이지 4 · 전투력 420 · 골드 500 · 다이아 15";
        }

        private string MiniStatus()
        {
            bool unlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            return unlocked
                ? $"미니 3/3 활동 · 부화 도감 {ReadInt(hatchedMinisField, idle)}"
                : "2차 전직 후 미니 3슬롯 해금";
        }

        private string IncubationStatus()
        {
            bool active = incubatingField != null && idle != null && (bool)incubatingField.GetValue(idle);
            return active
                ? $"부화 중 · {Mathf.CeilToInt(ReadFloat(incubationRemainingField, idle))}초"
                : $"대기 · 보유 알 {ReadInt(eggsField, idle)}";
        }

        private string AdventureStatus()
        {
            return $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage} · 처치 {stageFlow.MonsterKills}/100 · 최고 {FormatStage(HighestGlobalStage)}";
        }

        private string IdleMessage()
        {
            if (idle == null || idleMessageField == null) return "방치 시스템 초기화 대기";
            return idleMessageField.GetValue(idle) as string ?? "최근 알림 없음";
        }

        private GameObject Row(Transform content, float height, Color color)
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

        private Transform ScrollContent(Transform parent, float x, float y, float width, float height)
        {
            GameObject scrollObject = Panel(parent, "Scroll", x, y, width, height, new Color(1f, 1f, 1f, 0.18f));
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 36f;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = CreateRect(scrollObject.transform, "Viewport", 8f, 8f, width - 16f, height - 16f);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.sprite = whiteSprite;
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

        private Button UiButton(Transform parent, string value, float x, float y, float width, float height,
            Action action, Color color, Color? textColor = null)
        {
            GameObject go = Panel(parent, "Button", x, y, width, height, color);
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.78f, 0.88f, 0.70f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.75f);
            button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            Label(go.transform, value, 5f, 3f, width - 10f, height - 6f, 15,
                textColor ?? brown, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            fill.sprite = whiteSprite;
            fill.color = color;
            text = Label(back.transform, string.Empty, 0f, 0f, width, height, 13, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return fill;
        }

        private Text ResourceCell(Transform parent, float x, float width)
        {
            return Label(parent, string.Empty, x, 0f, width, 56f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static void SetBar(Image fill, Text text, float current, float maximum, string label)
        {
            if (fill == null || text == null) return;
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            RectTransform rect = fill.rectTransform;
            Vector2 size = rect.sizeDelta;
            size.x = Mathf.Max(0f, (rect.parent as RectTransform).rect.width - 4f) * ratio;
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
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
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

        private int ReadElement(FieldInfo field)
        {
            return field == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private string PageTitle(Page page)
        {
            return page switch
            {
                Page.Growth => "전사 성장",
                Page.Advancement => "전직",
                Page.Swords => "검 보관함",
                Page.Skills => "땅콩 검술",
                Page.Minis => "미니 땅콩",
                Page.Adventure => "모험",
                Page.Missions => "수호 임무",
                Page.Shop => "땅콩 상점",
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
        private float AttackDamage => ReadProperty(attackDamageProperty, arena, 0f);
        private int CombatPower => combatPowerProperty == null ? Mathf.RoundToInt(AttackDamage * 10f) : Convert.ToInt32(combatPowerProperty.GetValue(arena));
        private int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        private float[] SkillCooldowns => skillCooldownsField?.GetValue(arena) as float[];
        private bool[] SkillAuto => skillAutoField?.GetValue(skillManager) as bool[];
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

        private static long SkillCost(int index, int level)
        {
            return 2L + Mathf.Max(1, level) * 2L + index / 4;
        }

        private static string FormatStage(int globalStage)
        {
            int world = (globalStage - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (globalStage - 1) % StageFlowController.StagesPerWorld + 1;
            return $"{world}-{stage}";
        }
    }
}
