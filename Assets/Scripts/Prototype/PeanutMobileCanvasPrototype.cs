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
    [DefaultExecutionOrder(25000)]
    public sealed class PeanutMobileCanvasPrototype : MonoBehaviour
    {
        private enum Page { Main, Growth, Swords, Skills, Minis, Adventure, Missions, Shop, Settings }

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

        private Font font;
        private Sprite whiteSprite;
        private Page currentPage;
        private int growthTab;
        private int purchaseAmount = 1;
        private float refreshTimer;
        private float toastTimer;
        private Rect lastSafeArea;
        private readonly List<Action> refreshers = new List<Action>();

        private readonly Color cream = new Color(0.98f, 0.93f, 0.72f, 0.97f);
        private readonly Color paleGreen = new Color(0.82f, 0.93f, 0.76f, 0.97f);
        private readonly Color strongGreen = new Color(0.20f, 0.45f, 0.23f, 0.98f);
        private readonly Color darkGreen = new Color(0.08f, 0.28f, 0.12f, 1f);
        private readonly Color goldColor = new Color(0.96f, 0.66f, 0.14f, 1f);
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
            InputSystemUIInputModule module = root.AddComponent<InputSystemUIInputModule>();
            MethodInfo assign = typeof(InputSystemUIInputModule).GetMethod(
                "AssignDefaultActions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            assign?.Invoke(module, null);
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
            playerTitle = Label(player.transform, "", 14f, 5f, 292f, 34f, 24, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            hpFill = Bar(player.transform, 14f, 44f, 292f, 22f, red, out hpText);
            mpFill = Bar(player.transform, 14f, 75f, 292f, 22f, blue, out mpText);

            GameObject resources = Panel(mainPage.transform, "Resources", 480f, 16f, 510f, 56f, cream);
            goldText = ResourceCell(resources.transform, 0f);
            diamondText = ResourceCell(resources.transform, 172f);
            fragmentText = ResourceCell(resources.transform, 344f);

            GameObject stage = Panel(mainPage.transform, "Stage", 430f, 82f, 600f, 84f, cream);
            stageTitle = Label(stage.transform, "", 14f, 4f, 360f, 30f, 17, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            stageFill = Bar(stage.transform, 14f, 42f, 352f, 24f, goldColor, out stageProgressText);
            Button autoButton = UiButton(stage.transform, "", 378f, 37f, 104f, 38f, ToggleAutoBoss, paleGreen);
            autoBossText = autoButton.GetComponentInChildren<Text>();
            bossButton = UiButton(stage.transform, "균왕 도전", 492f, 35f, 94f, 42f, TryStartBoss, red, Color.white);

            GameObject status = Panel(mainPage.transform, "Status", 16f, 744f, 216f, 62f, cream);
            combatPowerText = Label(status.transform, "", 10f, 3f, 196f, 56f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);

            UiButton(mainPage.transform, "땅콩 메뉴", 1245f, 18f, 127f, 48f,
                () => quickMenu.SetActive(!quickMenu.activeSelf), paleGreen);
            BuildQuickMenu();
            BuildSkillDock();
            BuildBottomNavigation();
        }

        private void BuildQuickMenu()
        {
            quickMenu = Panel(mainPage.transform, "Quick Menu", 1090f, 78f, 282f, 360f, cream);
            Label(quickMenu.transform, "땅콩월드 바로가기", 12f, 8f, 258f, 30f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            string[] names = { "전직", "검 보관함", "알 부화", "스테이지", "방치 연구", "수호 임무", "검술 연구", "업적", "소환 상점", "설정", "도움말", "저장" };
            Page[] pages = { Page.Growth, Page.Swords, Page.Minis, Page.Adventure, Page.Growth, Page.Missions, Page.Skills, Page.Missions, Page.Shop, Page.Settings, Page.Settings, Page.Settings };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                UiButton(quickMenu.transform, names[i], 10f + (i % 3) * 90f, 46f + (i / 3) * 74f, 84f, 64f,
                    () =>
                    {
                        quickMenu.SetActive(false);
                        if (index == 0) growthTab = 2;
                        if (index == 4) growthTab = 4;
                        if (index == 11) InvokePrivate(saveBridge, "Save", null);
                        ShowPage(pages[index]);
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
                Button button = UiButton(dock.transform, "", 10f + i * 104f, 36f, 96f, 88f, () => ShowPage(Page.Skills), paleGreen);
                skillTexts[i] = button.GetComponentInChildren<Text>();
            }
        }

        private void BuildBottomNavigation()
        {
            string[] names = { "전사 성장", "검", "검술", "미니 땅콩", "모험", "수호 임무", "땅콩 상점" };
            Page[] pages = { Page.Growth, Page.Swords, Page.Skills, Page.Minis, Page.Adventure, Page.Missions, Page.Shop };
            for (int i = 0; i < names.Length; i++)
            {
                Page page = pages[i];
                UiButton(mainPage.transform, names[i], 238f + i * 132f, 744f, 127f, 62f, () => ShowPage(page), paleGreen);
            }
        }

        private void BuildMenuFrame()
        {
            Image background = menuPage.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.color = new Color(0.90f, 0.95f, 0.82f, 1f);
            Panel(menuPage.transform, "Header", 0f, 0f, ReferenceWidth, 78f, strongGreen);
            UiButton(menuPage.transform, "뒤로", 16f, 14f, 62f, 50f, () => ShowPage(Page.Main), cream);
            menuTitle = Label(menuPage.transform, "", 94f, 10f, 520f, 56f, 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            menuResources = Label(menuPage.transform, "", 900f, 10f, 466f, 56f, 17, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            contentHost = CreateRect(menuPage.transform, "Content", 0f, 78f, ReferenceWidth, ReferenceHeight - 78f).gameObject;
        }

        private void BuildToast()
        {
            toastPanel = Panel(safeRoot, "Toast", 454f, 680f, 480f, 52f, darkGreen);
            toastText = Label(toastPanel.transform, "", 12f, 4f, 456f, 44f, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            for (int i = 0; i < 4; i++)
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
            string[] tabs = { "기초 능력", "껍질 단련", "전직", "속성 연구", "방치 효율" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int tab = i;
                UiButton(contentHost.transform, tabs[i], 20f + i * 184f, 12f, 174f, 50f,
                    () => { growthTab = tab; RebuildContent(); }, growthTab == i ? goldColor : paleGreen);
            }
            Transform content = ScrollContent(contentHost.transform, 20f, 78f, 1348f, 646f);
            if (growthTab == 0) BuildBaseGrowth(content);
            else if (growthTab == 1) BuildShellGrowth(content);
            else if (growthTab == 2) BuildAdvancement(content);
            else if (growthTab == 3) BuildElementResearch(content);
            else BuildIdleGrowth(content);
        }

        private void BuildBaseGrowth(Transform content)
        {
            Section(content, "현재 전투 능력", () => $"검 공격력 {AttackDamage:N1} · 전투력 {CombatPower:N0} · HP {MaxHp:N0} · MP {MaxMp:N0}");
            GameObject selector = Row(content, 70f, paleGreen);
            Label(selector.transform, "한 번에 강화", 18f, 8f, 520f, 52f, 19, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                UiButton(selector.transform, $"×{amount}", 850f + i * 128f, 10f, 116f, 50f,
                    () => { purchaseAmount = amount; RebuildContent(); }, purchaseAmount == amount ? goldColor : cream);
            }
            UpgradeRow(content, "검 공격력", attackLevelField, arena, 20L);
            UpgradeRow(content, "껍질 생명력", hpLevelField, arena, 25L);
            UpgradeRow(content, "최대 마력", maxMpLevelField, arena, 30L);
            UpgradeRow(content, "껍질 재생", hpRegenLevelField, growth, 40L);
            UpgradeRow(content, "마력 회복", mpRegenLevelField, arena, 35L);
            UpgradeRow(content, "정밀 베기", critChanceLevelField, growth, 45L);
            UpgradeRow(content, "치명 일격", critDamageLevelField, growth, 55L);
            UpgradeRow(content, "땅콩 수확량", goldGainLevelField, growth, 65L);
        }

        private void BuildShellGrowth(Transform content)
        {
            Section(content, "껍질 단련", () => "별도 방어구 대신 껍질 자체를 단련하며 실제 HP와 재생 능력에 연결됩니다.");
            ActionRow(content, "껍질 생명 단련", () => $"Lv.{MetaValue(m => m.ShellVitalityLevel, 1)} · {MetaValue(m => m.ShellVitalityCost, 0L):N0}G", "단련",
                () => RunMeta(() => meta != null && meta.UpgradeShellVitality()));
            ActionRow(content, "껍질 재생 단련", () => $"Lv.{MetaValue(m => m.ShellRecoveryLevel, 1)} · {MetaValue(m => m.ShellRecoveryCost, 0L):N0}G", "단련",
                () => RunMeta(() => meta != null && meta.UpgradeShellRecovery()));
            Section(content, "전직 연계", () => "전직 단계가 오르면 껍질 명칭과 기본 공격 타수가 변하고 2차 전직에서 미니 3슬롯이 해금됩니다.");
        }

        private void BuildAdvancement(Transform content)
        {
            Section(content, "땅콩전사 전직", AdvancementStatus);
            ActionRow(content, "다음 껍질로 전직", AdvancementRequirements, "전직 시도", TryAdvance);
            Section(content, "전직 효과", () => "능력치 증가 · 기본 공격 타수 증가 · 검술 공격 횟수 증가 · 2차 전직 미니 3슬롯 해금");
        }

        private void BuildElementResearch(Transform content)
        {
            Section(content, "속성 연구", () => "검 등급·검 레벨과 함께 실제 기본 공격 및 자동 검술 보너스에 적용됩니다.");
            for (int i = 0; i < 4; i++)
            {
                int element = i;
                ActionRow(content, ElementNames[element],
                    () => $"연구 Lv.{MetaValue(m => m.GetElementResearchLevel(element), 1)} · 피해 ×{MetaValue(m => m.GetElementDamageMultiplier(element), 1f):0.000} · {MetaValue(m => m.ElementResearchCost(element), 0L):N0}조각",
                    "연구", () => RunMeta(() => meta != null && meta.UpgradeElementResearch(element)));
            }
        }

        private void BuildIdleGrowth(Transform content)
        {
            Section(content, "방치 효율", () => $"최대 방치 {MetaValue(m => m.MaximumOfflineHours, 8)}시간 · 기본 보상에 연구 보너스 추가");
            ActionRow(content, "방치 골드 연구", () => $"Lv.{MetaValue(m => m.IdleGoldLevel, 1)} · ×{MetaValue(m => m.OfflineGoldMultiplier, 1f):0.00} · {MetaValue(m => m.IdleGoldCost, 0L):N0}G", "연구",
                () => RunMeta(() => meta != null && meta.UpgradeIdleGold()));
            ActionRow(content, "방치 조각 연구", () => $"Lv.{MetaValue(m => m.IdleFragmentLevel, 1)} · ×{MetaValue(m => m.OfflineFragmentMultiplier, 1f):0.00} · {MetaValue(m => m.IdleFragmentCost, 0L):N0}G", "연구",
                () => RunMeta(() => meta != null && meta.UpgradeIdleFragments()));
            ActionRow(content, "최대 방치 시간", () => $"Lv.{MetaValue(m => m.IdleHourLevel, 1)} · 최대 {MetaValue(m => m.MaximumOfflineHours, 8)}시간 · {MetaValue(m => m.IdleHourCost, 0L):N0}G", "확장",
                () => RunMeta(() => meta != null && meta.UpgradeIdleHours()));
        }

        private void BuildSwordPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "검 보관함", () => $"사냥 검 {ElementNames[ReadElement(huntingElementField)]} · 균왕 검 {ElementNames[ReadElement(bossElementField)]}\n검은 소환·3대1 합성·골드 강화로 성장합니다.");
            for (int i = 0; i < 4; i++)
            {
                int element = i;
                GameObject row = Row(content, 132f, i % 2 == 0 ? cream : paleGreen);
                Text info = Label(row.transform, "", 18f, 10f, 610f, 108f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text copies = Label(row.transform, "", 650f, 74f, 650f, 40f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiButton(row.transform, "사냥 장착", 650f, 18f, 145f, 48f, () => Equip(huntingElementField, element, "사냥 검"), cream);
                UiButton(row.transform, "균왕 장착", 810f, 18f, 145f, 48f, () => Equip(bossElementField, element, "균왕 검"), cream);
                UiButton(row.transform, "검 강화", 970f, 18f, 145f, 48f, () => UpgradeSword(element), goldColor);
                UiButton(row.transform, "합성", 1130f, 18f, 170f, 48f, () => Synthesize(element), paleGreen);
                Action refresh = () =>
                {
                    SwordProgressionPrototype.SwordRarity rarity = swords == null ? SwordProgressionPrototype.SwordRarity.None : swords.GetHighestRarity(element);
                    int level = swords == null ? 1 : swords.GetLevel(element);
                    float multiplier = swords == null ? 1f : swords.GetDamageMultiplier(element);
                    long cost = swords == null ? 0L : swords.GetUpgradeCost(element);
                    info.text = $"{ElementNames[element]} 검 · {SwordProgressionPrototype.RarityName(rarity)} · Lv.{level}\n전투 피해 ×{multiplier:0.000} · 강화 {cost:N0}G";
                    copies.text = swords == null ? "검 시스템 초기화 대기" :
                        $"일반 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Common)} · 희귀 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Rare)} · 전설 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Legendary)} · 신화 {swords.GetCopies(element, SwordProgressionPrototype.SwordRarity.Mythic)}";
                };
                refreshers.Add(refresh);
                refresh();
            }
            ActionRow(content, "사냥 검 소환", () => "다이아 5개 · 사냥 슬롯 자동 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            ActionRow(content, "균왕 검 소환", () => "다이아 5개 · 균왕 슬롯 자동 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }));
        }

        private void BuildSkillPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "땅콩 검술", () => $"강화 조각 {Fragments:N0} · 사냥 4개와 균왕 4개를 별도 운용");
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                GameObject row = Row(content, 88f, i % 2 == 0 ? cream : paleGreen);
                Text info = Label(row.transform, "", 18f, 8f, 800f, 70f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Button upgrade = UiButton(row.transform, "", 920f, 16f, 180f, 54f, () => UpgradeSkill(index), goldColor);
                Button toggle = UiButton(row.transform, "", 1115f, 16f, 185f, 54f, () => ToggleSkill(index), paleGreen);
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
            ActionRow(content, "미니 알 구매", () => $"보유 알 {ReadInt(eggsField, idle)} · 다이아 3개", "구매", () => InvokePrivate(idle, "BuyEgg", null));
            ActionRow(content, "알 부화", IncubationStatus, "부화 시작", () => InvokePrivate(idle, "StartIncubation", null));
            Section(content, "편성 규칙", () => "사냥과 균왕 편성을 별도 저장하며 미니는 화염·냉기·번개 속성으로 독립 공격하고 사망하지 않습니다.");
        }

        private void BuildAdventurePage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "현재 모험", AdventureStatus);
            ActionRow(content, "이전 해금 스테이지", () => "한 단계 이전으로 이동", "이동", () => MoveStage(-1));
            ActionRow(content, "다음 해금 스테이지", () => $"최고 해금 {FormatStage(HighestGlobalStage)}", "이동", () => MoveStage(1));
            ActionRow(content, "자동 균왕 도전", () => stageFlow.AutoChallenge ? "ON · 100/100 즉시 입장" : "OFF · 100/100 이후에도 계속 방치 사냥", "전환", ToggleAutoBoss);
            ActionRow(content, "균왕 도전", () => stageFlow.CanChallengeBoss ? "도전 가능 · HP·MP·쿨타임 초기화" : $"현재 {stageFlow.MonsterKills}/100", "도전", TryStartBoss);
            Section(content, "확정 규칙", () => "100마리는 도전 자격만 해금합니다. 사냥 사망은 이전 스테이지, 균왕 사망은 현재 스테이지 0/100으로 복귀합니다.");
        }

        private void BuildMissionPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "수호 임무와 업적", () => "처치·스테이지·성장 진행에 따라 다이아와 검술 조각을 획득합니다.");
            ActionRow(content, "몬스터 정리", () => "누적 50마리 단위", "보상 수령", () => InvokePrivate(idle, "ClaimKillMission", null));
            ActionRow(content, "지역 개척", () => "2개 스테이지 진행 단위", "보상 수령", () => InvokePrivate(idle, "ClaimStageMission", null));
            ActionRow(content, "전사 성장", () => "전직과 미니 성장 단계", "보상 수령", () => InvokePrivate(idle, "ClaimGrowthAchievement", null));
        }

        private void BuildShopPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "땅콩 상점", () => shop == null ? "상점 초기화 대기" : shop.ShopMessage);
            ActionRow(content, "오늘의 보급", () => $"연속 접속 {(shop == null ? 0 : shop.DailyStreak)}일", "받기", () => InvokePrivate(shop, "ClaimDailyReward", null));
            ActionRow(content, "사냥 검 소환", () => "다이아 5개 · 일반/희귀/전설/신화", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }));
            ActionRow(content, "균왕 검 소환", () => "다이아 5개 · 균왕 슬롯 장착", "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }));
            ActionRow(content, "미니 알", () => "다이아 3개 · 부화 도감", "구매", () => InvokePrivate(shop, "BuyEgg", null));
        }

        private void BuildSettingsPage()
        {
            Transform content = ScrollContent(contentHost.transform, 20f, 16f, 1348f, 710f);
            Section(content, "실행 설정", () => $"목표 {Application.targetFrameRate} FPS · 가로 화면 · 안전영역 자동 적용");
            ActionRow(content, "60 FPS", () => "일반 플레이", "적용", () => SetPerformance(60));
            ActionRow(content, "30 FPS 절전", () => "발열과 배터리 사용 감소", "적용", () => SetPerformance(30));
            ActionRow(content, "즉시 저장", () => "스테이지·자원·성장·검·미니 데이터", "저장", () => InvokePrivate(saveBridge, "Save", null));
            Section(content, "검토 상태", () => "Console의 [PeanutWarrior Runtime Audit]가 PASS인지 확인하십시오. 실제 이미지·애니메이션·사운드는 리소스 제작 후 교체해야 합니다.");
        }

        private void UpgradeRow(Transform content, string name, FieldInfo field, object target, long baseCost)
        {
            GameObject row = Row(content, 82f, cream);
            Text info = Label(row.transform, "", 20f, 8f, 780f, 66f, 18, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button button = UiButton(row.transform, "", 1050f, 14f, 260f, 54f, () => Upgrade(field, target, baseCost, name), paleGreen);
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
            ActionRow(content, name, () =>
            {
                int level = ReadLevel(field, idle);
                return $"Lv.{level} · 다음 비용 {baseCost * level:N0}G";
            }, "강화", () =>
            {
                int level = ReadLevel(field, idle);
                long cost = baseCost * level;
                if (!SpendGold(cost)) { Toast($"골드 부족 · {cost:N0}G 필요"); return; }
                field?.SetValue(idle, level + 1);
                Toast($"{name} Lv.{level + 1}");
            });
        }

        private void Section(Transform content, string title, Func<string> detail)
        {
            GameObject row = Row(content, 112f, paleGreen);
            Label(row.transform, title, 18f, 8f, 340f, 36f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, "", 370f, 8f, 930f, 92f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Action refresh = () => body.text = detail == null ? "" : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void ActionRow(Transform content, string title, Func<string> detail, string actionName, Action action)
        {
            GameObject row = Row(content, 84f, cream);
            Label(row.transform, title, 18f, 8f, 360f, 68f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = Label(row.transform, "", 390f, 8f, 650f, 68f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiButton(row.transform, actionName, 1080f, 15f, 220f, 54f, () => { action?.Invoke(); RefreshAll(); }, goldColor);
            Action refresh = () => body.text = detail == null ? "" : detail();
            refreshers.Add(refresh);
            refresh();
        }

        private void Upgrade(FieldInfo field, object target, long baseCost, string name)
        {
            if (field == null || target == null) { Toast($"{name} 연결 대기"); return; }
            int level = ReadLevel(field, target);
            long cost = UpgradeCost(baseCost, level, purchaseAmount);
            if (!SpendGold(cost)) { Toast($"골드 부족 · {cost:N0}G 필요"); return; }
            field.SetValue(target, level + purchaseAmount);
            if (field == hpLevelField || field == maxMpLevelField) InvokePrivate(arena, "FullRestore", null);
            Toast($"{name} ×{purchaseAmount} 강화 완료");
        }

        private void RunMeta(Func<bool> operation)
        {
            bool success = operation != null && operation();
            Toast(meta == null ? "전용 성장 초기화 대기" : meta.LastMessage);
            if (!success) RefreshAll();
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
            if (swords == null) { Toast("검 성장 초기화 대기"); return; }
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
            if (Fragments < cost) { Toast($"조각 부족 · {cost}개 필요"); return; }
            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            Toast($"{SkillNames[index]} Lv.{levels[index]}");
        }

        private void ToggleSkill(int index)
        {
            bool[] auto = SkillAuto;
            if (auto == null || index < 0 || index >= auto.Length) { Toast("자동 검술 연결 대기"); return; }
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
            if (!stageFlow.TryStartBossBattle()) Toast("일반 몬스터 100마리를 먼저 처치해야 합니다");
        }

        private void MoveStage(int direction)
        {
            int target = CurrentGlobalStage + direction;
            if (target < 1) { Toast("1-1보다 이전으로 이동할 수 없습니다"); return; }
            if (target > HighestGlobalStage) { Toast($"미해금 · 최고 {FormatStage(HighestGlobalStage)}"); return; }
            int world = (target - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (target - 1) % StageFlowController.StagesPerWorld + 1;
            stageFlow.SelectStage(world, stage);
            Toast($"{world}-{stage}로 이동");
        }

        private void SetPerformance(int fps)
        {
            Application.targetFrameRate = fps;
            QualitySettings.antiAliasing = fps >= 60 ? 2 : 0;
            Toast($"{fps} FPS 모드 적용");
        }

        private void InvokePrivate(object target, string methodName, object[] arguments)
        {
            if (target == null) { Toast("해당 시스템 초기화 대기"); return; }
            object[] args = arguments ?? Array.Empty<object>();
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++) types[i] = args[i].GetType();
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance, null, types, null);
            if (method == null) { Toast($"기능 연결 실패 · {methodName}"); return; }
            try { method.Invoke(target, args.Length == 0 ? null : args); }
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
            return tier == 0 ? "전체 스테이지 2 · 전투력 180 · 150G · 다이아 5" : "전체 스테이지 4 · 전투력 420 · 500G · 다이아 15";
        }

        private string MiniStatus()
        {
            bool unlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            return unlocked ? $"미니 3/3 활동 · 부화 도감 {ReadInt(hatchedMinisField, idle)}" : "2차 전직 후 미니 3슬롯 해금";
        }

        private string IncubationStatus()
        {
            bool active = incubatingField != null && idle != null && (bool)incubatingField.GetValue(idle);
            return active ? $"부화 중 · {Mathf.CeilToInt(ReadFloat(incubationRemainingField, idle))}초" : $"대기 · 보유 알 {ReadInt(eggsField, idle)}";
        }

        private string AdventureStatus()
        {
            return $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage} · 처치 {stageFlow.MonsterKills}/100 · 최고 {FormatStage(HighestGlobalStage)}";
        }

        private T MetaValue<T>(Func<MetaProgressionPrototype, T> selector, T fallback)
        {
            return meta == null || selector == null ? fallback : selector(meta);
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

        private Text Label(Transform parent, string value, float x, float y, float width, float height, int size, Color color, TextAnchor alignment, FontStyle style)
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

        private Button UiButton(Transform parent, string value, float x, float y, float width, float height, Action action, Color color, Color? textColor = null)
        {
            GameObject go = Panel(parent, "Button", x, y, width, height, color);
            Button button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.9f, 0.55f, 1f);
            colors.pressedColor = new Color(0.75f, 0.85f, 0.64f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            button.colors = colors;
            Label(go.transform, value, 4f, 3f, width - 8f, height - 6f, 15, textColor ?? brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }

        private Text ResourceCell(Transform parent, float x)
        {
            GameObject cell = Panel(parent, "Resource", x, 0f, 166f, 56f, paleGreen);
            return Label(cell.transform, "", 4f, 2f, 158f, 52f, 18, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private Image Bar(Transform parent, float x, float y, float width, float height, Color fillColor, out Text text)
        {
            GameObject back = Panel(parent, "Bar", x, y, width, height, new Color(0.16f, 0.20f, 0.13f, 0.92f));
            GameObject fillObject = Panel(back.transform, "Fill", 2f, 2f, width - 4f, height - 4f, fillColor);
            Image fill = fillObject.GetComponent<Image>();
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(2f, 0f);
            fillRect.sizeDelta = new Vector2(width - 4f, -4f);
            text = Label(back.transform, "", 0f, 0f, width, height, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return fill;
        }

        private static void SetBar(Image fill, Text text, float current, float maximum, string value)
        {
            if (fill != null)
            {
                float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
                RectTransform parent = fill.rectTransform.parent as RectTransform;
                if (parent != null) fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(0f, parent.rect.width - 4f) * ratio, -4f);
            }
            if (text != null) text.text = value;
        }

        private void ApplySafeArea()
        {
            if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            safeRoot.anchorMin = min;
            safeRoot.anchorMax = max;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
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

        private static long UpgradeCost(long baseCost, int level, int count)
        {
            long n = Mathf.Max(1, count);
            long start = Mathf.Max(1, level);
            return baseCost * n * (2L * start + n - 1L) / 2L;
        }

        private static long SkillCost(int index, int level) => 2L + Mathf.Max(1, level) * 2L + index / 4;
        private static int ReadLevel(FieldInfo field, object target) => field == null || target == null ? 1 : Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        private static int ReadInt(FieldInfo field, object target) => field == null || target == null ? 0 : Convert.ToInt32(field.GetValue(target));
        private static long ReadLong(FieldInfo field, object target) => field == null || target == null ? 0L : Convert.ToInt64(field.GetValue(target));
        private static float ReadFloat(FieldInfo field, object target) => field == null || target == null ? 0f : Convert.ToSingle(field.GetValue(target));
        private static float ReadProperty(PropertyInfo property, object target, float fallback) => property == null || target == null ? fallback : Convert.ToSingle(property.GetValue(target));

        private static string FormatStage(int global)
        {
            int world = (global - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (global - 1) % StageFlowController.StagesPerWorld + 1;
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
