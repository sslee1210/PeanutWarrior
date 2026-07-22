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
            RequireSingle<BossPatternWorldViewPrototype>(errors);
            RequireSingle<RuntimeWorldViewPrototype>(errors);
            RequireSingle<PeanutMobileCanvasPrototype>(errors);
            RequireSingle<PeanutMenuLayoutV2>(errors);
            RequireSingle<PeanutCanvasLayoutGuard>(errors);
            RequireSingle<PrototypeSaveBridge>(errors);
            RequireSingle<PrototypeSaveIntegrityGuard>(errors);
            RequireSingle<IdleSystemsPrototype>(errors);
            RequireSingle<PrototypeShopAndDaily>(errors);
            RequireSingle<SkillManagementPrototype>(errors);
            RequireSingle<GlobalSkillAutoGatePrototype>(errors);
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
            AuditBoss(errors);
            AuditWorld(errors, warnings);
            AuditCanvas(errors, warnings);
            AuditMenuLayout(errors);
            AuditSaveIntegrity(errors);
            AuditFirstClear(errors);
            AuditEffectPool(errors, warnings);

            var report = new StringBuilder();
            report.AppendLine("[PeanutWarrior Runtime Audit]");
            if (errors.Count == 0)
                report.AppendLine("PASS · core idle combat, six-tab Canvas UI, menu layout V2, global skill AUTO, final ten-stat growth and save bindings are valid.");
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
            RequireProperties(type, new[]
            {
                "PlayerMaxHp", "PlayerMaxMp", "PlayerMpRegen", "PlayerAttackDamage", "CombatPower"
            }, PrivateInstance, errors);
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

            if (ReadPropertyFloat(type, arena, "PlayerMaxHp") <= 0f) errors.Add("PlayerMaxHp resolved to zero or less.");
            if (ReadPropertyFloat(type, arena, "PlayerMaxMp") <= 0f) errors.Add("PlayerMaxMp resolved to zero or less.");
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
            if (flow.MonsterKills < 0 || flow.MonsterKills > StageFlowController.RequiredKills)
                errors.Add($"Invalid kill counter: {flow.MonsterKills}");
        }

        private static void AuditGrowth(List<string> errors)
        {
            Type type = typeof(GrowthExpansionPrototype);
            RequireFields(type, new[]
            {
                "critChanceLevel", "critDamageLevel", "goldGainLevel", "hpRegenLevel",
                "expGainLevel", "equipmentGainLevel"
            }, PrivateInstance, errors);
            RequireProperties(type, new[] { "CritChance", "CritDamage" }, PrivateInstance, errors);
            RequireProperties(type, new[]
            {
                "CriticalChance", "CriticalDamageMultiplier", "ExperienceMultiplier",
                "EquipmentMaterialMultiplier", "PlayerLevel", "EquipmentEnhancementMaterials"
            }, PublicInstance, errors);

            GrowthExpansionPrototype growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            if (growth != null && (growth.CriticalChance < 0f || growth.CriticalChance > 1.0001f))
                errors.Add("Critical chance must remain between 0% and 100%.");
        }

        private static void AuditSkills(List<string> errors)
        {
            Type type = typeof(SkillManagementPrototype);
            RequireFields(type, new[] { "autoEnabled", "globalAutoEnabled" }, PrivateInstance, errors);
            RequireMethods(type, new[] { "ToggleGlobalAuto", "SetGlobalAuto", "UpgradeSkill" }, PublicInstance, errors);
            RequireProperties(type, new[] { "GlobalAutoEnabled", "SkillLevels", "Cooldowns" }, PublicInstance, errors);

            SkillManagementPrototype manager = FindFirstObjectByType<SkillManagementPrototype>();
            bool[] values = manager == null ? null : type.GetField("autoEnabled", PrivateInstance)?.GetValue(manager) as bool[];
            if (values == null || values.Length != 8) errors.Add("Compatibility skill auto array must contain 8 entries.");
            if (manager != null && values != null)
            {
                for (int i = 0; i < values.Length; i++)
                    if (values[i] != manager.GlobalAutoEnabled)
                        errors.Add("Every compatibility auto flag must mirror the global AUTO state.");
            }
        }

        private static void AuditSwords(List<string> errors)
        {
            Type type = typeof(SwordProgressionPrototype);
            RequireMethods(type, new[] { "RegisterSummon", "UpgradeSword", "ManualSynthesize", "GetDamageMultiplier" }, PublicInstance, errors);

            string[] names = { "Rare", "Epic", "Unique", "Legend" };
            int[] expectedValues = { 1, 2, 3, 4 };
            for (int i = 0; i < names.Length; i++)
            {
                object parsed = Enum.Parse(typeof(SwordProgressionPrototype.SwordRarity), names[i]);
                if (Convert.ToInt32(parsed) == expectedValues[i]) continue;
                errors.Add("Sword grade order must be Rare → Epic → Unique → Legend.");
                break;
            }
        }

        private static void AuditIdle(List<string> errors)
        {
            Type type = typeof(IdleSystemsPrototype);
            RequireFields(type, new[]
            {
                "miniAttackLevel", "miniCritLevel", "miniCritDamageLevel", "eggs", "hatchedMinis",
                "incubating", "incubationRemaining", "systemMessage"
            }, PrivateInstance, errors);
            RequireMethods(type, new[] { "BuyEgg", "StartIncubation" }, PrivateInstance, errors);
        }

        private static void AuditShop(List<string> errors)
        {
            RequireMethods(typeof(PrototypeShopAndDaily), new[] { "ClaimDailyReward", "SummonSword", "BuyEgg" }, PrivateInstance, errors);
        }

        private static void AuditBoss(List<string> errors)
        {
            Type type = typeof(BossPatternPrototype);
            RequireProperties(type, new[] { "RemainingTime", "PatternName", "EncounterActive" }, PublicInstance, errors);
            if (type.GetField("warningTimer", PrivateInstance) != null || type.GetField("warningCenter", PrivateInstance) != null)
                errors.Add("Manual dodge warning fields should not exist in the idle boss controller.");
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
            if (!ui.UsesSimplifiedGrowthMenu) errors.Add("Final growth menu flag is disabled.");
            if (!ui.UsesGlobalSkillAuto) errors.Add("Global skill AUTO flag is disabled.");
            if (!ui.HasTopStageSelector) errors.Add("Top stage selector flag is disabled.");
            if (ui.BottomMenuCount != 6) errors.Add($"Expected 6 bottom menus, found {ui.BottomMenuCount}.");
            Canvas runtimeCanvas = ui.GetComponentInChildren<Canvas>(true);
            if (runtimeCanvas == null) errors.Add("Mobile Canvas was not built.");
            else if (!runtimeCanvas.gameObject.activeInHierarchy) errors.Add("Mobile Canvas is inactive.");
            if (FindFirstObjectByType<EventSystem>() == null) errors.Add("No EventSystem exists for Canvas buttons.");
            if (Screen.width < Screen.height) warnings.Add("Current display is portrait; the game is designed for landscape.");
        }

        private static void AuditMenuLayout(List<string> errors)
        {
            PeanutMenuLayoutV2 layout = FindFirstObjectByType<PeanutMenuLayoutV2>();
            if (layout == null) return;
            if (!layout.enabled) errors.Add("PeanutMenuLayoutV2 is disabled.");
            if (layout.LayoutVersion != 2) errors.Add($"Expected menu layout version 2, found {layout.LayoutVersion}.");
            if (layout.ManagedPageCount != 8) errors.Add($"Expected 8 redesigned inner pages, found {layout.ManagedPageCount}.");
            if (!layout.UsesTwoColumnGrowth) errors.Add("The final growth page is not using the two-column layout.");
            if (!layout.UsesConstantButtonBackgrounds) errors.Add("Button backgrounds are not locked to the constant-color interaction style.");
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
