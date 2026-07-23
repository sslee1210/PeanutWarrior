using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(40000)]
    public sealed class BottomNavigationOrderV4 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] VisualOrder = { 2, 1, 0, 4, 3, 5 };
        private static readonly string[] Labels = { "성장", "장비", "스킬", "펫", "전직", "상점" };
        private static readonly string[] PageNames = { "Growth", "Equipment", "Skills", "Pets", "Advancement", "Shop" };

        private PeanutMobileCanvasPrototype ui;
        private Image[] backgrounds;
        private FieldInfo currentPageField;

        public string BottomMenuOrder => "성장 → 장비 → 스킬 → 펫 → 전직 → 상점";
        public bool AppliesFinalNavigationStateEveryFrame => true;

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
            for (int i = 0; i < 24; i++)
            {
                ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
                if (ui != null)
                {
                    FieldInfo backgroundsField = typeof(PeanutMobileCanvasPrototype).GetField("navBackgrounds", PrivateInstance);
                    backgrounds = backgroundsField?.GetValue(ui) as Image[];
                    currentPageField = typeof(PeanutMobileCanvasPrototype).GetField("currentPage", PrivateInstance);
                    if (backgrounds != null && backgrounds.Length >= 6) break;
                }
                yield return null;
            }
            Apply();
        }

        private void LateUpdate()
        {
            if (ui == null || backgrounds == null || backgrounds.Length < 6)
            {
                TryRebind();
                return;
            }
            Apply();
        }

        private void TryRebind()
        {
            ui = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (ui == null) return;
            FieldInfo backgroundsField = typeof(PeanutMobileCanvasPrototype).GetField("navBackgrounds", PrivateInstance);
            backgrounds = backgroundsField?.GetValue(ui) as Image[];
            currentPageField = typeof(PeanutMobileCanvasPrototype).GetField("currentPage", PrivateInstance);
        }

        private void Apply()
        {
            if (ui == null || backgrounds == null || backgrounds.Length < 6) return;
            string currentPage = currentPageField?.GetValue(ui)?.ToString() ?? "Main";

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
                    Text label = texts[t];
                    if (label == null) continue;
                    string value = label.text ?? string.Empty;
                    if (value.Contains("SKILL") || value == "스킬" || value == "장비" || value == "성장" ||
                        value == "전직" || value == "펫" || value == "상점")
                        label.text = Labels[visualIndex];
                }

                bool active = currentPage == PageNames[visualIndex];
                background.color = active
                    ? new Color(0.96f, 0.65f, 0.13f, 1f)
                    : new Color(0.16f, 0.42f, 0.22f, 0.98f);
                SetActiveLine(background.transform, active);
            }
        }

        private static void SetActiveLine(Transform parent, bool active)
        {
            Transform existing = parent.Find("Navigation Active Line");
            GameObject line;
            if (existing != null)
            {
                line = existing.gameObject;
            }
            else
            {
                line = new GameObject("Navigation Active Line", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = line.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.08f, 0f);
                rect.anchorMax = new Vector2(0.92f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 2f);
                rect.sizeDelta = new Vector2(0f, 4f);
                Image image = line.GetComponent<Image>();
                image.color = Color.white;
                image.raycastTarget = false;
            }
            line.SetActive(active);
            line.transform.SetAsLastSibling();
        }
    }
}
