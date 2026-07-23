using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(33600)]
    public sealed class PeanutEquipmentDetailMenuV7 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Width = 1388f;
        private const float Height = 650f;

        private PeanutMobileCanvasPrototype sourceUi;
        private ElementEquipmentCatalogPrototype equipment;
        private GrowthExpansionPrototype growth;
        private PrototypeShopAndDaily shop;
        private PeanutSaveGameService saveService;
        private FieldInfo contentHostField;
        private FieldInfo menuTitleField;
        private MethodInfo toastMethod;
        private GameObject contentHost;
        private Text menuTitle;

        private Font font;
        private Sprite solidSprite;
        private Sprite roundedSprite;
        private GameObject root;
        private int elementTab;
        private int selectedItemId = -1;
        private readonly List<Action> refreshers = new List<Action>();
        private readonly Dictionary<int, Sprite> weaponSprites = new Dictionary<int, Sprite>();

        private readonly Color background = new Color(0.95f, 0.97f, 0.92f, 1f);
        private readonly Color cream = new Color(0.99f, 0.96f, 0.84f, 1f);
        private readonly Color panel = new Color(1f, 0.985f, 0.92f, 1f);
        private readonly Color dark = new Color(0.13f, 0.10f, 0.08f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color red = new Color(0.80f, 0.20f, 0.16f, 1f);
        private readonly Color gold = new Color(0.93f, 0.61f, 0.12f, 1f);
        private readonly Color muted = new Color(0.48f, 0.47f, 0.42f, 1f);

        private static readonly string[] ElementNames = { "무속성", "화염", "냉기", "번개" };
        private static readonly Color[] ElementColors =
        {
            new Color(0.66f, 0.50f, 0.16f),
            new Color(0.91f, 0.25f, 0.13f),
            new Color(0.15f, 0.55f, 0.86f),
            new Color(0.55f, 0.30f, 0.88f)
        };

        public bool UsesLeftCatalogAndRightDetail => true;
        public bool ShowsSelectedWeaponAppearance => true;
        public bool ShowsFullCombatDetails => true;
        public int SelectedItemId => selectedItemId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutEquipmentDetailMenuV7>() != null) return;
            GameObject go = new GameObject("PeanutWarriorEquipmentDetailMenuV7");
            DontDestroyOnLoad(go);
            go.AddComponent<PeanutEquipmentDetailMenuV7>();
        }

        private bool Bind()
        {
            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            equipment = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            shop = FindFirstObjectByType<PrototypeShopAndDaily>();
            saveService = FindFirstObjectByType<PeanutSaveGameService>();
            if (sourceUi == null) return false;

            Type uiType = typeof(PeanutMobileCanvasPrototype);
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

        private void BuildPage()
        {
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            root = Rect(contentHost.transform, "Peanut Equipment Detail V7", 0f, 0f, Width, Height).gameObject;
            Image backgroundImage = root.AddComponent<Image>();
            backgroundImage.sprite = solidSprite;
            backgroundImage.color = background;
            backgroundImage.raycastTarget = false;
            if (menuTitle != null) menuTitle.text = "장비";

            EnsureSelectedItem();
            BuildHeader();
            BuildElementTabs();
            BuildCatalogPanel();
            BuildDetailPanel();
            RefreshNow();
        }

        private void BuildHeader()
        {
            Label(root.transform, "검 장비 도감", 20f, 4f, 300f, 42f, 23, dark, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(root.transform, "왼쪽에서 검을 선택하면 모습과 전투 효과를 자세히 확인할 수 있습니다.",
                320f, 7f, 650f, 38f, 14, muted, TextAnchor.MiddleLeft, FontStyle.Normal);

            Text resources = Label(root.transform, string.Empty, 970f, 4f, 398f, 42f, 14, dark,
                TextAnchor.MiddleRight, FontStyle.Bold);
            Button summon = Button(root.transform, "검 소환 · 5 다이아", 1090f, 48f, 278f, 38f,
                gold, Color.white, SummonSword);
            refreshers.Add(() =>
            {
                resources.text = $"강화 재료 {(growth == null ? 0 : growth.EquipmentEnhancementMaterials):N0}";
                summon.interactable = shop != null;
            });
        }

        private void BuildElementTabs()
        {
            for (int i = 0; i < ElementNames.Length; i++)
            {
                int captured = i;
                Button tab = Button(root.transform, ElementNames[i], 20f + i * 337f, 92f, 324f, 44f,
                    elementTab == i ? ElementColors[i] : cream,
                    elementTab == i ? Color.white : dark,
                    () => SelectElement(captured));
                refreshers.Add(() =>
                {
                    bool selected = elementTab == captured;
                    tab.GetComponent<Image>().color = selected ? ElementColors[captured] : cream;
                    tab.GetComponentInChildren<Text>().color = selected ? Color.white : dark;
                });
            }
        }

        private void BuildCatalogPanel()
        {
            GameObject catalog = Panel(root.transform, "Equipment Catalog", 20f, 148f, 586f, 482f, panel,
                ElementColors[Mathf.Clamp(elementTab, 0, ElementColors.Length - 1)]);
            Label(catalog.transform, "장비 목록", 16f, 8f, 220f, 32f, 18, dark, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(catalog.transform, "등급별 3종", 390f, 8f, 178f, 32f, 12, muted, TextAnchor.MiddleRight, FontStyle.Normal);

            for (int rarity = 1; rarity <= 4; rarity++)
            {
                int capturedRarity = rarity;
                float y = 46f + (rarity - 1) * 106f;
                Color rarityColor = RarityColor(rarity);
                Label(catalog.transform, equipment == null ? "등급" : equipment.RarityName(rarity),
                    12f, y + 28f, 74f, 52f, 15, rarityColor, TextAnchor.MiddleCenter, FontStyle.Bold);

                for (int variant = 0; variant < 3; variant++)
                {
                    int itemId = equipment == null ? -1 : equipment.GetUnifiedItemId(elementTab, rarity, variant);
                    BuildCatalogItem(catalog.transform, itemId, 90f + variant * 160f, y, 150f, 96f,
                        rarityColor, capturedRarity);
                }
            }
        }

        private void BuildCatalogItem(Transform parent, int itemId, float x, float y, float width, float height,
            Color rarityColor, int rarity)
        {
            GameObject item = Panel(parent, "Catalog Item " + itemId, x, y, width, height, cream, rarityColor);
            Button button = item.AddComponent<Button>();
            button.targetGraphic = item.GetComponent<Image>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => SelectItem(itemId));
            Outline outline = item.GetComponent<Outline>();

            Image icon = ImageBox(item.transform, "Sword", 8f, 10f, 40f, 72f, null, Color.white);
            Text name = Label(item.transform, string.Empty, 52f, 7f, 92f, 44f, 12, dark,
                TextAnchor.UpperLeft, FontStyle.Bold);
            Text state = Label(item.transform, string.Empty, 52f, 52f, 92f, 32f, 10, muted,
                TextAnchor.UpperLeft, FontStyle.Normal);

            refreshers.Add(() =>
            {
                if (equipment == null || itemId < 0)
                {
                    name.text = "연결 대기";
                    state.text = string.Empty;
                    button.interactable = false;
                    return;
                }

                ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment.GetDefinition(itemId);
                bool owned = equipment.IsOwned(itemId);
                bool selected = selectedItemId == itemId;
                icon.sprite = GetWeaponSprite(itemId);
                icon.color = owned ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.46f);
                name.text = definition == null ? "장비" : definition.Name;
                state.text = owned
                    ? $"Lv.{equipment.GetLevel(itemId)} · 보유 {equipment.GetCopies(itemId)}"
                    : "미보유 · 소환 필요";
                state.color = owned ? rarityColor : muted;
                item.GetComponent<Image>().color = selected
                    ? new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.18f)
                    : cream;
                outline.effectColor = selected ? rarityColor : new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.28f);
                outline.effectDistance = selected ? new Vector2(2.4f, -2.4f) : new Vector2(1f, -1f);
                button.interactable = true;
            });
        }

        private void BuildDetailPanel()
        {
            GameObject detail = Panel(root.transform, "Selected Equipment Detail", 624f, 148f, 744f, 482f,
                panel, ElementColors[Mathf.Clamp(elementTab, 0, ElementColors.Length - 1)]);

            GameObject previewBox = Panel(detail.transform, "Weapon Appearance", 18f, 18f, 220f, 214f,
                new Color(0.96f, 0.94f, 0.86f, 1f), ElementColors[Mathf.Clamp(elementTab, 0, ElementColors.Length - 1)]);
            Image preview = ImageBox(previewBox.transform, "Large Sword", 34f, 12f, 152f, 188f, null, Color.white);

            Text rarity = Label(detail.transform, string.Empty, 258f, 12f, 450f, 28f, 13, gold,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Text name = Label(detail.transform, string.Empty, 258f, 40f, 450f, 48f, 26, dark,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            Text identity = Label(detail.transform, string.Empty, 258f, 90f, 450f, 86f, 15, dark,
                TextAnchor.UpperLeft, FontStyle.Normal);
            Text ownership = Label(detail.transform, string.Empty, 258f, 180f, 450f, 42f, 13, muted,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            GameObject combatBox = Panel(detail.transform, "Combat Description", 18f, 244f, 708f, 156f,
                new Color(0.985f, 0.97f, 0.90f, 1f), new Color(0.58f, 0.45f, 0.22f));
            Text hunting = Label(combatBox.transform, string.Empty, 16f, 10f, 330f, 136f, 12, green,
                TextAnchor.UpperLeft, FontStyle.Normal);
            Text boss = Label(combatBox.transform, string.Empty, 362f, 10f, 330f, 136f, 12, red,
                TextAnchor.UpperLeft, FontStyle.Normal);

            Button huntingButton = Button(detail.transform, string.Empty, 18f, 414f, 214f, 50f,
                green, Color.white, () => EquipSelected(false));
            Button bossButton = Button(detail.transform, string.Empty, 246f, 414f, 214f, 50f,
                red, Color.white, () => EquipSelected(true));
            Button upgradeButton = Button(detail.transform, string.Empty, 474f, 414f, 252f, 50f,
                gold, Color.white, UpgradeSelected);
            Text huntingButtonText = huntingButton.GetComponentInChildren<Text>();
            Text bossButtonText = bossButton.GetComponentInChildren<Text>();
            Text upgradeButtonText = upgradeButton.GetComponentInChildren<Text>();

            refreshers.Add(() =>
            {
                if (equipment == null || selectedItemId < 0)
                {
                    rarity.text = "장비 연결 대기";
                    name.text = "선택된 장비 없음";
                    identity.text = string.Empty;
                    ownership.text = string.Empty;
                    hunting.text = string.Empty;
                    boss.text = string.Empty;
                    preview.sprite = null;
                    huntingButton.interactable = bossButton.interactable = upgradeButton.interactable = false;
                    return;
                }

                ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment.GetDefinition(selectedItemId);
                if (definition == null) return;
                bool owned = equipment.IsOwned(selectedItemId);
                bool huntingEquipped = equipment.IsEquipped(selectedItemId, false);
                bool bossEquipped = equipment.IsEquipped(selectedItemId, true);
                int level = equipment.GetLevel(selectedItemId);
                int copies = equipment.GetCopies(selectedItemId);
                int upgradeCost = equipment.GetUpgradeCost(selectedItemId);
                ElementEquipmentCatalogPrototype.HuntingModeProfile huntingProfile =
                    equipment.GetHuntingModeProfile(selectedItemId);
                ElementEquipmentCatalogPrototype.BossModeProfile bossProfile =
                    equipment.GetBossModeProfile(selectedItemId);

                preview.sprite = GetWeaponSprite(selectedItemId);
                preview.color = owned ? Color.white : new Color(0.42f, 0.42f, 0.42f, 0.55f);
                rarity.text = $"{equipment.ElementName((int)definition.Element)} · {equipment.RarityName((int)definition.Rarity)} · {WeaponTypeName(definition.Variant)}";
                name.text = owned ? definition.Name : "잠김 · " + definition.Name;
                identity.text = BuildWeaponIdentity(definition);
                ownership.text = owned
                    ? $"Lv.{level} · 보유 {copies} · 사냥 {(huntingEquipped ? "장착 중" : "미장착")} · 보스 {(bossEquipped ? "장착 중" : "미장착")}"
                    : "검 소환을 통해 획득하면 사냥·보스 효과가 동시에 해금됩니다.";

                hunting.text = BuildHuntingDetail(huntingProfile);
                boss.text = BuildBossDetail(bossProfile);

                huntingButton.interactable = owned && !huntingEquipped;
                bossButton.interactable = owned && !bossEquipped;
                upgradeButton.interactable = owned && growth != null &&
                                             growth.EquipmentEnhancementMaterials >= upgradeCost;
                huntingButtonText.text = huntingEquipped ? "사냥 장착 중" : "사냥 장착";
                bossButtonText.text = bossEquipped ? "보스 장착 중" : "보스 장착";
                upgradeButtonText.text = owned ? $"강화 · 재료 {upgradeCost:N0}" : "미보유 장비";
            });
        }

        private string BuildHuntingDetail(ElementEquipmentCatalogPrototype.HuntingModeProfile profile)
        {
            string extra = profile.Style == ElementEquipmentCatalogPrototype.HuntingAttackStyle.Chain
                ? $"\n연쇄 피해 유지율: {profile.ChainFalloff * 100f:0}%"
                : string.Empty;
            return "[사냥 모드]\n" +
                   $"공격: {profile.StyleName}\n" +
                   $"효과: 일반 몬스터 최대 {profile.MaxTargets}마리 동시 공격\n" +
                   $"공격 범위: {profile.Radius:0}\n" +
                   $"데미지: 공격력의 {profile.DamageRatio * 100f:0.##}%\n" +
                   $"공격력 증가율: +{profile.DamageRatio * 100f:0.##}%" + extra;
        }

        private string BuildBossDetail(ElementEquipmentCatalogPrototype.BossModeProfile profile)
        {
            string execution = profile.Style == ElementEquipmentCatalogPrototype.BossAttackStyle.Execution
                ? $"\n즉사 확률: {profile.ExecuteChance * 100f:0.####}% · 약 {Mathf.Max(1, Mathf.RoundToInt(1f / profile.ExecuteChance)):N0}분의 1"
                : string.Empty;
            return "[보스 모드]\n" +
                   $"공격: {profile.StyleName}\n" +
                   "효과: 보스 한 명에게 모든 추가 피해 집중\n" +
                   $"타격 수: {profile.HitCount}타\n" +
                   $"총 데미지: 공격력의 {profile.TotalDamageRatio * 100f:0.##}%\n" +
                   $"공격력 증가율: +{profile.TotalDamageRatio * 100f:0.##}%" + execution;
        }

        private string BuildWeaponIdentity(ElementEquipmentCatalogPrototype.EquipmentDefinition definition)
        {
            string elementText = definition.Element switch
            {
                ElementEquipmentCatalogPrototype.EquipmentElement.Fire => "불꽃을 압축한 칼날로 적을 태우는 검입니다.",
                ElementEquipmentCatalogPrototype.EquipmentElement.Ice => "차가운 서리를 퍼뜨려 넓은 전장을 얼리는 검입니다.",
                ElementEquipmentCatalogPrototype.EquipmentElement.Lightning => "번개가 다음 대상을 찾아 이어지는 고속 검입니다.",
                _ => "땅콩 대지의 힘을 안정적으로 끌어내는 기본형 검입니다."
            };
            string attackText = definition.Variant switch
            {
                0 => "사냥에서는 부채꼴로 여러 적을 베고, 보스전에서는 강한 한 번의 집중 참격을 사용합니다.",
                1 => "사냥에서는 원형 폭발을 만들고, 보스전에서는 한 대상에게 연속 타격을 퍼붓습니다.",
                _ => "사냥에서는 검격이 적 사이를 연쇄하며, 보스전에서는 극저확률 즉사 처형을 노립니다."
            };
            return elementText + "\n" + attackText;
        }

        private static string WeaponTypeName(int variant)
        {
            return variant switch
            {
                0 => "광역 참격형",
                1 => "범위 폭발·연타형",
                _ => "연쇄·처형형"
            };
        }

        private void SelectElement(int element)
        {
            elementTab = Mathf.Clamp(element, 0, 3);
            selectedItemId = -1;
            EnsureSelectedItem();
            BuildPage();
        }

        private void SelectItem(int itemId)
        {
            selectedItemId = itemId;
            RefreshNow();
        }

        private void EnsureSelectedItem()
        {
            if (equipment == null) return;
            ElementEquipmentCatalogPrototype.EquipmentDefinition current = equipment.GetDefinition(selectedItemId);
            if (current != null && (int)current.Element == elementTab) return;

            int hunting = equipment.GetEquippedItem(false);
            ElementEquipmentCatalogPrototype.EquipmentDefinition huntingDefinition = equipment.GetDefinition(hunting);
            if (huntingDefinition != null && (int)huntingDefinition.Element == elementTab)
            {
                selectedItemId = hunting;
                return;
            }

            for (int rarity = 4; rarity >= 1; rarity--)
            {
                for (int variant = 0; variant < 3; variant++)
                {
                    int id = equipment.GetUnifiedItemId(elementTab, rarity, variant);
                    if (!equipment.IsOwned(id)) continue;
                    selectedItemId = id;
                    return;
                }
            }
            selectedItemId = equipment.GetUnifiedItemId(elementTab, 1, 0);
        }

        private void EquipSelected(bool boss)
        {
            if (equipment == null || selectedItemId < 0) return;
            equipment.EquipItem(selectedItemId, boss);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
            RefreshNow();
        }

        private void UpgradeSelected()
        {
            if (equipment == null || selectedItemId < 0) return;
            equipment.UpgradeItem(selectedItemId);
            saveService?.SaveNow();
            Toast(equipment.LastMessage);
            RefreshNow();
        }

        private void SummonSword()
        {
            if (shop == null)
            {
                Toast("검 소환 시스템 연결 대기");
                return;
            }
            shop.TrySummonSword();
            saveService?.SaveNow();
            Toast(shop.ShopMessage);
            EnsureSelectedItem();
            RefreshNow();
        }

        private void RefreshNow()
        {
            for (int i = 0; i < refreshers.Count; i++)
            {
                try { refreshers[i]?.Invoke(); }
                catch (MissingReferenceException) { }
            }
        }

        private Sprite GetWeaponSprite(int itemId)
        {
            if (weaponSprites.TryGetValue(itemId, out Sprite sprite) && sprite != null) return sprite;
            ElementEquipmentCatalogPrototype.EquipmentDefinition definition = equipment?.GetDefinition(itemId);
            if (definition == null) return solidSprite;
            sprite = CreateWeaponSprite(definition);
            weaponSprites[itemId] = sprite;
            return sprite;
        }

        private static Sprite CreateWeaponSprite(ElementEquipmentCatalogPrototype.EquipmentDefinition definition)
        {
            const int width = 96;
            const int height = 144;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "WeaponPreview_" + definition.Id;
            texture.filterMode = FilterMode.Point;
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) texture.SetPixel(x, y, clear);

            Color element = definition.Element switch
            {
                ElementEquipmentCatalogPrototype.EquipmentElement.Fire => new Color(1f, 0.26f, 0.10f),
                ElementEquipmentCatalogPrototype.EquipmentElement.Ice => new Color(0.20f, 0.72f, 1f),
                ElementEquipmentCatalogPrototype.EquipmentElement.Lightning => new Color(0.70f, 0.32f, 1f),
                _ => new Color(0.88f, 0.68f, 0.22f)
            };
            Color rarity = RarityColor((int)definition.Rarity);
            Color blade = Color.Lerp(Color.white, element, 0.38f);
            Color shadow = Color.Lerp(element, Color.black, 0.42f);

            int bladeHalfWidth = definition.Variant == 0 ? 12 : definition.Variant == 1 ? 8 : 6;
            for (int y = 34; y < 126; y++)
            {
                float taper = Mathf.InverseLerp(126f, 34f, y);
                int half = Mathf.Max(2, Mathf.RoundToInt(bladeHalfWidth * taper));
                if (definition.Variant == 2 && y % 18 < 7) half += 4;
                for (int x = 48 - half; x <= 48 + half; x++)
                {
                    texture.SetPixel(x, y, x < 48 ? shadow : blade);
                }
            }
            for (int y = 124; y < 140; y++)
            {
                int half = Mathf.Max(0, 8 - (y - 124));
                for (int x = 48 - half; x <= 48 + half; x++) texture.SetPixel(x, y, blade);
            }

            for (int x = 24; x <= 72; x++)
                for (int y = 27; y <= 34; y++) texture.SetPixel(x, y, rarity);
            for (int x = 43; x <= 53; x++)
                for (int y = 7; y <= 28; y++) texture.SetPixel(x, y, new Color(0.34f, 0.20f, 0.10f));
            for (int x = 39; x <= 57; x++)
                for (int y = 3; y <= 8; y++) texture.SetPixel(x, y, rarity);

            if (definition.Variant == 1)
            {
                for (int radius = 18; radius <= 24; radius++)
                {
                    for (int angle = 0; angle < 360; angle += 8)
                    {
                        float rad = angle * Mathf.Deg2Rad;
                        int x = Mathf.RoundToInt(48 + Mathf.Cos(rad) * radius);
                        int y = Mathf.RoundToInt(79 + Mathf.Sin(rad) * radius);
                        if (x >= 0 && x < width && y >= 0 && y < height)
                            texture.SetPixel(x, y, new Color(element.r, element.g, element.b, 0.62f));
                    }
                }
            }
            else if (definition.Variant == 2)
            {
                for (int i = 0; i < 5; i++)
                {
                    int x = 20 + i * 14;
                    int y = 52 + (i % 2) * 20;
                    for (int px = x; px < x + 8; px++)
                        for (int py = y; py < y + 8; py++) texture.SetPixel(px, py, element);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void Toast(string message)
        {
            if (sourceUi != null && toastMethod != null) toastMethod.Invoke(sourceUi, new object[] { message });
            else Debug.Log("[PeanutWarrior] " + message);
        }

        private GameObject Panel(Transform parent, string name, float x, float y, float width, float height,
            Color color, Color border)
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
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.40f);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Label(go.transform, text, 4f, 2f, width - 8f, height - 4f, 13, textColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
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

        private Image ImageBox(Transform parent, string name, float x, float y, float width, float height,
            Sprite sprite, Color color)
        {
            GameObject go = Rect(parent, name, x, y, width, height).gameObject;
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
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
                1 => new Color(0.30f, 0.55f, 0.84f),
                2 => new Color(0.61f, 0.32f, 0.86f),
                3 => new Color(0.92f, 0.47f, 0.11f),
                4 => new Color(0.95f, 0.72f, 0.10f),
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
