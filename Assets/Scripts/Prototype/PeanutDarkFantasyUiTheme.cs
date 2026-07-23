using System.Collections;
using PeanutWarrior.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Applies the dark wood and gold mobile-RPG presentation used by the concept
    /// references to the runtime-generated Canvas. It also replaces the generic
    /// active-skill icons with the illustrated eight-skill sprite set.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class PeanutDarkFantasyUiTheme : MonoBehaviour
    {
        private readonly Color panel = new Color(0.105f, 0.060f, 0.025f, 0.96f);
        private readonly Color panelRaised = new Color(0.17f, 0.095f, 0.035f, 0.98f);
        private readonly Color panelSoft = new Color(0.23f, 0.135f, 0.055f, 0.96f);
        private readonly Color goldEdge = new Color(1f, 0.72f, 0.17f, 0.72f);
        private readonly Color cream = new Color(1f, 0.91f, 0.69f, 1f);
        private readonly Color mutedCream = new Color(0.86f, 0.75f, 0.55f, 1f);
        private readonly Color darkBack = new Color(0.035f, 0.022f, 0.014f, 0.96f);

        private PeanutMobileCanvasPrototype mobileUi;
        private ProceduralBattleArtPrototype rasterArt;
        private StageFlowController stageFlow;
        private Canvas runtimeCanvas;
        private float refreshTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<PeanutDarkFantasyUiTheme>() != null) return;
            GameObject host = new GameObject("Peanut Dark Fantasy UI Theme");
            DontDestroyOnLoad(host);
            host.AddComponent<PeanutDarkFantasyUiTheme>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 180; frame++)
            {
                mobileUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
                rasterArt = FindFirstObjectByType<ProceduralBattleArtPrototype>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                runtimeCanvas = mobileUi == null ? null : mobileUi.GetComponentInChildren<Canvas>(true);
                if (runtimeCanvas != null && rasterArt != null && rasterArt.ArtReady) break;
                yield return null;
            }

            if (runtimeCanvas == null || rasterArt == null || !rasterArt.ArtReady)
            {
                Debug.LogError("[PeanutUI] Runtime Canvas or raster art was not ready for theming.");
                enabled = false;
                yield break;
            }

            ApplyTheme();
            Debug.Log("[PeanutUI] Dark wood and gold illustrated UI theme applied.");
        }

        private void Update()
        {
            if (runtimeCanvas == null) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.22f;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            Image[] images = runtimeCanvas.GetComponentsInChildren<Image>(true);
            for (int index = 0; index < images.Length; index++) StyleImage(images[index]);

            Text[] texts = runtimeCanvas.GetComponentsInChildren<Text>(true);
            for (int index = 0; index < texts.Length; index++) StyleText(texts[index]);

            Button[] buttons = runtimeCanvas.GetComponentsInChildren<Button>(true);
            for (int index = 0; index < buttons.Length; index++) StyleButton(buttons[index]);

            ApplyIllustratedIcons();
        }

        private void StyleImage(Image image)
        {
            if (image == null) return;
            string name = image.gameObject.name;
            if (name == "Fill" || name == "Icon") return;

            if (name == "Bar Back")
            {
                image.color = new Color(0.025f, 0.018f, 0.012f, 0.94f);
                AddOutline(image, new Color(0.55f, 0.34f, 0.10f, 0.75f), 1f);
                return;
            }

            if (name == "Header" || name == "Bottom Navigation")
            {
                image.color = darkBack;
                AddOutline(image, goldEdge, 2f);
                return;
            }

            if (name == "Button")
            {
                Color original = image.color;
                if (original.r > 0.72f && original.g < 0.42f)
                    image.color = new Color(0.55f, 0.10f, 0.055f, 1f);
                else if (original.r > 0.70f && original.g > 0.42f)
                    image.color = new Color(0.70f, 0.36f, 0.025f, 1f);
                else
                    image.color = panelSoft;
                AddOutline(image, goldEdge, 1.5f);
                return;
            }

            if (name == "Menu Page")
            {
                image.color = new Color(0.025f, 0.015f, 0.008f, 0.97f);
                return;
            }

            if (name == "Player" || name == "Resources" || name == "Stage" ||
                name == "Status" || name == "Active Skills" || name == "Growth Summary" ||
                name == "Toast")
            {
                image.color = panel;
                AddOutline(image, goldEdge, 2f);
                return;
            }

            if (name == "Skill Slot" || name == "Row")
            {
                image.color = name == "Skill Slot" ? panelSoft : panelRaised;
                AddOutline(image, new Color(goldEdge.r, goldEdge.g, goldEdge.b, 0.5f), 1f);
                return;
            }

            RectTransform rect = image.rectTransform;
            float area = Mathf.Abs(rect.rect.width * rect.rect.height);
            if (area > 18000f && image.color.a > 0.5f)
            {
                image.color = panelRaised;
                AddOutline(image, new Color(goldEdge.r, goldEdge.g, goldEdge.b, 0.42f), 1f);
            }
        }

        private void StyleText(Text text)
        {
            if (text == null) return;
            Transform parent = text.transform.parent;
            bool barText = parent != null && parent.name == "Bar Back";
            if (!barText)
            {
                Color current = text.color;
                bool semantic =
                    current.r > 0.75f && current.g < 0.45f ||
                    current.b > 0.72f && current.r < 0.55f ||
                    current.g > 0.65f && current.r < 0.55f;
                text.color = semantic ? current :
                    (text.fontStyle == FontStyle.Bold ? cream : mutedCream);
            }

            Shadow shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.82f);
            shadow.effectDistance = new Vector2(1.2f, -1.2f);
            shadow.useGraphicAlpha = true;
        }

        private void StyleButton(Button button)
        {
            if (button == null) return;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.86f, 0.54f, 1f);
            colors.pressedColor = new Color(0.84f, 0.52f, 0.14f, 1f);
            colors.selectedColor = new Color(1f, 0.74f, 0.25f, 1f);
            colors.disabledColor = new Color(0.32f, 0.30f, 0.28f, 0.72f);
            button.colors = colors;
        }

        private void ApplyIllustratedIcons()
        {
            Transform mainHud = runtimeCanvas.transform.Find("Safe Area/Main HUD");
            if (mainHud == null) return;

            Transform playerPanel = mainHud.Find("Player");
            if (playerPanel != null)
            {
                Transform icon = playerPanel.Find("Icon");
                Image image = icon == null ? null : icon.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = rasterArt.GetUnitSprite(0);
                    image.color = Color.white;
                    image.preserveAspect = true;
                }
            }

            Transform dock = mainHud.Find("Active Skills");
            if (dock == null) return;
            int offset = stageFlow != null && stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            int slot = 0;
            for (int childIndex = 0; childIndex < dock.childCount; childIndex++)
            {
                Transform child = dock.GetChild(childIndex);
                if (child.name != "Skill Slot") continue;
                Transform icon = child.Find("Icon");
                Image image = icon == null ? null : icon.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = rasterArt.GetEffectSprite(offset + Mathf.Clamp(slot, 0, 3));
                    image.color = Color.white;
                    image.preserveAspect = true;
                    RectTransform rect = image.rectTransform;
                    rect.sizeDelta = new Vector2(58f, 58f);
                    rect.anchoredPosition = new Vector2(20f, -5f);
                }
                slot++;
            }
        }

        private static void AddOutline(Image image, Color color, float distance)
        {
            Outline outline = image.GetComponent<Outline>();
            if (outline == null) outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }
    }
}
