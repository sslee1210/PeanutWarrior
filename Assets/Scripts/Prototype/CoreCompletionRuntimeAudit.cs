using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(32500)]
    public sealed class CoreCompletionRuntimeAudit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CoreCompletionRuntimeAudit>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCoreCompletionAudit");
            DontDestroyOnLoad(root);
            root.AddComponent<CoreCompletionRuntimeAudit>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 15; i++) yield return null;

            var errors = new List<string>();
            RequireSingle<AdvancementProgressionPrototype>(errors);
            RequireSingle<PetProgressionPrototype>(errors);
            RequireSingle<GameSettingsPrototype>(errors);
            RequireSingle<PeanutSaveGameService>(errors);
            RequireSingle<OfflineProgressRewardPrototype>(errors);
            RequireSingle<CoreShopProgressionPrototype>(errors);
            RequireSingle<PeanutCoreMenuCompletionV3>(errors);

            if (PeanutGameRules.RequiredKillsPerStage != 100)
                errors.Add("Required kills must remain 100.");
            if (PeanutGameRules.StagesPerWorld != 30)
                errors.Add("Stages per world must remain 30.");
            if (PeanutGameRules.BossTimeLimitSeconds != 45)
                errors.Add("Boss time limit must remain 45 seconds.");
            if (PeanutGameRules.AdvancementCount != 8)
                errors.Add("Expected eight advancement definitions.");
            if (string.IsNullOrEmpty(PeanutGameRules.GetWorldName(1)) || string.IsNullOrEmpty(PeanutGameRules.GetBossName(1)))
                errors.Add("World or boss name rules are empty.");

            AdvancementProgressionPrototype advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            if (advancement != null)
            {
                if (advancement.Tier < 0 || advancement.Tier > advancement.MaxTier)
                    errors.Add("Advancement tier is outside the valid range.");
                if (advancement.MaxTier != 7)
                    errors.Add("Advancement max tier should be 7.");
            }

            PetProgressionPrototype pets = FindFirstObjectByType<PetProgressionPrototype>();
            if (pets != null)
            {
                int[] levels = pets.GetLevelsCopy();
                int[] stars = pets.GetStarsCopy();
                if (levels == null || levels.Length != 3) errors.Add("Pet level state must contain three entries.");
                if (stars == null || stars.Length != 3) errors.Add("Pet star state must contain three entries.");
                if (stars != null)
                    for (int i = 0; i < stars.Length; i++)
                        if (stars[i] < 1 || stars[i] > 5) errors.Add("Pet star value is outside 1..5.");
            }

            GameSettingsPrototype settings = FindFirstObjectByType<GameSettingsPrototype>();
            if (settings != null)
            {
                if (settings.BgmVolume < 0f || settings.BgmVolume > 1f) errors.Add("BGM volume is outside 0..1.");
                if (settings.SfxVolume < 0f || settings.SfxVolume > 1f) errors.Add("SFX volume is outside 0..1.");
                if (settings.TargetFrameRate != 30 && settings.TargetFrameRate != 60)
                    errors.Add("Target frame rate must be 30 or 60.");
            }

            PeanutSaveGameService save = FindFirstObjectByType<PeanutSaveGameService>();
            if (save != null)
            {
                if (save.SchemaVersion != PeanutSaveGameService.CurrentSchemaVersion)
                    errors.Add("Save schema version mismatch.");
                MethodInfo restore = typeof(PeanutSaveGameService).GetMethod("TryRestoreBackup", BindingFlags.Instance | BindingFlags.Public);
                if (restore == null) errors.Add("Backup restore method is missing.");
            }

            PeanutCoreMenuCompletionV3 menu = FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            if (menu != null)
            {
                if (menu.CompletedPageCount != 4) errors.Add("Expected four completed core menu pages.");
                if (!menu.LeavesSkillsAndEquipmentUntouched)
                    errors.Add("Skill and equipment pages must remain untouched.");
            }

            MethodInfo offlineGrant = typeof(GrowthExpansionPrototype).GetMethod(
                "GrantOfflineProgress", BindingFlags.Instance | BindingFlags.Public);
            if (offlineGrant == null) errors.Add("Growth offline reward entry point is missing.");

            if (errors.Count == 0)
            {
                Debug.Log(
                    "[PeanutWarrior Core Completion Audit]\n" +
                    "PASS · advancement, pets, stage rules, idle boss, growth, offline rewards, shop, settings, JSON save and core menu V3 are active. SKILL and equipment remain intentionally deferred.");
                yield break;
            }

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("[PeanutWarrior Core Completion Audit]");
            report.AppendLine($"FAIL · {errors.Count} blocking issue(s)");
            for (int i = 0; i < errors.Count; i++) report.AppendLine($"  ERROR {i + 1}. {errors[i]}");
            Debug.LogError(report.ToString());
        }

        private static void RequireSingle<T>(List<string> errors) where T : Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0) errors.Add("Missing runtime system: " + typeof(T).Name);
            else if (objects.Length > 1) errors.Add("Duplicate runtime system: " + typeof(T).Name + " ×" + objects.Length);
        }
    }
}
