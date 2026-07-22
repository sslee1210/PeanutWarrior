using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PeanutWarrior.Prototype
{
    public sealed class PrototypeRuntimeAudit : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PrototypeRuntimeAudit>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPrototypeRuntimeAudit");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeRuntimeAudit>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            RunAudit();
        }

        private static void RunAudit()
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            RequireSingle<StageFlowController>(errors);
            RequireSingle<CombatPrototypeArena>(errors);
            RequireSingle<CombatExpansionPrototype>(errors);
            RequireSingle<CombatIntegrationPrototype>(errors);
            RequireSingle<LoadoutBonusCombatPrototype>(errors);
            RequireSingle<BossPatternPrototype>(errors);
            RequireSingle<RuntimeWorldViewPrototype>(errors);
            RequireSingle<PeanutMobileCanvasPrototype>(errors);
            RequireSingle<PeanutCanvasLayoutGuard>(errors);
            RequireSingle<PrototypeSaveBridge>(errors);
            RequireSingle<PrototypeSaveIntegrityGuard>(errors);
            RequireSingle<IdleSystemsPrototype>(errors);
            RequireSingle<PrototypeShopAndDaily>(errors);
            RequireSingle<SkillManagementPrototype>(errors);
            RequireSingle<GrowthExpansionPrototype>(errors);
            RequireSingle<SwordProgressionPrototype>(errors);
            RequireSingle<FirstClearRewardPrototype>(errors);
            RequireSingle<ProgressNotificationBridge>(errors);
            RequireSingle<CombatEffectWorldViewPrototype>(errors);
            RequireSingle<LegacyGuiSuppressor>(errors);

            AuditArena(errors, warnings);
            AuditStageFlow(errors);
            AuditGrowth(errors);
            AuditSkills(errors);
            AuditSwords(errors);
            AuditIdle(errors);
            AuditShop(errors);
            AuditWorld(errors, warnings);
            AuditCanvas(errors, warnings);
            AuditSaveIntegrity(errors);
            AuditFirstClear(errors);
            AuditEffectPool(errors, warnings);

            var report = new StringBuilder();
            report.AppendLine("[PeanutWarrior Runtime Audit]");
            if (errors.Count == 0)
                report.AppendLine("PASS · core combat, simplified progression, Canvas UI, save integrity and reflection bindings are valid.");
            else
            {
                report.AppendLine($"FAIL · {errors.Count} blocking issue(s)");
                for (int i = 0; i < errors.Count; i++) report.AppendLine($"  ERROR {i + 1}. {errors[i]}");
            }

            if (warnings.Count > 0)
            {
                report.AppendLine($"WARN · {warnings.Count} item(s)");
                for (int i = 0; i < warnings.Count; i++) report.AppendLine($"  WARN {i + 1}. {warnings[i]}");
            }

            if (errors.Count == 0) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        private static void RequireSingle<T>(List<string> errors) where T : UnityEngine.Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0) errors.Add($"Missing runtime system: {typeof(T).Name}");
            else if (objects.Length > 1) errors.Add($"Duplicate runtime system: {typeof(T).Name} ×{objects.Length}");
        }

        private static void AuditArena(List<string> errors, List<string> warnings)
        {
            Type type = typeof(CombatPrototypeArena);
            RequireFields(type, new[]
            {
                "enemies", "playerPosition", "playerHp", "playerMp", "gold", "fragments", "diamonds",
                "skillLevels", "skillCooldowns", "huntingElement", "bossElement", "miniSlotsUnlocked",
                "advancementTier", "playerAttackCooldown", "lifetimeKills", "attackLevel", "hpLevel",
                "maxMpLevel", "mpRegenLevel", "basicAttackLevel"
            }, PrivateInstance, errors);
            RequireProperties(type, new[] { "PlayerMaxHp", "PlayerMaxMp", "PlayerAttackDamage", "CombatPower" }, PrivateInstance, errors);
            RequireMethods(type, new[] { "DealDamage", "SpawnNormalEnemy", "FullRestore", "TryAdvance" }, PrivateInstance, errors);

            CombatPrototypeArena arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (arena == null) return;
            int[] levels = type.GetField("skillLevels", PrivateInstance)?.GetValue(arena) as int[];
            float[] cooldowns = type.GetField("skillCooldowns", PrivateInstance)?.GetValue(arena) as float[];
            if (levels == null || levels.Length != 8) errors.Add("CombatPrototypeArena.skillLevels must contain 8 entries.");
            if (cooldowns == null || cooldowns.Length != 8) errors.Add("CombatPrototypeArena.skillCooldowns must contain 8 entries.");

            IList enemies = type.GetField("enemies", PrivateInstance)?.GetValue(arena) as IList;
            if (enemies == null) errors.Add("CombatPrototypeArena.enemies is not initialized.");
            else if (enemies.Count > 40) warnings.Add($"Unexpected active enemy count: {enemies.Count}");

            float hp = ReadPropertyFloat(type, arena, "PlayerMaxHp");
            float mp = ReadPropertyFloat(type, arena, "PlayerMaxMp");
            if (hp <= 0f) errors.Add("PlayerMaxHp resolved to zero or less.");
            if (mp <= 0f) errors.Add("PlayerMaxMp resolved to zero or less.");
        }

        private static void AuditStageFlow(List<string> errors)
        {
            Type type = typeof(StageFlowController);
            RequireMethods(type, new[]
            {
                "RegisterMonsterKill", "TryStartBossBattle", "StartBossBattle", "HandleBossBattleDeath",
                "HandleHuntingDeath", "HandleBossDefeated", "SelectStage", "SetAutoChallenge"
            }, PublicInstance, errors);
            StageFlowController flow = FindFirstObjectByType<StageFlowController>();
            if (flow == null) return;
            if (flow.World < 1) errors.Add("StageFlowController.World is below 1.");
            if (flow.Stage < 1 || flow.Stage > StageFlowController.StagesPerWorld) errors.Add($"Invalid stage: {flow.Stage}");
            if (flow.MonsterKills < 0 || flow.MonsterKills > StageFlowController.RequiredKills) errors.Add($"Invalid kill counter: {flow.MonsterKills}");
        }

        private static void AuditGrowth(List<string> errors)
        {
            Type type = typeof(GrowthExpansionPrototype);
            RequireFields(type, new[] { "critChanceLevel", "critDamageLevel", "goldGainLevel", "hpRegenLevel" }, PrivateInstance, errors);
            RequireProperties(type, new[] { "CritChance", "CritDamage" }, PrivateInstance, errors);
        }

        private static void AuditSkills(List<string> errors)
        {
            Type type = typeof(SkillManagementPrototype);
            RequireFields(type, new[] { "autoEnabled" }, PrivateInstance, errors);
            SkillManagementPrototype manager = FindFirstObjectByType<SkillManagementPrototype>();
            bool[] values = manager == null ? null : type.GetField("autoEnabled", PrivateInstance)?.GetValue(manager) as bool[];
            if (values == null || values.Length != 8) errors.Add("Skill auto-use array must contain 8 entries.");
        }

        private static void AuditSwords(List<string> errors)
        {
            Type type = typeof(SwordProgressionPrototype);
            RequireMethods(type, new[] { "RegisterSummon", "UpgradeSword", "ManualSynthesize", "GetDamageMultiplier" }, PublicInstance, errors);
        }

        private static void AuditIdle(List<string> errors)
        {
            Type type = typeof(IdleSystemsPrototype);
            RequireFields(type, new[]
            {
                "miniAttackLevel", "miniCritLevel", "miniCritDamageLevel", "eggs", "hatchedMinis",
                "incubating", "incubationRemaining", "systemMessage"
            }, PrivateInstance, errors);
            RequireMethods(type, new[]
            {
                "BuyEgg", "StartIncubation", "ClaimKillMission", "ClaimStageMission", "ClaimGrowthAchievement"
            }, PrivateInstance, errors);
        }

        private static void AuditShop(List<string> errors)
        {
            RequireMethods(typeof(PrototypeShopAndDaily), new[] { "ClaimDailyReward", "SummonSword", "BuyEgg" }, PrivateInstance, errors);
        }

        private static void AuditWorld(List<string> errors, List<string> warnings)
        {
            RuntimeWorldViewPrototype view = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (view == null) return;
            if (!view.enabled) errors.Add("RuntimeWorldViewPrototype is disabled.");
            GameObject root = typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance)?.GetValue(view) as GameObject;
            if (root == null) errors.Add("Runtime battlefield root was not built.");
            else if (!root.activeInHierarchy) errors.Add("Runtime battlefield root is inactive.");
            Camera camera = Camera.main;
            if (camera == null) errors.Add("No MainCamera is available.");
            else if (!camera.orthographic) warnings.Add("MainCamera is not orthographic.");
        }

        private static void AuditCanvas(List<string> errors, List<string> warnings)
        {
            PeanutMobileCanvasPrototype ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (ui == null) return;
            if (!ui.enabled) errors.Add("PeanutMobileCanvasPrototype is disabled.");
            if (!ui.UsesSimplifiedGrowthMenu) errors.Add("Simplified growth menu flag is disabled.");
            if (ui.BottomMenuCount != 7) errors.Add($"Expected 7 bottom menus, found {ui.BottomMenuCount}.");
            Canvas runtimeCanvas = ui.GetComponentInChildren<Canvas>(true);
            if (runtimeCanvas == null) errors.Add("Mobile Canvas was not built.");
            else if (!runtimeCanvas.gameObject.activeInHierarchy) errors.Add("Mobile Canvas is inactive.");
            if (FindFirstObjectByType<EventSystem>() == null) errors.Add("No EventSystem exists for Canvas buttons.");
            if (Screen.width < Screen.height) warnings.Add("Current display is portrait; the game is designed for landscape.");
        }

        private static void AuditSaveIntegrity(List<string> errors)
        {
            PrototypeSaveIntegrityGuard guard = FindFirstObjectByType<PrototypeSaveIntegrityGuard>();
            if (guard == null) return;
            if (guard.SchemaVersion <= 0) errors.Add("Save schema version must be positive.");
            RequireMethods(typeof(PrototypeSaveIntegrityGuard), new[] { "TryRestoreBackup" }, PublicInstance, errors);
        }

        private static void AuditFirstClear(List<string> errors)
        {
            FirstClearRewardPrototype rewards = FindFirstObjectByType<FirstClearRewardPrototype>();
            if (rewards == null) return;
            if (rewards.BossKills < 0 || rewards.UniqueClears < 0) errors.Add("First-clear counters cannot be negative.");
        }

        private static void AuditEffectPool(List<string> errors, List<string> warnings)
        {
            CombatEffectWorldViewPrototype effects = FindFirstObjectByType<CombatEffectWorldViewPrototype>();
            if (effects == null) return;
            if (effects.AvailableRingCount + effects.AvailableSlashCount <= 0 && effects.ActiveEffectCount <= 0)
                errors.Add("Combat effect pool was not initialized.");
            if (effects.ActiveEffectCount > 40)
                warnings.Add($"High active combat-effect count: {effects.ActiveEffectCount}");
        }

        private static float ReadPropertyFloat(Type type, object target, string name)
        {
            PropertyInfo property = type.GetProperty(name, PrivateInstance);
            return property == null ? 0f : Convert.ToSingle(property.GetValue(target));
        }

        private static void RequireFields(Type type, IEnumerable<string> names, BindingFlags flags, List<string> errors)
        {
            foreach (string name in names)
                if (type.GetField(name, flags) == null) errors.Add($"Missing field: {type.Name}.{name}");
        }

        private static void RequireProperties(Type type, IEnumerable<string> names, BindingFlags flags, List<string> errors)
        {
            foreach (string name in names)
                if (type.GetProperty(name, flags) == null) errors.Add($"Missing property: {type.Name}.{name}");
        }

        private static void RequireMethods(Type type, IEnumerable<string> names, BindingFlags flags, List<string> errors)
        {
            foreach (string name in names)
                if (type.GetMethod(name, flags) == null) errors.Add($"Missing method: {type.Name}.{name}");
        }
    }
}
