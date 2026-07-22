using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Rebuilds the inner menu pages with a compact idle-RPG information hierarchy:
    /// summary first, current state second, and the main action at the right/bottom.
    /// The existing six-tab navigation and combat HUD stay intact.
    /// </summary>
    [DefaultExecutionOrder(28000)]
    public sealed class PeanutMenuLayoutV2 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string LayoutRootName = "Peanut Menu Layout V2";
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const float ContentWidth = 1388f;
        private const float ContentHeight = 650f;

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };
        private static readonly string[] ElementShortNames = { "무", "화", "냉", "번" };
        private static readonly Color[] ElementColors =
        {
            new Color(0.78f, 0.63f, 0.18f),
            new Color(0.88f, 0.28f, 0.17f),
            new Color(0.18f, 0.58f, 0.82f),
            new Color(0.53f, 0.31f, 0.82f)
        };

        private PeanutMobileCanvasPrototype sourceUi;
        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private SkillManagementPrototype skills;
        private SwordProgressionPrototype equipment;
        private IdleSystemsPrototype pets;
        private PrototypeShopAndDaily shop;
        private PrototypeSaveBridge saveBridge;

        private FieldInfo contentHostField;
        private FieldInfo currentPageField;
        private FieldInfo sourceRefreshersField;
        private FieldInfo navBackgroundsField;
        private MethodInfo sourceToastMethod;
        private GameObject contentHost;
        private GameObject customRoot;
        private Image[] navBackgrounds;
        private readonly List<GameObject> navUnderlines = new List<GameObject>();

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

        private FieldInfo petAttackLevelField;
        private FieldInfo petCritLevelField;
        private FieldInfo petCritDamageLevelField;
        private FieldInfo eggsField;
        private FieldInfo hatchedPetsField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;
        private FieldInfo petMessageField;

        private Font font;
        private Sprite whiteSprite;
        private Sprite roundedSprite;
        private string activePage = string.Empty;
        private int purchaseAmount = 1;
        private int stageMapWorld = 1;
        private float refreshTimer;
        private readonly List<Action> refreshers = new List<Action>();

        private readonly Color pageBackground = new Color(0.93f, 0.95f, 0.87f, 1f);
        private readonly Color cream = new Color(0.98f, 0.95f, 0.82f, 1f);
        private readonly Color card = new Color(0.97f, 0.94f, 0.85f, 1f);
        private readonly Color cardAlt = new Color(0.88f, 0.94f, 0.84f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color darkGreen = new Color(0.06f, 0.23f, 0.10f, 1f);
        private readonly Color brown = new Color(0.20f, 0.12f, 0.06f, 1f);
        private readonly Color gold = new Color(0.94f, 0.61f, 0.10f, 1f);
        private readonly Color red = new Color(0.86f, 0.22f, 0.17f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.82f, 1f);
        private readonly Color muted = new Color(0.52f, 0.55f, 0.48f, 1f);

        public int LayoutVersion => 2;
        public int ManagedPageCount => 8;
        public bool UsesTwoColumnGrowth => true;
        public bool UsesConstantButtonBackgrounds => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutMenuLayoutV2>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMenuLayoutV2");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutMenuLayoutV2>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                if (TryBind()) break;
            }

            if (sourceUi == null || contentHost == null)
            {
                enabled = false;
                yield break;
            }

            CreateAssets();
            BuildNavigationIndicators();
            stageMapWorld = stageFlow == null ? 1 : stageFlow.World;
        }

        private bool TryBind()
        {
            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            skills = FindFirstObjectByType<SkillManagementPrototype>();
            equipment = FindFirstObjectByType<SwordProgressionPrototype>();
            pets = FindFirstObjectByType<IdleSystemsPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            saveBridge = FindFirstObjectByType<PrototypeSaveBridge>();
            if (sourceUi == null || arena == null || stageFlow == null) return false;

            Type uiType = typeof(PeanutMobileCanvasPrototype);
            contentHostField = uiType.GetField("contentHost", PrivateInstance);
            currentPageField = uiType.GetField("currentPage", PrivateInstance);
            sourceRefreshersField = uiType.GetField("refreshers", PrivateInstance);
            navBackgroundsField = uiType.GetField("navBackgrounds", PrivateInstance);
            sourceToastMethod = uiType.GetMethod("Toast", PrivateInstance);
            contentHost = contentHostField == null ? null : contentHostField.GetValue(sourceUi) as GameObject;
            navBackgrounds = navBackgroundsField == null ? null : navBackgroundsField.GetValue(sourceUi) as Image[];
            BindGameFields();
            return contentHost != null;
        }

        private void BindGameFields()
        {
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
                Type type = typeof(GrowthExpansionPrototype);
                critChanceLevelField = type.GetField("critChanceLevel", PrivateInstance);
                critDamageLevelField = type.GetField("critDamageLevel", PrivateInstance);
                goldGainLevelField = type.GetField("goldGainLevel", PrivateInstance);
                hpRegenLevelField = type.GetField("hpRegenLevel", PrivateInstance);
                expGainLevelField = type.GetField("expGainLevel", PrivateInstance);
                equipmentGainLevelField = type.GetField("equipmentGainLevel", PrivateInstance);
            }

            if (pets != null)
            {
                Type type = typeof(IdleSystemsPrototype);
                petAttackLevelField = type.GetField("miniAttackLevel", PrivateInstance);
                petCritLevelField = type.GetField("miniCritLevel", PrivateInstance);
                petCritDamageLevelField = type.GetField("miniCritDamageLevel", PrivateInstance);
                eggsField = type.GetField("eggs", PrivateInstance);
                hatchedPetsField = type.GetField("hatchedMinis", PrivateInstance);
                incubatingField = type.GetField("incubating", PrivateInstance);
                incubationRemainingField = type.GetField("incubationRemaining", PrivateInstance);
                petMessageField = type.GetField("systemMessage", PrivateInstance);
            }
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            whiteSprite = CreateSolidSprite();
            roundedSprite = CreateRoundedSprite();
        }

        private void LateUpdate()
        {
            if (sourceUi == null || contentHost == null)
            {
                TryBind();
                return;
            }

            string page = CurrentPageName;
            ApplyNavigationStyle(page);
            if (page == "Main")
            {
                activePage = page;
                return;
            }

            if (page != activePage || customRoot == null || customRoot.transform.parent != contentHost.transform)
            {
                activePage = page;
                RebuildPage(page);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            for (int i = 0; i < refreshers.Count; i++)
            {
                try { refreshers[i]?.Invoke(); }
                catch (MissingReferenceException) { }
            }
        }

        private string CurrentPageName
        {
            get
            {
                object value = currentPageField == null ? null : currentPageField.GetValue(sourceUi);
                return value == null ? "Main" : value.ToString();
            }
        }

        private void RebuildPage(string page)
        {
            ClearSourceRefreshers();
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            customRoot = CreateRect(contentHost.transform, LayoutRootName, 0f, 0f, ContentWidth, ContentHeight).gameObject;
            Image background = customRoot.AddComponent<Image>();
            background.sprite = whiteSprite;
            background.color = pageBackground;
            background.raycastTarget = false;

            switch (page)
            {
                case "Skills": BuildSkills(); break;
                case "Equipment": BuildEquipment(); break;
                case "Growth": BuildGrowth(); break;
                case "Advancement": BuildAdvancement(); break;
                case "Pets": BuildPets(); break;
                case "Shop": BuildShop(); break;
                case "StageSelect": BuildStageSelect(); break;
                case "Settings": BuildSettings(); break;
                default: BuildUnknown(page); break;
            }

            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
        }

        private void ClearSourceRefreshers()
        {
            IList list = sourceRefreshersField == null ? null : sourceRefreshersField.GetValue(sourceUi) as IList;
            list?.Clear();
        }

        private void BuildNavigationIndicators()
        {
            navUnderlines.Clear();
            if (navBackgrounds == null) return;
            for (int i = 0; i < navBackgrounds.Length; i++)
            {
                Image image = navBackgrounds[i];
                if (image == null) continue;
                Transform existing = image.transform.Find("V2 Active Line");
                GameObject line;
                if (existing != null)
                {
                    line = existing.gameObject;
                }
                else
                {
                    RectTransform rect = CreateRect(image.transform, "V2 Active Line", 12f, 56f,
                        Mathf.Max(20f, image.rectTransform.sizeDelta.x - 24f), 4f);
                    Image lineImage = rect.gameObject.AddComponent<Image>();
                    lineImage.sprite = whiteSprite;
                    lineImage.color = gold;
                    lineImage.raycastTarget = false;
                    line = rect.gameObject;
                }
                navUnderlines.Add(line);
            }
        }

        private void ApplyNavigationStyle(string page)
        {
            if (navBackgrounds == null) return;
            string[] pageNames = { "Skills", "Equipment", "Growth", "Advancement", "Pets", "Shop" };
            for (int i = 0; i < navBackgrounds.Length; i++)
            {
                if (navBackgrounds[i] != null)
                    navBackgrounds[i].color = new Color(0.94f, 0.96f, 0.89f, 0.98f);
                if (i < navUnderlines.Count && navUnderlines[i] != null)
                    navUnderlines[i].SetActive(i < pageNames.Length && page == pageNames[i]);
            }
        }

        private void BuildGrowth()
        {
            GameObject header = Card(customRoot.transform, "Growth Summary", 20f, 14f, 1348f, 92f, cream, green);
            Badge(header.transform, "성장", 16f, 17f, 82f, 58f, green, Color.white, 17);
            Text summary = TextLabel(header.transform, string.Empty, 116f, 9f, 430f, 72f, 18, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            refreshers.Add(() =>
            {
                long current = growth == null ? 0L : growth.CurrentExperience;
                long required = growth == null ? 0L : growth.ExperienceToNextLevel;
                summary.text = $"Lv.{(growth == null ? 1 : growth.PlayerLevel):N0}  ·  전투력 {CombatPower:N0}\nEXP {current:N0} / {required:N0}";
            });

            TextLabel(header.transform, "강화 수량", 660f, 22f, 130f, 46f, 16, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                bool selected = purchaseAmount == amount;
                ChoiceButton(header.transform, "×" + amount, 805f + i * 170f, 18f, 150f, 56f, selected,
                    () => { purchaseAmount = amount; RebuildPage("Growth"); });
            }

            float left = 20f;
            float right = 704f;
            float top = 120f;
            float width = 664f;
            float height = 96f;
            float gap = 10f;
            BuildGrowthCard(left, top + (height + gap) * 0f, width, height, "ATK", "공격력",
                () => $"현재 {AttackDamage:N1}", attackLevelField, arena, 20L, 0);
            BuildGrowthCard(right, top + (height + gap) * 0f, width, height, "HP", "HP",
                () => $"최대 {MaxHp:N0}", hpLevelField, arena, 25L, 0);
            BuildGrowthCard(left, top + (height + gap) * 1f, width, height, "REC", "HP 회복량",
                () => $"초당 {(growth == null ? 0f : growth.HpRecoveryPerSecond):0.0}", hpRegenLevelField, growth, 40L, 0);
            BuildGrowthCard(right, top + (height + gap) * 1f, width, height, "MP", "MP",
                () => $"최대 {MaxMp:N0}", maxMpLevelField, arena, 30L, 0);
            BuildGrowthCard(left, top + (height + gap) * 2f, width, height, "MREC", "MP 회복량",
                () => $"초당 {MpRegen:0.0}", mpRegenLevelField, arena, 35L, 0);
            BuildGrowthCard(right, top + (height + gap) * 2f, width, height, "CRIT", "치명타",
                () => $"{(growth == null ? 0f : growth.CriticalChance * 100f):0}% · 최대 100%",
                critChanceLevelField, growth, 45L, 49);
            BuildGrowthCard(left, top + (height + gap) * 3f, width, height, "C.DMG", "치명타 피해량",
                () => $"{(growth == null ? 100f : growth.CriticalDamageMultiplier * 100f):0}%",
                critDamageLevelField, growth, 55L, 0);
            BuildGrowthCard(right, top + (height + gap) * 3f, width, height, "EXP", "경험치 획득량 증가",
                () => $"+{((growth == null ? 1f : growth.ExperienceMultiplier) - 1f) * 100f:0}%",
                expGainLevelField, growth, 70L, 0);
            BuildGrowthCard(left, top + (height + gap) * 4f, width, height, "GOLD", "골드 획득량 증가",
                () => $"+{((growth == null ? 1f : growth.GoldMultiplier) - 1f) * 100f:0}%",
                goldGainLevelField, growth, 65L, 0);
            BuildGrowthCard(right, top + (height + gap) * 4f, width, height, "MAT", "장비 강화 획득량 증가",
                () => $"+{((growth == null ? 1f : growth.EquipmentMaterialMultiplier) - 1f) * 100f:0}%",
                equipmentGainLevelField, growth, 80L, 0);
        }

        private void BuildGrowthCard(float x, float y, float width, float height, string badge, string title,
            Func<string> value, FieldInfo field, object target, long baseCost, int maxLevel)
        {
            GameObject root = Card(customRoot.transform, title, x, y, width, height, card, new Color(0.78f, 0.82f, 0.70f));
            Badge(root.transform, badge, 14f, 16f, 70f, 64f, green, Color.white, 13);
            TextLabel(root.transform, title, 98f, 8f, 250f, 34f, 18, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text detail = TextLabel(root.transform, string.Empty, 98f, 42f, 310f, 34f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Text level = TextLabel(root.transform, string.Empty, 405f, 15f, 92f, 64f, 16, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button button = FlatButton(root.transform, string.Empty, 505f, 16f, 142f, 62f, gold, Color.white,
                () => UpgradeStat(field, target, baseCost, title, maxLevel));
            Text buttonText = button.GetComponentInChildren<Text>();

            refreshers.Add(() =>
            {
                int currentLevel = ReadLevel(field, target);
                int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - currentLevel)) : purchaseAmount;
                detail.text = value == null ? string.Empty : value();
                level.text = maxLevel > 0 && currentLevel >= maxLevel ? "MAX" : $"Lv.{currentLevel:N0}";
                if (amount <= 0)
                {
                    button.interactable = false;
                    buttonText.text = "최대치";
                }
                else
                {
                    long cost = UpgradeCost(baseCost, currentLevel, amount);
                    button.interactable = Gold >= cost;
                    buttonText.text = $"+{amount}\n{cost:N0}G";
                }
            });
        }

        private void BuildSkills()
        {
            GameObject header = Card(customRoot.transform, "Skill Auto", 20f, 14f, 1348f, 94f, cream, green);
            Badge(header.transform, "AUTO", 16f, 17f, 96f, 60f, green, Color.white, 16);
            TextLabel(header.transform, "전체 SKILL 자동 사용", 132f, 9f, 450f, 38f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            TextLabel(header.transform, "개별 AUTO 없이 이 버튼 하나로 모든 사냥·보스 스킬을 제어합니다.",
                132f, 47f, 740f, 30f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Button autoButton = FlatButton(header.transform, string.Empty, 1060f, 17f, 260f, 60f, green, Color.white,
                ToggleGlobalAuto);
            Text autoText = autoButton.GetComponentInChildren<Text>();
            refreshers.Add(() => autoText.text = skills != null && skills.GlobalAutoEnabled ? "전체 AUTO ON" : "전체 AUTO OFF");

            for (int i = 0; i < 8; i++)
            {
                int index = i;
                int column = i % 2;
                int row = i / 2;
                float x = 20f + column * 684f;
                float y = 122f + row * 124f;
                GameObject cardRoot = Card(customRoot.transform, "Skill " + i, x, y, 664f, 112f, card,
                    i < 4 ? new Color(0.35f, 0.62f, 0.36f) : new Color(0.64f, 0.42f, 0.23f));
                Badge(cardRoot.transform, i < 4 ? "사냥" : "보스", 14f, 16f, 70f, 64f,
                    i < 4 ? green : new Color(0.60f, 0.34f, 0.16f), Color.white, 14);
                Text name = TextLabel(cardRoot.transform, string.Empty, 100f, 8f, 260f, 34f, 18, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                TextLabel(cardRoot.transform, "임시 구성 · 스킬 종류와 장착 구조는 추후 재설계", 100f, 42f, 330f, 28f,
                    13, muted, TextAnchor.MiddleLeft, FontStyle.Normal);
                Text state = TextLabel(cardRoot.transform, string.Empty, 100f, 72f, 330f, 26f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                Button upgrade = FlatButton(cardRoot.transform, string.Empty, 468f, 24f, 176f, 64f, gold, Color.white,
                    () => UpgradeSkill(index));
                Text upgradeText = upgrade.GetComponentInChildren<Text>();

                refreshers.Add(() =>
                {
                    int[] levels = skills == null ? null : skills.SkillLevels;
                    float[] cooldowns = skills == null ? null : skills.Cooldowns;
                    int skillLevel = levels != null && index < levels.Length ? levels[index] : 1;
                    float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                    long cost = skills == null ? 0L : skills.GetUpgradeCost(index);
                    name.text = skills == null ? $"SKILL {index + 1}" : skills.GetSkillName(index);
                    state.text = $"Lv.{skillLevel:N0}  ·  {(cooldown > 0.05f ? "쿨타임 " + cooldown.ToString("0.0") + "초" : "사용 가능")}";
                    upgrade.interactable = skills != null && Fragments >= cost;
                    upgradeText.text = $"강화\n{cost:N0} 조각";
                });
            }
        }

        private void BuildEquipment()
        {
            GameObject header = Card(customRoot.transform, "Equipment Summary", 20f, 14f, 1348f, 94f, cream, green);
            Badge(header.transform, "장비", 16f, 17f, 82f, 60f, green, Color.white, 17);
            Text hunting = TextLabel(header.transform, string.Empty, 118f, 8f, 390f, 38f, 18, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text boss = TextLabel(header.transform, string.Empty, 118f, 47f, 390f, 32f, 16, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text material = TextLabel(header.transform, string.Empty, 650f, 16f, 650f, 60f, 17, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                int huntingIndex = ReadEnumIndex(huntingElementField);
                int bossIndex = ReadEnumIndex(bossElementField);
                hunting.text = $"사냥 장비  ·  {ElementNames[huntingIndex]} {RarityName(equipment == null ? SwordProgressionPrototype.SwordRarity.None : equipment.GetHighestRarity(huntingIndex))}";
                boss.text = $"보스 장비  ·  {ElementNames[bossIndex]} {RarityName(equipment == null ? SwordProgressionPrototype.SwordRarity.None : equipment.GetHighestRarity(bossIndex))}";
                material.text = $"장비 강화 재료 {(growth == null ? 0 : growth.EquipmentEnhancementMaterials):N0}\n등급  레어 → 에픽 → 유니크 → 레전드";
            });

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                int column = i % 2;
                int row = i / 2;
                float x = 20f + column * 684f;
                float y = 122f + row * 258f;
                GameObject root = Card(customRoot.transform, ElementNames[i], x, y, 664f, 244f, card, ElementColors[i]);
                Badge(root.transform, ElementShortNames[i], 18f, 18f, 82f, 82f, ElementColors[i], Color.white, 28);
                TextLabel(root.transform, ElementNames[i] + " 장비", 118f, 12f, 300f, 40f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text rarity = TextLabel(root.transform, string.Empty, 118f, 54f, 310f, 34f, 18, ElementColors[i], TextAnchor.MiddleLeft, FontStyle.Bold);
                Text copies = TextLabel(root.transform, string.Empty, 18f, 112f, 620f, 42f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
                Text state = TextLabel(root.transform, string.Empty, 18f, 158f, 330f, 60f, 14, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                FlatButton(root.transform, "사냥 장착", 365f, 164f, 132f, 56f, cardAlt, darkGreen,
                    () => EquipElement(index, false));
                FlatButton(root.transform, "보스 장착", 510f, 164f, 132f, 56f, cream, darkGreen,
                    () => EquipElement(index, true));

                refreshers.Add(() =>
                {
                    SwordProgressionPrototype.SwordRarity highest = equipment == null
                        ? SwordProgressionPrototype.SwordRarity.None
                        : equipment.GetHighestRarity(index);
                    rarity.text = $"{RarityName(highest)}  ·  Lv.{(equipment == null ? 1 : equipment.GetLevel(index)):N0}";
                    copies.text = equipment == null
                        ? "레어 0  ·  에픽 0  ·  유니크 0  ·  레전드 0"
                        : $"레어 {equipment.GetCopies(index, SwordProgressionPrototype.SwordRarity.Rare)}  ·  에픽 {equipment.GetCopies(index, SwordProgressionPrototype.SwordRarity.Epic)}  ·  유니크 {equipment.GetCopies(index, SwordProgressionPrototype.SwordRarity.Unique)}  ·  레전드 {equipment.GetCopies(index, SwordProgressionPrototype.SwordRarity.Legend)}";
                    bool huntingEquipped = ReadEnumIndex(huntingElementField) == index;
                    bool bossEquipped = ReadEnumIndex(bossElementField) == index;
                    state.text = $"{(huntingEquipped ? "사냥 장착 중" : "사냥 미장착")}\n{(bossEquipped ? "보스 장착 중" : "보스 미장착")}  ·  상세 옵션은 재설계 예정";
                });
            }
        }

        private void BuildAdvancement()
        {
            GameObject top = Card(customRoot.transform, "Advancement Header", 20f, 14f, 1348f, 82f, cream, green);
            Badge(top.transform, "전직", 16f, 12f, 82f, 58f, green, Color.white, 17);
            Text title = TextLabel(top.transform, string.Empty, 118f, 8f, 620f, 62f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text power = TextLabel(top.transform, string.Empty, 820f, 8f, 490f, 62f, 17, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                int tier = AdvancementTier;
                title.text = $"현재 {AdvancementName(tier)}  ·  전직 {tier}단계";
                power.text = $"전투력 {CombatPower:N0}  ·  스테이지 {GlobalStage:N0}";
            });

            GameObject current = Card(customRoot.transform, "Current Form", 20f, 110f, 410f, 520f, card, green);
            Badge(current.transform, "FORM", 128f, 28f, 154f, 82f, green, Color.white, 22);
            Text formName = TextLabel(current.transform, string.Empty, 28f, 130f, 354f, 56f, 25, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text formEffect = TextLabel(current.transform, string.Empty, 34f, 206f, 342f, 220f, 16, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Text petUnlock = TextLabel(current.transform, string.Empty, 34f, 438f, 342f, 54f, 16, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            refreshers.Add(() =>
            {
                int tier = AdvancementTier;
                formName.text = AdvancementName(tier);
                formEffect.text = $"기본 능력치 배율  ×{1f + tier * 0.35f:0.00}\n\n기본 공격 타수  {tier + 1}타\n\n스킬 공격 횟수 증가\n\n전직 시 HP·MP 즉시 회복";
                petUnlock.text = PetUnlocked ? "펫 슬롯 3/3 해금" : "2차 전직 시 펫 3슬롯 해금";
            });

            GameObject road = Card(customRoot.transform, "Advancement Road", 448f, 110f, 410f, 520f, cardAlt, green);
            TextLabel(road.transform, "전직 단계", 24f, 18f, 362f, 38f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < 3; i++)
            {
                int tierIndex = i;
                GameObject step = Card(road.transform, "Step " + i, 24f, 76f + i * 132f, 362f, 112f, cream,
                    i == 2 ? gold : green);
                Badge(step.transform, (i + 1).ToString(), 16f, 21f, 66f, 66f, i == 2 ? gold : green, Color.white, 22);
                TextLabel(step.transform, AdvancementName(i), 98f, 14f, 236f, 36f, 18, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text status = TextLabel(step.transform, string.Empty, 98f, 52f, 236f, 38f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                refreshers.Add(() =>
                {
                    int currentTier = AdvancementTier;
                    status.text = currentTier > tierIndex ? "완료" : currentTier == tierIndex ? "현재 단계" : "미달성";
                });
            }

            GameObject requirements = Card(customRoot.transform, "Requirements", 876f, 110f, 492f, 520f, card, gold);
            TextLabel(requirements.transform, "다음 전직 조건", 24f, 18f, 444f, 40f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text requirementText = TextLabel(requirements.transform, string.Empty, 28f, 78f, 436f, 260f, 17, brown, TextAnchor.UpperLeft, FontStyle.Bold);
            Text result = TextLabel(requirements.transform, string.Empty, 28f, 342f, 436f, 58f, 15, muted, TextAnchor.MiddleLeft, FontStyle.Normal);
            Button advance = FlatButton(requirements.transform, "전직 시도", 28f, 420f, 436f, 70f, gold, Color.white, TryAdvance);
            refreshers.Add(() =>
            {
                int tier = AdvancementTier;
                if (tier >= 2)
                {
                    requirementText.text = "현재 프로토타입의 최고 전직 단계입니다.";
                    result.text = "추가 전직 외형과 조건은 콘텐츠 확장 단계에서 제작합니다.";
                    advance.interactable = false;
                    return;
                }
                int requiredStage = tier == 0 ? 2 : 4;
                int requiredPower = tier == 0 ? 180 : 420;
                long requiredGold = tier == 0 ? 150L : 500L;
                int requiredDiamond = tier == 0 ? 5 : 15;
                requirementText.text =
                    RequirementLine("스테이지", GlobalStage >= requiredStage, $"{GlobalStage}/{requiredStage}") + "\n\n" +
                    RequirementLine("전투력", CombatPower >= requiredPower, $"{CombatPower}/{requiredPower}") + "\n\n" +
                    RequirementLine("골드", Gold >= requiredGold, $"{Gold:N0}/{requiredGold:N0}") + "\n\n" +
                    RequirementLine("다이아", Diamonds >= requiredDiamond, $"{Diamonds}/{requiredDiamond}");
                bool ready = GlobalStage >= requiredStage && CombatPower >= requiredPower && Gold >= requiredGold && Diamonds >= requiredDiamond;
                result.text = ready ? "모든 조건을 충족했습니다." : "부족한 조건을 먼저 달성해야 합니다.";
                advance.interactable = ready;
            });
        }

        private void BuildPets()
        {
            GameObject header = Card(customRoot.transform, "Pet Summary", 20f, 14f, 1348f, 88f, cream, green);
            Badge(header.transform, "펫", 16f, 14f, 82f, 60f, green, Color.white, 18);
            Text summary = TextLabel(header.transform, string.Empty, 118f, 8f, 600f, 68f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text collection = TextLabel(header.transform, string.Empty, 800f, 8f, 500f, 68f, 17, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                summary.text = PetUnlocked ? "펫 슬롯 3/3 활동 중" : "2차 전직 후 펫 슬롯 3개 해금";
                collection.text = $"보유 알 {ReadInt(eggsField, pets)}  ·  부화 도감 {ReadInt(hatchedPetsField, pets)}";
            });

            string[] elements = { "화염", "냉기", "번개" };
            for (int i = 0; i < 3; i++)
            {
                float x = 20f + i * 456f;
                GameObject slot = Card(customRoot.transform, "Pet Slot " + i, x, 116f, 436f, 138f, card, ElementColors[i + 1]);
                Badge(slot.transform, (i + 1).ToString(), 16f, 25f, 74f, 74f, ElementColors[i + 1], Color.white, 23);
                TextLabel(slot.transform, $"슬롯 {i + 1}  ·  {elements[i]} 펫", 108f, 15f, 296f, 40f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text state = TextLabel(slot.transform, string.Empty, 108f, 58f, 296f, 52f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
                refreshers.Add(() => state.text = PetUnlocked ? "자동 이동 · 자동 공격 · 무적" : "잠김");
            }

            GameObject incubation = Card(customRoot.transform, "Incubation", 20f, 270f, 436f, 360f, cardAlt, green);
            TextLabel(incubation.transform, "알 부화", 24f, 18f, 388f, 40f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text incubationState = TextLabel(incubation.transform, string.Empty, 24f, 76f, 388f, 120f, 18, brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            FlatButton(incubation.transform, "펫 알 구매 · 3 다이아", 24f, 222f, 388f, 54f, cream, darkGreen,
                () => InvokePrivate(pets, "BuyEgg", null, "펫 알 구매"));
            FlatButton(incubation.transform, "부화 시작", 24f, 288f, 388f, 54f, green, Color.white,
                () => InvokePrivate(pets, "StartIncubation", null, "부화 시작"));
            refreshers.Add(() =>
            {
                bool active = ReadBool(incubatingField, pets);
                incubationState.text = active
                    ? $"부화 진행 중\n{Mathf.CeilToInt(ReadFloat(incubationRemainingField, pets))}초 남음"
                    : $"부화 대기\n보유 알 {ReadInt(eggsField, pets)}";
            });

            GameObject growthCard = Card(customRoot.transform, "Pet Growth", 474f, 270f, 894f, 360f, card, green);
            TextLabel(growthCard.transform, "펫 성장", 24f, 16f, 846f, 42f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            BuildPetGrowthRow(growthCard.transform, 72f, "ATK", "펫 공격력", petAttackLevelField, 80L);
            BuildPetGrowthRow(growthCard.transform, 158f, "CRIT", "펫 치명타 확률", petCritLevelField, 100L);
            BuildPetGrowthRow(growthCard.transform, 244f, "C.DMG", "펫 치명타 피해", petCritDamageLevelField, 120L);
        }

        private void BuildPetGrowthRow(Transform parent, float y, string badge, string title, FieldInfo field, long baseCost)
        {
            GameObject row = Card(parent, title, 24f, y, 846f, 74f, cream, new Color(0.78f, 0.82f, 0.70f));
            Badge(row.transform, badge, 12f, 10f, 66f, 54f, green, Color.white, 12);
            TextLabel(row.transform, title, 92f, 8f, 310f, 30f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text level = TextLabel(row.transform, string.Empty, 92f, 38f, 310f, 25f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Button button = FlatButton(row.transform, string.Empty, 640f, 10f, 184f, 54f, gold, Color.white,
                () => UpgradePet(field, baseCost, title));
            Text buttonText = button.GetComponentInChildren<Text>();
            refreshers.Add(() =>
            {
                int current = ReadLevel(field, pets);
                long cost = baseCost * Math.Max(1, current);
                level.text = $"Lv.{current:N0}";
                button.interactable = Gold >= cost;
                buttonText.text = $"강화 {cost:N0}G";
            });
        }

        private void BuildShop()
        {
            GameObject summary = Card(customRoot.transform, "Shop Summary", 20f, 14f, 1348f, 88f, cream, green);
            Badge(summary.transform, "상점", 16f, 14f, 82f, 60f, green, Color.white, 17);
            Text message = TextLabel(summary.transform, string.Empty, 118f, 8f, 720f, 68f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text currencies = TextLabel(summary.transform, string.Empty, 860f, 8f, 440f, 68f, 16, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                message.text = shop == null ? "상점 초기화 대기" : shop.ShopMessage;
                currencies.text = $"골드 {Gold:N0}  ·  다이아 {Diamonds:N0}  ·  조각 {Fragments:N0}";
            });

            BuildShopCard(20f, 116f, "오늘의 보급", "접속 일수에 따라 골드·다이아·조각을 지급합니다.",
                "무료", green, "받기", () => InvokePrivate(shop, "ClaimDailyReward", null, "오늘의 보급"));
            BuildShopCard(704f, 116f, "사냥 장비 소환", "사냥 장착 슬롯에 사용할 속성 장비를 획득합니다.",
                "다이아 5", ElementColors[1], "소환", () => InvokePrivate(shop, "SummonSword", new object[] { false }, "사냥 장비 소환"));
            BuildShopCard(20f, 374f, "보스 장비 소환", "보스 장착 슬롯에 사용할 속성 장비를 획득합니다.",
                "다이아 5", red, "소환", () => InvokePrivate(shop, "SummonSword", new object[] { true }, "보스 장비 소환"));
            BuildShopCard(704f, 374f, "펫 알", "펫 화면에서 부화할 수 있는 알을 구매합니다.",
                "다이아 3", blue, "구매", () => InvokePrivate(shop, "BuyEgg", null, "펫 알 구매"));
        }

        private void BuildShopCard(float x, float y, string title, string description, string price,
            Color accent, string actionLabel, Action action)
        {
            GameObject root = Card(customRoot.transform, title, x, y, 664f, 244f, card, accent);
            Badge(root.transform, title.Substring(0, Math.Min(2, title.Length)), 22f, 24f, 86f, 86f, accent, Color.white, 20);
            TextLabel(root.transform, title, 130f, 18f, 480f, 42f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            TextLabel(root.transform, description, 130f, 68f, 480f, 58f, 15, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            TextLabel(root.transform, price, 24f, 150f, 280f, 64f, 19, accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            FlatButton(root.transform, actionLabel, 420f, 158f, 216f, 58f, accent, Color.white, action);
        }

        private void BuildStageSelect()
        {
            GameObject header = Card(customRoot.transform, "World Selector", 20f, 14f, 1348f, 78f, cream, green);
            FlatButton(header.transform, "‹ 이전 월드", 16f, 12f, 190f, 54f, cardAlt, darkGreen, () => ChangeWorld(-1));
            Text world = TextLabel(header.transform, string.Empty, 230f, 8f, 888f, 62f, 23, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            FlatButton(header.transform, "다음 월드 ›", 1142f, 12f, 190f, 54f, cardAlt, darkGreen, () => ChangeWorld(1));
            refreshers.Add(() => world.text = $"월드 {stageMapWorld}  ·  최고 해금 {FormatStage(HighestGlobalStage)}");

            GameObject grid = Card(customRoot.transform, "Stage Grid", 20f, 106f, 1348f, 524f, new Color(1f, 1f, 1f, 0.40f), green);
            const int columns = 6;
            const float buttonWidth = 196f;
            const float buttonHeight = 82f;
            for (int localStage = 1; localStage <= StageFlowController.StagesPerWorld; localStage++)
            {
                int stageNumber = localStage;
                int globalStage = (stageMapWorld - 1) * StageFlowController.StagesPerWorld + localStage;
                int column = (localStage - 1) % columns;
                int row = (localStage - 1) / columns;
                bool unlocked = globalStage <= HighestGlobalStage;
                bool selected = stageMapWorld == stageFlow.World && localStage == stageFlow.Stage;
                GameObject buttonRoot = Card(grid.transform, "Stage " + localStage,
                    24f + column * 216f, 22f + row * 96f, buttonWidth, buttonHeight,
                    unlocked ? cream : new Color(0.84f, 0.85f, 0.80f), selected ? gold : new Color(0.76f, 0.80f, 0.70f));
                Button button = buttonRoot.AddComponent<Button>();
                ConfigureButtonColors(button);
                if (unlocked)
                {
                    button.onClick.AddListener(() => SelectStage(stageMapWorld, stageNumber));
                    Badge(buttonRoot.transform, stageNumber.ToString(), 14f, 14f, 54f, 54f, selected ? gold : green, Color.white, 18);
                    TextLabel(buttonRoot.transform, $"{stageMapWorld}-{stageNumber}", 78f, 9f, 102f, 34f, 17, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
                    TextLabel(buttonRoot.transform, selected ? "현재" : "이동", 78f, 43f, 102f, 28f, 13,
                        selected ? gold : muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                else
                {
                    button.interactable = false;
                    TextLabel(buttonRoot.transform, "잠김", 0f, 0f, buttonWidth, buttonHeight, 16, muted, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                if (stageFlow.Phase == StageFlowPhase.BossBattle) button.interactable = false;
            }
        }

        private void BuildSettings()
        {
            GameObject header = Card(customRoot.transform, "Settings Summary", 20f, 14f, 1348f, 86f, cream, green);
            Badge(header.transform, "설정", 16f, 13f, 82f, 60f, green, Color.white, 17);
            TextLabel(header.transform, "게임 설정", 118f, 8f, 420f, 68f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            TextLabel(header.transform, "설정은 우측 상단 메뉴에서만 접근합니다.", 650f, 8f, 650f, 68f, 16, brown, TextAnchor.MiddleRight, FontStyle.Normal);

            BuildSettingCard(20f, 116f, "60 FPS", "일반 플레이에 사용하는 기본 설정입니다.", green, "적용", () => SetFrameRate(60));
            BuildSettingCard(704f, 116f, "30 FPS 절전", "발열과 배터리 사용량을 줄입니다.", blue, "적용", () => SetFrameRate(30));
            BuildSettingCard(20f, 374f, "즉시 저장", "스테이지·재화·성장·장비·전직·펫 데이터를 저장합니다.", gold, "저장", SaveNow);
            BuildSettingCard(704f, 374f, "현재 환경", $"Unity {Application.unityVersion}\n{Screen.width}×{Screen.height} · 안전영역 적용", muted, "확인", () => ShowToast("현재 실행 환경을 확인했습니다"));
        }

        private void BuildSettingCard(float x, float y, string title, string description, Color accent, string action, Action callback)
        {
            GameObject root = Card(customRoot.transform, title, x, y, 664f, 244f, card, accent);
            Badge(root.transform, "SET", 22f, 24f, 86f, 86f, accent, Color.white, 18);
            TextLabel(root.transform, title, 130f, 18f, 480f, 42f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            TextLabel(root.transform, description, 130f, 70f, 480f, 72f, 15, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            FlatButton(root.transform, action, 420f, 164f, 216f, 56f, accent, Color.white, callback);
        }

        private void BuildUnknown(string page)
        {
            GameObject root = Card(customRoot.transform, "Unknown", 20f, 20f, 1348f, 610f, cream, green);
            TextLabel(root.transform, page + " 화면을 준비 중입니다.", 40f, 40f, 1268f, 100f, 24, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private void UpgradeStat(FieldInfo field, object target, long baseCost, string title, int maxLevel)
        {
            if (field == null || target == null) { ShowToast("성장 데이터 연결 대기"); return; }
            int level = ReadLevel(field, target);
            int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - level)) : purchaseAmount;
            if (amount <= 0) { ShowToast(title + " 최대치"); return; }
            long cost = UpgradeCost(baseCost, level, amount);
            if (!SpendGold(cost)) { ShowToast($"골드 부족 · {cost:N0}G 필요"); return; }
            field.SetValue(target, level + amount);
            if (field == hpLevelField && playerHpField != null) playerHpField.SetValue(arena, MaxHp);
            if (field == maxMpLevelField && playerMpField != null) playerMpField.SetValue(arena, MaxMp);
            growth?.SaveNow();
            SaveSilently();
            ShowToast($"{title} +{amount} 강화 완료");
        }

        private void UpgradeSkill(int index)
        {
            if (skills == null) { ShowToast("SKILL 시스템 연결 대기"); return; }
            bool success = skills.UpgradeSkill(index);
            ShowToast(skills.LastMessage);
            if (success) SaveSilently();
        }

        private void ToggleGlobalAuto()
        {
            if (skills == null) { ShowToast("SKILL AUTO 연결 대기"); return; }
            skills.ToggleGlobalAuto();
            ShowToast(skills.LastMessage);
        }

        private void EquipElement(int index, bool boss)
        {
            FieldInfo field = boss ? bossElementField : huntingElementField;
            if (field == null) { ShowToast("장비 슬롯 연결 대기"); return; }
            field.SetValue(arena, Enum.ToObject(field.FieldType, index));
            SaveSilently();
            ShowToast($"{(boss ? "보스" : "사냥")} 장비 · {ElementNames[index]} 장착");
        }

        private void UpgradePet(FieldInfo field, long baseCost, string title)
        {
            if (field == null || pets == null) { ShowToast("펫 성장 연결 대기"); return; }
            int level = ReadLevel(field, pets);
            long cost = baseCost * Math.Max(1, level);
            if (!SpendGold(cost)) { ShowToast($"골드 부족 · {cost:N0}G 필요"); return; }
            field.SetValue(pets, level + 1);
            InvokePrivateRaw(pets, "SaveProgress", null);
            ShowToast($"{title} Lv.{level + 1} 강화 완료");
        }

        private void TryAdvance()
        {
            int before = AdvancementTier;
            InvokePrivateRaw(arena, "TryAdvance", null);
            int after = AdvancementTier;
            SaveSilently();
            ShowToast(after > before ? $"전직 성공 · {AdvancementName(after)}" : "전직 조건을 확인하십시오");
        }

        private void ChangeWorld(int direction)
        {
            int highestWorld = Mathf.Max(1, (HighestGlobalStage - 1) / StageFlowController.StagesPerWorld + 1);
            stageMapWorld = Mathf.Clamp(stageMapWorld + direction, 1, highestWorld);
            RebuildPage("StageSelect");
        }

        private void SelectStage(int world, int stage)
        {
            int global = (world - 1) * StageFlowController.StagesPerWorld + stage;
            if (global > HighestGlobalStage) { ShowToast("아직 해금되지 않은 스테이지입니다"); return; }
            if (stageFlow.Phase == StageFlowPhase.BossBattle) { ShowToast("보스전 중에는 스테이지를 변경할 수 없습니다"); return; }
            stageFlow.SelectStage(world, stage);
            stageMapWorld = world;
            ShowToast($"스테이지 {world}-{stage} 이동");
            RebuildPage("StageSelect");
        }

        private void SetFrameRate(int value)
        {
            Application.targetFrameRate = value;
            ShowToast(value + " FPS 적용");
        }

        private void SaveNow()
        {
            growth?.SaveNow();
            InvokePrivateRaw(pets, "SaveProgress", null);
            SaveSilently();
            ShowToast("현재 진행 상황을 저장했습니다");
        }

        private void SaveSilently()
        {
            InvokePrivateRaw(saveBridge, "Save", null);
        }

        private void InvokePrivate(object target, string methodName, object[] args, string label)
        {
            bool success = InvokePrivateRaw(target, methodName, args);
            ShowToast(success ? label + " 완료" : label + " 실행 실패");
        }

        private bool InvokePrivateRaw(object target, string methodName, object[] args)
        {
            if (target == null) return false;
            object[] values = args ?? Array.Empty<object>();
            Type[] types = new Type[values.Length];
            for (int i = 0; i < values.Length; i++) types[i] = values[i].GetType();
            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance, null, types, null);
            if (method == null) return false;
            try
            {
                method.Invoke(target, values.Length == 0 ? null : values);
                return true;
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception, target as UnityEngine.Object);
                return false;
            }
        }

        private void ShowToast(string message)
        {
            if (sourceToastMethod != null && sourceUi != null)
            {
                sourceToastMethod.Invoke(sourceUi, new object[] { message });
                return;
            }
            Debug.Log("[PeanutWarrior] " + message);
        }

        private GameObject Card(Transform parent, string name, float x, float y, float width, float height, Color color, Color border)
        {
            RectTransform rect = CreateRect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.48f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
            return rect.gameObject;
        }

        private GameObject Badge(Transform parent, string value, float x, float y, float width, float height,
            Color color, Color textColor, int fontSize)
        {
            GameObject root = Card(parent, "Badge", x, y, width, height, color, color);
            TextLabel(root.transform, value, 4f, 2f, width - 8f, height - 4f, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return root;
        }

        private Button FlatButton(Transform parent, string value, float x, float y, float width, float height,
            Color color, Color textColor, Action action)
        {
            GameObject root = Card(parent, "Button", x, y, width, height, color, color);
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);
            if (action != null) button.onClick.AddListener(() => action());
            TextLabel(root.transform, value, 6f, 3f, width - 12f, height - 6f, 15, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private Button ChoiceButton(Transform parent, string value, float x, float y, float width, float height,
            bool selected, Action action)
        {
            GameObject root = Card(parent, "Choice", x, y, width, height, cream, selected ? gold : new Color(0.72f, 0.76f, 0.67f));
            Button button = root.AddComponent<Button>();
            ConfigureButtonColors(button);
            if (action != null) button.onClick.AddListener(() => action());
            TextLabel(root.transform, value, 6f, 3f, width - 12f, height - 6f, 16,
                selected ? gold : darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (selected)
            {
                RectTransform line = CreateRect(root.transform, "Selected Line", 14f, height - 7f, width - 28f, 4f);
                Image lineImage = line.gameObject.AddComponent<Image>();
                lineImage.sprite = whiteSprite;
                lineImage.color = gold;
                lineImage.raycastTarget = false;
            }
            return button;
        }

        private void ConfigureButtonColors(Button button)
        {
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = button.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.46f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private Text TextLabel(Transform parent, string value, float x, float y, float width, float height,
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

        private RectTransform CreateRect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "PeanutMenuV2White";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 32;
            const float radius = 9f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "PeanutMenuV2Rounded";
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

        private long Gold => ReadLong(goldField, arena);
        private long Fragments => ReadLong(fragmentsField, arena);
        private int Diamonds => ReadInt(diamondsField, arena);
        private float MaxHp => ReadPropertyFloat(maxHpProperty, arena, 1f);
        private float MaxMp => ReadPropertyFloat(maxMpProperty, arena, 1f);
        private float MpRegen => ReadPropertyFloat(mpRegenProperty, arena, 0f);
        private float AttackDamage => ReadPropertyFloat(attackDamageProperty, arena, 0f);
        private int CombatPower => combatPowerProperty == null || arena == null ? 0 : Convert.ToInt32(combatPowerProperty.GetValue(arena));
        private int AdvancementTier => ReadInt(advancementTierField, arena);
        private bool PetUnlocked => ReadBool(miniSlotsUnlockedField, arena);
        private int GlobalStage => stageFlow == null ? 1 : (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
        private int HighestGlobalStage => Mathf.Max(1, PlayerPrefs.GetInt(HighestStageKey, GlobalStage));

        private bool SpendGold(long amount)
        {
            if (goldField == null || arena == null || amount < 0L || Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private int ReadEnumIndex(FieldInfo field)
        {
            if (field == null || arena == null) return 0;
            return Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, ElementNames.Length - 1);
        }

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

        private static bool ReadBool(FieldInfo field, object target)
        {
            return field != null && target != null && Convert.ToBoolean(field.GetValue(target));
        }

        private static float ReadPropertyFloat(PropertyInfo property, object target, float fallback)
        {
            if (property == null || target == null) return fallback;
            return Convert.ToSingle(property.GetValue(target));
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

        private static string RarityName(SwordProgressionPrototype.SwordRarity rarity)
        {
            return SwordProgressionPrototype.RarityName(rarity);
        }

        private static string AdvancementName(int tier)
        {
            switch (tier)
            {
                case 0: return "새싹 껍질";
                case 1: return "전투 껍질";
                case 2: return "황금 수호 껍질";
                default: return "전직 " + tier + "단계";
            }
        }

        private static string RequirementLine(string title, bool complete, string value)
        {
            return $"{(complete ? "완료" : "부족")}  {title}  {value}";
        }

        private static string FormatStage(int globalStage)
        {
            int world = (Mathf.Max(1, globalStage) - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (Mathf.Max(1, globalStage) - 1) % StageFlowController.StagesPerWorld + 1;
            return world + "-" + stage;
        }
    }
}
