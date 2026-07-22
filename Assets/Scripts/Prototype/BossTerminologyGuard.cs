using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Keeps the user-facing Korean terminology consistent while older prototype
    /// strings are gradually removed. Internal class and event names remain Boss*.
    /// </summary>
    [DefaultExecutionOrder(32700)]
    public sealed class BossTerminologyGuard : MonoBehaviour
    {
        private const string OldTerm = "균왕";
        private const string NewTerm = "보스";
        private float refreshTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BossTerminologyGuard>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBossTerminologyGuard");
            DontDestroyOnLoad(root);
            root.AddComponent<BossTerminologyGuard>();
        }

        private void LateUpdate()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.1f;

            Text[] uiTexts = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < uiTexts.Length; i++)
            {
                Text label = uiTexts[i];
                if (label == null || string.IsNullOrEmpty(label.text) || !label.text.Contains(OldTerm)) continue;
                label.text = label.text.Replace(OldTerm, NewTerm);
            }

            TextMesh[] worldTexts = FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < worldTexts.Length; i++)
            {
                TextMesh label = worldTexts[i];
                if (label == null || string.IsNullOrEmpty(label.text) || !label.text.Contains(OldTerm)) continue;
                label.text = label.text.Replace(OldTerm, NewTerm);
            }
        }
    }
}
