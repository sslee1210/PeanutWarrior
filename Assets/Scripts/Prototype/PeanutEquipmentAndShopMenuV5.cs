using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(33500)]
    public sealed class PeanutEquipmentAndShopMenuV5 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Width = 1388f;
        private const float Height = 650f;

        private PeanutMobileCanvasPrototype sourceUi;
        private PeanutMenuLayoutV2 layoutV2;
        private PeanutCoreMenuCompletionV3 layoutV3;
        private PeanutMenuLayoutV4 layoutV4;
        private ElementEquipmentCatalogPrototype equipment;
        private GrowthExpansionPrototype growth;
        private PrototypeShopAndDaily shop;
        private CoreShopProgressionPrototype coreShop;
        private PeanutSaveGameService saveService;

        private FieldInfo currentPageField;
        private FieldInfo contentHostField;
        private FieldInfo menuTitleField;
        private MethodInfo toastMethod;
        private GameObject contentHost;
        private Text menuTitle;

        private Font font;
        private Sprite solidSprite;
        private Sprite roundedSprite;
        private GameObject root;
        private string activePage = string.Empty;
        private bool bossCatalog;
        private int elementTab;
        private float refreshTimer;
        private readonly List<Action> refreshers = new List<Action>();

        private readonly Color background = new Color(0.94f, 0.96f, 0.89f, 1f);
        private readonly Color cream = new Color(0.98f, 0.95f, 0.82f, 1f);
        private readonly Color card = new Color(0.97f, 0.94f, 0.85f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color darkGreen = new Color(0.06f, 0.23f, 0.10f, 1f);
        private readonly Color brown = new Color(0.20f, 0.12f, 0.06f, 1f);
        private readonly Color gold = new Color(0.94f, 0.61f, 0.10f, 1f);
        private readonly Color red = new Color(0.82f, 0.20f, 0.15f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.82f, 1f);
        private readonly Color muted = new Color(0.52f, 0.55f, 0.48f, 1f);
        private readonly Color line = new Color(0.56f, 0.66f, 0.52f, 0.48f);

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };
        private static readonly Color[] ElementColors =
        {
            new Color(0.74f, 0.58f, 0.18f),
            new Color(0.90f, 0.27f, 0.14f),
            new Color(0.18f, 0.56f, 0.84f),
            new Color(0.54f, 0.31f, 0.84f)
        };

        public int EquipmentCatalogCount => 2;
        public int ElementsPerCatalog => 4;
        public int ItemsPerCatalog => equipment == null ? 48 : equipment.ItemCountPerUse;
        public bool UsesSeparateHuntingAndBossTabs => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>() != null) return;
            GameObject root = new GameObject("PeanutWarriorEquipmentAndShopMenuV5");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutEquipmentAndShopMenuV5>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 16; i++)
            {
                yield return null;
                if (Bind()) break;
            }
            if (sourceUi == null || contentHost == null)
            {
                enabled = false;
                yield break;
            }
            CreateAssets();
        }

        private bool Bind()
        {
            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            layoutV2 = FindFirstObjectByType<PeanutMenuLayoutV2>();
            layoutV3 = FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            layoutV4 = FindFirstObjectByType<PeanutMenuLayoutV4>();
            equipment = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            coreShop = FindFirstObjectByType<CoreShopProgressionPrototype>();
            saveService = FindFirstObjectByType<PeanutSaveGameService>();
            if (sourceUi == null) return false;

            Type uiType = typeof(PeanutMobileCanvasPrototype);
            currentPageField = uiType.GetField("currentPage", PrivateInstance);
            contentHostField = uiType.GetField("contentHost", PrivateInstance);
            menuTitleField = uiType.GetField("menuTitle", PrivateInstance);
            toastMethod = uiType.GetMethod("Toast", PrivateInstance);
            contentHost = contentHostField?.GetValue(sourceUi) as GameObject;
            menuTitle = menuTitleField?.GetValue(sourceUi) as Text;
            return contentHost != null;
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            solidSprite = CreateSolidSprite();
            roundedSprite = CreateRoundedSprite();
        }

        private void LateUpdate()
        {
            if (sourceUi == null || contentHost == null) return;
            string page = CurrentPage;
            bool managed = page == "Equipment" || page == "Shop";

            if (managed)
            {
                if (layoutV2 != null) layoutV2.enabled = false;
                if (layoutV3 != null) layoutV3.enabled = false;
                if (layoutV4 != null) layoutV4.enabled = false;

                if (page != activePage || root == null || root.transform.parent != contentHost.transform)
                {
                    activePage = page;
                    BuildPage(page);
                }

                refreshTimer -= Time.unscaledDeltaTime;
                if (refreshTimer <= 0f)
                {
                    refreshTimer = 0.18f;
                    for (int i = 0; i < refreshers.Count; i++)
                    {
                        try { refreshers[i]?.Invoke(); }
                        catch (MissingReferenceException) { }
                    }
                }
                return;
            }

            if (activePage == "Equipment" || activePage == "Shop")
            {
                activePage = page;
                if (layoutV4 != null) layoutV4.enabled = true;
            }
        }

        private string CurrentPage
        {
            get
            {
                object value = currentPageField?.GetValue(sourceUi);
                return value == null ? "Main" : value.ToString();
            }
        }

        private void BuildPage(string page)
        {
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            root = Rect(contentHost.transform, "Peanut Equipment Shop V5", 0f, 0f, Width, Height).gameObject;
            Image backgroundImage = root.AddComponent<Image>();
            backgroundImage.sprite = solidSprite;
            backgroundImage.color = background;
            backgroundImage.raycastTarget = false;

            if (menuTitle != null) menuTitle.text = page == "Equipment" ? "장비" : "상점";
            if (page == "Equipment") BuildEquipment();
            else BuildShop();

            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
        }

        private void BuildEquipment()
        {
            Label(root.transform, "검 장비 도감", 20f, 7f, 260f, 44f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text material = Label(root.transform, string.Empty, 1000f, 7f, 368f, 44f, 15, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() => material.text = $"장비 강화 재료 {(growth == null ? 0 : growth.EquipmentEnhancementMaterials):N0}");

            FlatButton(root.transform, "사냥용 검", 20f, 52f, 662f, 50f,
                bossCatalog ? cream : green, bossCatalog ? darkGreen : Color.white,
                () => { bossCatalog = false; BuildPage("Equipment"); });
            FlatButton(root.transform, "보스용 검", 706f, 52f, 662f, 50f,
                bossCatalog ? red : cream, bossCatalog ? Color.white : darkGreen,
                () => { bossCatalog = true; BuildPage("Equipment"); });

            for (int i = 0; i < ElementNames.Length; i++)
            {
                int captured = i;
                FlatButton(root.transform, ElementNames[i], 20f + i * 337f, 112f, 324f, 48f,
                    elementTab == i ? ElementColors[i] : cream,
                    elementTab == i ? Color.white : darkGreen,
                    () => { elementTab = captured; BuildPage("Equipment"); });
            }

            Text equipped = Label(root.transform, string.Empty, 20f, 164f, 1020f, 34f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button summon = FlatButton(root.transform, string.Empty, 1080f, 164f, 288f, 34f,
                bossCatalog ? red : green, Color.white, SummonCurrentCatalog);
            Text summonText = summon.GetComponentInChildren<Text>();
            refreshers.Add(() =>
            {
                equipped.text = $"{(bossCatalog ? "보스용" : "사냥용")} 현재 장착 · {EquippedName(bossCatalog)}";
                summonText.text = $"{(bossCatalog ? "보스용" : "사냥용")} 검 소환 · 5 다이아";
            });

            for (int rarity = 1; rarity <= 4; rarity++) BuildRarityRow(rarity);
        }

        private void BuildRarityRow(int rarity)
        {
            float y = 204f + (rarity - 1) * 106f;
            Color rarityColor = RarityColor(rarity);
            Label(root.transform, equipment == null ? "등급" : equipment.RarityName(rarity),
                20f, y + 16f, 112f, 62f, 18, rarityColor, TextAnchor.MiddleCenter, FontStyle.Bold);

            for (int variant = 0; variant < 3; variant++)
            {
                int itemId = equipment == null ? -1 : equipment.GetItemId(bossCatalog, elementTab, rarity, variant);
                float x = 142f + variant * 408f;
                BuildEquipmentItem(itemId, x, y, rarityColor);
            }
        }

        private void BuildEquipmentItem(int itemId, float x, float y, Color accent)
        {
            GameObject item = Panel(root.transform, "Equipment " + itemId, x, y, 390f, 96f, card, accent);
            Text name = Label(item.transform, string.Empty, 12f, 4f, 224f, 28f, 15, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text info = Label(item.transform, string.Empty, 12f, 31f, 224f, 56f, 12, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Button equipButton = FlatButton(item.transform, string.Empty, 246f, 8f, 132f, 34f, cream, darkGreen,
                () => EquipItem(itemId));
            Button upgradeButton = FlatButton(item.transform, string.Empty, 246f, 50f, 132f, 34f, accent, Color.white,
                () => UpgradeItem(itemId));
            Text equipText = equipButton.GetComponentInChildren<Text>();
            Text upgradeText = upgradeButton.GetComponentInChildren<Text>();

            refreshers.Add(() =>
            {
                if (equipment == null || itemId < 0)
                {
                    name.text = "장비 연결 대기";
                    info.text = string.Empty;
                    equipButton.interactable = upgradeButton.interactable = false;
                    return;
                }

                ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment.GetDefinition(itemId);
                bool owned = equipment.IsOwned(itemId);
                bool equippedNow = equipment.IsEquipped(itemId, bossCatalog);
                int cost = equipment.GetUpgradeCost(itemId);
                name.text = owned ? definition.Name : "잠김 · " + definition.Name;
                info.text = owned
                    ? $"Lv.{equipment.GetLevel(itemId)} · 보유 {equipment.GetCopies(itemId)}\n피해 ×{equipment.GetItemDamageMultiplier(itemId):0.00}"
                    : $"{(bossCatalog ? "보스용" : "사냥용")} 소환으로 획득";

                equipButton.interactable = owned && !equippedNow;
                equipText.text = equippedNow ? "장착 중" : "장착";
                equipButton.GetComponent<Image>().color = equippedNow ? (bossCatalog ? red : green) : cream;
                equipText.color = equippedNow ? Color.white : darkGreen;

                upgradeButton.interactable = owned && growth != null && growth.EquipmentEnhancementMaterials >= cost;
                upgradeText.text = owned ? $"강화 {cost}" : "잠김";
            });
        }

        private void BuildShop()
        {
            Text message = Label(root.transform, string.Empty, 20f, 8f, 900f, 46f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text resources = Label(root.transform, string.Empty, 940f, 8f, 428f, 46f, 15, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                message.text = shop == null ? "상점 연결 대기" : shop.ShopMessage;
                resources.text = coreShop == null ? string.Empty : $"골드 {coreShop.Gold:N0} · 다이아 {coreShop.Diamonds:N0}";
            });

            BuildSwordSummonCard(false, 20f, 66f, green);
            BuildSwordSummonCard(true, 704f, 66f, red);

            BuildShopActionCard(20f, 280f, "오늘의 접속 보상",
                "하루 한 번 골드·다이아·조각을 받습니다.", "무료", green, ClaimDaily);
            BuildShopActionCard(704f, 280f, "성장 골드 보급",
                "현재 스테이지에 맞춘 성장 골드를 받습니다.", "다이아 5", gold, BuyGoldSupply);
            BuildShopActionCard(20f, 458f, "펫 알",
                "화염·냉기·번개 펫 성장용 알을 구매합니다.", "다이아 3", blue, BuyPetEgg);
            BuildShopActionCard(704f, 458f, "부화 즉시 완료",
                "진행 중인 펫 알 부화를 즉시 끝냅니다.", "진행 상태에 따라 1~5 다이아", new Color(0.56f, 0.32f, 0.82f), FinishIncubation);
        }

        private void BuildSwordSummonCard(bool boss, float x, float y, Color accent)
        {
            GameObject panel = Panel(root.transform, boss ? "Boss Sword Summon" : "Hunting Sword Summon",
                x, y, 664f, 194f, card, accent);
            Label(panel.transform, boss ? "보스용 검 소환" : "사냥용 검 소환",
                22f, 14f, 420f, 38f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(panel.transform,
                "무속성·화염·냉기·번개 중 하나\n레어·에픽·유니크·레전드 등급 중 하나",
                22f, 60f, 420f, 68f, 15, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Text count = Label(panel.transform, string.Empty, 22f, 136f, 350f, 34f, 14, accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(panel.transform, "소환 · 5 다이아", 438f, 58f, 200f, 82f, accent, Color.white,
                () => SummonSword(boss));
            refreshers.Add(() => count.text = shop == null
                ? "소환 기록 0회"
                : $"누적 {(boss ? shop.BossSwordSummons : shop.HuntingSwordSummons):N0}회");
        }

        private void BuildShopActionCard(float x, float y, string title, string description, string cost, Color accent, Action action)
        {
            GameObject panel = Panel(root.transform, title, x, y, 664f, 158f, card, accent);
            Label(panel.transform, title, 20f, 12f, 390f, 34f, 19, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(panel.transform, description, 20f, 52f, 390f, 54f, 14, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Label(panel.transform, cost, 20f, 112f, 350f, 30f, 14, accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(panel.transform, "구매·수령", 438f, 46f, 200f, 72f, accent, Color.white, action);
        }

        private void SummonCurrentCatalog()
        {
            SummonSword(bossCatalog);
        }

        private void SummonSword(bool boss)
        {
            if (shop == null)
            {
                Toast("검 소환 시스템 연결 대기");
                return;
            }
            shop.TrySummonSword(boss);
            saveService?.SaveNow();
            Toast(shop.ShopMessage);
        }

        private void EquipItem(int itemId)
        {
            if (equipment == null)
            {
                Toast("장비 도감 연결 대기");
                return;
            }
            equipment.EquipItem(itemId, bossCatalog);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
        }

        private void UpgradeItem(int itemId)
        {
            if (equipment == null)
            {
                Toast("장비 도감 연결 대기");
                return;
            }
            equipment.UpgradeItem(itemId);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
        }

        private void ClaimDaily()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.ClaimDailyReward();
            saveService?.SaveNow();
            Toast(coreShop.LastMessage);
        }

        private void BuyGoldSupply()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.BuyGoldSupply();
            saveService?.SaveNow();
            Toast(coreShop.LastMessage);
        }

        private void BuyPetEgg()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.BuyPetEgg();
            saveService?.SaveNow();
            Toast(coreShop.LastMessage);
        }

        private void FinishIncubation()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.FinishIncubationNow();
            saveService?.SaveNow();
            Toast(coreShop.LastMessage);
        }

        private string EquippedName(bool boss)
        {
            if (equipment == null) return "미장착";
            ElementEquipmentCatalogPrototype.EquipmentDefinition definition =
                equipment.GetDefinition(equipment.GetEquippedItem(boss));
            return definition == null ? "미장착" : $"{equipment.ElementName((int)definition.Element)} · {definition.Name}";
        }

        private void Toast(string message)
        {
            if (sourceUi != null && toastMethod != null)
                toastMethod.Invoke(sourceUi, new object[] { message });
            else
                Debug.Log("[PeanutWarrior] " + message);
        }

        private GameObject Panel(Transform parent, string name, float x, float y, float width, float height, Color color, Color border)
        {
            GameObject go = Rect(parent, name, x, y, width, height).gameObject;
            Image image = go.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.38f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            outline.useGraphicAlpha = false;
            return go;
        }

        private Button Button(Transform parent, string text, float x, float y, float width, float height,
            Color color, Color textColor, Action action)
        {
            GameObject go = Panel(parent, "Button", x, y, width, height, color, color);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.42f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Label(go.transform, text, 4f, 2f, width - 8f, height - 4f, 14, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private Button FlatButton(Transform parent, string text, float x, float y, float width, float height,
            Color color, Color textColor, Action action)
        {
            return Button(parent, text, x, y, width, height, color, textColor, action);
        }

        private Text Label(Transform parent, string text, float x, float y, float width, float height,
            int size, Color color, TextAnchor anchor, FontStyle style)
        {
            GameObject go = Rect(parent, "Text", x, y, width, height).gameObject;
            Text label = go.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Color RarityColor(int rarity)
        {
            return rarity switch
            {
                1 => new Color(0.31f, 0.56f, 0.82f),
                2 => new Color(0.62f, 0.33f, 0.85f),
                3 => new Color(0.92f, 0.48f, 0.12f),
                4 => new Color(0.94f, 0.70f, 0.10f),
                _ => Color.gray
            };
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite CreateRoundedSprite()
        {
            const int size = 32;
            const float radius = 9f;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, radius - Mathf.Min(x, size - 1 - x));
                    float dy = Mathf.Max(0f, radius - Mathf.Min(y, size - 1 - y));
                    float alpha = Mathf.Clamp01(radius + 0.6f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }
    }
}
