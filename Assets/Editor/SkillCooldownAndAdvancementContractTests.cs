#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using PeanutWarrior.Prototype;

namespace PeanutWarrior.Tests
{
    public sealed class SkillCooldownAndAdvancementContractTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Auto_CastsByCooldownCompletionAndAllowsOpeningOverlap()
        {
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("UsesCooldownCompletionOrder", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("AllowsOpeningSkillOverlap", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetProperty("WaitingSkillCount", PublicInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetMethod("Update", PrivateInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetMethod("LateUpdate", PrivateInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetMethod("CastQueuedSkill", PrivateInstance));
            Assert.NotNull(typeof(GlobalSkillAutoGatePrototype).GetMethod("FindTarget", BindingFlags.Static | BindingFlags.NonPublic));
        }

        [Test]
        public void Advancement_EvolvesActualSkillDamageCooldownAndMp()
        {
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("UsesAdvancementSkillEvolution", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("CurrentAdvancementTier", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("CurrentAdvancementName", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillAdvancementDamageBonus", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillAdvancementCooldownReduction", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillAdvancementMpReduction", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillAdvancementSummary", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillDamageMultiplier", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillBaseCooldown", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillMpCost", PublicInstance));
        }

        [Test]
        public void Advancement_ChangesVisibleHitTargetWaveAndRangeStructure()
        {
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("EvolvesHitCounts", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("EvolvesTargetCounts", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("EvolvesVisualDensity", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("EvolvesSkillPatterns", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetProperty("CurrentEvolutionRank", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillHitCount", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillTargetCount", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillWaveCount", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillVisualObjectCount", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillRangeMultiplier", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetSkillEvolutionSummary", PublicInstance));
            Assert.NotNull(typeof(SkillManagementPrototype).GetMethod("GetNextAdvancementEvolutionSummary", PublicInstance));

            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("UsesAdvancementHitEvolution", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("UsesAdvancementTargetEvolution", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetProperty("UsesAdvancementPatternEvolution", PublicInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteShellCyclone", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteFallingFlowerSwordRain", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteLeylinePodFormation", PrivateInstance));
            Assert.NotNull(typeof(SpectacularPeanutSkillCombatPrototype).GetMethod("ExecuteGoldenCoreHeavenSever", PrivateInstance));
        }

        [Test]
        public void Advancement_AddsPerTierSkillEffectsAndAscensionBurst()
        {
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetProperty("UsesPerTierVisualEvolution", PublicInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetProperty("ScalesEffectObjectCounts", PublicInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetProperty("ChangesAdvancementColorTheme", PublicInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetProperty("ShowsAdvancementAscensionBurst", PublicInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetMethod("PlayEvolutionOverlay", PrivateInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetMethod("PlayAdvancementAscension", PrivateInstance));
            Assert.NotNull(typeof(AdvancementSkillEvolutionWorldViewPrototype).GetMethod("AdvancementColor", PrivateInstance));
        }
    }
}
#endif
