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

        private const string Prefix = "PeanutWarrior.ElementEquipment.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int CurrentSchemaVersion = 2;
        private const int UseCount = 2;
        private const int ElementCount = 4;
        private const int RarityCount = 4;
        private const int VariantsPerRarity = 3;
        private const int ItemsPerUse = ElementCount * RarityCount * VariantsPerRarity;
        private const int ItemCount = UseCount * ItemsPerUse;

        private static readonly string[,,] HuntingNames =
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

        private static readonly string[,,] BossNames =
        {
            {
                { "도전자 검", "거대수 사냥검", "결투 껍질검" },
                { "토벌대 검", "파괴자 대검", "수호자 처단검" },
                { "군주 절단검", "폭군 사냥검", "심판의 대검" },
                { "보스 종결검", "왕의 파멸검", "차원 토벌검" }
            },
            {
                { "화염 추적검", "불꽃 토벌검", "적열 처단검" },
                { "홍련 사냥검", "용암 파괴검", "폭염 결투검" },
                { "업화 심판검", "불사조 토벌검", "화신 절단검" },
                { "태양 파멸검", "종말의 홍염검", "영겁의 화염검" }
            },
            {
                { "빙결 추적검", "서리 토벌검", "냉기 처단검" },
                { "설원 사냥검", "빙하 파괴검", "혹한 결투검" },
                { "동토 심판검", "서리왕 토벌검", "빙결 절단검" },
                { "절대영도 파멸검", "겨울왕 종결검", "영겁의 빙검" }
            },
            {
                { "뇌전 추적검", "낙뢰 토벌검", "전광 처단검" },
                { "천둥 사냥검", "폭풍 파괴검", "섬광 결투검" },
                { "뇌신 심판검", "폭풍왕 토벌검", "천뢰 절단검" },
                { "천벌 파멸검", "뇌광왕 종결검", "영겁의 낙뢰검" }
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
        private string lastMessage = "사냥·보스 장비 도감 준비";

        public int TotalItemCount => ItemCount;
        public int ItemCountPerUse => ItemsPerUse;
        public int VariantsPerGrade => VariantsPerRarity;
        public string LastMessage => lastMessage;
        public int HuntingItem => huntingItem;
        public int BossItem => bossItem;
        public bool UsesSeparateHuntingAndBossCatalogs => true;

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
            Load();
            MigrateSharedCatalogToSeparatedCatalogs();
            MigrateLegacyCollection();
            EnsureStarterEquipment();
            RepairEquippedItems();
            Save();
        }

        public EquipmentDefinition GetDefinition(int itemId)
        {
            return IsValidItem(itemId) ? definitions[itemId] : null;
        }

        public int GetItemId(bool boss, int elementIndex, int rarityIndex, int variant)
        {
            return GetItemId(boss ? EquipmentUse.Boss : EquipmentUse.Hunting, elementIndex, rarityIndex, variant);
        }

        public int GetItemId(EquipmentUse use, int elementIndex, int rarityIndex, int variant)
        {
            int useIndex = (int)use;
            if (useIndex < 0 || useIndex >= UseCount) return -1;
            if (elementIndex < 0 || elementIndex >= ElementCount) return -1;
            if (rarityIndex < 1 || rarityIndex > RarityCount) return -1;
            if (variant < 0 || variant >= VariantsPerRarity) return -1;
            return useIndex * ItemsPerUse +
                   elementIndex * RarityCount * VariantsPerRarity +
                   (rarityIndex - 1) * VariantsPerRarity + variant;
        }

        public int GetItemId(int elementIndex, int rarityIndex, int variant)
        {
            return GetItemId(false, elementIndex, rarityIndex, variant);
        }

        public IEnumerable<int> GetItemIds(bool boss, int elementIndex, int rarityIndex)
        {
            for (int variant = 0; variant < VariantsPerRarity; variant++)
            {
                int id = GetItemId(boss, elementIndex, rarityIndex, variant);
                if (id >= 0) yield return id;
            }
        }

        public IEnumerable<int> GetItemIds(int elementIndex, int rarityIndex)
        {
            return GetItemIds(false, elementIndex, rarityIndex);
        }

        public int GetLevel(int itemId)
        {
            return IsValidItem(itemId) ? levels[itemId] : 1;
        }

        public int GetCopies(int itemId)
        {
            return IsValidItem(itemId) ? copies[itemId] : 0;
        }

        public bool IsOwned(int itemId)
        {
            return GetCopies(itemId) > 0;
        }

        public int GetOwnedCount(bool boss, int elementIndex, int rarityIndex)
        {
            int count = 0;
            foreach (int id in GetItemIds(boss, elementIndex, rarityIndex))
                if (IsOwned(id)) count++;
            return count;
        }

        public int GetOwnedCount(int elementIndex, int rarityIndex)
        {
            return GetOwnedCount(false, elementIndex, rarityIndex);
        }

        public int GetUpgradeCost(int itemId)
        {
            if (!IsValidItem(itemId)) return int.MaxValue;
            EquipmentDefinition definition = definitions[itemId];
            int rarity = (int)definition.Rarity;
            return Mathf.Max(1, rarity * 3 + levels[itemId] * rarity * 2);
        }

        public bool UpgradeItem(int itemId)
        {
            if (!IsValidItem(itemId)) return Fail("잘못된 장비 선택");
            if (!IsOwned(itemId)) return Fail("먼저 장비를 획득해야 합니다");
            int cost = GetUpgradeCost(itemId);
            if (growth == null || !growth.TrySpendEquipmentMaterials(cost))
                return Fail($"장비 강화 재료 {cost:N0}개 필요");

            levels[itemId]++;
            lastMessage = $"{definitions[itemId].Name} Lv.{levels[itemId]} 강화";
            Save();
            return true;
        }

        public void RegisterSummon(bool boss, int elementIndex, int rarityIndex)
        {
            elementIndex = Mathf.Clamp(elementIndex, 0, ElementCount - 1);
            rarityIndex = Mathf.Clamp(rarityIndex, 1, RarityCount);
            int selected = GetItemId(boss, elementIndex, rarityIndex, 0);
            int minimum = int.MaxValue;
            foreach (int id in GetItemIds(boss, elementIndex, rarityIndex))
            {
                if (copies[id] >= minimum) continue;
                minimum = copies[id];
                selected = id;
            }

            copies[selected]++;
            if (copies[selected] > 1 && copies[selected] % 3 == 0) levels[selected]++;
            EquipmentDefinition definition = definitions[selected];
            lastMessage = $"{UseName(boss)} {definition.Name} 획득 · 보유 {copies[selected]}";
            RepairEquippedItems();
            Save();
        }

        public void RegisterSummon(int elementIndex, int rarityIndex)
        {
            RegisterSummon(false, elementIndex, rarityIndex);
        }

        public bool EquipItem(int itemId, bool boss)
        {
            if (!IsValidItem(itemId)) return Fail("잘못된 장비 선택");
            if (!IsOwned(itemId)) return Fail("보유하지 않은 장비입니다");

            EquipmentDefinition definition = definitions[itemId];
            EquipmentUse expectedUse = boss ? EquipmentUse.Boss : EquipmentUse.Hunting;
            if (definition.Use != expectedUse)
                return Fail($"{UseName(boss)} 장비만 해당 슬롯에 장착할 수 있습니다");

            FieldInfo elementField = boss ? bossElementField : huntingElementField;
            if (elementField != null)
                elementField.SetValue(arena, Enum.ToObject(elementField.FieldType, (int)definition.Element));

            if (boss) bossItem = itemId;
            else huntingItem = itemId;
            lastMessage = $"{definition.Name} · {UseName(boss)} 장착";
            Save();
            return true;
        }

        public int GetEquippedItem(bool boss)
        {
            return boss ? bossItem : huntingItem;
        }

        public bool IsEquipped(int itemId, bool boss)
        {
            return GetEquippedItem(boss) == itemId;
        }

        public float GetItemDamageMultiplier(int itemId)
        {
            if (!IsOwned(itemId) || !IsValidItem(itemId)) return 1f;
            EquipmentDefinition definition = definitions[itemId];
            float rarityBonus = (int)definition.Rarity * 0.10f;
            float levelBonus = Mathf.Max(0, levels[itemId] - 1) * 0.018f;
            float duplicateBonus = Mathf.Min(0.12f, Mathf.Max(0, copies[itemId] - 1) * 0.015f);
            return 1f + rarityBonus + levelBonus + duplicateBonus;
        }

        public float GetActiveDamageMultiplier(bool boss)
        {
            return GetItemDamageMultiplier(GetEquippedItem(boss));
        }

        public string UseName(bool boss)
        {
            return boss ? "보스용" : "사냥용";
        }

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

        private void BuildDefinitions()
        {
            for (int use = 0; use < UseCount; use++)
            {
                for (int element = 0; element < ElementCount; element++)
                {
                    for (int rarity = 1; rarity <= RarityCount; rarity++)
                    {
                        for (int variant = 0; variant < VariantsPerRarity; variant++)
                        {
                            EquipmentUse equipmentUse = (EquipmentUse)use;
                            int id = GetItemId(equipmentUse, element, rarity, variant);
                            definitions[id] = new EquipmentDefinition
                            {
                                Id = id,
                                Name = use == 0
                                    ? HuntingNames[element, rarity - 1, variant]
                                    : BossNames[element, rarity - 1, variant],
                                Use = equipmentUse,
                                Element = (EquipmentElement)element,
                                Rarity = (EquipmentRarity)rarity,
                                Variant = variant
                            };
                        }
                    }
                }
            }
        }

        private void MigrateSharedCatalogToSeparatedCatalogs()
        {
            if (schemaVersion >= CurrentSchemaVersion) return;

            for (int localId = 0; localId < ItemsPerUse; localId++)
            {
                int bossId = ItemsPerUse + localId;
                levels[bossId] = Mathf.Max(levels[bossId], levels[localId]);
                copies[bossId] = Mathf.Max(copies[bossId], copies[localId]);
            }

            if (bossItem >= 0 && bossItem < ItemsPerUse) bossItem += ItemsPerUse;
            schemaVersion = CurrentSchemaVersion;
            lastMessage = "기존 공유 장비를 사냥용·보스용 장비군으로 분리 완료";
        }

        private void MigrateLegacyCollection()
        {
            if (legacyMigrated || legacySwords == null) return;
            for (int element = 0; element < ElementCount; element++)
            {
                for (int rarity = 1; rarity <= RarityCount; rarity++)
                {
                    int legacyCopies = legacySwords.GetCopies(
                        element, (SwordProgressionPrototype.SwordRarity)rarity);
                    for (int copy = 0; copy < legacyCopies; copy++)
                    {
                        int variant = copy % VariantsPerRarity;
                        int huntingId = GetItemId(false, element, rarity, variant);
                        int bossId = GetItemId(true, element, rarity, variant);
                        copies[huntingId]++;
                        copies[bossId]++;
                    }
                }
            }
            legacyMigrated = true;
        }

        private void EnsureStarterEquipment()
        {
            if (FindFirstOwned(false, 0) < 0)
                copies[GetItemId(false, 0, 1, 0)] = 1;
            if (FindFirstOwned(true, 0) < 0)
                copies[GetItemId(true, 0, 1, 0)] = 1;
        }

        private void RepairEquippedItems()
        {
            if (!IsValidForUse(huntingItem, false) || !IsOwned(huntingItem))
                huntingItem = FindFirstOwned(false, ReadElement(huntingElementField));
            if (!IsValidForUse(bossItem, true) || !IsOwned(bossItem))
                bossItem = FindFirstOwned(true, ReadElement(bossElementField));

            ApplyEquippedElement(huntingItem, false);
            ApplyEquippedElement(bossItem, true);
        }

        private int FindFirstOwned(bool boss, int preferredElement)
        {
            preferredElement = Mathf.Clamp(preferredElement, 0, ElementCount - 1);
            for (int rarity = RarityCount; rarity >= 1; rarity--)
                foreach (int id in GetItemIds(boss, preferredElement, rarity))
                    if (IsOwned(id)) return id;

            int start = boss ? ItemsPerUse : 0;
            int end = start + ItemsPerUse;
            for (int id = start; id < end; id++)
                if (IsOwned(id)) return id;
            return -1;
        }

        private void ApplyEquippedElement(int itemId, bool boss)
        {
            if (!IsValidForUse(itemId, boss)) return;
            EquipmentDefinition definition = definitions[itemId];
            FieldInfo field = boss ? bossElementField : huntingElementField;
            if (field != null)
                field.SetValue(arena, Enum.ToObject(field.FieldType, (int)definition.Element));
        }

        private int ReadElement(FieldInfo field)
        {
            return field == null || arena == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private bool IsValidForUse(int itemId, bool boss)
        {
            if (!IsValidItem(itemId)) return false;
            return definitions[itemId].Use == (boss ? EquipmentUse.Boss : EquipmentUse.Hunting);
        }

        private bool Fail(string value)
        {
            lastMessage = value;
            return false;
        }

        private bool IsValidItem(int itemId)
        {
            return itemId >= 0 && itemId < ItemCount;
        }

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

        private void Load()
        {
            schemaVersion = PlayerPrefs.GetInt(Prefix + "Schema", 1);
            legacyMigrated = PlayerPrefs.GetInt(Prefix + "Migrated", 0) == 1;
            huntingItem = PlayerPrefs.GetInt(Prefix + "HuntingItem", -1);
            bossItem = PlayerPrefs.GetInt(Prefix + "BossItem", -1);
            for (int i = 0; i < ItemCount; i++)
            {
                levels[i] = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Level." + i, 1));
                copies[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Copies." + i, 0));
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
