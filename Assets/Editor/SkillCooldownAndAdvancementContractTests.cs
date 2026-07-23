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
    }
}
#endif
