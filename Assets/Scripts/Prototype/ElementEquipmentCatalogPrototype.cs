using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(7400)]
    public sealed class ElementEquipmentCatalogPrototype : MonoBehaviour
    {
        public enum EquipmentUse
        {
            Hunting = 0,
            Boss = 1
        }

        public enum EquipmentElement
        {
            Neutral = 0,
            Fire = 1,
            Ice = 2,
            Lightning = 3
        }

        public enum EquipmentRarity
        {
            Rare = 1,
            Epic = 2,
            Unique = 3,
            Legend = 4
        }

        public enum HuntingAttackStyle
        {
            Cleave = 0,
            AreaBurst = 1,
            Chain = 2
        }

        public enum BossAttackStyle
        {
            FocusStrike = 0,
            RapidPierce = 1,
            Execution = 2
        }

        [Serializable]
        public sealed class EquipmentDefinition
        {
            public int Id;
            public string Name;
            public EquipmentUse Use;
            public EquipmentElement Element;
            public EquipmentRarity Rarity;
            public int Variant;
        }

        public struct HuntingModeProfile
        {
            public HuntingAttackStyle Style;
            public string StyleName;
            public int MaxTargets;
            public float Radius;
            public float DamageRatio;
            public float ChainFalloff;
        }

        public struct BossModeProfile
        {
            public BossAttackStyle Style;
            public string StyleName;
            public int HitCount;
            public float TotalDamageRatio;
            public float ExecuteThreshold;
            public float ExecuteBonusRatio;
        }

        private const string Prefix = "PeanutWarrior.ElementEquipment.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int CurrentSchemaVersion = 4;
        private const int ElementCount = 4;
        private const int RarityCount = 4;
        private const int VariantsPerRarity = 3;
        private const int ItemCount = ElementCount * RarityCount * VariantsPerRarity;
        private const int LegacyItemsPerUse = ItemCount;

        private static readonly string[,,] EquipmentNames =
        {
            {
                { "나뭇가지 검", "단단한 껍질검", "들판 수호검" },
                { "볶은 땅콩검", "농부의 대검", "황금 꼬투리검" },
                { "수확의 칼날", "대지 수호검", "풍요의 대검" },
                { "태초의 땅콩검", "대지왕의 칼날", "무한 수확검" }
            },
            {
                { "불씨검", "숯불 껍질검", "화로검" },
                { "홍염 땅콩검", "용광로 대검", "타오르는 꼬투리" },
                { "업화의 칼날", "불사조 껍질검", "화염 군주의 검" },
                { "태양 땅콩검", "종말의 화염검", "영원한 불씨" }
            },
            {
                { "서리검", "얼음 껍질검", "찬바람 검" },
                { "설원 땅콩검", "빙하 대검", "푸른 고드름검" },
                { "영구동토 칼날", "서리 군주의 검", "빙결 수호검" },
                { "절대영도 땅콩검", "겨울왕의 칼날", "영원의 빙검" }
            },
            {
                { "전류검", "번개 껍질검", "폭풍 단검" },
                { "낙뢰 땅콩검", "천둥 대검", "구름 가르개" },
                { "뇌신의 칼날", "폭풍 군주의 검", "섬광 수호검" },
                { "천벌 땅콩검", "뇌광왕의 칼날", "무한 낙뢰검" }
            }
        };

        private readonly EquipmentDefinition[] definitions = new EquipmentDefinition[ItemCount];
        private readonly int[] levels = new int[ItemCount];
        private readonly int[] copies = new int[ItemCount];

        private CombatPrototypeArena arena;
        private GrowthExpansionPrototype growth;
        private SwordProgressionPrototype legacySwords;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private int huntingItem = -1;
        private int bossItem = -1;
        private int schemaVersion;
        private bool legacyMigrated;
        private string lastMessage = "전투 모드 변환 장비 준비";

        public int TotalItemCount => ItemCount;
        public int UnifiedItemCount => ItemCount;
        public int ItemCountPerUse => ItemCount;
        public int VariantsPerGrade => VariantsPerRarity;
        public string LastMessage => lastMessage;
        public int HuntingItem => huntingItem;
        public int BossItem => bossItem;
        public bool UsesSeparateHuntingAndBossCatalogs => false;
        public bool UsesUnifiedEquipmentEntries => true;
        public bool ShowsDualBattleEffects => true;
        public bool ChangesAttackPatternByBattleMode => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<ElementEquipmentCatalogPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorElementEquipmentCatalog");
            DontDestroyOnLoad(root);
            root.AddComponent<ElementEquipmentCatalogPrototype>();
        }

        private void Awake()
        {
            BuildDefinitions();
            for (int i = 0; i < levels.Length; i++) levels[i] = 1;
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            legacySwords = FindFirstObjectByType<SwordProgressionPrototype>();
            if (arena == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            LoadAndMigrateSeparatedCatalog();
            MigrateLegacyCollection();
            EnsureStarterEquipment();
            RepairEquippedItems();
            Save();
        }

        public EquipmentDefinition GetDefinition(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            return IsValidItem(itemId) ? definitions[itemId] : null;
        }

        public int GetUnifiedItemId(int elementIndex, int rarityIndex, int variant)
        {
            if (elementIndex < 0 || elementIndex >= ElementCount) return -1;
            if (rarityIndex < 1 || rarityIndex > RarityCount) return -1;
            if (variant < 0 || variant >= VariantsPerRarity) return -1;
            return elementIndex * RarityCount * VariantsPerRarity +
                   (rarityIndex - 1) * VariantsPerRarity + variant;
        }

        public int GetItemId(bool boss, int elementIndex, int rarityIndex, int variant)
        {
            return GetUnifiedItemId(elementIndex, rarityIndex, variant);
        }

        public int GetItemId(EquipmentUse use, int elementIndex, int rarityIndex, int variant)
        {
            return GetUnifiedItemId(elementIndex, rarityIndex, variant);
        }

        public int GetItemId(int elementIndex, int rarityIndex, int variant)
        {
            return GetUnifiedItemId(elementIndex, rarityIndex, variant);
        }

        public IEnumerable<int> GetUnifiedItemIds(int elementIndex, int rarityIndex)
        {
            for (int variant = 0; variant < VariantsPerRarity; variant++)
            {
                int id = GetUnifiedItemId(elementIndex, rarityIndex, variant);
                if (id >= 0) yield return id;
            }
        }

        public IEnumerable<int> GetItemIds(bool boss, int elementIndex, int rarityIndex)
        {
            return GetUnifiedItemIds(elementIndex, rarityIndex);
        }

        public IEnumerable<int> GetItemIds(int elementIndex, int rarityIndex)
        {
            return GetUnifiedItemIds(elementIndex, rarityIndex);
        }

        public int GetLevel(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            return IsValidItem(itemId) ? levels[itemId] : 1;
        }

        public int GetCopies(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            return IsValidItem(itemId) ? copies[itemId] : 0;
        }

        public bool IsOwned(int itemId) => GetCopies(itemId) > 0;

        public int GetOwnedCount(bool boss, int elementIndex, int rarityIndex)
        {
            return GetOwnedCount(elementIndex, rarityIndex);
        }

        public int GetOwnedCount(int elementIndex, int rarityIndex)
        {
            int count = 0;
            foreach (int id in GetUnifiedItemIds(elementIndex, rarityIndex))
                if (IsOwned(id)) count++;
            return count;
        }

        public int GetUpgradeCost(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return int.MaxValue;
            int rarity = (int)definitions[itemId].Rarity;
            return Mathf.Max(1, rarity * 3 + levels[itemId] * rarity * 2);
        }

        public bool UpgradeItem(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return Fail("잘못된 장비 선택");
            if (!IsOwned(itemId)) return Fail("먼저 장비를 획득해야 합니다");
            int cost = GetUpgradeCost(itemId);
            if (growth == null || !growth.TrySpendEquipmentMaterials(cost))
                return Fail($"장비 강화 재료 {cost:N0}개 필요");

            levels[itemId]++;
            lastMessage = $"{definitions[itemId].Name} Lv.{levels[itemId]} 강화 · 두 전투 모드 동시 성장";
            Save();
            return true;
        }

        public void RegisterSummon(bool boss, int elementIndex, int rarityIndex)
        {
            RegisterSummon(elementIndex, rarityIndex);
        }

        public void RegisterSummon(int elementIndex, int rarityIndex)
        {
            elementIndex = Mathf.Clamp(elementIndex, 0, ElementCount - 1);
            rarityIndex = Mathf.Clamp(rarityIndex, 1, RarityCount);
            int selected = GetUnifiedItemId(elementIndex, rarityIndex, 0);
            int minimum = int.MaxValue;
            foreach (int id in GetUnifiedItemIds(elementIndex, rarityIndex))
            {
                if (copies[id] >= minimum) continue;
                minimum = copies[id];
                selected = id;
            }

            copies[selected]++;
            if (copies[selected] > 1 && copies[selected] % 3 == 0) levels[selected]++;
            EquipmentDefinition definition = definitions[selected];
            lastMessage = $"{definition.Name} 획득 · 사냥 패턴과 보스 패턴 동시 해금";
            RepairEquippedItems();
            Save();
        }

        public bool EquipItem(int itemId, bool boss)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return Fail("잘못된 장비 선택");
            if (!IsOwned(itemId)) return Fail("보유하지 않은 장비입니다");

            EquipmentDefinition definition = definitions[itemId];
            FieldInfo elementField = boss ? bossElementField : huntingElementField;
            if (elementField != null)
                elementField.SetValue(arena, Enum.ToObject(elementField.FieldType, (int)definition.Element));

            if (boss) bossItem = itemId;
            else huntingItem = itemId;
            lastMessage = $"{definition.Name} · {(boss ? "보스 집중" : "사냥 범위")} 모드 장착";
            Save();
            return true;
        }

        public int GetEquippedItem(bool boss) => boss ? bossItem : huntingItem;

        public bool IsEquipped(int itemId, bool boss)
        {
            return GetEquippedItem(boss) == CanonicalItemId(itemId);
        }

        public HuntingModeProfile GetHuntingModeProfile(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return DefaultHuntingProfile();
            EquipmentDefinition definition = definitions[itemId];
            int rarity = (int)definition.Rarity;
            int level = GetLevel(itemId);
            int ownedCopies = GetCopies(itemId);
            float growthPower = rarity * 0.075f + Mathf.Max(0, level - 1) * 0.014f +
                                Mathf.Min(0.10f, Mathf.Max(0, ownedCopies - 1) * 0.012f);
            float elementDamage = definition.Element == EquipmentElement.Fire ? 1.08f :
                                  definition.Element == EquipmentElement.Ice ? 0.97f :
                                  definition.Element == EquipmentElement.Lightning ? 1.03f : 1f;
            float elementRadius = definition.Element == EquipmentElement.Ice ? 1.14f : 1f;
            int elementTargets = definition.Element == EquipmentElement.Lightning ? 1 : 0;

            HuntingAttackStyle style = (HuntingAttackStyle)Mathf.Clamp(definition.Variant, 0, 2);
            switch (style)
            {
                case HuntingAttackStyle.Cleave:
                    return new HuntingModeProfile
                    {
                        Style = style,
                        StyleName = "부채꼴 다중 베기",
                        MaxTargets = 2 + rarity + elementTargets,
                        Radius = (100f + rarity * 16f) * elementRadius,
                        DamageRatio = (0.28f + growthPower) * elementDamage,
                        ChainFalloff = 1f
                    };
                case HuntingAttackStyle.AreaBurst:
                    return new HuntingModeProfile
                    {
                        Style = style,
                        StyleName = "원형 범위 폭발",
                        MaxTargets = 3 + rarity + elementTargets,
                        Radius = (132f + rarity * 22f) * elementRadius,
                        DamageRatio = (0.24f + growthPower * 0.95f) * elementDamage,
                        ChainFalloff = 1f
                    };
                default:
                    return new HuntingModeProfile
                    {
                        Style = HuntingAttackStyle.Chain,
                        StyleName = "연쇄 검격",
                        MaxTargets = 2 + rarity + elementTargets,
                        Radius = (190f + rarity * 26f) * elementRadius,
                        DamageRatio = (0.26f + growthPower) * elementDamage,
                        ChainFalloff = Mathf.Clamp01(0.80f + rarity * 0.025f)
                    };
            }
        }

        public BossModeProfile GetBossModeProfile(int itemId)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return DefaultBossProfile();
            EquipmentDefinition definition = definitions[itemId];
            int rarity = (int)definition.Rarity;
            int level = GetLevel(itemId);
            int ownedCopies = GetCopies(itemId);
            float growthPower = rarity * 0.105f + Mathf.Max(0, level - 1) * 0.019f +
                                Mathf.Min(0.14f, Mathf.Max(0, ownedCopies - 1) * 0.015f);
            float elementDamage = definition.Element == EquipmentElement.Fire ? 1.08f :
                                  definition.Element == EquipmentElement.Lightning ? 1.04f : 1f;
            int lightningHit = definition.Element == EquipmentElement.Lightning ? 1 : 0;

            BossAttackStyle style = (BossAttackStyle)Mathf.Clamp(definition.Variant, 0, 2);
            switch (style)
            {
                case BossAttackStyle.FocusStrike:
                    return new BossModeProfile
                    {
                        Style = style,
                        StyleName = "집중 참격",
                        HitCount = 1,
                        TotalDamageRatio = (0.50f + growthPower) * elementDamage,
                        ExecuteThreshold = 0f,
                        ExecuteBonusRatio = 0f
                    };
                case BossAttackStyle.RapidPierce:
                    return new BossModeProfile
                    {
                        Style = style,
                        StyleName = "단일 연속 타격",
                        HitCount = 2 + rarity + lightningHit,
                        TotalDamageRatio = (0.46f + growthPower) * elementDamage,
                        ExecuteThreshold = 0f,
                        ExecuteBonusRatio = 0f
                    };
                default:
                    return new BossModeProfile
                    {
                        Style = BossAttackStyle.Execution,
                        StyleName = "처형 참격",
                        HitCount = 1,
                        TotalDamageRatio = (0.42f + growthPower) * elementDamage,
                        ExecuteThreshold = 0.25f + rarity * 0.025f,
                        ExecuteBonusRatio = (0.30f + rarity * 0.08f) * elementDamage
                    };
            }
        }

        public float GetItemDamageMultiplier(int itemId) => GetHuntingDamageMultiplier(itemId);

        public float GetHuntingDamageMultiplier(int itemId)
        {
            return 1f + GetHuntingModeProfile(itemId).DamageRatio;
        }

        public float GetBossDamageMultiplier(int itemId)
        {
            return 1f + GetBossModeProfile(itemId).TotalDamageRatio;
        }

        public string GetHuntingEffectDescription(int itemId)
        {
            HuntingModeProfile profile = GetHuntingModeProfile(itemId);
            return $"사냥 · {profile.StyleName} · 최대 {profile.MaxTargets}마리 · 공격력 {profile.DamageRatio * 100f:0}%";
        }

        public string GetBossEffectDescription(int itemId)
        {
            BossModeProfile profile = GetBossModeProfile(itemId);
            if (profile.Style == BossAttackStyle.RapidPierce)
                return $"보스 · {profile.StyleName} · 단일 1명 {profile.HitCount}타 · 총 {profile.TotalDamageRatio * 100f:0}%";
            if (profile.Style == BossAttackStyle.Execution)
                return $"보스 · {profile.StyleName} · 단일 1명 · HP {profile.ExecuteThreshold * 100f:0}% 이하 추가 {profile.ExecuteBonusRatio * 100f:0}%";
            return $"보스 · {profile.StyleName} · 단일 1명 · 공격력 {profile.TotalDamageRatio * 100f:0}%";
        }

        public float GetActiveDamageMultiplier(bool boss)
        {
            int itemId = GetEquippedItem(boss);
            return boss ? GetBossDamageMultiplier(itemId) : GetHuntingDamageMultiplier(itemId);
        }

        public string UseName(bool boss) => boss ? "보스" : "사냥";

        public string ElementName(int elementIndex)
        {
            return elementIndex switch
            {
                0 => "무속성",
                1 => "화염",
                2 => "냉기",
                3 => "번개",
                _ => "속성"
            };
        }

        public string RarityName(int rarityIndex)
        {
            return rarityIndex switch
            {
                1 => "레어",
                2 => "에픽",
                3 => "유니크",
                4 => "레전드",
                _ => "등급"
            };
        }

        private static HuntingModeProfile DefaultHuntingProfile()
        {
            return new HuntingModeProfile
            {
                Style = HuntingAttackStyle.Cleave,
                StyleName = "부채꼴 다중 베기",
                MaxTargets = 2,
                Radius = 110f,
                DamageRatio = 0.20f,
                ChainFalloff = 1f
            };
        }

        private static BossModeProfile DefaultBossProfile()
        {
            return new BossModeProfile
            {
                Style = BossAttackStyle.FocusStrike,
                StyleName = "집중 참격",
                HitCount = 1,
                TotalDamageRatio = 0.35f,
                ExecuteThreshold = 0f,
                ExecuteBonusRatio = 0f
            };
        }

        private void BuildDefinitions()
        {
            for (int element = 0; element < ElementCount; element++)
            {
                for (int rarity = 1; rarity <= RarityCount; rarity++)
                {
                    for (int variant = 0; variant < VariantsPerRarity; variant++)
                    {
                        int id = GetUnifiedItemId(element, rarity, variant);
                        definitions[id] = new EquipmentDefinition
                        {
                            Id = id,
                            Name = EquipmentNames[element, rarity - 1, variant],
                            Use = EquipmentUse.Hunting,
                            Element = (EquipmentElement)element,
                            Rarity = (EquipmentRarity)rarity,
                            Variant = variant
                        };
                    }
                }
            }
        }

        private void LoadAndMigrateSeparatedCatalog()
        {
            schemaVersion = PlayerPrefs.GetInt(Prefix + "Schema", 1);
            legacyMigrated = PlayerPrefs.GetInt(Prefix + "Migrated", 0) == 1;
            huntingItem = CanonicalItemId(PlayerPrefs.GetInt(Prefix + "HuntingItem", -1));
            bossItem = CanonicalItemId(PlayerPrefs.GetInt(Prefix + "BossItem", -1));

            for (int i = 0; i < ItemCount; i++)
            {
                int huntingLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Level." + i, 1));
                int huntingCopies = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Copies." + i, 0));
                int legacyBossId = LegacyItemsPerUse + i;
                int bossLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Level." + legacyBossId, 1));
                int bossCopies = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Copies." + legacyBossId, 0));
                levels[i] = Mathf.Max(huntingLevel, bossLevel);
                copies[i] = Mathf.Max(huntingCopies, bossCopies);
            }

            if (schemaVersion < CurrentSchemaVersion)
            {
                schemaVersion = CurrentSchemaVersion;
                lastMessage = "장비 효과를 사냥 범위형·보스 집중형 전투 모드로 변환 완료";
            }
        }

        private void MigrateLegacyCollection()
        {
            if (legacyMigrated || legacySwords == null) return;
            for (int element = 0; element < ElementCount; element++)
            {
                for (int rarity = 1; rarity <= RarityCount; rarity++)
                {
                    int legacyCopies = legacySwords.GetCopies(element, (SwordProgressionPrototype.SwordRarity)rarity);
                    for (int copy = 0; copy < legacyCopies; copy++)
                        copies[GetUnifiedItemId(element, rarity, copy % VariantsPerRarity)]++;
                }
            }
            legacyMigrated = true;
        }

        private void EnsureStarterEquipment()
        {
            if (FindFirstOwned(0) < 0) copies[GetUnifiedItemId(0, 1, 0)] = 1;
        }

        private void RepairEquippedItems()
        {
            if (!IsValidItem(huntingItem) || !IsOwned(huntingItem))
                huntingItem = FindFirstOwned(ReadElement(huntingElementField));
            if (!IsValidItem(bossItem) || !IsOwned(bossItem))
                bossItem = FindFirstOwned(ReadElement(bossElementField));
            ApplyEquippedElement(huntingItem, false);
            ApplyEquippedElement(bossItem, true);
        }

        private int FindFirstOwned(int preferredElement)
        {
            preferredElement = Mathf.Clamp(preferredElement, 0, ElementCount - 1);
            for (int rarity = RarityCount; rarity >= 1; rarity--)
                foreach (int id in GetUnifiedItemIds(preferredElement, rarity))
                    if (IsOwned(id)) return id;
            for (int id = 0; id < ItemCount; id++)
                if (IsOwned(id)) return id;
            return -1;
        }

        private void ApplyEquippedElement(int itemId, bool boss)
        {
            itemId = CanonicalItemId(itemId);
            if (!IsValidItem(itemId)) return;
            FieldInfo field = boss ? bossElementField : huntingElementField;
            if (field != null)
                field.SetValue(arena, Enum.ToObject(field.FieldType, (int)definitions[itemId].Element));
        }

        private int ReadElement(FieldInfo field)
        {
            return field == null || arena == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private static int CanonicalItemId(int itemId)
        {
            if (itemId >= LegacyItemsPerUse && itemId < LegacyItemsPerUse * 2)
                return itemId - LegacyItemsPerUse;
            return itemId;
        }

        private bool Fail(string value)
        {
            lastMessage = value;
            return false;
        }

        private static bool IsValidItem(int itemId) => itemId >= 0 && itemId < ItemCount;

        private void Save()
        {
            PlayerPrefs.SetInt(Prefix + "Schema", CurrentSchemaVersion);
            PlayerPrefs.SetInt(Prefix + "Migrated", legacyMigrated ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "HuntingItem", huntingItem);
            PlayerPrefs.SetInt(Prefix + "BossItem", bossItem);
            for (int i = 0; i < ItemCount; i++)
            {
                PlayerPrefs.SetInt(Prefix + "Level." + i, Mathf.Max(1, levels[i]));
                PlayerPrefs.SetInt(Prefix + "Copies." + i, Mathf.Max(0, copies[i]));
            }
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();
    }
}
