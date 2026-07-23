using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(36600)]
    public sealed class SkillEvolutionDetailLayoutPatchPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public bool ShowsCurrentEvolutionStructure => true;
        public bool ShowsNextAdvancementChanges => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SkillEvolutionDetailLayoutPatchPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorSkillEvolutionDetailLayoutPatch");
            DontDestroyOnLoad(go);
            go.AddComponent<SkillEvolutionDetailLayoutPatchPrototype>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 60; frame++)
            {
                PeanutSkillMenuV6 menu = FindFirstObjectByType<PeanutSkillMenuV6>();
                if (menu != null && Apply(menu)) yield break;
                yield return null;
            }
            enabled = false;
        }

        private static bool Apply(PeanutSkillMenuV6 menu)
        {
            Text stats = typeof(PeanutSkillMenuV6).GetField("detailStats", PrivateInstance)?.GetValue(menu) as Text;
            Text cost = typeof(PeanutSkillMenuV6).GetField("detailCost", PrivateInstance)?.GetValue(menu) as Text;
            Button upgrade = typeof(PeanutSkillMenuV6).GetField("detailUpgradeButton", PrivateInstance)?.GetValue(menu) as Button;
            if (stats == null || cost == null || upgrade == null) return false;

            RectTransform statsRect = stats.rectTransform;
            statsRect.anchoredPosition = new Vector2(34f, -304f);
            statsRect.sizeDelta = new Vector2(712f, 116f);
            stats.fontSize = 15;
            stats.lineSpacing = 0.92f;
            stats.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform costRect = cost.rectTransform;
            costRect.anchoredPosition = new Vector2(34f, -424f);
            costRect.sizeDelta = new Vector2(430f, 58f);

            RectTransform buttonRect = upgrade.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(500f, -426f);
            buttonRect.sizeDelta = new Vector2(246f, 58f);
            return true;
        }
    }
}
