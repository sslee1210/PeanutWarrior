using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(32000)]
    public sealed class PeanutMenuLayoutV4 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Width = 1388f;
        private const float Height = 650f;

        private static readonly Color[] ElementColors =
        {
            new Color(0.74f, 0.58f, 0.18f),
            new Color(0.90f, 0.27f, 0.14f),
            new Color(0.18f, 0.56f, 0.84f),
            new Color(0.54f, 0.31f, 0.84f)
        };

        private PeanutMobileCanvasPrototype sourceUi;
        private PeanutMenuLayoutV2 layoutV2;
        private PeanutCoreMenuCompletionV3 layoutV3;
        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private SkillManagementPrototype skills;
        private AdvancementProgressionPrototype advancement;
        private ElementEquipmentCatalogPrototype equipment;
        private PeanutSaveGameService saveService;

        private FieldInfo contentHostField;
        private FieldInfo currentPageField;
        private FieldInfo sourceRefreshersField;
        private FieldInfo menuTitleField;
        private MethodInfo toastMethod;
        private GameObject contentHost;
        private Text menuTitle;
        private IList sourceRefreshers;
        private IList v2Refreshers;
        private IList v3Refreshers;

        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo attackLevelField;
        private FieldInfo hpLevelField;
        private FieldInfo maxMpLevelField;
        private FieldInfo mpRegenLevelField;
        private FieldInfo critChanceLevelField;
        private FieldInfo critDamageLevelField;
        private FieldInfo goldGainLevelField;
        private FieldInfo hpRegenLevelField;
        private FieldInfo expGainLevelField;
        private FieldInfo equipmentGainLevelField;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;
        private PropertyInfo mpRegenProperty;
        private PropertyInfo combatPowerProperty;

        private Font font;
        private Sprite solidSprite;
        private Sprite roundedSprite;
        private Sprite circleSprite;
        private GameObject root;
        private string activePage = string.Empty;
        private int purchaseAmount = 1;
        private int equipmentElementTab;
        private float refreshTimer;
        private readonly List<Action> refreshers = new List<Action>();

        private readonly Color background = new Color(0.94f, 0.96f, 0.89f, 1f);
        private readonly Color cream = new Color(0.98f, 0.95f, 0.82f, 1f);
        private readonly Color card = new Color(0.97f, 0.94f, 0.85f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color darkGreen = new Color(0.06f, 0.23f, 0.10f, 1f);
        private readonly Color brown = new Color(0.20f, 0.12f, 0.06f, 1f);
        private readonly Color gold = new Color(0.94f, 0.61f, 0.10f, 1f);
        private readonly Color red = new Color(0.86f, 0.22f, 0.17f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.82f, 1f);
        private readonly Color muted = new Color(0.50f, 0.53f, 0.47f, 1f);
        private readonly Color line = new Color(0.56f, 0.66f, 0.52f, 0.55f);

        public int LayoutVersion => 4;
        public string BottomMenuOrder => "성장 → 장비 → 스킬 → 펫 → 전직 → 상점";
        public bool UsesCircularSkillLayout => true;
        public bool UsesElementEquipmentTabs => true;
        public bool UsesSplitGrowthLayout => true;
        public bool UsesPerTierAdvancementButtons => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutMenuLayoutV4>() != null) return;
            GameObject go = new GameObject("PeanutWarriorMenuLayoutV4");
            DontDestroyOnLoad(go);
            go.AddComponent<PeanutMenuLayoutV4>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 14; i++)
            {
                yield return null;
                if (Bind()) break;
            }
            if (sourceUi == null || contentHost == null)
            {
                enabled = false;
                yield break;
            }
            CreateAssets();
        }

        private bool Bind()
        {
            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            layoutV2 = FindFirstObjectByType<PeanutMenuLayoutV2>();
            layoutV3 = FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            skills = FindFirstObjectByType<SkillManagementPrototype>();
            advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            equipment = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            saveService = FindFirstObjectByType<PeanutSaveGameService>();
            if (sourceUi == null || arena == null || stageFlow == null) return false;

            Type uiType = typeof(PeanutMobileCanvasPrototype);
            contentHostField = uiType.GetField("contentHost", PrivateInstance);
            currentPageField = uiType.GetField("currentPage", PrivateInstance);
            sourceRefreshersField = uiType.GetField("refreshers", PrivateInstance);
            menuTitleField = uiType.GetField("menuTitle", PrivateInstance);
            toastMethod = uiType.GetMethod("Toast", PrivateInstance);
            contentHost = contentHostField?.GetValue(sourceUi) as GameObject;
            sourceRefreshers = sourceRefreshersField?.GetValue(sourceUi) as IList;
            menuTitle = menuTitleField?.GetValue(sourceUi) as Text;

            if (layoutV2 != null)
                v2Refreshers = typeof(PeanutMenuLayoutV2).GetField("refreshers", PrivateInstance)?.GetValue(layoutV2) as IList;
            if (layoutV3 != null)
                v3Refreshers = typeof(PeanutCoreMenuCompletionV3).GetField("refreshers", PrivateInstance)?.GetValue(layoutV3) as IList;

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            attackLevelField = arenaType.GetField("attackLevel", PrivateInstance);
            hpLevelField = arenaType.GetField("hpLevel", PrivateInstance);
            maxMpLevelField = arenaType.GetField("maxMpLevel", PrivateInstance);
            mpRegenLevelField = arenaType.GetField("mpRegenLevel", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            maxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            maxMpProperty = arenaType.GetProperty("PlayerMaxMp", PrivateInstance);
            mpRegenProperty = arenaType.GetProperty("PlayerMpRegen", PrivateInstance);
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
            return contentHost != null;
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            solidSprite = CreateSolidSprite();
            roundedSprite = CreateRoundedSprite();
            circleSprite = CreateCircleSprite();
        }

        private void LateUpdate()
        {
            if (sourceUi == null || contentHost == null) return;
            string page = CurrentPage;
            bool managed = IsManaged(page);

            if (managed)
            {
                if (layoutV2 != null) layoutV2.enabled = false;
                if (layoutV3 != null) layoutV3.enabled = false;
                if (page != activePage || root == null || root.transform.parent != contentHost.transform)
                {
                    activePage = page;
                    BuildPage(page);
                }

                refreshTimer -= Time.unscaledDeltaTime;
                if (refreshTimer <= 0f)
                {
                    refreshTimer = 0.18f;
                    for (int i = 0; i < refreshers.Count; i++)
                    {
                        try { refreshers[i]?.Invoke(); }
                        catch (MissingReferenceException) { }
                    }
                }
                return;
            }

            activePage = page;
            if (page == "StageSelect")
            {
                if (layoutV3 != null) layoutV3.enabled = false;
                if (layoutV2 != null) layoutV2.enabled = true;
            }
            else if (page == "Pets" || page == "Shop" || page == "Settings")
            {
                if (layoutV2 != null) layoutV2.enabled = false;
                if (layoutV3 != null) layoutV3.enabled = true;
            }
            else
            {
                if (layoutV2 != null) layoutV2.enabled = true;
                if (layoutV3 != null) layoutV3.enabled = true;
            }
        }

        private string CurrentPage
        {
            get
            {
                object value = currentPageField?.GetValue(sourceUi);
                return value == null ? "Main" : value.ToString();
            }
        }

        private static bool IsManaged(string page)
        {
            return page == "Growth" || page == "Equipment" || page == "Skills" || page == "Advancement";
        }

        private void BuildPage(string page)
        {
            sourceRefreshers?.Clear();
            v2Refreshers?.Clear();
            v3Refreshers?.Clear();
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            root = Rect(contentHost.transform, "Peanut Menu Layout V4", 0f, 0f, Width, Height).gameObject;
            Image backgroundImage = root.AddComponent<Image>();
            backgroundImage.sprite = solidSprite;
            backgroundImage.color = background;
            backgroundImage.raycastTarget = false;

            if (menuTitle != null)
            {
                menuTitle.text = page switch
                {
                    "Growth" => "성장",
                    "Equipment" => "장비",
                    "Skills" => "스킬",
                    "Advancement" => "전직",
                    _ => menuTitle.text
                };
            }

            switch (page)
            {
                case "Growth": BuildGrowth(); break;
                case "Equipment": BuildEquipment(); break;
                case "Skills": BuildSkills(); break;
                case "Advancement": BuildAdvancement(); break;
            }

            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
        }

        private void BuildGrowth()
        {
            GameObject profile = Panel(root.transform, "Peanut Profile", 20f, 14f, 420f, 616f, cream, green);
            Label(profile.transform, "땅콩 외형·스펙", 22f, 14f, 376f, 40f, 22, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);

            Image upper;
            Image lower;
            BuildPeanutAvatar(profile.transform, 115f, 72f, out upper, out lower);
            Text formName = Label(profile.transform, string.Empty, 28f, 292f, 364f, 50f, 24, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text specs = Label(profile.transform, string.Empty, 34f, 354f, 352f, 226f, 16, brown, TextAnchor.UpperLeft, FontStyle.Bold);
            refreshers.Add(() =>
            {
                int tier = advancement == null ? 0 : advancement.Tier;
                Color formColor = TierColor(tier);
                upper.color = formColor;
                lower.color = Color.Lerp(formColor, Color.black, 0.06f);
                formName.text = advancement == null ? "새싹 땅콩" : advancement.CurrentName;
                string hunt = EquippedName(false);
                string boss = EquippedName(true);
                specs.text =
                    $"레벨  Lv.{(growth == null ? 1 : growth.PlayerLevel):N0}\n" +
                    $"전투력  {CombatPower:N0}\n\n" +
                    $"공격력  {AttackDamage:N1}\n" +
                    $"HP  {MaxHp:N0}\n" +
                    $"MP  {MaxMp:N0}\n" +
                    $"치명타  {(growth == null ? 0f : growth.CriticalChance * 100f):0}%\n" +
                    $"치명타 피해  {(growth == null ? 150f : growth.CriticalDamageMultiplier * 100f):0}%\n\n" +
                    $"사냥 장비  {hunt}\n" +
                    $"보스 장비  {boss}";
            });

            GameObject growthPanel = Panel(root.transform, "Growth Upgrades", 458f, 14f, 910f, 616f, card, green);
            Label(growthPanel.transform, "능력치 강화", 22f, 12f, 290f, 44f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(growthPanel.transform, "강화 수량", 450f, 12f, 110f, 44f, 15, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int amount = amounts[i];
                FlatButton(growthPanel.transform, "×" + amount, 570f + i * 104f, 10f, 94f, 46f,
                    purchaseAmount == amount ? gold : cream,
                    purchaseAmount == amount ? Color.white : darkGreen,
                    () => { purchaseAmount = amount; BuildPage("Growth"); });
            }

            Transform scroll = ScrollContent(growthPanel.transform, 16f, 66f, 878f, 534f, 10 * 60f + 8f);
            BuildGrowthRow(scroll, 0, "공격력", () => $"현재 {AttackDamage:N1}", attackLevelField, arena, 20L, 0);
            BuildGrowthRow(scroll, 1, "HP", () => $"최대 {MaxHp:N0}", hpLevelField, arena, 25L, 0);
            BuildGrowthRow(scroll, 2, "HP 회복량", () => $"초당 {(growth == null ? 0f : growth.HpRecoveryPerSecond):0.0}", hpRegenLevelField, growth, 40L, 0);
            BuildGrowthRow(scroll, 3, "MP", () => $"최대 {MaxMp:N0}", maxMpLevelField, arena, 30L, 0);
            BuildGrowthRow(scroll, 4, "MP 회복량", () => $"초당 {MpRegen:0.0}", mpRegenLevelField, arena, 35L, 0);
            BuildGrowthRow(scroll, 5, "치명타", () => $"{(growth == null ? 0f : growth.CriticalChance * 100f):0}% · 최대 100%", critChanceLevelField, growth, 45L, 49);
            BuildGrowthRow(scroll, 6, "치명타 피해량", () => $"{(growth == null ? 150f : growth.CriticalDamageMultiplier * 100f):0}%", critDamageLevelField, growth, 55L, 0);
            BuildGrowthRow(scroll, 7, "경험치 획득량 증가", () => $"+{((growth == null ? 1f : growth.ExperienceMultiplier) - 1f) * 100f:0}%", expGainLevelField, growth, 70L, 0);
            BuildGrowthRow(scroll, 8, "골드 획득량 증가", () => $"+{((growth == null ? 1f : growth.GoldMultiplier) - 1f) * 100f:0}%", goldGainLevelField, growth, 65L, 0);
            BuildGrowthRow(scroll, 9, "장비 강화 획득량 증가", () => $"+{((growth == null ? 1f : growth.EquipmentMaterialMultiplier) - 1f) * 100f:0}%", equipmentGainLevelField, growth, 80L, 0);
        }

        private void BuildGrowthRow(Transform parent, int row, string title, Func<string> detailValue,
            FieldInfo field, object target, long baseCost, int maxLevel)
        {
            float y = row * 60f;
            GameObject lineRoot = Panel(parent, "Growth " + title, 0f, y, 860f, 54f, row % 2 == 0 ? cream : new Color(0.91f, 0.95f, 0.87f), line);
            Label(lineRoot.transform, title, 18f, 5f, 280f, 24f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text detail = Label(lineRoot.transform, string.Empty, 18f, 27f, 360f, 20f, 13, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Text level = Label(lineRoot.transform, string.Empty, 398f, 6f, 120f, 42f, 15, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button button = FlatButton(lineRoot.transform, string.Empty, 590f, 6f, 246f, 42f, gold, Color.white,
                () => UpgradeStat(field, target, baseCost, title, maxLevel));
            Text buttonText = button.GetComponentInChildren<Text>();
            refreshers.Add(() =>
            {
                int currentLevel = ReadLevel(field, target);
                int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - currentLevel)) : purchaseAmount;
                detail.text = detailValue == null ? string.Empty : detailValue();
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
                    buttonText.text = $"+{amount}  ·  {cost:N0}G";
                }
            });
        }

        private void BuildSkills()
        {
            FlatButton(root.transform, string.Empty, 20f, 14f, 118f, 40f, green, Color.white, ToggleSkillAuto);
            Text autoText = root.transform.GetChild(root.transform.childCount - 1).GetComponentInChildren<Text>();
            refreshers.Add(() => autoText.text = skills != null && skills.GlobalAutoEnabled ? "AUTO ON" : "AUTO OFF");

            Label(root.transform, "사냥 스킬", 162f, 12f, 470f, 46f, 23, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            Label(root.transform, "보스 스킬", 756f, 12f, 470f, 46f, 23, new Color(0.55f, 0.23f, 0.12f), TextAnchor.MiddleCenter, FontStyle.Bold);
            Image divider = Rect(root.transform, "Skill Divider", 693f, 62f, 2f, 548f).gameObject.AddComponent<Image>();
            divider.sprite = solidSprite;
            divider.color = line;
            divider.raycastTarget = false;

            for (int i = 0; i < 8; i++) BuildCircularSkill(i);
            Label(root.transform, "동그란 스킬을 누르면 조각을 사용해 강화합니다.", 20f, 608f, 1348f, 28f, 14, muted, TextAnchor.MiddleCenter, FontStyle.Normal);
        }

        private void BuildCircularSkill(int index)
        {
            bool boss = index >= 4;
            int local = index % 4;
            int column = local % 2;
            int row = local / 2;
            float baseX = boss ? 780f : 100f;
            float x = baseX + column * 300f;
            float y = 102f + row * 250f;
            Color skillColor = boss ? new Color(0.66f, 0.31f, 0.17f) : green;

            GameObject circle = new GameObject("Circular Skill " + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            RectTransform rect = circle.GetComponent<RectTransform>();
            rect.SetParent(root.transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(172f, 172f);
            Image image = circle.GetComponent<Image>();
            image.sprite = circleSprite;
            image.color = skillColor;
            Button button = circle.GetComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            int captured = index;
            button.onClick.AddListener(() => UpgradeSkill(captured));

            Text name = Label(circle.transform, string.Empty, 20f, 31f, 132f, 56f, 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text state = Label(circle.transform, string.Empty, 18f, 90f, 136f, 54f, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text cost = Label(root.transform, string.Empty, x - 22f, y + 178f, 216f, 40f, 14, brown, TextAnchor.MiddleCenter, FontStyle.Bold);

            refreshers.Add(() =>
            {
                int[] levels = skills == null ? null : skills.SkillLevels;
                float[] cooldowns = skills == null ? null : skills.Cooldowns;
                int levelValue = levels != null && captured < levels.Length ? levels[captured] : 1;
                float cooldown = cooldowns != null && captured < cooldowns.Length ? Mathf.Max(0f, cooldowns[captured]) : 0f;
                long upgradeCost = skills == null ? 0L : skills.GetUpgradeCost(captured);
                name.text = skills == null ? $"스킬 {captured + 1}" : skills.GetSkillName(captured);
                state.text = $"Lv.{levelValue}\n{(cooldown > 0.05f ? cooldown.ToString("0.0") + "초" : "READY")}";
                cost.text = $"강화 {upgradeCost:N0} 조각";
                button.interactable = skills != null && Fragments >= upgradeCost;
                image.color = button.interactable ? skillColor : Color.Lerp(skillColor, Color.gray, 0.55f);
            });
        }

        private void BuildEquipment()
        {
            Label(root.transform, "속성 장비 도감", 20f, 10f, 260f, 46f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text materials = Label(root.transform, string.Empty, 1010f, 10f, 358f, 46f, 15, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() => materials.text = $"강화 재료 {(growth == null ? 0 : growth.EquipmentEnhancementMaterials):N0}");

            string[] tabs = { "무속성", "화염", "냉기", "번개" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int element = i;
                FlatButton(root.transform, tabs[i], 20f + i * 337f, 60f, 324f, 54f,
                    equipmentElementTab == i ? ElementColors[i] : cream,
                    equipmentElementTab == i ? Color.white : darkGreen,
                    () => { equipmentElementTab = element; BuildPage("Equipment"); });
            }

            Text equipped = Label(root.transform, string.Empty, 20f, 118f, 1348f, 34f, 14, brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            refreshers.Add(() => equipped.text = $"사냥 장착: {EquippedName(false)}    ·    보스 장착: {EquippedName(true)}");

            for (int rarity = 1; rarity <= 4; rarity++) BuildEquipmentRarityRow(rarity);
        }

        private void BuildEquipmentRarityRow(int rarity)
        {
            float y = 160f + (rarity - 1) * 118f;
            Color rarityColor = RarityColor(rarity);
            Label(root.transform, equipment == null ? "등급" : equipment.RarityName(rarity), 20f, y + 24f, 112f, 60f, 18, rarityColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int variant = 0; variant < 3; variant++)
            {
                int itemId = equipment == null ? -1 : equipment.GetItemId(equipmentElementTab, rarity, variant);
                float x = 142f + variant * 408f;
                BuildEquipmentItem(itemId, x, y, rarityColor);
            }
        }

        private void BuildEquipmentItem(int itemId, float x, float y, Color accent)
        {
            GameObject item = Panel(root.transform, "Equipment Item " + itemId, x, y, 390f, 108f, card, accent);
            Text name = Label(item.transform, string.Empty, 14f, 6f, 218f, 30f, 16, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text info = Label(item.transform, string.Empty, 14f, 36f, 218f, 58f, 13, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Button hunting = FlatButton(item.transform, "사냥", 244f, 8f, 64f, 28f, cream, darkGreen, () => EquipItem(itemId, false));
            Button boss = FlatButton(item.transform, "보스", 314f, 8f, 64f, 28f, cream, darkGreen, () => EquipItem(itemId, true));
            Button upgrade = FlatButton(item.transform, string.Empty, 244f, 46f, 134f, 48f, accent, Color.white, () => UpgradeEquipment(itemId));
            Text upgradeText = upgrade.GetComponentInChildren<Text>();

            refreshers.Add(() =>
            {
                if (equipment == null || itemId < 0)
                {
                    name.text = "장비 연결 대기";
                    info.text = string.Empty;
                    hunting.interactable = boss.interactable = upgrade.interactable = false;
                    return;
                }

                ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment.GetDefinition(itemId);
                bool owned = equipment.IsOwned(itemId);
                int copies = equipment.GetCopies(itemId);
                int levelValue = equipment.GetLevel(itemId);
                int cost = equipment.GetUpgradeCost(itemId);
                name.text = owned ? definition.Name : "잠김 · " + definition.Name;
                info.text = owned
                    ? $"Lv.{levelValue} · 보유 {copies}\n피해 ×{equipment.GetItemDamageMultiplier(itemId):0.00}"
                    : "소환으로 획득";
                hunting.interactable = owned;
                boss.interactable = owned;
                upgrade.interactable = owned && growth != null && growth.EquipmentEnhancementMaterials >= cost;
                hunting.GetComponent<Image>().color = equipment.IsEquipped(itemId, false) ? green : cream;
                hunting.GetComponentInChildren<Text>().color = equipment.IsEquipped(itemId, false) ? Color.white : darkGreen;
                boss.GetComponent<Image>().color = equipment.IsEquipped(itemId, true) ? red : cream;
                boss.GetComponentInChildren<Text>().color = equipment.IsEquipped(itemId, true) ? Color.white : darkGreen;
                upgradeText.text = owned ? $"강화\n{cost} 재료" : "잠김";
            });
        }

        private void BuildAdvancement()
        {
            Text summary = Label(root.transform, string.Empty, 20f, 8f, 1348f, 52f, 19, darkGreen, TextAnchor.MiddleCenter, FontStyle.Bold);
            refreshers.Add(() => summary.text = advancement == null
                ? "전직 시스템 연결 대기"
                : $"현재 {advancement.CurrentName} · 전투력 {advancement.CombatPower:N0} · 스테이지 {advancement.GlobalStage:N0}");

            Transform content = ScrollContent(root.transform, 20f, 66f, 1348f, 564f,
                PeanutGameRules.AdvancementCount * 118f + 12f);
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++) BuildAdvancementRow(content, i);
        }

        private void BuildAdvancementRow(Transform parent, int tierIndex)
        {
            PeanutGameRules.AdvancementDefinition definition = PeanutGameRules.GetAdvancement(tierIndex);
            float y = tierIndex * 118f;
            GameObject row = Panel(parent, "Advancement " + tierIndex, 0f, y, 1326f, 108f, card, tierIndex >= 6 ? gold : green);
            Badge(row.transform, (tierIndex + 1).ToString(), 14f, 20f, 66f, 66f, tierIndex >= 6 ? gold : green, Color.white, 21);
            Label(row.transform, definition.Name, 98f, 8f, 330f, 36f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(row.transform,
                $"능력치 ×{definition.StatMultiplier:0.00} · 기본 공격 {definition.BasicAttackHits}타" +
                (definition.UnlocksPets && tierIndex == 2 ? " · 펫 3슬롯 해금" : string.Empty),
                98f, 44f, 450f, 48f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Normal);
            Text requirements = Label(row.transform, string.Empty, 560f, 10f, 470f, 84f, 13, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button action = FlatButton(row.transform, string.Empty, 1060f, 22f, 244f, 64f, green, Color.white,
                () => TryAdvanceTier(tierIndex));
            Text actionText = action.GetComponentInChildren<Text>();

            refreshers.Add(() =>
            {
                int currentTier = advancement == null ? 0 : advancement.Tier;
                requirements.text =
                    $"스테이지 {definition.RequiredGlobalStage:N0} · 전투력 {definition.RequiredCombatPower:N0}\n" +
                    $"골드 {definition.RequiredGold:N0} · 다이아 {definition.RequiredDiamonds:N0}";

                if (tierIndex < currentTier)
                {
                    action.interactable = false;
                    actionText.text = "전직 완료";
                    action.GetComponent<Image>().color = muted;
                }
                else if (tierIndex == currentTier)
                {
                    action.interactable = false;
                    actionText.text = "현재 전직";
                    action.GetComponent<Image>().color = gold;
                }
                else if (tierIndex == currentTier + 1)
                {
                    bool ready = advancement != null && advancement.MeetsNextRequirements(out string reason);
                    action.interactable = ready;
                    actionText.text = ready ? "전직하기" : "조건 부족";
                    action.GetComponent<Image>().color = ready ? green : muted;
                }
                else
                {
                    action.interactable = false;
                    actionText.text = "잠김";
                    action.GetComponent<Image>().color = new Color(0.62f, 0.64f, 0.58f);
                }
            });
        }

        private void TryAdvanceTier(int tierIndex)
        {
            if (advancement == null)
            {
                Toast("전직 시스템 연결 대기");
                return;
            }
            if (tierIndex != advancement.Tier + 1)
            {
                Toast("현재 다음 단계만 전직할 수 있습니다");
                return;
            }
            advancement.TryAdvance();
            saveService?.SaveNow();
            Toast(advancement.LastMessage);
        }

        private void UpgradeStat(FieldInfo field, object target, long baseCost, string title, int maxLevel)
        {
            if (field == null || target == null)
            {
                Toast(title + " 연결 대기");
                return;
            }
            int current = ReadLevel(field, target);
            int amount = maxLevel > 0 ? Mathf.Max(0, Mathf.Min(purchaseAmount, maxLevel - current)) : purchaseAmount;
            if (amount <= 0)
            {
                Toast(title + " 최대치");
                return;
            }
            long cost = UpgradeCost(baseCost, current, amount);
            if (Gold < cost || goldField == null)
            {
                Toast($"골드 부족 · {cost:N0}G 필요");
                return;
            }
            goldField.SetValue(arena, Gold - cost);
            field.SetValue(target, current + amount);
            growth?.SaveNow();
            saveService?.SaveNow();
            Toast($"{title} +{amount} 강화 완료");
        }

        private void UpgradeSkill(int index)
        {
            if (skills == null)
            {
                Toast("스킬 시스템 연결 대기");
                return;
            }
            skills.UpgradeSkill(index);
            saveService?.SaveNow();
            Toast(skills.LastMessage);
        }

        private void ToggleSkillAuto()
        {
            if (skills == null)
            {
                Toast("스킬 시스템 연결 대기");
                return;
            }
            skills.ToggleGlobalAuto();
            Toast(skills.LastMessage);
        }

        private void EquipItem(int itemId, bool boss)
        {
            if (equipment == null)
            {
                Toast("장비 도감 연결 대기");
                return;
            }
            equipment.EquipItem(itemId, boss);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
        }

        private void UpgradeEquipment(int itemId)
        {
            if (equipment == null)
            {
                Toast("장비 도감 연결 대기");
                return;
            }
            equipment.UpgradeItem(itemId);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
        }

        private string EquippedName(bool boss)
        {
            if (equipment == null) return "미장착";
            int itemId = equipment.GetEquippedItem(boss);
            ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment.GetDefinition(itemId);
            return definition == null ? "미장착" : definition.Name;
        }

        private void BuildPeanutAvatar(Transform parent, float x, float y, out Image upper, out Image lower)
        {
            GameObject avatar = Rect(parent, "Peanut Avatar", x, y, 190f, 198f).gameObject;
            upper = CircleImage(avatar.transform, "Upper Shell", 35f, 0f, 120f, 120f, gold);
            lower = CircleImage(avatar.transform, "Lower Shell", 35f, 78f, 120f, 120f, new Color(0.78f, 0.48f, 0.13f));
            CircleImage(avatar.transform, "Left Eye", 72f, 60f, 12f, 12f, brown);
            CircleImage(avatar.transform, "Right Eye", 108f, 60f, 12f, 12f, brown);
            GameObject mouth = Rect(avatar.transform, "Mouth", 84f, 84f, 22f, 4f).gameObject;
            Image mouthImage = mouth.AddComponent<Image>();
            mouthImage.sprite = roundedSprite;
            mouthImage.color = brown;
            mouthImage.raycastTarget = false;
            GameObject stripe = Rect(avatar.transform, "Shell Stripe", 91f, 24f, 8f, 150f).gameObject;
            Image stripeImage = stripe.AddComponent<Image>();
            stripeImage.sprite = roundedSprite;
            stripeImage.color = new Color(1f, 0.93f, 0.55f, 0.62f);
            stripeImage.raycastTarget = false;
        }

        private Image CircleImage(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            GameObject go = Rect(parent, name, x, y, width, height).gameObject;
            Image image = go.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private Transform ScrollContent(Transform parent, float x, float y, float width, float height, float contentHeight)
        {
            GameObject viewport = Rect(parent, "Scroll View", x, y, width, height).gameObject;
            Image backgroundImage = viewport.AddComponent<Image>();
            backgroundImage.sprite = roundedSprite;
            backgroundImage.color = new Color(1f, 1f, 1f, 0.24f);
            RectMask2D mask = viewport.AddComponent<RectMask2D>();
            mask.padding = new Vector4(2f, 2f, 2f, 2f);
            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            RectTransform content = Rect(viewport.transform, "Content", 8f, 8f, width - 16f, Mathf.Max(height - 16f, contentHeight));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = content;
            return content;
        }

        private GameObject Panel(Transform parent, string name, float x, float y, float width, float height, Color color, Color border)
        {
            GameObject go = Rect(parent, name, x, y, width, height).gameObject;
            Image image = go.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.38f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = false;
            return go;
        }

        private GameObject Badge(Transform parent, string text, float x, float y, float width, float height, Color color, Color textColor, int size)
        {
            GameObject badge = Panel(parent, "Badge", x, y, width, height, color, color);
            Label(badge.transform, text, 3f, 2f, width - 6f, height - 4f, size, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return badge;
        }

        private Button FlatButton(Transform parent, string text, float x, float y, float width, float height,
            Color color, Color textColor, Action action)
        {
            GameObject go = Panel(parent, "Button", x, y, width, height, color, color);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Label(go.transform, text, 4f, 2f, width - 8f, height - 4f, 14, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private Text Label(Transform parent, string text, float x, float y, float width, float height,
            int size, Color color, TextAnchor anchor, FontStyle style)
        {
            GameObject go = Rect(parent, "Text", x, y, width, height).gameObject;
            Text label = go.AddComponent<Text>();
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

        private RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
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

        private void Toast(string message)
        {
            if (sourceUi != null && toastMethod != null)
                toastMethod.Invoke(sourceUi, new object[] { message });
            else
                Debug.Log("[PeanutWarrior] " + message);
        }

        private int ReadLevel(FieldInfo field, object target)
        {
            return field == null || target == null ? 1 : Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        }

        private static long UpgradeCost(long baseCost, int currentLevel, int amount)
        {
            long total = 0L;
            for (int i = 0; i < amount; i++)
            {
                long level = Math.Max(1, currentLevel + i);
                if (level > long.MaxValue / Math.Max(1L, baseCost)) return long.MaxValue;
                long next = baseCost * level;
                if (long.MaxValue - total < next) return long.MaxValue;
                total += next;
            }
            return total;
        }

        private long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null || arena == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));
        private float AttackDamage => attackDamageProperty == null ? 0f : Convert.ToSingle(attackDamageProperty.GetValue(arena));
        private float MaxHp => maxHpProperty == null ? 0f : Convert.ToSingle(maxHpProperty.GetValue(arena));
        private float MaxMp => maxMpProperty == null ? 0f : Convert.ToSingle(maxMpProperty.GetValue(arena));
        private float MpRegen => mpRegenProperty == null ? 0f : Convert.ToSingle(mpRegenProperty.GetValue(arena));
        private int CombatPower => combatPowerProperty == null ? 0 : Convert.ToInt32(combatPowerProperty.GetValue(arena));

        private static Color TierColor(int tier)
        {
            Color[] colors =
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
            return colors[Mathf.Clamp(tier, 0, colors.Length - 1)];
        }

        private static Color RarityColor(int rarity)
        {
            return rarity switch
            {
                1 => new Color(0.31f, 0.56f, 0.82f),
                2 => new Color(0.62f, 0.33f, 0.85f),
                3 => new Color(0.92f, 0.48f, 0.12f),
                4 => new Color(0.94f, 0.70f, 0.10f),
                _ => Color.gray
            };
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius + 0.9f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 32;
            const float radius = 9f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, radius - Mathf.Min(x, size - 1 - x));
                    float dy = Mathf.Max(0f, radius - Mathf.Min(y, size - 1 - y));
                    float alpha = Mathf.Clamp01(radius + 0.6f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }
    }
}
