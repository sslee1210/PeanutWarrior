using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(30500)]
    public sealed class BottomNavigationOrderV4 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] VisualOrder = { 2, 1, 0, 4, 3, 5 };
        private static readonly string[] Labels = { "성장", "장비", "스킬", "펫", "전직", "상점" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BottomNavigationOrderV4>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBottomNavigationOrderV4");
            DontDestroyOnLoad(root);
            root.AddComponent<BottomNavigationOrderV4>();
        }

        private IEnumerator Start()
        {
            PeanutMobileCanvasPrototype ui = null;
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
                if (ui != null) break;
            }
            if (ui == null) yield break;

            FieldInfo backgroundsField = typeof(PeanutMobileCanvasPrototype).GetField("navBackgrounds", PrivateInstance);
            Image[] backgrounds = backgroundsField?.GetValue(ui) as Image[];
            if (backgrounds == null || backgrounds.Length < 6) yield break;

            const float startX = 6f;
            const float spacing = 188f;
            for (int visualIndex = 0; visualIndex < VisualOrder.Length; visualIndex++)
            {
                int sourceIndex = VisualOrder[visualIndex];
                Image background = backgrounds[sourceIndex];
                if (background == null) continue;
                RectTransform rect = background.rectTransform;
                Vector2 position = rect.anchoredPosition;
                position.x = startX + visualIndex * spacing;
                rect.anchoredPosition = position;

                Text[] texts = background.GetComponentsInChildren<Text>(true);
                for (int t = 0; t < texts.Length; t++)
                {
                    if (texts[t] == null) continue;
                    string value = texts[t].text ?? string.Empty;
                    if (value.Contains("SKILL") || value == "스킬" || value == "장비" ||
                        value == "성장" || value == "전직" || value == "펫" || value == "상점")
                        texts[t].text = Labels[visualIndex];
                }
            }
        }
    }
}
