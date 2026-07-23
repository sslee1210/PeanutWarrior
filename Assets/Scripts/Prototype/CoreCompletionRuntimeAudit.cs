using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(37000)]
    public sealed class CoreCompletionRuntimeAudit : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
        private const BindingFlags PublicStatic = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

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
            for (int i = 0; i < 36; i++) yield return null;

            var errors = new List<string>();
            RequireSingle<IdleSystemsPrototype>(errors);
            RequireSingle<PetProgressionPrototype>(errors);
            RequireSingle<MiniPeanutWorldViewPrototype>(errors);
            RequireSingle<PeanutSkillMenuV6>(errors);
            RequireSingle<BattleSkillDockV6>(errors);
            RequireSingle<MenuLayoutCoordinatorV6>(errors);
            RequireSingle<PeanutMenuLayoutV4>(errors);
            RequireSingle<PeanutEquipmentAndShopMenuV5>(errors);
            RequireSingle<PeanutEquipmentDetailMenuV7>(errors);
            RequireSingle<ElementEquipmentCatalogPrototype>(errors);
            RequireSingle<LoadoutBonusCombatPrototype>(errors);
            RequireSingle<AdvancementProgressionPrototype>(errors);
            RequireSingle<PeanutSaveGameService>(errors);

            ValidateRules(errors);
            ValidateDirectPetCombat(errors);
            ValidateSkills(errors);
            ValidateMenus(errors);
            ValidateEquipment(errors);

            if (errors.Count == 0)
            {
                Debug.Log(
                    "[PeanutWarrior Core Completion Audit]\n" +
                    "PASS · equipment uses a left catalog and right detailed weapon view, hunting and boss effects show full combat values, and execution remains capped at 0.001%.");
                yield break;
            }

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("[PeanutWarrior Core Completion Audit]");
            report.AppendLine($"FAIL · {errors.Count} blocking issue(s)");
            for (int i = 0; i < errors.Count; i++) report.AppendLine($"  ERROR {i + 1}. {errors[i]}");
            Debug.LogError(report.ToString());
        }

        private static void ValidateRules(List<string> errors)
        {
            int requiredKills = ReadPublicConstant(nameof(PeanutGameRules.RequiredKillsPerStage));
            int stagesPerWorld = ReadPublicConstant(nameof(PeanutGameRules.StagesPerWorld));
            int bossTimeLimit = ReadPublicConstant(nameof(PeanutGameRules.BossTimeLimitSeconds));

            if (requiredKills != 100) errors.Add("Required kills must remain 100.");
            if (stagesPerWorld != 30) errors.Add("Stages per world must remain 30.");
            if (bossTimeLimit != 45) errors.Add("Boss time limit must remain 45 seconds.");
            if (PeanutGameRules.AdvancementCount != 8) errors.Add("Expected eight advancement definitions.");
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++)
                if (!PeanutGameRules.GetAdvancement(i).Name.EndsWith("땅콩"))
                    errors.Add("Every advancement name must end with 땅콩.");
        }

        private static int ReadPublicConstant(string fieldName)
        {
            FieldInfo field = typeof(PeanutGameRules).GetField(fieldName, PublicStatic);
            if (field == null || !field.IsLiteral) return int.MinValue;
            return Convert.ToInt32(field.GetRawConstantValue());
        }

        private static void ValidateDirectPetCombat(List<string> errors)
        {
            Type type = typeof(IdleSystemsPrototype);
            FieldInfo spacing = type.GetField("MinimumPetSpacing", PrivateStatic);
            FieldInfo claimed = type.GetField("claimedTargets", PrivateInstance);
            MethodInfo separate = type.GetMethod("SeparatePets", PrivateInstance);
            MethodInfo uniqueTarget = type.GetMethod("FindClosestUnclaimedEnemy", PrivateInstance);

            if (spacing == null) errors.Add("IdleSystemsPrototype must define a direct pet spacing constant.");
            else if (Convert.ToSingle(spacing.GetRawConstantValue()) < 80f)
                errors.Add("Direct pet minimum spacing is too small.");
            if (claimed == null) errors.Add("IdleSystemsPrototype must track claimed enemy targets.");
            if (separate == null) errors.Add("IdleSystemsPrototype must apply pairwise pet separation.");
            if (uniqueTarget == null) errors.Add("IdleSystemsPrototype must assign unclaimed hunting targets.");
        }

        private static void ValidateSkills(List<string> errors)
        {
            PeanutSkillMenuV6 menu = FindFirstObjectByType<PeanutSkillMenuV6>();
            if (menu != null)
            {
                if (menu.SkillIconCount != 8) errors.Add("Skill menu must contain eight skill icons.");
                if (!menu.UsesCardlessSkillLayout) errors.Add("Skill menu must not use rectangular cards.");
                if (!menu.UsesNamedSkillSilhouettes) errors.Add("Each skill needs a name-specific icon.");
                if (!menu.AutoButtonIsTopLeft) errors.Add("Skill AUTO must be at the top-left.");
                if (!menu.UsesSkillDetailWindow) errors.Add("Skill icons must open a detail window.");
                if (!menu.ShowsAccurateDamageDetails) errors.Add("Skill detail window must show real combat values.");
            }

            Type skillType = typeof(SkillManagementPrototype);
            if (skillType.GetMethod("GetSkillDescription", PublicInstance) == null)
                errors.Add("Skill description API is missing.");
            if (skillType.GetMethod("GetSkillTotalDamage", PublicInstance) == null)
                errors.Add("Skill damage detail API is missing.");
            if (skillType.GetMethod("GetSkillCombatSummary", PublicInstance) == null)
                errors.Add("Skill combat summary API is missing.");

            BattleSkillDockV6 dock = FindFirstObjectByType<BattleSkillDockV6>();
            if (dock != null)
            {
                if (!dock.HidesLegacySkillBlocks) errors.Add("Legacy battle skill blocks must be hidden.");
                if (!dock.UsesCircularBattleSkills) errors.Add("Battle skills must use named icons.");
                if (!dock.AutoButtonIsTopLeft) errors.Add("Battle AUTO must be at the top-left.");
            }
        }

        private static void ValidateMenus(List<string> errors)
        {
            MenuLayoutCoordinatorV6 coordinator = FindFirstObjectByType<MenuLayoutCoordinatorV6>();
            if (coordinator != null)
            {
                if (!coordinator.UsesSingleOwnerPerPage)
                    errors.Add("Menu pages must have exactly one active renderer.");
                if (!coordinator.KeepsLegacyLayoutLoopsDisabled)
                    errors.Add("Legacy menu layout loops must remain disabled.");
            }

            PeanutMenuLayoutV4 v4 = FindFirstObjectByType<PeanutMenuLayoutV4>();
            if (v4 != null)
            {
                if (v4.BottomMenuOrder != "성장 → 장비 → 스킬 → 펫 → 전직 → 상점")
                    errors.Add("Bottom navigation order is incorrect.");
                if (!v4.UsesSplitGrowthLayout) errors.Add("Growth page must use split profile and stats.");
                if (!v4.UsesPerTierAdvancementButtons) errors.Add("Advancement rows need individual buttons.");
            }
        }

        private static void ValidateEquipment(List<string> errors)
        {
            ElementEquipmentCatalogPrototype catalog = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            if (catalog != null)
            {
                if (catalog.TotalItemCount != 48) errors.Add("Unified equipment catalog must contain 48 swords.");
                if (!catalog.UsesUnifiedEquipmentEntries) errors.Add("Equipment entries must be unified.");
                if (!catalog.ShowsDualBattleEffects) errors.Add("Each equipment entry must expose hunting and boss effects.");
                if (!catalog.ChangesAttackPatternByBattleMode)
                    errors.Add("Equipment must change its attack pattern between hunting and boss modes.");
                if (!catalog.ExecutionUsesExtremeRandomInstantKill)
                    errors.Add("Execution must use an extremely rare random instant-kill roll.");
                if (catalog.UsesSeparateHuntingAndBossCatalogs)
                    errors.Add("Hunting and boss catalogs must not remain separate.");

                int starter = catalog.GetUnifiedItemId(0, 1, 0);
                ElementEquipmentCatalogPrototype.HuntingModeProfile hunting = catalog.GetHuntingModeProfile(starter);
                if (hunting.MaxTargets < 2) errors.Add("Hunting equipment must attack multiple enemies.");
                if (hunting.Radius <= 0f) errors.Add("Hunting equipment must define an attack radius.");

                for (int rarity = 1; rarity <= 4; rarity++)
                {
                    int executionId = catalog.GetUnifiedItemId(0, rarity, 2);
                    ElementEquipmentCatalogPrototype.BossModeProfile execution = catalog.GetBossModeProfile(executionId);
                    if (execution.Style != ElementEquipmentCatalogPrototype.BossAttackStyle.Execution)
                        errors.Add("Third equipment variant must use execution style.");
                    if (execution.ExecuteChance <= 0f)
                        errors.Add("Execution chance must be greater than zero.");
                    if (execution.ExecuteChance > 0.00001f)
                        errors.Add("Execution chance must never exceed 0.001%.");
                }
            }

            LoadoutBonusCombatPrototype combat = FindFirstObjectByType<LoadoutBonusCombatPrototype>();
            if (combat != null)
            {
                if (!combat.UsesHuntingMultiTargetPatterns)
                    errors.Add("Equipment combat must apply hunting multi-target patterns.");
                if (!combat.UsesBossSingleTargetPatterns)
                    errors.Add("Equipment combat must apply boss single-target patterns.");
                if (!combat.ExecutionKillsBossOnExtremeChance)
                    errors.Add("Execution success must remove the boss's remaining HP.");
            }

            PeanutEquipmentDetailMenuV7 detailMenu = FindFirstObjectByType<PeanutEquipmentDetailMenuV7>();
            if (detailMenu != null)
            {
                if (!detailMenu.UsesLeftCatalogAndRightDetail)
                    errors.Add("Equipment page must place the equipment catalog on the left and details on the right.");
                if (!detailMenu.ShowsSelectedWeaponAppearance)
                    errors.Add("Equipment detail must show the selected weapon appearance.");
                if (!detailMenu.ShowsFullCombatDetails)
                    errors.Add("Equipment detail must show identity, effects, damage, targets, range, hits and attack increase values.");
            }

            PrototypeShopAndDaily shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            if (shop != null && !shop.UsesUnifiedSwordSummon)
                errors.Add("Sword summon must unlock one dual-effect equipment entry.");
        }

        private static void RequireSingle<T>(List<string> errors) where T : UnityEngine.Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0) errors.Add("Missing runtime system: " + typeof(T).Name);
            else if (objects.Length > 1) errors.Add("Duplicate runtime system: " + typeof(T).Name + " ×" + objects.Length);
        }
    }
}
