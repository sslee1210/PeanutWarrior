using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(7400)]
    public sealed class ElementEquipmentCatalogPrototype : MonoBehaviour
    {
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
            public EquipmentElement Element;
            public EquipmentRarity Rarity;
            public int Variant;
        }

        private const string Prefix = "PeanutWarrior.ElementEquipment.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int ElementCount = 4;
        private const int RarityCount = 4;
        private const int VariantsPerRarity = 3;
        private const int ItemCount = ElementCount * RarityCount * VariantsPerRarity;

        private static readonly string[,,] Names =
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
        private bool migrated;
        private string lastMessage = "속성 장비 도감 준비";

        public int TotalItemCount => ItemCount;
        public int VariantsPerGrade => VariantsPerRarity;
        public string LastMessage => lastMessage;
        public int HuntingItem => huntingItem;
        public int BossItem => bossItem;

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
            MigrateLegacyCollection();
            RepairEquippedItems();
            Save();
        }

        public EquipmentDefinition GetDefinition(int itemId)
        {
            return IsValidItem(itemId) ? definitions[itemId] : null;
        }

        public int GetItemId(int elementIndex, int rarityIndex, int variant)
        {
            if (elementIndex < 0 || elementIndex >= ElementCount) return -1;
            if (rarityIndex < 1 || rarityIndex > RarityCount) return -1;
            if (variant < 0 || variant >= VariantsPerRarity) return -1;
            return elementIndex * RarityCount * VariantsPerRarity +
                   (rarityIndex - 1) * VariantsPerRarity + variant;
        }

        public IEnumerable<int> GetItemIds(int elementIndex, int rarityIndex)
        {
            for (int variant = 0; variant < VariantsPerRarity; variant++)
            {
                int id = GetItemId(elementIndex, rarityIndex, variant);
                if (id >= 0) yield return id;
            }
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

        public int GetOwnedCount(int elementIndex, int rarityIndex)
        {
            int count = 0;
            foreach (int id in GetItemIds(elementIndex, rarityIndex))
                if (IsOwned(id)) count++;
            return count;
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

        public void RegisterSummon(int elementIndex, int rarityIndex)
        {
            elementIndex = Mathf.Clamp(elementIndex, 0, ElementCount - 1);
            rarityIndex = Mathf.Clamp(rarityIndex, 1, RarityCount);
            int selected = GetItemId(elementIndex, rarityIndex, 0);
            int minimum = int.MaxValue;
            foreach (int id in GetItemIds(elementIndex, rarityIndex))
            {
                if (copies[id] >= minimum) continue;
                minimum = copies[id];
                selected = id;
            }

            copies[selected]++;
            if (copies[selected] > 1 && copies[selected] % 3 == 0) levels[selected]++;
            lastMessage = $"{definitions[selected].Name} 획득 · 보유 {copies[selected]}";
            RepairEquippedItems();
            Save();
        }

        public bool EquipItem(int itemId, bool boss)
        {
            if (!IsValidItem(itemId)) return Fail("잘못된 장비 선택");
            if (!IsOwned(itemId)) return Fail("보유하지 않은 장비입니다");

            EquipmentDefinition definition = definitions[itemId];
            FieldInfo elementField = boss ? bossElementField : huntingElementField;
            if (elementField != null)
                elementField.SetValue(arena, Enum.ToObject(elementField.FieldType, (int)definition.Element));

            if (boss) bossItem = itemId;
            else huntingItem = itemId;
            lastMessage = $"{definition.Name} · {(boss ? "보스" : "사냥")} 장착";
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
            for (int element = 0; element < ElementCount; element++)
            {
                for (int rarity = 1; rarity <= RarityCount; rarity++)
                {
                    for (int variant = 0; variant < VariantsPerRarity; variant++)
                    {
                        int id = GetItemId(element, rarity, variant);
                        definitions[id] = new EquipmentDefinition
                        {
                            Id = id,
                            Name = Names[element, rarity - 1, variant],
                            Element = (EquipmentElement)element,
                            Rarity = (EquipmentRarity)rarity,
                            Variant = variant
                        };
                    }
                }
            }
        }

        private void MigrateLegacyCollection()
        {
            if (migrated || legacySwords == null) return;
            for (int element = 0; element < ElementCount; element++)
            {
                for (int rarity = 1; rarity <= RarityCount; rarity++)
                {
                    int legacyCopies = legacySwords.GetCopies(
                        element, (SwordProgressionPrototype.SwordRarity)rarity);
                    for (int copy = 0; copy < legacyCopies; copy++)
                    {
                        int id = GetItemId(element, rarity, copy % VariantsPerRarity);
                        copies[id]++;
                    }
                }
            }
            migrated = true;
        }

        private void RepairEquippedItems()
        {
            if (!IsOwned(huntingItem)) huntingItem = FindFirstOwned(ReadElement(huntingElementField));
            if (!IsOwned(bossItem)) bossItem = FindFirstOwned(ReadElement(bossElementField));
        }

        private int FindFirstOwned(int preferredElement)
        {
            preferredElement = Mathf.Clamp(preferredElement, 0, ElementCount - 1);
            for (int rarity = RarityCount; rarity >= 1; rarity--)
                foreach (int id in GetItemIds(preferredElement, rarity))
                    if (IsOwned(id)) return id;
            for (int id = 0; id < ItemCount; id++)
                if (IsOwned(id)) return id;
            return -1;
        }

        private int ReadElement(FieldInfo field)
        {
            return field == null || arena == null ? 0 : Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
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
            PlayerPrefs.SetInt(Prefix + "Migrated", migrated ? 1 : 0);
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
            migrated = PlayerPrefs.GetInt(Prefix + "Migrated", 0) == 1;
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
