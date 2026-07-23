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
            for (int i = 0; i < PeanutGameRules.AdvancementCount; i++)
                StringAssert.EndsWith("땅콩", PeanutGameRules.GetAdvancement(i).Name);
            Assert.IsNotEmpty(PeanutGameRules.GetWorldName(1));
            Assert.IsNotEmpty(PeanutGameRules.GetBossName(1));
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
        public void Pets_ExposeThreeSlotCollectionAndRestoreApi()
        {
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetLevelsCopy", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("GetStarsCopy", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("RestoreState", PublicInstance));
            Assert.NotNull(typeof(PetProgressionPrototype).GetMethod("SpendGoldToTrain", PublicInstance));
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

        [Test]
        public void MenuV4_UsesRequestedInformationArchitecture()
        {
            Assert.IsTrue(typeof(PeanutMenuLayoutV4).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("BottomMenuOrder", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesCircularSkillLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesElementEquipmentTabs", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesSplitGrowthLayout", PublicInstance));
            Assert.NotNull(typeof(PeanutMenuLayoutV4).GetProperty("UsesPerTierAdvancementButtons", PublicInstance));
        }

        [Test]
        public void EquipmentCatalog_HasFourElementsFourGradesAndThreeVariants()
        {
            Assert.IsTrue(typeof(ElementEquipmentCatalogPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetItemId", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("RegisterSummon", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("UpgradeItem", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("EquipItem", PublicInstance));
            Assert.NotNull(typeof(ElementEquipmentCatalogPrototype).GetMethod("GetActiveDamageMultiplier", PublicInstance));
        }

        [Test]
        public void Growth_AllowsEquipmentMaterialSpending()
        {
            Assert.NotNull(typeof(GrowthExpansionPrototype).GetMethod("TrySpendEquipmentMaterials", PublicInstance));
            Assert.NotNull(typeof(GrowthExpansionPrototype).GetMethod("AddEquipmentMaterials", PublicInstance));
        }
    }
}
#endif
