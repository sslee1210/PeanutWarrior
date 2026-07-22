using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class PrototypeShopAndDaily : MonoBehaviour
    {
        private const string Prefix = "PeanutWarrior.Shop.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private IdleSystemsPrototype idleSystems;
        private SwordProgressionPrototype swordProgression;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private FieldInfo basicAttackLevelField;
        private FieldInfo eggsField;

        private readonly int[] swordCopies = new int[4];
        private int dailyStreak;
        private int totalSwordSummons;
        private string lastClaimDate = string.Empty;
        private string shopMessage = "일일 보상·소환 준비";

        public string ShopMessage => shopMessage;
        public int DailyStreak => dailyStreak;
        public int TotalSwordSummons => totalSwordSummons;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateShop()
        {
            if (FindFirstObjectByType<PrototypeShopAndDaily>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPrototypeShop");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeShopAndDaily>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            idleSystems = FindFirstObjectByType<IdleSystemsPrototype>();
            swordProgression = FindFirstObjectByType<SwordProgressionPrototype>();
            if (arena == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            diamondsField = arenaType.GetField("diamonds", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            basicAttackLevelField = arenaType.GetField("basicAttackLevel", PrivateInstance);
            if (idleSystems != null) eggsField = typeof(IdleSystemsPrototype).GetField("eggs", PrivateInstance);
            Load();
        }

        private long Gold => goldField == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));
        private int Diamonds => diamondsField == null ? 0 : Convert.ToInt32(diamondsField.GetValue(arena));

        private void AddGold(long amount) { if (goldField != null) goldField.SetValue(arena, Gold + amount); }
        private void AddFragments(long amount) { if (fragmentsField != null) fragmentsField.SetValue(arena, Fragments + amount); }
        private void AddDiamonds(int amount) { if (diamondsField != null) diamondsField.SetValue(arena, Diamonds + amount); }

        private bool SpendDiamonds(int amount)
        {
            if (diamondsField == null || Diamonds < amount) return false;
            diamondsField.SetValue(arena, Diamonds - amount);
            return true;
        }

        private void ClaimDailyReward()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (lastClaimDate == today)
            {
                shopMessage = "오늘의 접속 보상은 이미 수령함";
                return;
            }

            bool consecutive = DateTime.TryParseExact(
                    lastClaimDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out DateTime previous) &&
                (DateTime.UtcNow.Date - previous.Date).TotalDays <= 1.1d;

            dailyStreak = consecutive ? dailyStreak + 1 : 1;
            if (dailyStreak > 7) dailyStreak = 1;
            lastClaimDate = today;

            long goldReward = 100L * dailyStreak;
            int diamondReward = dailyStreak == 7 ? 10 : 2;
            long fragmentReward = dailyStreak * 2L;
            AddGold(goldReward);
            AddDiamonds(diamondReward);
            AddFragments(fragmentReward);
            shopMessage = $"접속 {dailyStreak}일차 · {goldReward:N0}G, 다이아 {diamondReward}, 조각 {fragmentReward}";
            Save();
        }

        private void SummonSword(bool equipForBoss)
        {
            if (!SpendDiamonds(5))
            {
                shopMessage = "검 소환에 다이아 5개 필요";
                return;
            }

            int elementIndex = UnityEngine.Random.Range(0, 4);
            int rarityRoll = UnityEngine.Random.Range(0, 1000);
            int rarity = rarityRoll < 20 ? 4 : rarityRoll < 120 ? 3 : rarityRoll < 420 ? 2 : 1;
            swordCopies[elementIndex]++;
            totalSwordSummons++;
            swordProgression?.RegisterSummon(elementIndex, rarity);

            FieldInfo targetField = equipForBoss ? bossElementField : huntingElementField;
            if (targetField != null)
                targetField.SetValue(arena, Enum.ToObject(targetField.FieldType, elementIndex));

            if (totalSwordSummons % 5 == 0 && basicAttackLevelField != null)
            {
                int level = Convert.ToInt32(basicAttackLevelField.GetValue(arena));
                basicAttackLevelField.SetValue(arena, level + 1);
                shopMessage = $"{ElementName(elementIndex)} 검 {RarityName(rarity)} 획득 · 장비 도감으로 기본 공격 강화";
            }
            else
            {
                shopMessage = $"{ElementName(elementIndex)} 검 {RarityName(rarity)} 획득 · {(equipForBoss ? "균왕" : "사냥")} 장착";
            }
            Save();
        }

        private void BuyEgg()
        {
            if (idleSystems == null || eggsField == null)
            {
                shopMessage = "펫 시스템 초기화 대기";
                return;
            }
            if (!SpendDiamonds(3))
            {
                shopMessage = "펫 알 구매에 다이아 3개 필요";
                return;
            }
            int eggs = Convert.ToInt32(eggsField.GetValue(idleSystems));
            eggsField.SetValue(idleSystems, eggs + 1);
            shopMessage = "펫 알 구매 완료 · 펫 화면에서 부화";
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(Prefix + "Streak", dailyStreak);
            PlayerPrefs.SetInt(Prefix + "SwordSummons", totalSwordSummons);
            PlayerPrefs.SetString(Prefix + "LastClaim", lastClaimDate);
            for (int i = 0; i < swordCopies.Length; i++)
                PlayerPrefs.SetInt(Prefix + "SwordCopies" + i, swordCopies[i]);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            dailyStreak = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "Streak", 0));
            totalSwordSummons = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "SwordSummons", 0));
            lastClaimDate = PlayerPrefs.GetString(Prefix + "LastClaim", string.Empty);
            for (int i = 0; i < swordCopies.Length; i++)
                swordCopies[i] = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "SwordCopies" + i, 0));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private static string ElementName(int index)
        {
            return index switch { 0 => "무속성", 1 => "화염", 2 => "냉기", 3 => "번개", _ => "검" };
        }

        private static string RarityName(int rarity)
        {
            return rarity switch { 4 => "레전드", 3 => "유니크", 2 => "에픽", _ => "레어" };
        }
    }
}
