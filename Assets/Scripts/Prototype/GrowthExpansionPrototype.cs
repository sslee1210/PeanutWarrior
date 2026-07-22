using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Owns the final Peanut Warrior growth stats that are not stored directly in
    /// CombatPrototypeArena. It also provides the lightweight player EXP loop and
    /// equipment-enhancement material income used by the idle prototype.
    /// </summary>
    public sealed class GrowthExpansionPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Prefix = "PeanutWarrior.Growth.";

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo goldField;
        private FieldInfo playerHpField;
        private FieldInfo lifetimeKillsField;
        private PropertyInfo maxHpProperty;

        private int critChanceLevel = 1;
        private int critDamageLevel = 1;
        private int goldGainLevel = 1;
        private int hpRegenLevel = 1;
        private int expGainLevel = 1;
        private int equipmentGainLevel = 1;

        private int playerLevel = 1;
        private long currentExperience;
        private int equipmentEnhancementMaterials;
        private float equipmentMaterialProgress;
        private float regenTimer;
        private int observedKills;
        private string message = "성장 시스템 준비";

        // Keep these private property names because the existing critical-hit bridge
        // resolves them through reflection.
        private float CritChance => Mathf.Min(1f, 0.05f + (critChanceLevel - 1) * 0.02f);
        private float CritDamage => 1.5f + (critDamageLevel - 1) * 0.12f;
        private float GoldGainMultiplier => 1f + (goldGainLevel - 1) * 0.08f;
        private float HpRegenPerSecond => 1.5f + (hpRegenLevel - 1) * 1.1f;

        public float CriticalChance => CritChance;
        public float CriticalDamageMultiplier => CritDamage;
        public float GoldMultiplier => GoldGainMultiplier;
        public float HpRecoveryPerSecond => HpRegenPerSecond;
        public float ExperienceMultiplier => 1f + (expGainLevel - 1) * 0.05f;
        public float EquipmentMaterialMultiplier => 1f + (equipmentGainLevel - 1) * 0.05f;
        public int PlayerLevel => playerLevel;
        public long CurrentExperience => currentExperience;
        public long ExperienceToNextLevel => RequiredExperience(playerLevel);
        public int EquipmentEnhancementMaterials => equipmentEnhancementMaterials;
        public bool CriticalChanceIsMax => CritChance >= 0.9999f;
        public string LastMessage => message;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<GrowthExpansionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorGrowthExpansionPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<GrowthExpansionPrototype>();
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

            Type type = typeof(CombatPrototypeArena);
            goldField = type.GetField("gold", PrivateInstance);
            playerHpField = type.GetField("playerHp", PrivateInstance);
            lifetimeKillsField = type.GetField("lifetimeKills", PrivateInstance);
            maxHpProperty = type.GetProperty("PlayerMaxHp", PrivateInstance);

            Load();
            observedKills = LifetimeKills;
            stageFlow.BossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.BossDefeated -= HandleBossDefeated;
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;

            regenTimer += Time.deltaTime;
            if (regenTimer >= 0.25f)
            {
                float elapsed = regenTimer;
                regenTimer = 0f;
                RegenerateHp(elapsed);
            }

            int kills = LifetimeKills;
            if (kills <= observedKills) return;

            int gainedKills = kills - observedKills;
            observedKills = kills;
            GrantKillProgress(gainedKills);
        }

        private void GrantKillProgress(int gainedKills)
        {
            if (gainedKills <= 0) return;

            int globalStage = GlobalStage;
            long bonusGold = Mathf.RoundToInt(
                gainedKills * Mathf.Max(0f, GoldGainMultiplier - 1f) * (globalStage + 2));
            if (bonusGold > 0 && goldField != null) goldField.SetValue(arena, Gold + bonusGold);

            long baseExperience = Mathf.RoundToInt(gainedKills * (4f + globalStage * 0.35f));
            AddExperience(Mathf.Max(1L, (long)Math.Round(baseExperience * ExperienceMultiplier)));

            equipmentMaterialProgress += gainedKills * 0.08f * EquipmentMaterialMultiplier;
            ConvertMaterialProgress();
        }

        private void HandleBossDefeated()
        {
            int globalStage = GlobalStage;
            long bossExperience = Mathf.RoundToInt((30f + globalStage * 5f) * ExperienceMultiplier);
            AddExperience(Mathf.Max(1L, bossExperience));

            equipmentMaterialProgress += (1f + stageFlow.World * 0.25f) * EquipmentMaterialMultiplier;
            ConvertMaterialProgress();
            message = $"균왕 보상 · EXP +{bossExperience:N0} · 장비 강화 재료 {equipmentEnhancementMaterials:N0}";
            Save();
        }

        private void AddExperience(long amount)
        {
            if (amount <= 0) return;
            currentExperience += amount;

            int gainedLevels = 0;
            while (currentExperience >= RequiredExperience(playerLevel) && playerLevel < 100000)
            {
                currentExperience -= RequiredExperience(playerLevel);
                playerLevel++;
                gainedLevels++;
            }

            if (gainedLevels > 0)
                message = $"레벨 상승 · Lv.{playerLevel} (+{gainedLevels})";
        }

        private void ConvertMaterialProgress()
        {
            if (equipmentMaterialProgress < 1f) return;
            int gained = Mathf.FloorToInt(equipmentMaterialProgress);
            equipmentMaterialProgress -= gained;
            equipmentEnhancementMaterials += gained;
        }

        private void RegenerateHp(float elapsed)
        {
            if (playerHpField == null || maxHpProperty == null) return;
            float hp = Convert.ToSingle(playerHpField.GetValue(arena));
            float maxHp = Convert.ToSingle(maxHpProperty.GetValue(arena));
            if (hp <= 0f || hp >= maxHp) return;
            playerHpField.SetValue(arena, Mathf.Min(maxHp, hp + HpRegenPerSecond * elapsed));
        }

        public void SaveNow()
        {
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(Prefix + "CritChance", critChanceLevel);
            PlayerPrefs.SetInt(Prefix + "CritDamage", critDamageLevel);
            PlayerPrefs.SetInt(Prefix + "GoldGain", goldGainLevel);
            PlayerPrefs.SetInt(Prefix + "HpRegen", hpRegenLevel);
            PlayerPrefs.SetInt(Prefix + "ExpGain", expGainLevel);
            PlayerPrefs.SetInt(Prefix + "EquipmentGain", equipmentGainLevel);
            PlayerPrefs.SetInt(Prefix + "PlayerLevel", playerLevel);
            PlayerPrefs.SetString(Prefix + "CurrentExperience", currentExperience.ToString());
            PlayerPrefs.SetInt(Prefix + "EquipmentMaterials", equipmentEnhancementMaterials);
            PlayerPrefs.SetFloat(Prefix + "EquipmentMaterialProgress", equipmentMaterialProgress);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            critChanceLevel = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "CritChance", 1), 1, 49);
            critDamageLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "CritDamage", 1));
            goldGainLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "GoldGain", 1));
            hpRegenLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "HpRegen", 1));
            expGainLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "ExpGain", 1));
            equipmentGainLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "EquipmentGain", 1));
            playerLevel = Mathf.Max(1, PlayerPrefs.GetInt(Prefix + "PlayerLevel", 1));
            if (!long.TryParse(PlayerPrefs.GetString(Prefix + "CurrentExperience", "0"), out currentExperience))
                currentExperience = 0L;
            currentExperience = Math.Max(0L, currentExperience);
            equipmentEnhancementMaterials = Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "EquipmentMaterials", 0));
            equipmentMaterialProgress = Mathf.Max(0f, PlayerPrefs.GetFloat(Prefix + "EquipmentMaterialProgress", 0f));
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private int GlobalStage =>
            (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;

        private long Gold => goldField == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));
        private int LifetimeKills => lifetimeKillsField == null ? 0 : Convert.ToInt32(lifetimeKillsField.GetValue(arena));

        private static long RequiredExperience(int level)
        {
            double value = 50d * Math.Pow(1.16d, Math.Max(0, level - 1));
            return (long)Math.Min(long.MaxValue / 4d, Math.Max(50d, value));
        }
    }
}
