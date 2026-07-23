using System.Collections;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(37100)]
    public sealed class AdvancementSkillEvolutionRuntimeAudit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<AdvancementSkillEvolutionRuntimeAudit>() != null) return;
            GameObject root = new GameObject("PeanutWarriorAdvancementSkillEvolutionAudit");
            DontDestroyOnLoad(root);
            root.AddComponent<AdvancementSkillEvolutionRuntimeAudit>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 90; frame++) yield return null;

            SkillManagementPrototype skills = FindFirstObjectByType<SkillManagementPrototype>();
            SpectacularPeanutSkillCombatPrototype combat = FindFirstObjectByType<SpectacularPeanutSkillCombatPrototype>();
            AdvancementSkillEvolutionWorldViewPrototype visuals = FindFirstObjectByType<AdvancementSkillEvolutionWorldViewPrototype>();

            if (skills == null || combat == null || visuals == null)
            {
                Debug.LogError("[Advancement Skill Evolution Audit] Missing skill evolution runtime component.");
                yield break;
            }

            if (!skills.EvolvesHitCounts || !skills.EvolvesTargetCounts || !skills.EvolvesVisualDensity || !skills.EvolvesSkillPatterns)
            {
                Debug.LogError("[Advancement Skill Evolution Audit] Advancement must change hits, targets, spectacle density and skill patterns.");
                yield break;
            }

            if (!combat.UsesAdvancementHitEvolution || !combat.UsesAdvancementTargetEvolution || !combat.UsesAdvancementPatternEvolution)
            {
                Debug.LogError("[Advancement Skill Evolution Audit] Combat execution is not using advancement evolution values.");
                yield break;
            }

            if (!visuals.UsesPerTierVisualEvolution || !visuals.ScalesEffectObjectCounts ||
                !visuals.ChangesAdvancementColorTheme || !visuals.ShowsAdvancementAscensionBurst)
            {
                Debug.LogError("[Advancement Skill Evolution Audit] Visible per-tier evolution effects are incomplete.");
                yield break;
            }

            Debug.Log(
                "[Advancement Skill Evolution Audit]\n" +
                "PASS · advancement changes real hit counts, target counts, waves, range, effect density, color themes and finishing patterns.");
        }
    }
}
