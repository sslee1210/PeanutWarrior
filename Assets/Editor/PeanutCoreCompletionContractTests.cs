#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PeanutWarrior.Core;
using PeanutWarrior.Prototype;
using UnityEngine;

namespace PeanutWarrior.Tests
{
    public sealed class PeanutCoreCompletionContractTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        [Test]
        public void CanonicalRules_MatchIdleGameSpecification()
        {
            Assert.AreEqual(100, PeanutGameRules.RequiredKillsPerStage);
            Assert.AreEqual(30, PeanutGameRules.StagesPerWorld);
            Assert.AreEqual(45, PeanutGameRules.BossTimeLimitSeconds);
            Assert.AreEqual(8, PeanutGameRules.MaxOfflineHours);
            Assert.AreEqual(8, PeanutGameRules.AdvancementCount);
            Assert.IsNotEmpty(PeanutGameRules.GetWorldName(1));
            Assert.IsNotEmpty(PeanutGameRules.GetBossName(1));
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++)
                StringAssert.EndsWith("땅콩", PeanutGameRules.GetAdvancement(i).Name);
        }

        [Test]
        public void Advancement_ExposesCompleteProgressionApi()
        {
            Assert.IsTrue(typeof(AdvancementProgressionPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(AdvancementProgressionPrototype).GetMethod("TryAdvance", PublicInstance));
            Assert.NotNull(typeof(AdvancementProgressionPrototype).GetMethod("RestoreTier", PublicInstance));
            Assert.NotNull(typeof(AdvancementProgressionPrototype).GetProperty("NextDefinition", PublicInstance));
            Assert.NotNull(typeof(AdvancementProgressionPrototype).GetProperty("PetsUnlocked", PublicInstance));
        }

        [Test]
        public void Pets_ExposeCollectionAndSeparateCombatContracts()
        {
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetLevelsCopy", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetStarsCopy", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("RestoreState", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("SpendGoldToTrain", PublicInstance));
            Assert.NotNull(typeof(PetCombatSpreadPrototype).GetProperty("MinimumSpacing", PublicInstance));
            Assert.NotNull(typeof(PetCombatSpreadPrototype).GetProperty("UsesSeparateTargets", PublicInstance));
            Assert.NotNull(typeof(PetCombatSpreadPrototype).GetProperty("UsesBossSurroundFormation", PublicInstance));
        }

        [Test]
        public void Equipment_SeparatesHuntingAndBossCatalogs()
        {
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("TotalItemCount", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("ItemCountPerUse", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetProperty("UsesSeparateHuntingAndBossCatalogs", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod(
                "GetItemId", PublicInstance, null,
                new[] { typeof(bool), typeof(int), typeof(int), typeof(int) }, null));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod(
                "RegisterSummon", PublicInstance, null,
                new[] { typeof(bool), typeof(int), typeof(int) }, null));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod(
                "EquipItem", PublicInstance, null,
                new[] { typeof(int), typeof(bool) }, null));
        }

        [Test]
        public void MenuLayouts_ExposeConfirmedStructureWithoutFlicker()
        {
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesSplitGrowthLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesPerTierAdvancementButtons", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentAndShopMenuV5).GetProperty("UsesSeparateHuntingAndBossTabs", PublicInstance));
            Assert.NotNull(typeof(PeanutEquipmentAndShopMenuV5).GetProperty("EquipmentCatalogCount", PublicInstance));
            Assert.NotNull(typeof(BottomNavigationOrderV4).GetProperty("BottomMenuOrder", PublicInstance));
            Assert.NotNull(typeof(MenuLayoutCoordinatorV6).GetProperty("UsesSingleOwnerPerPage", PublicInstance));
        }

        [Test]
        public void SkillMenu_IsCardlessAndUsesNamedCircularIcons()
        {
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("SkillIconCount", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesCardlessSkillLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("UsesNamedSkillSilhouettes", PublicInstance));
            Assert.NotNull(typeof(PeanutSkillMenuV6).GetProperty("AutoButtonIsTopLeft", PublicInstance));
        }

        [Test]
        public void BattleSkillDock_HidesLegacyBlocksAndUsesCircularIcons()
        {
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("HidesLegacySkillBlocks", PublicInstance));
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("UsesCircularBattleSkills", PublicInstance));
            Assert.NotNull(typeof(BattleSkillDockV6).GetProperty("AutoButtonIsTopLeft", PublicInstance));
        }

        [Test]
        public void Shop_ExposesPurposeSpecificSwordSummons()
        {
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod(
                "TrySummonSword", PublicInstance, null, new[] { typeof(bool) }, null));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetProperty("HuntingSwordSummons", PublicInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetProperty("BossSwordSummons", PublicInstance));
        }

        [Test]
        public void SaveService_UsesVersionedJsonAndBackupRecovery()
        {
            Assert.AreEqual(3, PeanutSaveGameService.CurrentSchemaVersion);
            Assert.NotNull(typeof(PeanutSaveGameService).GetMethod("SaveNow", PublicInstance));
            Assert.NotNull(typeof(PeanutSaveGameService).GetMethod("TryRestoreBackup", PublicInstance));
            Assert.NotNull(typeof(PeanutSaveGameService).GetProperty("MainSavePath", PublicInstance));
            Assert.NotNull(typeof(PeanutSaveGameService).GetProperty("BackupSavePath", PublicInstance));
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
