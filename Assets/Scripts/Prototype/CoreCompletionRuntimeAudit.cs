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
            RequireSingle<ElementEquipmentCatalogPrototype>(errors);
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
                    "PASS · direct pet combat uses separate targets and spacing, legacy skill blocks are hidden, cardless skill icons are active, and each menu page has one renderer.");
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
            }

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
                if (catalog.TotalItemCount != 96) errors.Add("Equipment catalog must contain 96 swords.");
                if (catalog.ItemCountPerUse != 48) errors.Add("Each hunting/boss catalog must contain 48 swords.");
                if (!catalog.UsesSeparateHuntingAndBossCatalogs)
                    errors.Add("Hunting and boss equipment catalogs must remain separate.");
            }

            PeanutEquipmentAndShopMenuV5 menu = FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>();
            if (menu != null && !menu.UsesSeparateHuntingAndBossTabs)
                errors.Add("Equipment page needs separate hunting and boss tabs.");
        }

        private static void RequireSingle<T>(List<string> errors) where T : UnityEngine.Object
        {
            T[] objects = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (objects.Length == 0) errors.Add("Missing runtime system: " + typeof(T).Name);
            else if (objects.Length > 1) errors.Add("Duplicate runtime system: " + typeof(T).Name + " ×" + objects.Length);
        }
    }
}
