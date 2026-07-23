using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(35000)]
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
            for (int i = 0; i < 32; i++) yield return null;

            var errors = new List<string>();
            RequireSingle<AdvancementProgressionPrototype>(errors);
            RequireSingle<PetProgressionPrototype>(errors);
            RequireSingle<PetCombatSpreadPrototype>(errors);
            RequireSingle<GameSettingsPrototype>(errors);
            RequireSingle<PeanutSaveGameService>(errors);
            RequireSingle<OfflineProgressRewardPrototype>(errors);
            RequireSingle<OfflineRewardPopupPrototype>(errors);
            RequireSingle<CoreShopProgressionPrototype>(errors);
            RequireSingle<PeanutCoreMenuCompletionV3>(errors);
            RequireSingle<PeanutMenuLayoutV4>(errors);
            RequireSingle<PeanutEquipmentAndShopMenuV5>(errors);
            RequireSingle<PeanutSkillMenuV6>(errors);
            RequireSingle<MenuLayoutCoordinatorV6>(errors);
            RequireSingle<BottomNavigationOrderV4>(errors);
            RequireSingle<ElementEquipmentCatalogPrototype>(errors);
            RequireSingle<AdvancementWorldViewPrototype>(errors);
            RequireSingle<MiniPeanutWorldViewPrototype>(errors);
            RequireSingle<SaveLoadBattlefieldSync>(errors);
            RequireSingle<IdleFirstRunDefaults>(errors);

            if (PeanutGameRules.RequiredKillsPerStage != 100)
                errors.Add("Required kills must remain 100.");
            if (PeanutGameRules.StagesPerWorld != 30)
                errors.Add("Stages per world must remain 30.");
            if (PeanutGameRules.BossTimeLimitSeconds != 45)
                errors.Add("Boss time limit must remain 45 seconds.");
            if (PeanutGameRules.MaxOfflineHours != 8)
                errors.Add("Offline reward cap must remain eight hours.");
            if (PeanutGameRules.AdvancementCount != 8)
                errors.Add("Expected eight advancement definitions.");
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++)
                if (!PeanutGameRules.GetAdvancement(i).Name.EndsWith("땅콩"))
                    errors.Add("Every advancement name must end with 땅콩.");
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

            PetCombatSpreadPrototype spread = FindFirstObjectByType<PetCombatSpreadPrototype>();
            if (spread != null)
            {
                if (!spread.UsesSeparateTargets) errors.Add("Pets must receive separate hunting targets.");
                if (!spread.UsesBossSurroundFormation) errors.Add("Pets must surround bosses from separate positions.");
                if (spread.MinimumSpacing < 70f) errors.Add("Pet minimum spacing is too small.");
            }

            ElementEquipmentCatalogPrototype catalog = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            if (catalog != null)
            {
                if (catalog.TotalItemCount != 96)
                    errors.Add("Separated equipment catalogs must contain 96 items in total.");
                if (catalog.ItemCountPerUse != 48)
                    errors.Add("Hunting and boss catalogs must each contain 48 items.");
                if (catalog.VariantsPerGrade != 3)
                    errors.Add("Each use, element and rarity must contain three equipment variants.");
                if (!catalog.UsesSeparateHuntingAndBossCatalogs)
                    errors.Add("Hunting and boss equipment catalogs must be separate.");

                for (int element = 0; element < 4; element++)
                {
                    int huntingId = catalog.GetItemId(false, element, 1, 0);
                    int bossId = catalog.GetItemId(true, element, 1, 0);
                    if (huntingId < 0 || bossId < 0 || huntingId == bossId)
                        errors.Add("Hunting and boss element item IDs must be distinct.");
                }
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

            PeanutMenuLayoutV4 menuV4 = FindFirstObjectByType<PeanutMenuLayoutV4>();
            if (menuV4 != null)
            {
                if (menuV4.LayoutVersion != 4) errors.Add("Core menu layout version must be 4.");
                if (menuV4.BottomMenuOrder != "성장 → 장비 → 스킬 → 펫 → 전직 → 상점")
                    errors.Add("Bottom navigation order is incorrect.");
                if (!menuV4.UsesSplitGrowthLayout) errors.Add("Growth page must split profile and upgrades.");
                if (!menuV4.UsesPerTierAdvancementButtons) errors.Add("Advancement rows need individual buttons.");
            }

            PeanutSkillMenuV6 skillMenu = FindFirstObjectByType<PeanutSkillMenuV6>();
            if (skillMenu != null)
            {
                if (skillMenu.SkillIconCount != 8) errors.Add("Skill menu must contain eight skill icons.");
                if (!skillMenu.UsesCardlessSkillLayout) errors.Add("Skill menu must not use rectangular skill cards.");
                if (!skillMenu.UsesNamedSkillSilhouettes) errors.Add("Each skill needs a name-specific icon silhouette.");
                if (!skillMenu.AutoButtonIsTopLeft) errors.Add("The global AUTO button must remain at the top-left.");
            }

            MenuLayoutCoordinatorV6 coordinator = FindFirstObjectByType<MenuLayoutCoordinatorV6>();
            if (coordinator != null && !coordinator.UsesSingleOwnerPerPage)
                errors.Add("Menu pages must have exactly one active layout owner.");

            PeanutEquipmentAndShopMenuV5 menuV5 = FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>();
            if (menuV5 != null)
            {
                if (!menuV5.UsesSeparateHuntingAndBossTabs)
                    errors.Add("Equipment page needs separate hunting and boss tabs.");
                if (menuV5.EquipmentCatalogCount != 2 || menuV5.ElementsPerCatalog != 4)
                    errors.Add("Equipment page must expose two catalogs with four elements each.");
                if (menuV5.ItemsPerCatalog != 48)
                    errors.Add("Each equipment use catalog must expose 48 items.");
            }

            MethodInfo summonByUse = typeof(PrototypeShopAndDaily).GetMethod(
                "TrySummonSword", BindingFlags.Instance | BindingFlags.Public);
            if (summonByUse == null)
                errors.Add("Shop must expose separate hunting and boss sword summons.");

            AdvancementWorldViewPrototype advancementView = FindFirstObjectByType<AdvancementWorldViewPrototype>();
            if (advancementView != null && advancement != null && advancementView.AppliedTier != advancement.Tier)
                errors.Add("Advancement world view is not synchronized with the current tier.");

            MiniPeanutWorldViewPrototype petView = FindFirstObjectByType<MiniPeanutWorldViewPrototype>();
            if (petView != null && pets != null && pets.IsUnlocked && petView.VisiblePetCount != 3)
                errors.Add("All three unlocked pets should be visible in the battlefield.");

            MethodInfo offlineGrant = typeof(GrowthExpansionPrototype).GetMethod(
                "GrantOfflineProgress", BindingFlags.Instance | BindingFlags.Public);
            if (offlineGrant == null) errors.Add("Growth offline reward entry point is missing.");
            MethodInfo materialSpend = typeof(GrowthExpansionPrototype).GetMethod(
                "TrySpendEquipmentMaterials", BindingFlags.Instance | BindingFlags.Public);
            if (materialSpend == null) errors.Add("Equipment material spending entry point is missing.");

            if (errors.Count == 0)
            {
                Debug.Log(
                    "[PeanutWarrior Core Completion Audit]\n" +
                    "PASS · pets use separate targets and spacing, menu pages have one owner without flicker, skills use eight name-specific cardless icons with top-left AUTO, and the separated equipment catalogs remain active.");
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
