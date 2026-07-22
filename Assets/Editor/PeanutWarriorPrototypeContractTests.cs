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
        public void SwordProgression_ExposesStablePublicContracts()
        {
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("RegisterSummon", PublicInstance));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("ManualSynthesize", PublicInstance));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("UpgradeSword", PublicInstance));
            Assert.NotNull(typeof(SwordProgressionPrototype).GetMethod("GetDamageMultiplier", PublicInstance));
        }

        [Test]
        public void Canvas_UsesSimplifiedDevelopmentMenuContract()
        {
            Assert.IsTrue(typeof(PeanutMobileCanvasPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("BottomMenuCount", PublicInstance));
            Assert.NotNull(typeof(PeanutMobileCanvasPrototype).GetProperty("UsesSimplifiedGrowthMenu", PublicInstance));
            Assert.IsTrue(typeof(RuntimeWorldViewPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.IsTrue(typeof(BossPatternWorldViewPrototype).IsSubclassOf(typeof(MonoBehaviour)));
            Assert.IsTrue(typeof(WorldThemePrototype).IsSubclassOf(typeof(MonoBehaviour)));
        }

        [Test]
        public void ShopAndIdlePrivateActions_MatchUiBindings()
        {
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("ClaimDailyReward", PrivateInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("SummonSword", PrivateInstance));
            Assert.NotNull(typeof(PrototypeShopAndDaily).GetMethod("BuyEgg", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("StartIncubation", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("ClaimKillMission", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("ClaimStageMission", PrivateInstance));
            Assert.NotNull(typeof(IdleSystemsPrototype).GetMethod("ClaimGrowthAchievement", PrivateInstance));
        }
    }
}
#endif
