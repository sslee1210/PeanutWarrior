#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PeanutWarrior.Core;
using PeanutWarrior.Prototype;

namespace PeanutWarrior.Tests
{
    public sealed class PeanutCoreCompletionContractTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        [Test]
        public void CanonicalRules_MatchIdleGameSpecification()
        {
            Assert.AreEqual(100, PeanutGameRules.RequiredKillsPerStage);
            Assert.AreEqual(30, PeanutGameRules.StagesPerWorld);
            Assert.AreEqual(45, PeanutGameRules.BossTimeLimitSeconds);
            Assert.AreEqual(8, PeanutGameRules.MaxOfflineHours);
            Assert.AreEqual(8, PeanutGameRules.AdvancementCount);
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++)
                StringAssert.EndsWith("땅콩", PeanutGameRules.GetAdvancement(i).Name);
        }

        [Test]
        public void Pets_UseDirectSeparateTargetCombatInIdleSystem()
        {
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetLevelsCopy", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetStarsCopy", PublicInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetField("MinimumPetSpacing", PrivateStatic));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetField("claimedTargets", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("FindClosestUnclaimedEnemy", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("SeparatePets", PrivateInstance));
        }

        [Test]
        public void Equipment_ChangesAttackPatternAndUsesExtremeExecutionChance()
        {
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("UnifiedItemCount", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("UsesUnifiedEquipmentEntries", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("ShowsDualBattleEffects", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("ChangesAttackPatternByBattleMode", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("ExecutionUsesExtremeRandomInstantKill", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetUnifiedItemId", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetHuntingModeProfile", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetBossModeProfile", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetExecutionChance", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetHuntingEffectDescription", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetBossEffectDescription", PublicInstance));

            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetProperty("UsesHuntingMultiTargetPatterns", PublicInstance));
            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetProperty("UsesBossSingleTargetPatterns", PublicInstance));
            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetProperty("ExecutionKillsBossOnExtremeChance", PublicInstance));
            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetMethod("ApplyHuntingPattern", PrivateInstance));
            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetMethod("ApplyBossPattern", PrivateInstance));
            Assert.NotNull(typeof(LoadoutBonusCombatPrototype).GetMethod("GetEnemyCurrentHp", PrivateStatic));
        }

        [Test]
        public void MenuLayouts_HaveOneOwnerWithoutFlicker()
        {
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesSplitGrowthLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesPerTierAdvancementButtons", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentAndShopMenuV5).GetProperty("UsesUnifiedDualEffectCards", PublicInstance));
            Assert.NotNull(typeof(BottomNavigationOrderV4).GetProperty("BottomMenuOrder", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetProperty("UsesSingleOwnerPerPage", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetProperty("CurrentOwner", PublicInstance));
        }

        [Test]
        public void SkillMenu_IsCardlessAndShowsAccurateDetails()
        {
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("SkillIconCount", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesCardlessSkillLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesNamedSkillSilhouettes", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("AutoButtonIsTopLeft", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesSkillDetailWindow", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("ShowsAccurateDamageDetails", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillDescription", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillTotalDamage", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillCombatSummary", PublicInstance));
        }

        [Test]
        public void BattleSkillDock_DirectlyHidesLegacyBlocks()
        {
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("HidesLegacySkillBlocks", PublicInstance));
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("UsesCircularBattleSkills", PublicInstance));
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("AutoButtonIsTopLeft", PublicInstance));
            Assert.NotNull(typeof(BattleSkillDockV6).GetMethod("RemoveLegacyDock", PrivateInstance));
        }

        [Test]
        public void Shop_UsesOneDualEffectSwordSummon()
        {
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod(
                "TrySummonSword", PublicInstance, null, System.Type.EmptyTypes, null));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetProperty("UsesUnifiedSwordSummon", PublicInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetProperty("TotalSwordSummons", PublicInstance));
        }

        [Test]
        public void SaveService_UsesVersionedJsonAndBackupRecovery()
        {
            Assert.AreEqual(3, PeanutSaveGameService.CurrentSchemaVersion);
            Assert.NotNull(typeof(PeanutSaveGameService).GetMethod("SaveNow", PublicInstance));
            Assert.NotNull(typeof(PeanutSaveGameService).GetMethod("TryRestoreBackup", PublicInstance));
        }

        [Test]
        public void Settings_ExposeAllConfirmedOptions()
        {
            Assert.NotNull(typeof(GameSettingsPrototype).GetMethod("SetBgmVolume", PublicInstance));
            Assert.NotNull(typeof(GameSettingsPrototype).GetMethod("SetSfxVolume", PublicInstance));
            Assert.NotNull(typeof(GameSettingsPrototype).GetMethod("SetVibration", PublicInstance));
            Assert.NotNull(typeof(GameSettingsPrototype).GetMethod("SetFrameRate", PublicInstance));
        }
    }
}
#endif
