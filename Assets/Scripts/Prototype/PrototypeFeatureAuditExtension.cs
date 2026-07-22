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
            Require<BossPatternWorldViewPrototype>(failures);
            Require<CombatEffectWorldViewPrototype>(failures);
            Require<PeanutMobileCanvasPrototype>(failures);
            Require<PeanutCanvasLayoutGuard>(failures);
            Require<FirstClearRewardPrototype>(failures);
            Require<ProgressNotificationBridge>(failures);
            Require<PrototypeSaveIntegrityGuard>(failures);

            PrototypeShopAndDaily shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            FieldInfo swordField = typeof(PrototypeShopAndDaily).GetField("swordProgression", PrivateInstance);
            if (shop == null || swordField == null || swordField.GetValue(shop) == null)
                failures.Add("Shop is not connected to SwordProgressionPrototype.");

            PeanutMobileCanvasPrototype ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            Canvas canvas = ui != null ? ui.GetComponentInChildren<Canvas>(true) : null;
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
                failures.Add("The responsive mobile Canvas is missing or inactive.");
            if (ui != null && (!ui.UsesSimplifiedGrowthMenu || ui.BottomMenuCount != 7))
                failures.Add("The simplified Peanut Warrior menu contract is not active.");

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
                report.AppendLine("PASS · simplified Canvas, stage map, first-clear rewards, save integrity, pooled effects and core progression are active.");
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
