using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(9000)]
    public sealed class OfflineProgressRewardPrototype : MonoBehaviour
    {
        private const string LastTicksKey = "PeanutWarrior.OfflineProgress.LastUtcTicks";
        private const string LastGrantKey = "PeanutWarrior.OfflineProgress.LastGrantSourceTicks";
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private PropertyInfo combatPowerProperty;
        private string lastMessage = "방치 진행 보상 대기";

        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<OfflineProgressRewardPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorOfflineProgressReward");
            DontDestroyOnLoad(root);
            root.AddComponent<OfflineProgressRewardPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            if (arena == null || stageFlow == null || growth == null)
            {
                enabled = false;
                return;
            }

            combatPowerProperty = typeof(CombatPrototypeArena).GetProperty("CombatPower", PrivateInstance);
            GrantIfEligible();
            RecordCurrentTime();
        }

        private void GrantIfEligible()
        {
            string rawTicks = PlayerPrefs.GetString(LastTicksKey, "0");
            if (!long.TryParse(rawTicks, out long previousTicks) || previousTicks <= 0L) return;
            if (PlayerPrefs.GetString(LastGrantKey, string.Empty) == rawTicks) return;

            long nowTicks = DateTime.UtcNow.Ticks;
            if (nowTicks <= previousTicks) return;
            double seconds = new TimeSpan(nowTicks - previousTicks).TotalSeconds;
            seconds = Math.Max(0d, Math.Min(seconds, PeanutGameRules.MaxOfflineHours * 60d * 60d));
            if (seconds < 30d) return;

            int currentGlobal = PeanutGameRules.ToGlobalStage(stageFlow.World, stageFlow.Stage);
            int highestGlobal = Mathf.Max(currentGlobal, PlayerPrefs.GetInt(HighestStageKey, currentGlobal));
            int combatPower = combatPowerProperty == null
                ? 100
                : Mathf.Max(1, Convert.ToInt32(combatPowerProperty.GetValue(arena)));
            float recommendedPower = 140f + highestGlobal * 28f;
            float efficiency = Mathf.Clamp(combatPower / recommendedPower, 0.45f, 1.50f);
            int simulatedKills = Mathf.FloorToInt((float)seconds / 6f * efficiency);

            long experience = (long)Math.Floor(
                simulatedKills * (4d + highestGlobal * 0.35d) * growth.ExperienceMultiplier);
            int materials = Mathf.FloorToInt(
                simulatedKills * 0.08f * growth.EquipmentMaterialMultiplier);
            int minutes = Mathf.FloorToInt((float)seconds / 60f);
            growth.GrantOfflineProgress(Math.Max(0L, experience), Mathf.Max(0, materials), minutes);

            PlayerPrefs.SetString(LastGrantKey, rawTicks);
            PlayerPrefs.Save();
            lastMessage = $"방치 {minutes}분 · EXP +{experience:N0}, 장비 강화 재료 +{materials:N0}";
            Debug.Log("[PeanutWarrior] " + lastMessage);
        }

        private void RecordCurrentTime()
        {
            PlayerPrefs.SetString(LastTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) RecordCurrentTime();
        }

        private void OnApplicationQuit()
        {
            RecordCurrentTime();
        }
    }
}
