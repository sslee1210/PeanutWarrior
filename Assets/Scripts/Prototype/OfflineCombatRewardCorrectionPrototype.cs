using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Adds only the difference between the legacy local-stage offline reward and a
    /// combat-power/highest-stage based reward. This is a core idle-game calculation,
    /// not a separate growth or research menu.
    /// </summary>
    [DefaultExecutionOrder(5000)]
    public sealed class OfflineCombatRewardCorrectionPrototype : MonoBehaviour
    {
        private const string LastTicksKey = "PeanutWarrior.Prototype.LastUtcTicks";
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const string LastGrantTicksKey = "PeanutWarrior.OfflineCorrection.LastGrantSourceTicks";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private IdleSystemsPrototype idle;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo idleMessageField;
        private PropertyInfo combatPowerProperty;
        private string lastMessage = "오프라인 전투 보정 대기";

        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<OfflineCombatRewardCorrectionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorOfflineCombatRewardCorrection");
            DontDestroyOnLoad(root);
            root.AddComponent<OfflineCombatRewardCorrectionPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            idle = FindFirstObjectByType<IdleSystemsPrototype>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            combatPowerProperty = arenaType.GetProperty("CombatPower", PrivateInstance);
            if (idle != null)
                idleMessageField = typeof(IdleSystemsPrototype).GetField("systemMessage", PrivateInstance);

            ApplyCorrection();
        }

        private void ApplyCorrection()
        {
            string rawTicks = PlayerPrefs.GetString(LastTicksKey, "0");
            if (!long.TryParse(rawTicks, out long previousTicks) || previousTicks <= 0) return;
            if (PlayerPrefs.GetString(LastGrantTicksKey, string.Empty) == rawTicks) return;

            double seconds = new TimeSpan(DateTime.UtcNow.Ticks - previousTicks).TotalSeconds;
            seconds = Math.Max(0d, Math.Min(seconds, 8d * 60d * 60d));
            if (seconds < 30d) return;

            int simulatedKills = Mathf.FloorToInt((float)seconds / 6f);
            int currentGlobalStage = (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
            int highestGlobalStage = Mathf.Max(currentGlobalStage,
                PlayerPrefs.GetInt(HighestStageKey, currentGlobalStage));
            int combatPower = combatPowerProperty == null
                ? 100
                : Mathf.Max(1, Convert.ToInt32(combatPowerProperty.GetValue(arena)));

            float recommendedPower = 140f + highestGlobalStage * 28f;
            float efficiency = Mathf.Clamp(combatPower / recommendedPower, 0.55f, 1.60f);

            long legacyGold = (long)simulatedKills * Math.Max(2, stageFlow.Stage + 2);
            long desiredGold = (long)Math.Floor(simulatedKills * (highestGlobalStage + 2d) * efficiency);
            long extraGold = Math.Max(0L, desiredGold - legacyGold);

            long legacyFragments = simulatedKills / 20L;
            int fragmentInterval = Mathf.Clamp(20 - highestGlobalStage / 12, 8, 20);
            long desiredFragments = simulatedKills / fragmentInterval;
            long extraFragments = Math.Max(0L, desiredFragments - legacyFragments);

            if (extraGold > 0L) AddGold(extraGold);
            if (extraFragments > 0L) AddFragments(extraFragments);
            PlayerPrefs.SetString(LastGrantTicksKey, rawTicks);
            PlayerPrefs.Save();

            int minutes = Mathf.FloorToInt((float)seconds / 60f);
            lastMessage = $"오프라인 전투력 보정 {minutes}분 · 추가 {extraGold:N0}G, 조각 {extraFragments:N0}";
            if (idle != null && idleMessageField != null)
                idleMessageField.SetValue(idle, lastMessage);
            if (extraGold > 0L || extraFragments > 0L)
                Debug.Log($"[PeanutWarrior] {lastMessage}");
        }

        private long Gold => goldField == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));

        private void AddGold(long amount)
        {
            if (goldField != null) goldField.SetValue(arena, Gold + amount);
        }

        private void AddFragments(long amount)
        {
            if (fragmentsField != null) fragmentsField.SetValue(arena, Fragments + amount);
        }
    }
}
