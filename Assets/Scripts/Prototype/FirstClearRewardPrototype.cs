using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Grants a one-time reward for the first boss clear of each stage and records
    /// total boss victories. Repeating an already-cleared stage keeps the normal boss
    /// reward but never duplicates the first-clear reward.
    /// </summary>
    public sealed class FirstClearRewardPrototype : MonoBehaviour
    {
        private const string Prefix = "PeanutWarrior.FirstClear.";
        private const string BossKillsKey = Prefix + "BossKills";
        private const string UniqueClearsKey = Prefix + "UniqueClears";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;

        private int encounterGlobalStage;
        private int bossKills;
        private int uniqueClears;
        private string lastMessage = "최초 클리어 기록 준비";

        public int BossKills => bossKills;
        public int UniqueClears => uniqueClears;
        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<FirstClearRewardPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorFirstClearRewardPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<FirstClearRewardPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            diamondsField = arenaType.GetField("diamonds", PrivateInstance);

            bossKills = Mathf.Max(0, PlayerPrefs.GetInt(BossKillsKey, 0));
            uniqueClears = Mathf.Max(0, PlayerPrefs.GetInt(UniqueClearsKey, 0));
            stageFlow.BossBattleStarted += CaptureEncounterStage;
            stageFlow.BossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (stageFlow == null) return;
            stageFlow.BossBattleStarted -= CaptureEncounterStage;
            stageFlow.BossDefeated -= HandleBossDefeated;
        }

        private void CaptureEncounterStage()
        {
            encounterGlobalStage = CurrentGlobalStage;
        }

        private void HandleBossDefeated()
        {
            int clearedGlobalStage = encounterGlobalStage > 0 ? encounterGlobalStage : CurrentGlobalStage;
            bossKills++;

            string clearKey = Prefix + "Stage." + clearedGlobalStage;
            if (PlayerPrefs.GetInt(clearKey, 0) == 1)
            {
                lastMessage = $"보스 반복 처치 · 누적 {bossKills}회";
                SaveCounters();
                return;
            }

            PlayerPrefs.SetInt(clearKey, 1);
            uniqueClears++;

            int world = (clearedGlobalStage - 1) / StageFlowController.StagesPerWorld + 1;
            int localStage = (clearedGlobalStage - 1) % StageFlowController.StagesPerWorld + 1;
            long goldReward = 75L + clearedGlobalStage * 15L;
            long fragmentReward = 3L + world + localStage / 10;
            int diamondReward = 2 + Mathf.Min(8, world / 2);
            bool milestone = uniqueClears % 5 == 0;
            if (milestone) diamondReward += 5;

            AddGold(goldReward);
            AddFragments(fragmentReward);
            AddDiamonds(diamondReward);
            lastMessage = $"{world}-{localStage} 최초 클리어 · {goldReward:N0}G, 조각 {fragmentReward}, 다이아 {diamondReward}";
            SaveCounters();
            Debug.Log($"[PeanutWarrior] {lastMessage}");
        }

        private void SaveCounters()
        {
            PlayerPrefs.SetInt(BossKillsKey, bossKills);
            PlayerPrefs.SetInt(UniqueClearsKey, uniqueClears);
            PlayerPrefs.Save();
        }

        private int CurrentGlobalStage =>
            (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;

        private long Gold => goldField == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));
        private int Diamonds => diamondsField == null ? 0 : Convert.ToInt32(diamondsField.GetValue(arena));

        private void AddGold(long amount)
        {
            if (goldField != null && amount > 0) goldField.SetValue(arena, Gold + amount);
        }

        private void AddFragments(long amount)
        {
            if (fragmentsField != null && amount > 0) fragmentsField.SetValue(arena, Fragments + amount);
        }

        private void AddDiamonds(int amount)
        {
            if (diamondsField != null && amount > 0) diamondsField.SetValue(arena, Diamonds + amount);
        }
    }
}
