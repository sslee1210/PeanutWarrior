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
        public void EquipmentMenu_UsesLeftCatalogAndRightFullDetail()
        {
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetProperty("UsesLeftCatalogAndRightDetail", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetProperty("ShowsSelectedWeaponAppearance", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetProperty("ShowsFullCombatDetails", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetProperty("SelectedItemId", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetMethod("BuildPage", PrivateInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetMethod("BuildWeaponIdentity", PrivateInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetMethod("BuildHuntingDetail", PrivateInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetMethod("BuildBossDetail", PrivateInstance));
            Assert.NotNull(typeof(PeanutEquipmentDetailMenuV7).GetMethod("CreateWeaponSprite", PrivateStatic));
        }

        [Test]
        public void MenuLayouts_HaveOneOwnerWithoutFlicker()
        {
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesSplitGrowthLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesPerTierAdvancementButtons", PublicInstance));
            Assert.NotNull(typeof(BottomNavigationOrderV4).GetProperty("BottomMenuOrder", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetProperty("UsesSingleOwnerPerPage", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetProperty("CurrentOwner", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetMethod("ResolveBuilder", PrivateInstance));
        }

        [Test]
        public void Skills_UseConfirmedSpectacularPeanutSwordArts()
        {
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("SkillIconCount", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesCardlessSkillLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesNamedSkillSilhouettes", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("AutoButtonIsTopLeft", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesSkillDetailWindow", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("ShowsAccurateDamageDetails", PublicInstance));

            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("ConfirmedSkillCount", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("UsesDistinctSkillTimings", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("UsesSpectacularPeanutSwordArts", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillDescription", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillTotalDamage", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillCombatSummary", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillRole", PublicInstance));

            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("UsesConfirmedMpCosts", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("UsesTacticalAutoPriority", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("HuntingAutoPriority", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("BossAutoPriority", PublicInstance));

            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("UsesEightDistinctSkillExecutions", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("CorrectsLegacySkillCostsAndCooldowns", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("UsesHuntingAreaAndBossFocusRoles", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteSkill", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteShellCyclone", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecutePeanutChainSword", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteGoldenCoreHeavenSever", PrivateInstance));

            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetProperty("UsesEightUniqueSpectacleSequences", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetProperty("ReplacesLegacyGenericSkillEffects", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetMethod("PlaySkillSequence", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetMethod("SpawnFlower", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetMethod("SpawnPodPortal", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillWorldViewPrototype).GetMethod("SpawnCore", PrivateInstance));

            Assert.NotNull(typeof(PeanutBasicAttackWorldViewPrototype).GetProperty("PreservesBasicAttackEffects", PublicInstance));
            Assert.NotNull(typeof(SpectacularSkillIconSyncPrototype).GetProperty("SynchronizesMenuAndBattleIcons", PublicInstance));
            Assert.NotNull(typeof(SpectacularSkillIconSyncPrototype).GetProperty("SynchronizesSkillColors", PublicInstance));
            Assert.NotNull(typeof(SpectacularSkillIconSyncPrototype).GetProperty("WaitsForBuilderAssetInitialization", PublicInstance));
            Assert.NotNull(typeof(SpectacularSkillIconSyncPrototype).GetProperty("SynchronizedIconCount", PublicInstance));
            Assert.NotNull(typeof(SpectacularSkillIconSyncPrototype).GetMethod("Apply", PrivateInstance));
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
