using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(7000)]
    public sealed class AdvancementProgressionPrototype : MonoBehaviour
    {
        private const string SaveKey = "PeanutWarrior.Advancement.Tier";
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo tierField;
        private FieldInfo petUnlockField;
        private FieldInfo goldField;
        private FieldInfo diamondField;
        private FieldInfo playerHpField;
        private FieldInfo playerMpField;
        private PropertyInfo combatPowerProperty;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;

        private int tier;
        private string lastMessage = "전직 시스템 준비";

        public int Tier => tier;
        public int MaxTier => PeanutGameRules.AdvancementCount - 1;
        public bool HasNextTier => PeanutGameRules.HasNextAdvancement(tier);
        public string CurrentName => PeanutGameRules.GetAdvancement(tier).Name;
        public PeanutGameRules.AdvancementDefinition CurrentDefinition => PeanutGameRules.GetAdvancement(tier);
        public PeanutGameRules.AdvancementDefinition NextDefinition => PeanutGameRules.GetNextAdvancement(tier);
        public string LastMessage => lastMessage;
        public bool PetsUnlocked => CurrentDefinition.UnlocksPets || tier >= 2;
        public int GlobalStage => stageFlow == null ? 1 : PeanutGameRules.ToGlobalStage(stageFlow.World, stageFlow.Stage);
        public int CombatPower => combatPowerProperty == null || arena == null ? 0 : Convert.ToInt32(combatPowerProperty.GetValue(arena));
        public long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        public int Diamonds => diamondField == null || arena == null ? 0 : Convert.ToInt32(diamondField.GetValue(arena));

        public event Action AdvancementChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<AdvancementProgressionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorAdvancementProgression");
            DontDestroyOnLoad(root);
            root.AddComponent<AdvancementProgressionPrototype>();
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
            tierField = arenaType.GetField("advancementTier", PrivateInstance);
            petUnlockField = arenaType.GetField("miniSlotsUnlocked", PrivateInstance);
            goldField = arenaType.GetField("gold", PrivateInstance);
            diamondField = arenaType.GetField("diamonds", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            combatPowerProperty = arenaType.GetProperty("CombatPower", PrivateInstance);
            maxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            maxMpProperty = arenaType.GetProperty("PlayerMaxMp", PrivateInstance);

            int legacyTier = tierField == null ? 0 : Convert.ToInt32(tierField.GetValue(arena));
            tier = Mathf.Clamp(Mathf.Max(legacyTier, PlayerPrefs.GetInt(SaveKey, legacyTier)), 0, MaxTier);
            ApplyTier(false);
        }

        public bool MeetsNextRequirements(out string reason)
        {
            PeanutGameRules.AdvancementDefinition next = NextDefinition;
            if (next == null)
            {
                reason = "최고 전직 단계 달성";
                return false;
            }

            if (GlobalStage < next.RequiredGlobalStage)
            {
                reason = $"스테이지 {next.RequiredGlobalStage} 필요";
                return false;
            }
            if (CombatPower < next.RequiredCombatPower)
            {
                reason = $"전투력 {next.RequiredCombatPower:N0} 필요";
                return false;
            }
            if (Gold < next.RequiredGold)
            {
                reason = $"골드 {next.RequiredGold:N0} 필요";
                return false;
            }
            if (Diamonds < next.RequiredDiamonds)
            {
                reason = $"다이아 {next.RequiredDiamonds:N0} 필요";
                return false;
            }

            reason = "전직 가능";
            return true;
        }

        public bool TryAdvance()
        {
            if (!MeetsNextRequirements(out string reason))
            {
                lastMessage = reason;
                return false;
            }

            PeanutGameRules.AdvancementDefinition next = NextDefinition;
            goldField.SetValue(arena, Gold - next.RequiredGold);
            diamondField.SetValue(arena, Diamonds - next.RequiredDiamonds);
            tier++;
            ApplyTier(true);
            lastMessage = $"전직 성공 · {CurrentName}";
            AdvancementChanged?.Invoke();
            return true;
        }

        public string GetRequirementSummary()
        {
            PeanutGameRules.AdvancementDefinition next = NextDefinition;
            if (next == null) return "모든 전직 완료";
            return $"스테이지 {GlobalStage}/{next.RequiredGlobalStage} · " +
                   $"전투력 {CombatPower:N0}/{next.RequiredCombatPower:N0} · " +
                   $"골드 {Gold:N0}/{next.RequiredGold:N0} · " +
                   $"다이아 {Diamonds}/{next.RequiredDiamonds}";
        }

        public float GetCurrentStatMultiplier()
        {
            return CurrentDefinition.StatMultiplier;
        }

        public int GetCurrentAttackHits()
        {
            return CurrentDefinition.BasicAttackHits;
        }

        public void SaveNow()
        {
            PlayerPrefs.SetInt(SaveKey, tier);
            PlayerPrefs.Save();
        }

        private void ApplyTier(bool restoreVitals)
        {
            if (tierField != null) tierField.SetValue(arena, tier);
            if (petUnlockField != null) petUnlockField.SetValue(arena, PetsUnlocked);
            if (restoreVitals)
            {
                if (playerHpField != null && maxHpProperty != null)
                    playerHpField.SetValue(arena, Convert.ToSingle(maxHpProperty.GetValue(arena)));
                if (playerMpField != null && maxMpProperty != null)
                    playerMpField.SetValue(arena, Convert.ToSingle(maxMpProperty.GetValue(arena)));
            }
            SaveNow();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }
    }
}
