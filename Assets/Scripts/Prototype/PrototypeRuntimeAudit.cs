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
    /// <summary>
    /// Lightweight runtime validation for the active mobile prototype. Legacy IMGUI
    /// components are intentionally disabled by LegacyGuiSuppressor after the uGUI
    /// Canvas is ready, so that state is valid rather than a blocking error.
    /// </summary>
    public sealed class PrototypeRuntimeAudit : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

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
            // Allow all RuntimeInitializeOnLoadMethod installers and their Start methods
            // to finish before inspecting the assembled prototype.
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            RunAudit();
        }

        private static void RunAudit()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            RequireSingle<StageFlowController>(errors);
            RequireSingle<CombatPrototypeArena>(errors);
            RequireSingle<RuntimeWorldViewPrototype>(errors);
            RequireSingle<ProceduralBattleArtPrototype>(errors);
            RequireSingle<PeanutMobileCanvasPrototype>(errors);
            RequireSingle<PeanutMenuLayoutV2>(errors);
            RequireSingle<LegacyGuiSuppressor>(errors);
            RequireSingle<SkillManagementPrototype>(errors);
            RequireSingle<GlobalSkillAutoGatePrototype>(errors);
            RequireSingle<GrowthExpansionPrototype>(errors);
            RequireSingle<SwordProgressionPrototype>(errors);
            RequireSingle<CombatEffectWorldViewPrototype>(errors);
            RequireSingle<PrototypeSaveBridge>(errors);
            RequireSingle<PrototypeSaveIntegrityGuard>(errors);

            AuditArena(errors, warnings);
            AuditStageFlow(errors);
            AuditWorld(errors, warnings);
            AuditCanvas(errors, warnings);
            AuditMenuLayout(errors, warnings);
            AuditBattleArt(errors);
            AuditSkills(errors);
            AuditEffects(errors, warnings);
            AuditSave(errors);

            StringBuilder report = new StringBuilder();
            report.AppendLine("[PeanutWarrior Runtime Audit]");
            if (errors.Count == 0)
            {
                report.AppendLine(
                    "PASS · illustrated battle art, mobile Canvas UI, automatic combat, " +
                    "boss transitions and save bindings are active.");
            }
            else
            {
                report.AppendLine($"FAIL · {errors.Count} blocking issue(s)");
                for (int index = 0; index < errors.Count; index++)
                    report.AppendLine($"  ERROR {index + 1}. {errors[index]}");
            }

            if (warnings.Count > 0)
            {
                report.AppendLine($"WARN · {warnings.Count} item(s)");
                for (int index = 0; index < warnings.Count; index++)
                    report.AppendLine($"  WARN {index + 1}. {warnings[index]}");
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
            CombatPrototypeArena arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (arena == null) return;

            Type type = typeof(CombatPrototypeArena);
            int[] levels = type.GetField("skillLevels", PrivateInstance)?.GetValue(arena) as int[];
            float[] cooldowns = type.GetField("skillCooldowns", PrivateInstance)?.GetValue(arena) as float[];
            IList enemies = type.GetField("enemies", PrivateInstance)?.GetValue(arena) as IList;

            if (levels == null || levels.Length != 8)
                errors.Add("CombatPrototypeArena.skillLevels must contain 8 entries.");
            if (cooldowns == null || cooldowns.Length != 8)
                errors.Add("CombatPrototypeArena.skillCooldowns must contain 8 entries.");
            if (enemies == null)
                errors.Add("CombatPrototypeArena.enemies is not initialized.");
            else if (enemies.Count > 40)
                warnings.Add($"Unexpected active enemy count: {enemies.Count}");

            if (ReadPrivatePropertyFloat(type, arena, "PlayerMaxHp") <= 0f)
                errors.Add("PlayerMaxHp resolved to zero or less.");
            if (ReadPrivatePropertyFloat(type, arena, "PlayerMaxMp") <= 0f)
                errors.Add("PlayerMaxMp resolved to zero or less.");
        }

        private static void AuditStageFlow(List<string> errors)
        {
            StageFlowController flow = FindFirstObjectByType<StageFlowController>();
            if (flow == null) return;

            if (flow.World < 1) errors.Add("StageFlowController.World is below 1.");
            if (flow.Stage < 1 || flow.Stage > StageFlowController.StagesPerWorld)
                errors.Add($"Invalid stage: {flow.Stage}");
            if (flow.MonsterKills < 0 || flow.MonsterKills > StageFlowController.RequiredKills)
                errors.Add($"Invalid kill counter: {flow.MonsterKills}");
        }

        private static void AuditWorld(List<string> errors, List<string> warnings)
        {
            RuntimeWorldViewPrototype view = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (view == null) return;

            if (!view.enabled) errors.Add("RuntimeWorldViewPrototype is disabled.");

            GameObject root = typeof(RuntimeWorldViewPrototype)
                .GetField("worldRoot", PrivateInstance)?.GetValue(view) as GameObject;
            if (root == null)
                errors.Add("Runtime battlefield root was not built.");
            else if (!root.activeInHierarchy)
                errors.Add("Runtime battlefield root is inactive.");

            Camera camera = Camera.main;
            if (camera == null)
                errors.Add("No MainCamera is available.");
            else if (!camera.orthographic)
                warnings.Add("MainCamera is not orthographic.");
        }

        private static void AuditCanvas(List<string> errors, List<string> warnings)
        {
            PeanutMobileCanvasPrototype ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (ui == null) return;

            if (!ui.enabled) errors.Add("PeanutMobileCanvasPrototype is disabled.");
            if (!ui.UsesSimplifiedGrowthMenu) errors.Add("Final growth menu flag is disabled.");
            if (!ui.UsesGlobalSkillAuto) errors.Add("Global skill AUTO flag is disabled.");
            if (!ui.HasTopStageSelector) errors.Add("Top stage selector flag is disabled.");
            if (ui.BottomMenuCount != 6)
                errors.Add($"Expected 6 bottom menus, found {ui.BottomMenuCount}.");

            Canvas runtimeCanvas = ui.GetComponentInChildren<Canvas>(true);
            if (runtimeCanvas == null)
                errors.Add("Mobile Canvas was not built.");
            else if (!runtimeCanvas.gameObject.activeInHierarchy)
                errors.Add("Mobile Canvas is inactive.");

            if (FindFirstObjectByType<EventSystem>() == null)
                errors.Add("No EventSystem exists for Canvas buttons.");
            if (Screen.width < Screen.height)
                warnings.Add("Current display is portrait; the game is designed for landscape.");
        }

        private static void AuditMenuLayout(List<string> errors, List<string> warnings)
        {
            PeanutMenuLayoutV2 layout = FindFirstObjectByType<PeanutMenuLayoutV2>();
            if (layout == null) return;

            PeanutMobileCanvasPrototype canvas = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            LegacyGuiSuppressor suppressor = FindFirstObjectByType<LegacyGuiSuppressor>();
            bool intentionallySuppressed =
                !layout.enabled &&
                canvas != null && canvas.enabled &&
                suppressor != null && suppressor.enabled;

            if (!layout.enabled && !intentionallySuppressed)
                errors.Add("PeanutMenuLayoutV2 is unexpectedly disabled.");
            else if (intentionallySuppressed)
                warnings.Add("Legacy PeanutMenuLayoutV2 is intentionally suppressed by the active mobile Canvas.");

            if (layout.LayoutVersion != 2)
                errors.Add($"Expected menu layout version 2, found {layout.LayoutVersion}.");
            if (layout.ManagedPageCount != 8)
                errors.Add($"Expected 8 redesigned inner pages, found {layout.ManagedPageCount}.");
            if (!layout.UsesTwoColumnGrowth)
                errors.Add("The final growth page is not using the two-column layout.");
            if (!layout.UsesConstantButtonBackgrounds)
                errors.Add("Button backgrounds are not using the constant-color interaction style.");
        }

        private static void AuditBattleArt(List<string> errors)
        {
            ProceduralBattleArtPrototype art = FindFirstObjectByType<ProceduralBattleArtPrototype>();
            if (art == null) return;
            if (!art.enabled)
                errors.Add("ProceduralBattleArtPrototype is disabled because the illustrated atlas failed to load.");
        }

        private static void AuditSkills(List<string> errors)
        {
            SkillManagementPrototype manager = FindFirstObjectByType<SkillManagementPrototype>();
            if (manager == null) return;

            Type type = typeof(SkillManagementPrototype);
            bool[] values = type.GetField("autoEnabled", PrivateInstance)?.GetValue(manager) as bool[];
            if (values == null || values.Length != 8)
                errors.Add("Compatibility skill auto array must contain 8 entries.");
        }

        private static void AuditEffects(List<string> errors, List<string> warnings)
        {
            CombatEffectWorldViewPrototype effects = FindFirstObjectByType<CombatEffectWorldViewPrototype>();
            if (effects == null) return;

            if (effects.AvailableRingCount + effects.AvailableSlashCount <= 0 && effects.ActiveEffectCount <= 0)
                errors.Add("Combat effect pool was not initialized.");
            if (effects.ActiveEffectCount > 40)
                warnings.Add($"High active combat-effect count: {effects.ActiveEffectCount}");
        }

        private static void AuditSave(List<string> errors)
        {
            PrototypeSaveIntegrityGuard guard = FindFirstObjectByType<PrototypeSaveIntegrityGuard>();
            if (guard != null && guard.SchemaVersion <= 0)
                errors.Add("Save schema version must be positive.");
        }

        private static float ReadPrivatePropertyFloat(Type type, object target, string name)
        {
            PropertyInfo property = type.GetProperty(name, PrivateInstance);
            return property == null ? 0f : Convert.ToSingle(property.GetValue(target));
        }
    }
}
