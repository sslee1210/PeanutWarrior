#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PeanutWarrior.Core;
using PeanutWarrior.Prototype;
using UnityEngine;

namespace PeanutWarrior.Tests
{
    public sealed class PeanutWarriorPrototypeContractTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        [Test]
        public void StageFlow_UsesConfirmedIdleRules()
        {
            Assert.AreEqual(100, StageFlowController.RequiredKills);
            Assert.AreEqual(30, StageFlowController.StagesPerWorld);
            Assert.NotNull(typeof(StageFlowController).GetMethod("TryStartBossBattle", PublicInstance));
            Assert.NotNull(typeof(StageFlowController).GetMethod("HandleBossBattleDeath", PublicInstance));
            Assert.NotNull(typeof(StageFlowController).GetMethod("HandleHuntingDeath", PublicInstance));
            Assert.NotNull(typeof(StageFlowController).GetMethod("SelectStage", PublicInstance));
        }

        [Test]
        public void CombatArena_HasEightSkillsAndRequiredBindings()
        {
            GameObject root = new GameObject("CombatArenaContractTest");
            try
            {
                CombatPrototypeArena arena = root.AddComponent<CombatPrototypeArena>();
                int[] levels = typeof(CombatPrototypeArena).GetField("skillLevels", PrivateInstance)?.GetValue(arena) as int[];
                float[] cooldowns = typeof(CombatPrototypeArena).GetField("skillCooldowns", PrivateInstance)?.GetValue(arena) as float[];
                Assert.NotNull(levels);
                Assert.NotNull(cooldowns);
                Assert.AreEqual(8, levels.Length);
                Assert.AreEqual(8, cooldowns.Length);
                Assert.NotNull(typeof(CombatPrototypeArena).GetMethod("DealDamage", PrivateInstance));
                Assert.NotNull(typeof(CombatPrototypeArena).GetMethod("FullRestore", PrivateInstance));
                Assert.NotNull(typeof(CombatPrototypeArena).GetMethod("TryAdvance", PrivateInstance));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Growth_ContainsOnlyTheFinalSupportingFields()
        {
            TypeAssert.IsSubclassOf(typeof(MonoBehaviour), typeof(GrowthExpansionPrototype));
            string[] requiredFields =
            {
                "critChanceLevel", "critDamageLevel", "goldGainLevel", "hpRegenLevel",
                "expGainLevel", "equipmentGainLevel"
            };
            foreach (string field in requiredFields)
                Assert.NotNull(typeof(GrowthExpansionPrototype).GetField(field, PrivateInstance), field);

            Assert.NotNull(typeof(GrowthExpansionPrototype).GetProperty("CriticalChance", PublicInstance));
            Assert.NotNull(typeof(GrowthExpansionPrototype).GetProperty("ExperienceMultiplier", PublicInstance));
            Assert.NotNull(typeof(GrowthExpansionPrototype).GetProperty("EquipmentMaterialMultiplier", PublicInstance));
            Assert.NotNull(typeof(GrowthExpansionPrototype).GetProperty("EquipmentEnhancementMaterials", PublicInstance));
        }

        [Test]
        public void SkillAuto_UsesOneGlobalControl()
        {
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("GlobalAutoEnabled", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("ToggleGlobalAuto", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("SetGlobalAuto", PublicInstance));
            Assert.IsTrue(typeof(GlobalSkillAutoGatePrototype).IsSubclassOf(typeof(MonoBehaviour)));
        }

        [Test]
        public void SwordGrades_AreRareEpicUniqueLegend()
        {
            Assert.AreEqual(1, (int)SwordProgressionPrototype.SwordRarity.Rare);
            Assert.AreEqual(2, (int)SwordProgressionPrototype.SwordRarity.Epic);
            Assert.AreEqual(3, (int)SwordProgressionPrototype.SwordRarity.Unique);
            Assert.AreEqual(4, (int)SwordProgressionPrototype.SwordRarity.Legend);
            Assert.AreEqual("레어", SwordProgressionPrototype.RarityName(SwordProgressionPrototype.SwordRarity.Rare));
            Assert.AreEqual("레전드", SwordProgressionPrototype.RarityName(SwordProgressionPrototype.SwordRarity.Legend));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("RegisterSummon", PublicInstance));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("ManualSynthesize", PublicInstance));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("GetDamageMultiplier", PublicInstance));
        }

        [Test]
        public void Canvas_UsesSixTabsTopStageSelectorAndGlobalAuto()
        {
            Assert.IsTrue(typeof(PeanutMobileCanvasPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("BottomMenuCount", PublicInstance));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("UsesSimplifiedGrowthMenu", PublicInstance));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("UsesGlobalSkillAuto", PublicInstance));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("HasTopStageSelector", PublicInstance));
            Assert.IsTrue(typeof(RuntimeWorldViewPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.IsTrue(typeof(BossPatternWorldViewPrototype).IsSubclassOf(typeof(MonoBehaviour)));
        }

        [Test]
        public void BossController_HasTimerButNoManualDodgeFields()
        {
            Assert.NotNull(typeof(BossPatternPrototype).GetProperty("RemainingTime", PublicInstance));
            Assert.NotNull(typeof(BossPatternPrototype).GetProperty("EncounterActive", PublicInstance));
            Assert.IsNull(typeof(BossPatternPrototype).GetField("warningTimer", PrivateInstance));
            Assert.IsNull(typeof(BossPatternPrototype).GetField("warningCenter", PrivateInstance));
            Assert.IsNull(typeof(BossPatternPrototype).GetField("warningRadius", PrivateInstance));
        }

        [Test]
        public void ProtectionRewardAndShopContractsRemainAvailable()
        {
            Assert.NotNull(typeof(PrototypeSaveIntegrityGuard).GetMethod("TryRestoreBackup", PublicInstance));
            Assert.NotNull(typeof(PrototypeSaveIntegrityGuard).GetProperty("SchemaVersion", PublicInstance));
            Assert.NotNull(typeof(FirstClearRewardPrototype).GetProperty("BossKills", PublicInstance));
            Assert.NotNull(typeof(FirstClearRewardPrototype).GetProperty("UniqueClears", PublicInstance));
            Assert.NotNull(typeof(CombatEffectWorldViewPrototype).GetProperty("ActiveEffectCount", PublicInstance));
            Assert.NotNull(typeof(PeanutCanvasLayoutGuard).GetProperty("RepairedBars", PublicInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("ClaimDailyReward", PrivateInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("SummonSword", PrivateInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("BuyEgg", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("StartIncubation", PrivateInstance));
        }
    }
}
#endif
