using System;
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
        private bool panelOpen;

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

        private long Gold => goldField == null ? 0L : (long)goldField.GetValue(arena);
        private long Fragments => fragmentsField == null ? 0L : (long)fragmentsField.GetValue(arena);
        private int Diamonds => diamondsField == null ? 0 : (int)diamondsField.GetValue(arena);

        private void AddGold(long amount) { if (goldField != null) goldField.SetValue(arena, Gold + amount); }
        private void AddFragments(long amount) { if (fragmentsField != null) fragmentsField.SetValue(arena, Fragments + amount); }
        private void AddDiamonds(int amount) { if (diamondsField != null) diamondsField.SetValue(arena, Diamonds + amount); }

        private bool SpendDiamonds(int amount)
        {
            if (Diamonds < amount) return false;
            diamondsField.SetValue(arena, Diamonds - amount);
            return true;
        }

        private void ClaimDailyReward()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (lastClaimDate == today)
            {
                shopMessage = "오늘의 접속 보상은 이미 수령함";
                return;
            }

            DateTime previous;
            bool consecutive = DateTime.TryParse(lastClaimDate, out previous) &&
                               (DateTime.UtcNow.Date - previous.Date).TotalDays <= 1.1d;
            dailyStreak = consecutive ? dailyStreak + 1 : 1;
            dailyStreak = Mathf.Clamp(dailyStreak, 1, 7);
            lastClaimDate = today;

            long goldReward = 100L * dailyStreak;
            int diamondReward = dailyStreak == 7 ? 10 : 2;
            long fragmentReward = dailyStreak * 2L;
            AddGold(goldReward);
            AddDiamonds(diamondReward);
            AddFragments(fragmentReward);
            shopMessage = $"접속 {dailyStreak}일차 · {goldReward}G, 다이아 {diamondReward}, 조각 {fragmentReward}";
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

            FieldInfo targetField = equipForBoss ? bossElementField : huntingElementField;
            if (targetField != null) targetField.SetValue(arena, Enum.ToObject(targetField.FieldType, elementIndex));

            if (totalSwordSummons % 5 == 0 && basicAttackLevelField != null)
            {
                int level = (int)basicAttackLevelField.GetValue(arena);
                basicAttackLevelField.SetValue(arena, level + 1);
                shopMessage = $"{ElementName(elementIndex)} 검 {RarityName(rarity)} 획득 · 기본 공격 강화";
            }
            else
            {
                shopMessage = $"{ElementName(elementIndex)} 검 {RarityName(rarity)} 획득 · {(equipForBoss ? "보스" : "사냥")} 장착";
            }
            Save();
        }

        private void BuyEgg()
        {
            if (idleSystems == null || eggsField == null)
            {
                shopMessage = "미니 시스템 초기화 대기";
                return;
            }
            if (!SpendDiamonds(3))
            {
                shopMessage = "알 구매에 다이아 3개 필요";
                return;
            }
            int eggs = (int)eggsField.GetValue(idleSystems);
            eggsField.SetValue(idleSystems, eggs + 1);
            shopMessage = "미니 알 구매 완료 · 미니 패널에서 부화";
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(Prefix + "Streak", dailyStreak);
            PlayerPrefs.SetInt(Prefix + "SwordSummons", totalSwordSummons);
            PlayerPrefs.SetString(Prefix + "LastClaim", lastClaimDate);
            for (int i = 0; i < swordCopies.Length; i++) PlayerPrefs.SetInt(Prefix + "SwordCopies" + i, swordCopies[i]);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            dailyStreak = PlayerPrefs.GetInt(Prefix + "Streak", 0);
            totalSwordSummons = PlayerPrefs.GetInt(Prefix + "SwordSummons", 0);
            lastClaimDate = PlayerPrefs.GetString(Prefix + "LastClaim", string.Empty);
            for (int i = 0; i < swordCopies.Length; i++) swordCopies[i] = PlayerPrefs.GetInt(Prefix + "SwordCopies" + i, 0);
        }

        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() => Save();

        private void OnGUI()
        {
            float buttonWidth = 118f;
            Rect toggle = new Rect(Screen.width - buttonWidth - 15f, 15f, buttonWidth, 36f);
            if (GUI.Button(toggle, panelOpen ? "상점 닫기" : "상점·보상")) panelOpen = !panelOpen;
            if (!panelOpen) return;

            float width = Mathf.Min(300f, Screen.width - 30f);
            float x = Mathf.Clamp(Screen.width - width - 15f, 15f, Screen.width - width - 15f);
            Rect panel = new Rect(x, 58f, width, 242f);
            GUI.Box(panel, "접속 보상·소환 상점");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 28f, panel.width - 24f, 42f), shopMessage);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 70f, panel.width - 24f, 22f),
                $"연속 접속 {dailyStreak}일 · 검 소환 {totalSwordSummons}회 · 다이아 {Diamonds}");

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 96f, panel.width - 24f, 34f), "오늘의 접속 보상 받기")) ClaimDailyReward();
            float half = (panel.width - 30f) * 0.5f;
            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 136f, half, 38f), "사냥 검 소환\n5◆")) SummonSword(false);
            if (GUI.Button(new Rect(panel.x + 18f + half, panel.y + 136f, half, 38f), "보스 검 소환\n5◆")) SummonSword(true);
            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 180f, panel.width - 24f, 34f), "미니 알 구매 · 3◆")) BuyEgg();
            GUI.Label(new Rect(panel.x + 12f, panel.y + 218f, panel.width - 24f, 18f),
                $"검 도감: 무{swordCopies[0]} 화{swordCopies[1]} 빙{swordCopies[2]} 뇌{swordCopies[3]}");
        }

        private static string ElementName(int index)
        {
            return index switch { 0 => "무속성", 1 => "화염", 2 => "냉기", 3 => "번개", _ => "검" };
        }

        private static string RarityName(int rarity)
        {
            return rarity switch { 4 => "신화", 3 => "전설", 2 => "희귀", _ => "일반" };
        }
    }
}
