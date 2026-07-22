using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Own progression that is specific to Peanut Warrior: shell training,
    /// elemental sword research, and offline-efficiency growth.
    /// </summary>
    public sealed class MetaProgressionPrototype : MonoBehaviour
    {
        private const string Prefix = "PeanutWarrior.MetaProgression.";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly int[] elementResearchLevels = { 1, 1, 1, 1 };

        private CombatPrototypeArena arena;
        private GrowthExpansionPrototype growth;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo hpLevelField;
        private FieldInfo hpRegenLevelField;

        private int shellVitalityLevel = 1;
        private int shellRecoveryLevel = 1;
        private int idleGoldLevel = 1;
        private int idleFragmentLevel = 1;
        private int idleHourLevel = 1;
        private float saveTimer;
        private string lastMessage = "전용 성장 준비";

        public int ShellVitalityLevel => shellVitalityLevel;
        public int ShellRecoveryLevel => shellRecoveryLevel;
        public int IdleGoldLevel => idleGoldLevel;
        public int IdleFragmentLevel => idleFragmentLevel;
        public int IdleHourLevel => idleHourLevel;
        public int MaximumOfflineHours => Mathf.Clamp(8 + (idleHourLevel - 1) * 2, 8, 24);
        public float OfflineGoldMultiplier => 1f + (idleGoldLevel - 1) * 0.12f;
        public float OfflineFragmentMultiplier => 1f + (idleFragmentLevel - 1) * 0.10f;
        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MetaProgressionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMetaProgressionPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<MetaProgressionPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            hpLevelField = arenaType.GetField("hpLevel", PrivateInstance);
            if (growth != null)
                hpRegenLevelField = typeof(GrowthExpansionPrototype).GetField("hpRegenLevel", PrivateInstance);

            Load();
            GrantAdditionalOfflineRewards();
        }

        private void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer < 10f) return;
            saveTimer = 0f;
            Save();
        }

        public int GetElementResearchLevel(int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= elementResearchLevels.Length) return 1;
            return elementResearchLevels[elementIndex];
        }

        public float GetElementDamageMultiplier(int elementIndex)
        {
            int level = GetElementResearchLevel(elementIndex);
            return 1f + (level - 1) * 0.025f;
        }

        public long ShellVitalityCost => 120L * shellVitalityLevel;
        public long ShellRecoveryCost => 140L * shellRecoveryLevel;
        public long ElementResearchCost(int elementIndex) => 4L + GetElementResearchLevel(elementIndex) * 3L;
        public long IdleGoldCost => 180L * idleGoldLevel;
        public long IdleFragmentCost => 210L * idleFragmentLevel;
        public long IdleHourCost => 260L * idleHourLevel;

        public bool UpgradeShellVitality()
        {
            long cost = ShellVitalityCost;
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");

            shellVitalityLevel++;
            IncrementIntField(hpLevelField, arena, 1);
            RestorePlayerVitals();
            lastMessage = $"껍질 생명 단련 Lv.{shellVitalityLevel}";
            Save();
            return true;
        }

        public bool UpgradeShellRecovery()
        {
            long cost = ShellRecoveryCost;
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");

            shellRecoveryLevel++;
            IncrementIntField(hpRegenLevelField, growth, 1);
            lastMessage = $"껍질 재생 단련 Lv.{shellRecoveryLevel}";
            Save();
            return true;
        }

        public bool UpgradeElementResearch(int elementIndex)
        {
            if (elementIndex < 0 || elementIndex >= elementResearchLevels.Length)
                return Fail("잘못된 속성 연구 대상");

            long cost = ElementResearchCost(elementIndex);
            if (!SpendFragments(cost)) return Fail($"조각 부족 · {cost:N0}개 필요");

            elementResearchLevels[elementIndex]++;
            lastMessage = $"{ElementName(elementIndex)} 연구 Lv.{elementResearchLevels[elementIndex]}";
            Save();
            return true;
        }

        public bool UpgradeIdleGold()
        {
            long cost = IdleGoldCost;
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");
            idleGoldLevel++;
            lastMessage = $"방치 골드 효율 Lv.{idleGoldLevel}";
            Save();
            return true;
        }

        public bool UpgradeIdleFragments()
        {
            long cost = IdleFragmentCost;
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");
            idleFragmentLevel++;
            lastMessage = $"방치 조각 효율 Lv.{idleFragmentLevel}";
            Save();
            return true;
        }

        public bool UpgradeIdleHours()
        {
            if (MaximumOfflineHours >= 24) return Fail("최대 방치 시간 24시간 달성");
            long cost = IdleHourCost;
            if (!SpendGold(cost)) return Fail($"골드 부족 · {cost:N0}G 필요");
            idleHourLevel++;
            lastMessage = $"최대 방치 시간 {MaximumOfflineHours}시간";
            Save();
            return true;
        }

        private bool Fail(string message)
        {
            lastMessage = message;
            return false;
        }

        private long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null || arena == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));

        private bool SpendGold(long amount)
        {
            if (goldField == null || arena == null || Gold < amount) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private bool SpendFragments(long amount)
        {
            if (fragmentsField == null || arena == null || Fragments < amount) return false;
            fragmentsField.SetValue(arena, Fragments - amount);
            return true;
        }

        private void AddGold(long amount)
        {
            if (goldField != null && arena != null && amount > 0) goldField.SetValue(arena, Gold + amount);
        }

        private void AddFragments(long amount)
        {
            if (fragmentsField != null && arena != null && amount > 0) fragmentsField.SetValue(arena, Fragments + amount);
        }

        private static void IncrementIntField(FieldInfo field, object target, int amount)
        {
            if (field == null || target == null) return;
            int current = Convert.ToInt32(field.GetValue(target));
            field.SetValue(target, Mathf.Max(1, current + amount));
        }

        private void RestorePlayerVitals()
        {
            MethodInfo restore = typeof(CombatPrototypeArena).GetMethod("FullRestore", PrivateInstance);
            restore?.Invoke(arena, null);
        }

        private void GrantAdditionalOfflineRewards()
        {
            string key = Prefix + "LastUtcTicks";
            if (!long.TryParse(PlayerPrefs.GetString(key, "0"), out long previousTicks) || previousTicks <= 0)
            {
                PlayerPrefs.SetString(key, DateTime.UtcNow.Ticks.ToString());
                return;
            }

            double elapsedSeconds = new TimeSpan(Math.Max(0L, DateTime.UtcNow.Ticks - previousTicks)).TotalSeconds;
            elapsedSeconds = Math.Min(elapsedSeconds, MaximumOfflineHours * 60d * 60d);
            if (elapsedSeconds < 30d) return;

            int simulatedKills = Mathf.FloorToInt((float)elapsedSeconds / 6f);
            long baseGold = simulatedKills * Math.Max(2, stageFlow.Stage + 2);
            long baseFragments = simulatedKills / 20;
            long bonusGold = Math.Max(0L, (long)Math.Floor(baseGold * (OfflineGoldMultiplier - 1f)));
            long bonusFragments = Math.Max(0L, (long)Math.Floor(baseFragments * (OfflineFragmentMultiplier - 1f)));

            AddGold(bonusGold);
            AddFragments(bonusFragments);
            lastMessage = $"방치 연구 추가 보상 · {bonusGold:N0}G, 조각 {bonusFragments:N0}";
        }

        private void Save()
        {
            PlayerPrefs.SetInt(Prefix + "ShellVitality", shellVitalityLevel);
            PlayerPrefs.SetInt(Prefix + "ShellRecovery", shellRecoveryLevel);
            PlayerPrefs.SetInt(Prefix + "IdleGold", idleGoldLevel);
            PlayerPrefs.SetInt(Prefix + "IdleFragments", idleFragmentLevel);
            PlayerPrefs.SetInt(Prefix + "IdleHours", idleHourLevel);
            for (int i = 0; i < elementResearchLevels.Length; i++)
                PlayerPrefs.SetInt(Prefix + "Element" + i, elementResearchLevels[i]);
            PlayerPrefs.SetString(Prefix + "LastUtcTicks", DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private void Load()
        {
            shellVitalityLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "ShellVitality", 1));
            shellRecoveryLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "ShellRecovery", 1));
            idleGoldLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "IdleGold", 1));
            idleFragmentLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "IdleFragments", 1));
            idleHourLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "IdleHours", 1));
            for (int i = 0; i < elementResearchLevels.Length; i++)
                elementResearchLevels[i] = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "Element" + i, 1));
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private static string ElementName(int index)
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
    }
}
