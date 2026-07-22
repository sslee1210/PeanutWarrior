using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Displays only the automatic boss timer. Warning zones were removed because
    /// the game is designed for unattended idle combat rather than manual dodging.
    /// </summary>
    [DefaultExecutionOrder(18000)]
    public sealed class BossPatternWorldViewPrototype : MonoBehaviour
    {
        private BossPatternPrototype pattern;
        private RuntimeWorldViewPrototype worldView;
        private GameObject root;
        private TextMesh timerLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BossPatternWorldViewPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBossPatternWorldViewPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<BossPatternWorldViewPrototype>();
        }

        private System.Collections.IEnumerator Start()
        {
            yield return null;
            pattern = FindFirstObjectByType<BossPatternPrototype>();
            worldView = FindFirstObjectByType<RuntimeWorldViewPrototype>();
            if (pattern == null || worldView == null)
            {
                enabled = false;
                yield break;
            }

            GameObject worldRoot = typeof(RuntimeWorldViewPrototype)
                .GetField("worldRoot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(worldView) as GameObject;

            root = new GameObject("Boss Auto Timer");
            root.transform.SetParent(worldRoot != null ? worldRoot.transform : transform, false);
            root.transform.localPosition = new Vector3(0f, 4.55f, 0f);

            timerLabel = root.AddComponent<TextMesh>();
            timerLabel.anchor = TextAnchor.MiddleCenter;
            timerLabel.alignment = TextAlignment.Center;
            timerLabel.fontSize = 44;
            timerLabel.characterSize = 0.075f;
            timerLabel.color = new Color(1f, 0.93f, 0.52f);
            timerLabel.GetComponent<MeshRenderer>().sortingOrder = 25;
            root.SetActive(false);
        }

        private void LateUpdate()
        {
            if (root == null || pattern == null) return;
            root.SetActive(pattern.EncounterActive);
            if (!pattern.EncounterActive) return;

            timerLabel.text = $"보스 AUTO · {Mathf.CeilToInt(pattern.RemainingTime)}초\n{pattern.PatternName}";
            timerLabel.color = pattern.IsEnraged
                ? new Color(1f, 0.42f, 0.22f)
                : new Color(1f, 0.93f, 0.52f);
        }
    }
}
