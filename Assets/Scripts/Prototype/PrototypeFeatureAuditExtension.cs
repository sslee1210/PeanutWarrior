using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class PrototypeFeatureAuditExtension : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PrototypeFeatureAuditExtension>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPrototypeFeatureAuditExtension");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeFeatureAuditExtension>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            RunAudit();
        }

        private static void RunAudit()
        {
            var failures = new List<string>();
            Require<SwordProgressionPrototype>(failures);
            Require<LoadoutBonusCombatPrototype>(failures);
            Require<WorldBalanceRuntimePrototype>(failures);
            Require<WorldThemePrototype>(failures);
            Require<BossPatternPrototype>(failures);
            Require<BossPatternWorldViewPrototype>(failures);
            Require<CombatEffectWorldViewPrototype>(failures);
            Require<MiniPeanutWorldViewPrototype>(failures);
            Require<PeanutMobileCanvasPrototype>(failures);
            Require<PeanutCanvasLayoutGuard>(failures);
            Require<StageTransitionCombatResetBridge>(failures);
            Require<GlobalSkillAutoGatePrototype>(failures);
            Require<FirstClearRewardPrototype>(failures);
            Require<ProgressNotificationBridge>(failures);
            Require<PrototypeSaveIntegrityGuard>(failures);
            Require<OfflineCombatRewardCorrectionPrototype>(failures);

            PrototypeShopAndDaily shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            FieldInfo swordField = typeof(PrototypeShopAndDaily).GetField("swordProgression", PrivateInstance);
            if (shop == null || swordField == null || swordField.GetValue(shop) == null)
                failures.Add("Shop is not connected to SwordProgressionPrototype.");

            PeanutMobileCanvasPrototype ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            Canvas canvas = ui != null ? ui.GetComponentInChildren<Canvas>(true) : null;
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
                failures.Add("The responsive mobile Canvas is missing or inactive.");
            if (ui != null &&
                (!ui.UsesSimplifiedGrowthMenu || ui.BottomMenuCount != 6 ||
                 !ui.UsesGlobalSkillAuto || !ui.HasTopStageSelector))
                failures.Add("The six-tab idle RPG menu contract is not active.");

            SkillManagementPrototype skillManager = FindFirstObjectByType<SkillManagementPrototype>();
            if (skillManager == null || typeof(SkillManagementPrototype).GetProperty("GlobalAutoEnabled") == null)
                failures.Add("The global skill AUTO state is unavailable.");

            GrowthExpansionPrototype growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            if (growth == null ||
                typeof(GrowthExpansionPrototype).GetField("expGainLevel", PrivateInstance) == null ||
                typeof(GrowthExpansionPrototype).GetField("equipmentGainLevel", PrivateInstance) == null)
                failures.Add("The final ten-stat growth model is incomplete.");

            RuntimeWorldViewPrototype world = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            GameObject worldRoot = world == null
                ? null
                : typeof(RuntimeWorldViewPrototype).GetField("worldRoot", PrivateInstance)?.GetValue(world) as GameObject;
            if (worldRoot == null || !worldRoot.activeInHierarchy)
                failures.Add("The runtime 2D battlefield is missing or inactive.");

            CombatEffectWorldViewPrototype effects = FindFirstObjectByType<CombatEffectWorldViewPrototype>();
            if (effects != null && effects.AvailableRingCount + effects.AvailableSlashCount <= 0)
                failures.Add("The procedural combat-effect pools were not prewarmed.");

            PrototypeSaveIntegrityGuard integrity = FindFirstObjectByType<PrototypeSaveIntegrityGuard>();
            if (integrity != null && integrity.SchemaVersion <= 0)
                failures.Add("The save schema version is invalid.");

            var report = new StringBuilder();
            report.AppendLine("[PeanutWarrior Feature Audit]");
            if (failures.Count == 0)
            {
                report.AppendLine("PASS · six-tab icon UI, top stage selector, global skill AUTO, idle boss timer, final growth stats and core progression are active.");
                Debug.Log(report.ToString());
                return;
            }

            report.AppendLine($"FAIL · {failures.Count} issue(s)");
            for (int i = 0; i < failures.Count; i++) report.AppendLine($"  {i + 1}. {failures[i]}");
            Debug.LogError(report.ToString());
        }

        private static void Require<T>(List<string> failures) where T : Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0) failures.Add($"Missing {typeof(T).Name}.");
            else if (objects.Length > 1) failures.Add($"Duplicate {typeof(T).Name} ×{objects.Length}.");
        }
    }
}
