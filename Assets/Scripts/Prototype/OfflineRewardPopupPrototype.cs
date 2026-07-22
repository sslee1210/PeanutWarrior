using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(31500)]
    public sealed class OfflineRewardPopupPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string ShownTicksKey = "PeanutWarrior.OfflinePopup.LastShownTicks";

        private PeanutMobileCanvasPrototype mobileUi;
        private IdleSystemsPrototype idle;
        private OfflineProgressRewardPrototype progressReward;
        private OfflineCombatRewardCorrectionPrototype combatCorrection;
        private GameObject popup;
        private Font font;
        private Sprite sprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<OfflineRewardPopupPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorOfflineRewardPopup");
            DontDestroyOnLoad(root);
            root.AddComponent<OfflineRewardPopupPrototype>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 14; i++) yield return null;
            mobileUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            idle = FindFirstObjectByType<IdleSystemsPrototype>();
            progressReward = FindFirstObjectByType<OfflineProgressRewardPrototype>();
            combatCorrection = FindFirstObjectByType<OfflineCombatRewardCorrectionPrototype>();
            if (mobileUi == null) yield break;

            string summary = BuildSummary();
            if (string.IsNullOrWhiteSpace(summary)) yield break;

            long sessionTicks = DateTime.UtcNow.Date.Ticks + DateTime.UtcNow.Hour;
            if (long.TryParse(PlayerPrefs.GetString(ShownTicksKey, "0"), out long shown) && shown == sessionTicks)
                yield break;
            PlayerPrefs.SetString(ShownTicksKey, sessionTicks.ToString());
            PlayerPrefs.Save();
            BuildPopup(summary);
        }

        private string BuildSummary()
        {
            string idleMessage = ReadIdleMessage();
            string progressMessage = progressReward == null ? string.Empty : progressReward.LastMessage;
            string correctionMessage = combatCorrection == null ? string.Empty : combatCorrection.LastMessage;

            bool hasIdle = ContainsOfflineReward(idleMessage);
            bool hasProgress = ContainsOfflineReward(progressMessage);
            bool hasCorrection = ContainsOfflineReward(correctionMessage);
            if (!hasIdle && !hasProgress && !hasCorrection) return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (hasIdle) builder.AppendLine(idleMessage);
            if (hasCorrection && correctionMessage != idleMessage) builder.AppendLine(correctionMessage);
            if (hasProgress) builder.AppendLine(progressMessage);
            return builder.ToString().Trim();
        }

        private string ReadIdleMessage()
        {
            if (idle == null) return string.Empty;
            FieldInfo field = typeof(IdleSystemsPrototype).GetField("systemMessage", PrivateInstance);
            return field?.GetValue(idle) as string ?? string.Empty;
        }

        private static bool ContainsOfflineReward(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.Contains("오프라인") || message.Contains("방치");
        }

        private void BuildPopup(string summary)
        {
            Canvas canvas = mobileUi.GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            sprite = SolidSprite();

            popup = new GameObject("Offline Reward Modal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rootRect = popup.GetComponent<RectTransform>();
            rootRect.SetParent(canvas.transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            Image dim = popup.GetComponent<Image>();
            dim.sprite = sprite;
            dim.color = new Color(0.02f, 0.05f, 0.02f, 0.72f);

            GameObject card = CreatePanel(popup.transform, "Reward Card", 0.5f, 0.5f, 650f, 400f,
                new Color(0.98f, 0.95f, 0.82f, 1f));
            CreateText(card.transform, "방치 보상", 30f, 24f, 590f, 58f, 28,
                new Color(0.07f, 0.25f, 0.11f), TextAnchor.MiddleCenter, FontStyle.Bold);
            CreateText(card.transform, "자리를 비운 동안 땅콩전사와 펫이 자동으로 사냥했습니다.",
                36f, 88f, 578f, 42f, 16, new Color(0.28f, 0.19f, 0.10f), TextAnchor.MiddleCenter, FontStyle.Normal);
            CreateText(card.transform, summary, 44f, 144f, 562f, 142f, 18,
                new Color(0.18f, 0.11f, 0.05f), TextAnchor.MiddleCenter, FontStyle.Bold);

            GameObject buttonObject = CreatePanel(card.transform, "Confirm", 0.5f, 0.86f, 400f, 62f,
                new Color(0.16f, 0.42f, 0.22f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(Close);
            CreateText(buttonObject.transform, "보상 확인", 0f, 0f, 400f, 62f, 18, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold, true);
            popup.transform.SetAsLastSibling();
        }

        private GameObject CreatePanel(Transform parent, string name, float anchorX, float anchorY, float width, float height, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(anchorX, anchorY);
            rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
            Image image = root.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return root;
        }

        private Text CreateText(Transform parent, string value, float x, float y, float width, float height,
            int size, Color color, TextAnchor anchor, FontStyle style, bool stretch = false)
        {
            GameObject root = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = stretch ? Vector2.zero : new Vector2(0f, 1f);
            rect.anchorMax = stretch ? Vector2.one : new Vector2(0f, 1f);
            rect.pivot = stretch ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 1f);
            if (stretch)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchoredPosition = new Vector2(x, -y);
                rect.sizeDelta = new Vector2(width, height);
            }
            Text text = root.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private void Close()
        {
            if (popup != null) Destroy(popup);
        }

        private static Sprite SolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
