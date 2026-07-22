using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(31000)]
    public sealed class PeanutCoreMenuCompletionV3 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Width = 1388f;
        private const float Height = 650f;

        private PeanutMobileCanvasPrototype sourceUi;
        private PeanutMenuLayoutV2 layoutV2;
        private AdvancementProgressionPrototype advancement;
        private PetProgressionPrototype petProgression;
        private IdleSystemsPrototype legacyPets;
        private CoreShopProgressionPrototype coreShop;
        private GameSettingsPrototype settings;
        private PeanutSaveGameService saveService;
        private CombatPrototypeArena arena;

        private FieldInfo sourceCurrentPageField;
        private FieldInfo sourceContentHostField;
        private FieldInfo sourceToastMethodField;
        private MethodInfo sourceToastMethod;
        private FieldInfo v2CustomRootField;
        private FieldInfo v2RefreshersField;
        private GameObject contentHost;
        private GameObject root;
        private string activePage = string.Empty;
        private readonly List<Action> refreshers = new List<Action>();
        private float refreshTimer;

        private FieldInfo goldField;
        private FieldInfo eggsField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;
        private MethodInfo startIncubationMethod;

        private Font font;
        private Sprite whiteSprite;
        private Sprite roundedSprite;

        private readonly Color background = new Color(0.93f, 0.95f, 0.87f, 1f);
        private readonly Color cream = new Color(0.98f, 0.95f, 0.82f, 1f);
        private readonly Color card = new Color(0.97f, 0.94f, 0.85f, 1f);
        private readonly Color mint = new Color(0.86f, 0.94f, 0.82f, 1f);
        private readonly Color green = new Color(0.16f, 0.42f, 0.22f, 1f);
        private readonly Color darkGreen = new Color(0.06f, 0.23f, 0.10f, 1f);
        private readonly Color brown = new Color(0.20f, 0.12f, 0.06f, 1f);
        private readonly Color gold = new Color(0.94f, 0.61f, 0.10f, 1f);
        private readonly Color red = new Color(0.86f, 0.22f, 0.17f, 1f);
        private readonly Color blue = new Color(0.18f, 0.48f, 0.82f, 1f);
        private readonly Color muted = new Color(0.52f, 0.55f, 0.48f, 1f);

        public int CompletedPageCount => 4;
        public bool LeavesSkillsAndEquipmentUntouched => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutCoreMenuCompletionV3>() != null) return;
            GameObject go = new GameObject("PeanutWarriorCoreMenuCompletionV3");
            DontDestroyOnLoad(go);
            go.AddComponent<PeanutCoreMenuCompletionV3>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 10; i++)
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
            advancement = FindFirstObjectByType<AdvancementProgressionPrototype>();
            petProgression = FindFirstObjectByType<PetProgressionPrototype>();
            legacyPets = FindFirstObjectByType<IdleSystemsPrototype>();
            coreShop = FindFirstObjectByType<CoreShopProgressionPrototype>();
            settings = FindFirstObjectByType<GameSettingsPrototype>();
            saveService = FindFirstObjectByType<PeanutSaveGameService>();
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (sourceUi == null || layoutV2 == null || arena == null) return false;

            Type sourceType = typeof(PeanutMobileCanvasPrototype);
            sourceCurrentPageField = sourceType.GetField("currentPage", PrivateInstance);
            sourceContentHostField = sourceType.GetField("contentHost", PrivateInstance);
            sourceToastMethod = sourceType.GetMethod("Toast", PrivateInstance);
            contentHost = sourceContentHostField?.GetValue(sourceUi) as GameObject;

            Type v2Type = typeof(PeanutMenuLayoutV2);
            v2CustomRootField = v2Type.GetField("customRoot", PrivateInstance);
            v2RefreshersField = v2Type.GetField("refreshers", PrivateInstance);

            goldField = typeof(CombatPrototypeArena).GetField("gold", PrivateInstance);
            if (legacyPets != null)
            {
                Type petType = typeof(IdleSystemsPrototype);
                eggsField = petType.GetField("eggs", PrivateInstance);
                incubatingField = petType.GetField("incubating", PrivateInstance);
                incubationRemainingField = petType.GetField("incubationRemaining", PrivateInstance);
                startIncubationMethod = petType.GetMethod("StartIncubation", PrivateInstance);
            }
            return contentHost != null;
        }

        private void CreateAssets()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 18);
            whiteSprite = SolidSprite();
            roundedSprite = RoundedSprite();
        }

        private void LateUpdate()
        {
            if (sourceUi == null || contentHost == null) return;
            string page = CurrentPage;
            if (!IsManaged(page))
            {
                activePage = page;
                return;
            }

            if (page != activePage || root == null || root.transform.parent != contentHost.transform)
            {
                activePage = page;
                Rebuild(page);
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.2f;
            for (int i = 0; i < refreshers.Count; i++)
            {
                try { refreshers[i]?.Invoke(); }
                catch (MissingReferenceException) { }
            }
        }

        private string CurrentPage
        {
            get
            {
                object value = sourceCurrentPageField?.GetValue(sourceUi);
                return value == null ? "Main" : value.ToString();
            }
        }

        private static bool IsManaged(string page)
        {
            return page == "Advancement" || page == "Pets" || page == "Shop" || page == "Settings";
        }

        private void Rebuild(string page)
        {
            IList v2Refreshers = v2RefreshersField?.GetValue(layoutV2) as IList;
            v2Refreshers?.Clear();
            refreshers.Clear();
            for (int i = contentHost.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            root = Rect(contentHost.transform, "Peanut Core Menu V3", 0f, 0f, Width, Height).gameObject;
            Image bg = root.AddComponent<Image>();
            bg.sprite = whiteSprite;
            bg.color = background;
            bg.raycastTarget = false;
            v2CustomRootField?.SetValue(layoutV2, root);

            switch (page)
            {
                case "Advancement": BuildAdvancement(); break;
                case "Pets": BuildPets(); break;
                case "Shop": BuildShop(); break;
                case "Settings": BuildSettings(); break;
            }
            for (int i = 0; i < refreshers.Count; i++) refreshers[i]?.Invoke();
        }

        private void BuildAdvancement()
        {
            GameObject header = Card(root.transform, "Advancement Summary", 20f, 14f, 1348f, 84f, cream, green);
            Badge(header.transform, "전직", 16f, 13f, 84f, 58f, green, Color.white, 18);
            Text current = Label(header.transform, string.Empty, 120f, 8f, 600f, 66f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text summary = Label(header.transform, string.Empty, 760f, 8f, 540f, 66f, 16, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                if (advancement == null)
                {
                    current.text = "전직 시스템 연결 대기";
                    summary.text = string.Empty;
                    return;
                }
                current.text = $"{advancement.CurrentName}  ·  {advancement.Tier}/{advancement.MaxTier}단계";
                summary.text = $"능력치 ×{advancement.GetCurrentStatMultiplier():0.00}  ·  기본 공격 {advancement.GetCurrentAttackHits()}타\n펫 {(advancement.PetsUnlocked ? "3슬롯 해금" : "잠김")}";
            });

            GameObject tierGrid = Card(root.transform, "Tier Grid", 20f, 112f, 842f, 518f, card, green);
            Label(tierGrid.transform, "전직 로드맵", 22f, 14f, 798f, 38f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < PeanutWarrior.Core.PeanutGameRules.AdvancementCount; i++)
            {
                int tierIndex = i;
                int col = i % 2;
                int row = i / 2;
                float x = 22f + col * 399f;
                float y = 66f + row * 108f;
                PeanutWarrior.Core.PeanutGameRules.AdvancementDefinition definition = PeanutWarrior.Core.PeanutGameRules.GetAdvancement(i);
                GameObject step = Card(tierGrid.transform, "Tier " + i, x, y, 377f, 96f, cream, i >= 6 ? gold : green);
                Badge(step.transform, i.ToString(), 12f, 15f, 60f, 60f, i >= 6 ? gold : green, Color.white, 19);
                Label(step.transform, definition.Name, 88f, 9f, 264f, 34f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text state = Label(step.transform, string.Empty, 88f, 45f, 264f, 30f, 14, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
                refreshers.Add(() =>
                {
                    int currentTier = advancement == null ? 0 : advancement.Tier;
                    state.text = currentTier > tierIndex ? "완료" : currentTier == tierIndex ? "현재 단계" : $"스테이지 {definition.RequiredGlobalStage} 필요";
                });
            }

            GameObject nextCard = Card(root.transform, "Next Advancement", 880f, 112f, 488f, 518f, mint, gold);
            Label(nextCard.transform, "다음 전직", 26f, 18f, 436f, 42f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text nextName = Label(nextCard.transform, string.Empty, 26f, 74f, 436f, 48f, 25, gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text requirements = Label(nextCard.transform, string.Empty, 32f, 142f, 424f, 230f, 17, brown, TextAnchor.UpperLeft, FontStyle.Bold);
            Text result = Label(nextCard.transform, string.Empty, 32f, 382f, 424f, 44f, 15, muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button advanceButton = Button(nextCard.transform, "전직 시도", 32f, 438f, 424f, 58f, gold, Color.white, TryAdvance);
            refreshers.Add(() =>
            {
                if (advancement == null || !advancement.HasNextTier)
                {
                    nextName.text = "최고 전직 달성";
                    requirements.text = "현재 구현된 모든 전직 단계를 완료했습니다.";
                    result.text = advancement == null ? "전직 시스템 연결 대기" : advancement.CurrentName;
                    advanceButton.interactable = false;
                    return;
                }
                var next = advancement.NextDefinition;
                nextName.text = next.Name;
                requirements.text =
                    Requirement("스테이지", advancement.GlobalStage, next.RequiredGlobalStage) + "\n\n" +
                    Requirement("전투력", advancement.CombatPower, next.RequiredCombatPower) + "\n\n" +
                    Requirement("골드", advancement.Gold, next.RequiredGold) + "\n\n" +
                    Requirement("다이아", advancement.Diamonds, next.RequiredDiamonds);
                advanceButton.interactable = advancement.MeetsNextRequirements(out string reason);
                result.text = reason;
            });
        }

        private void BuildPets()
        {
            GameObject header = Card(root.transform, "Pet Summary", 20f, 14f, 1348f, 86f, cream, green);
            Badge(header.transform, "펫", 16f, 14f, 84f, 58f, green, Color.white, 18);
            Text title = Label(header.transform, string.Empty, 120f, 8f, 600f, 66f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text bonus = Label(header.transform, string.Empty, 720f, 8f, 580f, 66f, 16, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                title.text = petProgression != null && petProgression.IsUnlocked ? "펫 슬롯 3/3 활동 중" : "2차 전직 후 펫 3슬롯 해금";
                bonus.text = petProgression == null
                    ? "펫 성장 연결 대기"
                    : $"도감 Lv.{petProgression.CollectionLevel} · 별 {petProgression.CollectionStars}\n공격 보조 ×{petProgression.AttackMultiplier:0.00}";
            });

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                PetProgressionPrototype.PetElement element = (PetProgressionPrototype.PetElement)i;
                Color accent = i == 0 ? red : i == 1 ? blue : new Color(0.55f, 0.34f, 0.83f);
                GameObject petCard = Card(root.transform, "Pet " + i, 20f + i * 456f, 114f, 436f, 256f, card, accent);
                Badge(petCard.transform, (i + 1).ToString(), 18f, 18f, 76f, 76f, accent, Color.white, 24);
                Text name = Label(petCard.transform, string.Empty, 112f, 14f, 294f, 38f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
                Text details = Label(petCard.transform, string.Empty, 112f, 56f, 294f, 72f, 15, brown, TextAnchor.UpperLeft, FontStyle.Bold);
                Text shards = Label(petCard.transform, string.Empty, 22f, 138f, 392f, 36f, 14, muted, TextAnchor.MiddleLeft, FontStyle.Bold);
                Button train = Button(petCard.transform, string.Empty, 22f, 184f, 392f, 52f, gold, Color.white, () => TrainPet(element));
                Text trainText = train.GetComponentInChildren<Text>();
                refreshers.Add(() =>
                {
                    if (petProgression == null)
                    {
                        name.text = "펫 연결 대기";
                        details.text = string.Empty;
                        shards.text = string.Empty;
                        train.interactable = false;
                        return;
                    }
                    name.text = petProgression.GetDisplayName(element);
                    details.text = petProgression.GetPassiveDescription(element) + $"\n누적 부화 {petProgression.GetLifetimeHatches(element)}회";
                    int required = petProgression.GetRequiredShards(element);
                    shards.text = required <= 0
                        ? "최고 5성 달성"
                        : $"승급 조각 {petProgression.GetDuplicateShards(element)}/{required}";
                    long cost = petProgression.GetTrainingCost(element);
                    train.interactable = Gold >= cost && petProgression.IsUnlocked;
                    trainText.text = $"레벨 성장 · {cost:N0}G";
                });
            }

            GameObject incubator = Card(root.transform, "Incubator", 20f, 388f, 656f, 242f, mint, green);
            Label(incubator.transform, "펫 알 부화", 24f, 16f, 608f, 38f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text incubation = Label(incubator.transform, string.Empty, 24f, 66f, 608f, 70f, 18, brown, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(incubator.transform, "펫 알 구매 · 다이아 3", 24f, 156f, 292f, 58f, cream, darkGreen, BuyEgg);
            Button(incubator.transform, "부화 시작", 340f, 156f, 292f, 58f, green, Color.white, StartIncubation);
            refreshers.Add(() =>
            {
                int eggs = ReadInt(eggsField, legacyPets);
                bool active = ReadBool(incubatingField, legacyPets);
                float remaining = ReadFloat(incubationRemainingField, legacyPets);
                incubation.text = active ? $"부화 진행 중 · {Mathf.CeilToInt(remaining)}초 남음\n보유 알 {eggs}" : $"부화 대기\n보유 알 {eggs}";
            });

            GameObject rule = Card(root.transform, "Pet Rules", 694f, 388f, 674f, 242f, card, green);
            Label(rule.transform, "펫 전투 규칙", 24f, 16f, 626f, 38f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(rule.transform,
                "· 화염·냉기·번개 펫 3마리 고정 편성\n· 주인공과 독립 이동 및 자동 공격\n· HP·MP 없음, 적의 공격 대상이 되지 않음\n· 부화 중복은 레벨과 별 승급으로 전환",
                28f, 66f, 618f, 146f, 16, brown, TextAnchor.UpperLeft, FontStyle.Normal);
        }

        private void BuildShop()
        {
            GameObject header = Card(root.transform, "Shop Summary", 20f, 14f, 1348f, 84f, cream, green);
            Badge(header.transform, "상점", 16f, 13f, 84f, 58f, green, Color.white, 17);
            Text message = Label(header.transform, string.Empty, 120f, 8f, 760f, 66f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text resources = Label(header.transform, string.Empty, 900f, 8f, 400f, 66f, 16, darkGreen, TextAnchor.MiddleRight, FontStyle.Bold);
            refreshers.Add(() =>
            {
                message.text = coreShop == null ? "상점 연결 대기" : coreShop.LastMessage;
                resources.text = coreShop == null ? string.Empty : $"골드 {coreShop.Gold:N0}\n다이아 {coreShop.Diamonds:N0}";
            });

            BuildShopCard(20f, 116f, "오늘의 접속 보상", "하루 한 번 골드·다이아·조각을 받습니다.", "무료", green, ClaimDaily);
            BuildShopCard(704f, 116f, "성장 골드 보급", "현재 최고 스테이지에 맞춘 성장 골드를 획득합니다.",
                coreShop == null ? "다이아 5" : $"다이아 {coreShop.GetGoldSupplyCost()} · {coreShop.GetGoldSupplyAmount():N0}G", gold, BuyGoldSupply);
            BuildShopCard(20f, 374f, "펫 알", "부화 시 화염·냉기·번개 펫 성장으로 전환됩니다.", "다이아 3", blue, BuyEgg);
            BuildShopCard(704f, 374f, "부화 즉시 완료", "진행 중인 펫 알 부화를 즉시 완료합니다.",
                coreShop == null || !coreShop.IsIncubating ? "부화 진행 필요" : $"다이아 {coreShop.GetIncubationFinishCost()}", red, FinishIncubation);
        }

        private void BuildShopCard(float x, float y, string title, string description, string price, Color accent, Action action)
        {
            GameObject cardRoot = Card(root.transform, title, x, y, 664f, 244f, card, accent);
            Badge(cardRoot.transform, title.Substring(0, Mathf.Min(2, title.Length)), 22f, 24f, 86f, 86f, accent, Color.white, 19);
            Label(cardRoot.transform, title, 130f, 18f, 480f, 42f, 22, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Label(cardRoot.transform, description, 130f, 70f, 480f, 70f, 15, brown, TextAnchor.UpperLeft, FontStyle.Normal);
            Label(cardRoot.transform, price, 24f, 160f, 330f, 56f, 18, accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(cardRoot.transform, "구매·수령", 420f, 160f, 216f, 56f, accent, Color.white, action);
        }

        private void BuildSettings()
        {
            GameObject header = Card(root.transform, "Settings Summary", 20f, 14f, 1348f, 82f, cream, green);
            Badge(header.transform, "설정", 16f, 12f, 84f, 58f, green, Color.white, 17);
            Text status = Label(header.transform, string.Empty, 120f, 8f, 1180f, 62f, 17, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            refreshers.Add(() => status.text = settings == null
                ? "설정 연결 대기"
                : $"{settings.TargetFrameRate} FPS · BGM {Mathf.RoundToInt(settings.BgmVolume * 100f)}% · 효과음 {Mathf.RoundToInt(settings.SfxVolume * 100f)}% · 진동 {(settings.VibrationEnabled ? "ON" : "OFF")}");

            BuildVolumeCard(20f, 112f, "BGM 음량", true, green);
            BuildVolumeCard(704f, 112f, "효과음 음량", false, blue);

            GameObject play = Card(root.transform, "Play Settings", 20f, 360f, 664f, 270f, card, green);
            Label(play.transform, "플레이 설정", 24f, 18f, 616f, 40f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(play.transform, "60 FPS", 24f, 78f, 190f, 58f, green, Color.white, () => SetFps(60));
            Button(play.transform, "30 FPS 절전", 236f, 78f, 190f, 58f, blue, Color.white, () => SetFps(30));
            Button playVibration = Button(play.transform, string.Empty, 448f, 78f, 190f, 58f, cream, darkGreen, ToggleVibration);
            Text vibrationText = playVibration.GetComponentInChildren<Text>();
            Text saveState = Label(play.transform, string.Empty, 24f, 162f, 614f, 70f, 15, brown, TextAnchor.MiddleLeft, FontStyle.Bold);
            refreshers.Add(() =>
            {
                vibrationText.text = settings != null && settings.VibrationEnabled ? "진동 ON" : "진동 OFF";
                saveState.text = saveService == null
                    ? "정식 저장 서비스 연결 대기"
                    : $"자동 저장 {PeanutWarrior.Core.PeanutGameRules.AutoSaveIntervalSeconds}초 · 백업 {PeanutWarrior.Core.PeanutGameRules.BackupSaveIntervalSeconds}초\n{saveService.LastMessage}";
            });

            GameObject save = Card(root.transform, "Save Settings", 704f, 360f, 664f, 270f, mint, gold);
            Label(save.transform, "저장 및 복구", 24f, 18f, 616f, 40f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Button(save.transform, "즉시 저장", 24f, 78f, 294f, 60f, gold, Color.white, SaveNow);
            Button(save.transform, "백업 복구", 346f, 78f, 294f, 60f, cream, darkGreen, RestoreBackup);
            Text path = Label(save.transform, string.Empty, 24f, 158f, 616f, 86f, 13, muted, TextAnchor.UpperLeft, FontStyle.Normal);
            refreshers.Add(() => path.text = saveService == null
                ? string.Empty
                : $"스키마 v{saveService.SchemaVersion}\n주 저장: {(saveService.HasMainSave ? "정상" : "없음")} · 백업: {(saveService.HasBackupSave ? "정상" : "없음")}");
        }

        private void BuildVolumeCard(float x, float y, string title, bool bgm, Color accent)
        {
            GameObject panel = Card(root.transform, title, x, y, 664f, 230f, card, accent);
            Label(panel.transform, title, 24f, 18f, 616f, 40f, 21, darkGreen, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text value = Label(panel.transform, string.Empty, 220f, 66f, 224f, 72f, 30, accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Button(panel.transform, "−10%", 24f, 76f, 170f, 58f, cream, darkGreen, () => ChangeVolume(bgm, -0.1f));
            Button(panel.transform, "+10%", 470f, 76f, 170f, 58f, accent, Color.white, () => ChangeVolume(bgm, 0.1f));
            Label(panel.transform, "설정은 재실행 후에도 유지됩니다.", 24f, 158f, 616f, 42f, 14, muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            refreshers.Add(() =>
            {
                float amount = settings == null ? 0f : bgm ? settings.BgmVolume : settings.SfxVolume;
                value.text = Mathf.RoundToInt(amount * 100f) + "%";
            });
        }

        private void TryAdvance()
        {
            if (advancement == null) { Toast("전직 시스템 연결 대기"); return; }
            advancement.TryAdvance();
            Toast(advancement.LastMessage);
        }

        private void TrainPet(PetProgressionPrototype.PetElement element)
        {
            if (petProgression == null || goldField == null) { Toast("펫 성장 연결 대기"); return; }
            long before = Gold;
            if (!petProgression.SpendGoldToTrain(element, before, out long cost))
            {
                Toast(petProgression.LastMessage);
                return;
            }
            goldField.SetValue(arena, Math.Max(0L, before - cost));
            saveService?.SaveNow();
            Toast(petProgression.LastMessage);
        }

        private void BuyEgg()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.BuyPetEgg();
            Toast(coreShop.LastMessage);
        }

        private void StartIncubation()
        {
            if (legacyPets == null || startIncubationMethod == null) { Toast("부화 시스템 연결 대기"); return; }
            startIncubationMethod.Invoke(legacyPets, null);
            Toast("부화 시작 요청 완료");
        }

        private void ClaimDaily()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.ClaimDailyReward();
            Toast(coreShop.LastMessage);
        }

        private void BuyGoldSupply()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.BuyGoldSupply();
            saveService?.SaveNow();
            Toast(coreShop.LastMessage);
        }

        private void FinishIncubation()
        {
            if (coreShop == null) { Toast("상점 연결 대기"); return; }
            coreShop.FinishIncubationNow();
            Toast(coreShop.LastMessage);
        }

        private void ChangeVolume(bool bgm, float delta)
        {
            if (settings == null) { Toast("설정 연결 대기"); return; }
            if (bgm) settings.SetBgmVolume(settings.BgmVolume + delta);
            else settings.SetSfxVolume(settings.SfxVolume + delta);
            Toast(settings.LastMessage);
        }

        private void SetFps(int fps)
        {
            if (settings == null) { Toast("설정 연결 대기"); return; }
            settings.SetFrameRate(fps);
            Toast(settings.LastMessage);
        }

        private void ToggleVibration()
        {
            if (settings == null) { Toast("설정 연결 대기"); return; }
            settings.ToggleVibration();
            Toast(settings.LastMessage);
        }

        private void SaveNow()
        {
            saveService?.SaveNow(true);
            Toast(saveService == null ? "저장 서비스 연결 대기" : saveService.LastMessage);
        }

        private void RestoreBackup()
        {
            bool restored = saveService != null && saveService.TryRestoreBackup();
            Toast(restored ? saveService.LastMessage : saveService == null ? "저장 서비스 연결 대기" : saveService.LastMessage);
        }

        private void Toast(string message)
        {
            if (sourceToastMethod != null && sourceUi != null)
            {
                sourceToastMethod.Invoke(sourceUi, new object[] { message });
                return;
            }
            Debug.Log("[PeanutWarrior] " + message);
        }

        private long Gold => goldField == null || arena == null ? 0L : Convert.ToInt64(goldField.GetValue(arena));

        private static string Requirement(string title, long current, long required)
        {
            return $"{(current >= required ? "완료" : "부족")}  {title}  {current:N0}/{required:N0}";
        }

        private static int ReadInt(FieldInfo field, object target)
        {
            return field == null || target == null ? 0 : Convert.ToInt32(field.GetValue(target));
        }

        private static float ReadFloat(FieldInfo field, object target)
        {
            return field == null || target == null ? 0f : Convert.ToSingle(field.GetValue(target));
        }

        private static bool ReadBool(FieldInfo field, object target)
        {
            return field != null && target != null && Convert.ToBoolean(field.GetValue(target));
        }

        private GameObject Card(Transform parent, string name, float x, float y, float width, float height, Color color, Color border)
        {
            RectTransform rect = Rect(parent, name, x, y, width, height);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            Outline outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(border.r, border.g, border.b, 0.45f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
            return rect.gameObject;
        }

        private GameObject Badge(Transform parent, string text, float x, float y, float width, float height, Color color, Color textColor, int size)
        {
            GameObject badge = Card(parent, "Badge", x, y, width, height, color, color);
            Label(badge.transform, text, 4f, 2f, width - 8f, height - 4f, size, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return badge;
        }

        private Button Button(Transform parent, string text, float x, float y, float width, float height, Color color, Color textColor, Action action)
        {
            GameObject go = Card(parent, "Button", x, y, width, height, color, color);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            if (action != null) button.onClick.AddListener(() => action());
            Label(go.transform, text, 6f, 3f, width - 12f, height - 6f, 15, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        private Text Label(Transform parent, string text, float x, float y, float width, float height, int size, Color color, TextAnchor anchor, FontStyle style)
        {
            RectTransform rect = Rect(parent, "Text", x, y, width, height);
            Text label = rect.gameObject.AddComponent<Text>();
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

        private static Sprite SolidSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite RoundedSprite()
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
