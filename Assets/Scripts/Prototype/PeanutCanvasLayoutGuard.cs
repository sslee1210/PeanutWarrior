using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Normalizes runtime-created uGUI geometry after layout initialization. This keeps
    /// HP, MP and stage bars at a consistent height across aspect ratios and prevents
    /// keyboard navigation from jumping to hidden menu buttons.
    /// </summary>
    [DefaultExecutionOrder(27000)]
    public sealed class PeanutCanvasLayoutGuard : MonoBehaviour
    {
        private int repairedBars;
        private int normalizedButtons;

        public int RepairedBars => repairedBars;
        public int NormalizedButtons => normalizedButtons;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutCanvasLayoutGuard>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCanvasLayoutGuard");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutCanvasLayoutGuard>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            Repair();
        }

        private void Repair()
        {
            PeanutMobileCanvasPrototype ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (ui == null) return;

            RectTransform[] rects = ui.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || rect.name != "Fill") continue;
                float width = Mathf.Max(0f, rect.sizeDelta.x);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(2f, 0f);
                rect.sizeDelta = new Vector2(width, -4f);
                repairedBars++;
            }

            Button[] buttons = ui.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                Navigation navigation = buttons[i].navigation;
                navigation.mode = Navigation.Mode.None;
                buttons[i].navigation = navigation;
                normalizedButtons++;
            }
        }
    }
}
