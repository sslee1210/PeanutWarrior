using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Corrects prototype values that previously used only the local stage number.
    /// Rewards and incoming monster damage now continue scaling across world borders.
    /// </summary>
    [DefaultExecutionOrder(15500)]
    public sealed class WorldBalanceRuntimePrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo lifetimeKillsField;
        private FieldInfo playerHpField;
        private PropertyInfo playerMaxHpProperty;

        private int observedLifetimeKills;
        private float previousHp;
        private string lastMessage = "월드 밸런스 준비";

        public string LastMessage => lastMessage;
        public int GlobalStage => (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
        public float EnemyDamageMultiplier => 1f + Mathf.Min(4f, (GlobalStage - 1) * 0.015f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<WorldBalanceRuntimePrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorWorldBalanceRuntimePrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<WorldBalanceRuntimePrototype>();
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
            lifetimeKillsField = arenaType.GetField("lifetimeKills", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMaxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            observedLifetimeKills = LifetimeKills;
            previousHp = PlayerHp;
            stageFlow.BossDefeated += AddWorldBossRewardCorrection;
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.BossDefeated -= AddWorldBossRewardCorrection;
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;
            CorrectNormalMonsterRewards();
            ApplyWorldDamageScaling();
        }

        private void CorrectNormalMonsterRewards()
        {
            int kills = LifetimeKills;
            if (kills <= observedLifetimeKills) return;

            int gained = kills - observedLifetimeKills;
            observedLifetimeKills = kills;
            int worldOffset = Mathf.Max(0, (stageFlow.World - 1) * StageFlowController.StagesPerWorld);
            long extraGold = (long)gained * worldOffset;
            long extraFragments = (long)gained * Mathf.Max(0, stageFlow.World - 1) / 12L;
            AddGold(extraGold);
            AddFragments(extraFragments);
            if (extraGold > 0 || extraFragments > 0)
                lastMessage = $"월드 보정 · +{extraGold:N0}G, 조각 +{extraFragments:N0}";
        }

        private void AddWorldBossRewardCorrection()
        {
            int worldOffset = Mathf.Max(0, (stageFlow.World - 1) * StageFlowController.StagesPerWorld);
            long extraGold = worldOffset * 10L;
            long extraFragments = Mathf.Max(0, stageFlow.World - 1) * 2L;
            AddGold(extraGold);
            AddFragments(extraFragments);
            if (extraGold > 0 || extraFragments > 0)
                lastMessage = $"균왕 월드 보정 · +{extraGold:N0}G, 조각 +{extraFragments:N0}";
        }

        private void ApplyWorldDamageScaling()
        {
            float current = PlayerHp;
            if (current >= previousHp || previousHp <= 0f)
            {
                previousHp = current;
                return;
            }

            float baseLoss = previousHp - current;
            float extraLoss = baseLoss * Mathf.Max(0f, EnemyDamageMultiplier - 1f);
            if (extraLoss <= 0f)
            {
                previousHp = current;
                return;
            }

            float corrected = Mathf.Max(0f, current - extraLoss);
            playerHpField?.SetValue(arena, corrected);
            previousHp = corrected;
            if (corrected > 0f) return;

            if (stageFlow.Phase == StageFlowPhase.BossBattle)
                stageFlow.HandleBossBattleDeath();
            else
                stageFlow.HandleHuntingDeath();
            previousHp = PlayerMaxHp;
        }

        private int LifetimeKills => lifetimeKillsField == null ? 0 : Convert.ToInt32(lifetimeKillsField.GetValue(arena));
        private float PlayerHp => playerHpField == null ? 0f : Convert.ToSingle(playerHpField.GetValue(arena));
        private float PlayerMaxHp => playerMaxHpProperty == null ? 100f : Convert.ToSingle(playerMaxHpProperty.GetValue(arena));
        private long Gold => goldField == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private long Fragments => fragmentsField == null ? 0L : Convert.ToInt64(fragmentsField.GetValue(arena));

        private void AddGold(long amount)
        {
            if (goldField != null && amount > 0) goldField.SetValue(arena, Gold + amount);
        }

        private void AddFragments(long amount)
        {
            if (fragmentsField != null && amount > 0) fragmentsField.SetValue(arena, Fragments + amount);
        }
    }
}
