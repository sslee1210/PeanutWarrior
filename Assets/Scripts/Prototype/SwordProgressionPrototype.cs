using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Stores the four elemental sword collections. Grade order is fixed to
    /// Rare → Epic → Unique → Legend. The detailed equipment design is intentionally
    /// kept minimal until the final weapon structure is confirmed.
    /// </summary>
    public sealed class SwordProgressionPrototype : MonoBehaviour
    {
        public enum SwordRarity
        {
            None = 0,
            Rare = 1,
            Epic = 2,
            Unique = 3,
            Legend = 4
        }

        private const string Prefix = "PeanutWarrior.SwordProgression.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const int ElementCount = 4;
        private const int RarityCount = 5;

        private readonly int[] swordLevels = { 1, 1, 1, 1 };
        private readonly int[] highestRarities = new int[ElementCount];
        private readonly int[,] rarityCopies = new int[ElementCount, RarityCount];
        private readonly int[] lifetimeCopies = new int[ElementCount];

        private CombatPrototypeArena arena;
        private FieldInfo goldField;
        private string lastMessage = "장비 보관함 준비";

        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SwordProgressionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorSwordProgressionPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<SwordProgressionPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (arena == null)
            {
                enabled = false;
                return;
            }

            goldField = typeof(CombatPrototypeArena).GetField("gold", PrivateInstance);
            Load();
        }

        public int GetLevel(int elementIndex)
        {
            return IsValidElement(elementIndex) ? swordLevels[elementIndex] : 1;
        }

        public SwordRarity GetHighestRarity(int elementIndex)
        {
            return IsValidElement(elementIndex)
                ? (SwordRarity)Mathf.Clamp(highestRarities[elementIndex], 0, RarityCount - 1)
                : SwordRarity.None;
        }

        public int GetCopies(int elementIndex, SwordRarity rarity)
        {
            int rarityIndex = (int)rarity;
            if (!IsValidElement(elementIndex) || rarityIndex < 0 || rarityIndex >= RarityCount) return 0;
            return rarityCopies[elementIndex, rarityIndex];
        }

        public int GetLifetimeCopies(int elementIndex)
        {
            return IsValidElement(elementIndex) ? lifetimeCopies[elementIndex] : 0;
        }

        public long GetUpgradeCost(int elementIndex)
        {
            if (!IsValidElement(elementIndex)) return long.MaxValue;
            int rarity = Mathf.Max(1, highestRarities[elementIndex]);
            return 120L * swordLevels[elementIndex] * rarity;
        }

        public float GetDamageMultiplier(int elementIndex)
        {
            if (!IsValidElement(elementIndex)) return 1f;
            int rarity = highestRarities[elementIndex];
            if (rarity <= 0) return 1f;

            float rarityBonus = rarity * 0.12f;
            float levelBonus = (swordLevels[elementIndex] - 1) * 0.025f;
            float collectionBonus = Mathf.Min(0.15f, lifetimeCopies[elementIndex] * 0.005f);
            return 1f + rarityBonus + levelBonus + collectionBonus;
        }

        public void RegisterSummon(int elementIndex, int rarityIndex)
        {
            if (!IsValidElement(elementIndex)) return;
            rarityIndex = Mathf.Clamp(rarityIndex, 1, RarityCount - 1);

            rarityCopies[elementIndex, rarityIndex]++;
            lifetimeCopies[elementIndex]++;
            highestRarities[elementIndex] = Mathf.Max(highestRarities[elementIndex], rarityIndex);
            AutoSynthesize(elementIndex);
            lastMessage = $"{ElementName(elementIndex)} 검 {RarityName((SwordRarity)rarityIndex)} 획득";
            Save();
        }

        public bool UpgradeSword(int elementIndex)
        {
            if (!IsValidElement(elementIndex)) return Fail("잘못된 검 선택");
            if (highestRarities[elementIndex] <= 0) return Fail("먼저 해당 속성 검을 획득해야 합니다");

            long cost = GetUpgradeCost(elementIndex);
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");

            swordLevels[elementIndex]++;
            lastMessage = $"{ElementName(elementIndex)} 검 Lv.{swordLevels[elementIndex]} 강화";
            Save();
            return true;
        }

        public bool ManualSynthesize(int elementIndex, SwordRarity rarity)
        {
            int rarityIndex = (int)rarity;
            if (!IsValidElement(elementIndex) || rarityIndex < 1 || rarityIndex >= RarityCount - 1)
                return Fail("합성할 수 없는 검 등급");
            if (rarityCopies[elementIndex, rarityIndex] < 3)
                return Fail("동일 등급 검 3개가 필요합니다");

            rarityCopies[elementIndex, rarityIndex] -= 3;
            rarityCopies[elementIndex, rarityIndex + 1]++;
            highestRarities[elementIndex] = Mathf.Max(highestRarities[elementIndex], rarityIndex + 1);
            AutoSynthesize(elementIndex);
            lastMessage = $"{ElementName(elementIndex)} 검 {RarityName((SwordRarity)(rarityIndex + 1))} 합성 완료";
            Save();
            return true;
        }

        private void AutoSynthesize(int elementIndex)
        {
            for (int rarity = 1; rarity < RarityCount - 1; rarity++)
            {
                while (rarityCopies[elementIndex, rarity] >= 3)
                {
                    rarityCopies[elementIndex, rarity] -= 3;
                    rarityCopies[elementIndex, rarity + 1]++;
                    highestRarities[elementIndex] = Mathf.Max(highestRarities[elementIndex], rarity + 1);
                    lastMessage = $"자동 합성 · {ElementName(elementIndex)} {RarityName((SwordRarity)(rarity + 1))}";
                }
            }
        }

        private bool Fail(string value)
        {
            lastMessage = value;
            return false;
        }

        private long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));

        private bool SpendGold(long amount)
        {
            if (goldField == null || arena == null || Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private void Save()
        {
            for (int element = 0; element < ElementCount; element++)
            {
                PlayerPrefs.SetInt(Prefix + "Level" + element, swordLevels[element]);
                PlayerPrefs.SetInt(Prefix + "Highest" + element, highestRarities[element]);
                PlayerPrefs.SetInt(Prefix + "Lifetime" + element, lifetimeCopies[element]);
                for (int rarity = 1; rarity < RarityCount; rarity++)
                    PlayerPrefs.SetInt(Prefix + $"Copies{element}_{rarity}", rarityCopies[element, rarity]);
            }
            PlayerPrefs.Save();
        }

        private void Load()
        {
            // Enum values are intentionally kept at 1..4, so old prototype saves
            // migrate without data loss; only their display names change.
            for (int element = 0; element < ElementCount; element++)
            {
                swordLevels[element] = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Level" + element, 1));
                highestRarities[element] = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Highest" + element, 0), 0, RarityCount - 1);
                lifetimeCopies[element] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Lifetime" + element, 0));
                for (int rarity = 1; rarity < RarityCount; rarity++)
                    rarityCopies[element, rarity] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + $"Copies{element}_{rarity}", 0));
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

        private static bool IsValidElement(int elementIndex)
        {
            return elementIndex >= 0 && elementIndex < ElementCount;
        }

        public static string ElementName(int index)
        {
            return index switch
            {
                0 => "무속성",
                1 => "화염",
                2 => "냉기",
                3 => "번개",
                _ => "속성"
            };
        }

        public static string RarityName(SwordRarity rarity)
        {
            return rarity switch
            {
                SwordRarity.Legend => "레전드",
                SwordRarity.Unique => "유니크",
                SwordRarity.Epic => "에픽",
                SwordRarity.Rare => "레어",
                _ => "미보유"
            };
        }
    }
}
