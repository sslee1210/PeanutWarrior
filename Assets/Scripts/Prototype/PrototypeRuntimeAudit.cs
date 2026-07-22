using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Runs a lightweight self-check after scene startup. This does not replace
    /// Unity compilation or play-mode testing, but it catches broken reflection
    /// member names, missing runtime systems, duplicate bootstraps and invalid
    /// core arrays before those faults turn into hard-to-read UI symptoms.
    /// </summary>
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
            // RuntimeInitializeOnLoadMethod creates several systems in the same frame.
            // Waiting two frames gives every Awake/Start method time to bind its data.
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
            RequireSingle<BossPatternPrototype>(errors);
            RequireSingle<RuntimeWorldViewPrototype>(errors);
            RequireSingle<MobileIdleUiPrototype>(errors);
            RequireSingle<PrototypeSaveBridge>(errors);
            RequireSingle<IdleSystemsPrototype>(errors);
            RequireSingle<PrototypeShopAndDaily>(errors);
            RequireSingle<SkillManagementPrototype>(errors);
            RequireSingle<GrowthExpansionPrototype>(errors);
            RequireSingle<LegacyGuiSuppressor>(errors);

            AuditArena(errors, warnings);
            AuditStageFlow(errors);
            AuditGrowth(errors);
            AuditSkills(errors);
            AuditIdleSystems(errors);
            AuditShop(errors);
            AuditWorldView(errors, warnings);

            var report = new StringBuilder();
            report.AppendLine("[PeanutWarrior Runtime Audit]");

            if (errors.Count == 0)
            {
                report.AppendLine("PASS · required prototype systems and reflection bindings are valid.");
            }
            else
            {
                report.AppendLine($"FAIL · {errors.Count} blocking issue(s)");
                for (int i = 0; i < errors.Count; i++)
                    report.AppendLine($"  ERROR {i + 1}. {errors[i]}");
            }

            if (warnings.Count > 0)
            {
                report.AppendLine($"WARN · {warnings.Count} item(s)");
                for (int i = 0; i < warnings.Count; i++)
                    report.AppendLine($"  WARN {i + 1}. {warnings[i]}");
            }

            if (errors.Count == 0) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        private static void RequireSingle<T>(List<string> errors) where T : UnityEngine.Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0)
                errors.Add($"Missing runtime system: {typeof(T).Name}");
            else if (objects.Length > 1)
                errors.Add($"Duplicate runtime system: {typeof(T).Name} ×{objects.Length}");
        }

        private static void AuditArena(List<string> errors, List<string> warnings)
        {
            Type type = typeof(CombatPrototypeArena);
            string[] fields =
            {
                "enemies", "playerPosition", "playerHp", "playerMp",
                "gold", "fragments", "diamonds", "skillLevels", "skillCooldowns",
                "huntingElement", "bossElement", "miniSlotsUnlocked", "advancementTier",
                "playerAttackCooldown", "lifetimeKills"
            };
            string[] properties = { "PlayerMaxHp", "PlayerMaxMp", "PlayerAttackDamage", "CombatPower" };
            string[] methods = { "DealDamage", "SpawnNormalEnemy", "FullRestore" };

            RequireFields(type, fields, PrivateInstance, errors);
            RequireProperties(type, properties, PrivateInstance, errors);
            RequireMethods(type, methods, PrivateInstance, errors);

            CombatPrototypeArena arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (arena == null) return;

            FieldInfo levelField = type.GetField("skillLevels", PrivateInstance);
            FieldInfo cooldownField = type.GetField("skillCooldowns", PrivateInstance);
            int[] levels = levelField?.GetValue(arena) as int[];
            float[] cooldowns = cooldownField?.GetValue(arena) as float[];
            if (levels == null || levels.Length != 8)
                errors.Add("CombatPrototypeArena.skillLevels must contain exactly 8 entries.");
            if (cooldowns == null || cooldowns.Length != 8)
                errors.Add("CombatPrototypeArena.skillCooldowns must contain exactly 8 entries.");

            PropertyInfo hpProperty = type.GetProperty("PlayerMaxHp", PrivateInstance);
            PropertyInfo mpProperty = type.GetProperty("PlayerMaxMp", PrivateInstance);
            if (hpProperty != null && Convert.ToSingle(hpProperty.GetValue(arena)) <= 0f)
                errors.Add("PlayerMaxHp resolved to zero or a negative value.");
            if (mpProperty != null && Convert.ToSingle(mpProperty.GetValue(arena)) <= 0f)
                errors.Add("PlayerMaxMp resolved to zero or a negative value.");

            FieldInfo enemiesField = type.GetField("enemies", PrivateInstance);
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null)
                errors.Add("CombatPrototypeArena.enemies is not initialized.");
            else if (enemies.Count > 40)
                warnings.Add($"Unexpectedly high active enemy count: {enemies.Count}");
        }

        private static void AuditStageFlow(List<string> errors)
        {
            Type type = typeof(StageFlowController);
            string[] methods =
            {
                "RegisterMonsterKill", "TryStartBossBattle", "StartBossBattle",
                "HandleBossBattleDeath", "HandleHuntingDeath", "HandleBossDefeated",
                "SelectStage", "SetAutoChallenge"
            };
            RequireMethods(type, methods, PublicInstance, errors);

            StageFlowController flow = FindFirstObjectByType<StageFlowController>();
            if (flow == null) return;
            if (flow.World < 1) errors.Add("StageFlowController.World is below 1.");
            if (flow.Stage < 1 || flow.Stage > StageFlowController.StagesPerWorld)
                errors.Add($"Invalid stage value: {flow.Stage}");
            if (flow.MonsterKills < 0 || flow.MonsterKills > StageFlowController.RequiredKills)
                errors.Add($"Invalid monster kill counter: {flow.MonsterKills}");
        }

        private static void AuditGrowth(List<string> errors)
        {
            Type type = typeof(GrowthExpansionPrototype);
            RequireFields(type,
                new[] { "critChanceLevel", "critDamageLevel", "goldGainLevel", "hpRegenLevel" },
                PrivateInstance, errors);
            RequireProperties(type, new[] { "CritChance", "CritDamage" }, PrivateInstance, errors);
        }

        private static void AuditSkills(List<string> errors)
        {
            Type type = typeof(SkillManagementPrototype);
            RequireFields(type, new[] { "autoEnabled" }, PrivateInstance, errors);

            SkillManagementPrototype manager = FindFirstObjectByType<SkillManagementPrototype>();
            FieldInfo autoField = type.GetField("autoEnabled", PrivateInstance);
            bool[] auto = manager == null ? null : autoField?.GetValue(manager) as bool[];
            if (auto == null || auto.Length != 8)
                errors.Add("SkillManagementPrototype.autoEnabled must contain exactly 8 entries.");
        }

        private static void AuditIdleSystems(List<string> errors)
        {
            Type type = typeof(IdleSystemsPrototype);
            RequireFields(type,
                new[]
                {
                    "miniAttackLevel", "miniCritLevel", "miniCritDamageLevel",
                    "eggs", "hatchedMinis", "incubating", "incubationRemaining"
                },
                PrivateInstance, errors);
            RequireMethods(type,
                new[]
                {
                    "BuyEgg", "StartIncubation", "ClaimKillMission",
                    "ClaimStageMission", "ClaimGrowthAchievement"
                },
                PrivateInstance, errors);
        }

        private static void AuditShop(List<string> errors)
        {
            Type type = typeof(PrototypeShopAndDaily);
            RequireMethods(type,
                new[] { "ClaimDailyReward", "SummonSword", "BuyEgg" },
                PrivateInstance, errors);
        }

        private static void AuditWorldView(List<string> errors, List<string> warnings)
        {
            RuntimeWorldViewPrototype view = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (view == null) return;
            if (!view.enabled)
                errors.Add("RuntimeWorldViewPrototype is disabled, so the battlefield cannot render.");

            Type type = typeof(RuntimeWorldViewPrototype);
            FieldInfo rootField = type.GetField("worldRoot", PrivateInstance);
            GameObject root = rootField?.GetValue(view) as GameObject;
            if (root == null)
                errors.Add("RuntimeWorldViewPrototype.worldRoot was not built.");
            else if (!root.activeInHierarchy)
                errors.Add("Runtime 2D battlefield root is inactive.");

            Camera camera = Camera.main;
            if (camera == null)
                errors.Add("No MainCamera is available for the runtime battlefield.");
            else if (!camera.orthographic)
                warnings.Add("MainCamera is not orthographic; the 2D field may be distorted.");
        }

        private static void RequireFields(
            Type type,
            IEnumerable<string> names,
            BindingFlags flags,
            List<string> errors)
        {
            foreach (string name in names)
            {
                if (type.GetField(name, flags) == null)
                    errors.Add($"Missing field binding: {type.Name}.{name}");
            }
        }

        private static void RequireProperties(
            Type type,
            IEnumerable<string> names,
            BindingFlags flags,
            List<string> errors)
        {
            foreach (string name in names)
            {
                if (type.GetProperty(name, flags) == null)
                    errors.Add($"Missing property binding: {type.Name}.{name}");
            }
        }

        private static void RequireMethods(
            Type type,
            IEnumerable<string> names,
            BindingFlags flags,
            List<string> errors)
        {
            foreach (string name in names)
            {
                if (type.GetMethod(name, flags) == null)
                    errors.Add($"Missing method binding: {type.Name}.{name}");
            }
        }
    }
}
