using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Bright, Peanut Warrior-specific mobile HUD and progression screens.
    /// The supplied idle-RPG screenshots are used only as layout references;
    /// menu names, progression concepts, colors, and copy are original to this game.
    /// </summary>
    public sealed class MobileIdleUiPrototype : MonoBehaviour
    {
        private enum ScreenMode { Main, Growth, Swords, Skills, Minis, Adventure, Missions, Shop }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private ScreenMode mode;
        private int growthTab;
        private int purchaseAmount = 1;
        private string toast = "땅콩월드 전투 UI 적용";
        private float toastTimer = 2.5f;

        private FieldInfo goldField;
        private FieldInfo fragmentsField;
        private FieldInfo diamondsField;
        private FieldInfo playerHpField;
        private FieldInfo playerMpField;
        private FieldInfo attackLevelField;
        private FieldInfo hpLevelField;
        private FieldInfo maxMpLevelField;
        private FieldInfo mpRegenLevelField;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo combatPowerProperty;

        private FieldInfo critChanceLevelField;
        private FieldInfo critDamageLevelField;
        private FieldInfo goldGainLevelField;
        private FieldInfo hpRegenLevelField;

        private Texture2D fieldWashTexture;
        private Texture2D panelTexture;
        private Texture2D panelAltTexture;
        private Texture2D accentTexture;
        private Texture2D selectedTexture;
        private Texture2D barBackTexture;
        private Texture2D redTexture;
        private Texture2D blueTexture;
        private Texture2D goldTexture;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle darkTextStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle bossButtonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MobileIdleUiPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMobileIdleUiPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<MobileIdleUiPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            goldField = arenaType.GetField("gold", PrivateInstance);
            fragmentsField = arenaType.GetField("fragments", PrivateInstance);
            diamondsField = arenaType.GetField("diamonds", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            attackLevelField = arenaType.GetField("attackLevel", PrivateInstance);
            hpLevelField = arenaType.GetField("hpLevel", PrivateInstance);
            maxMpLevelField = arenaType.GetField("maxMpLevel", PrivateInstance);
            mpRegenLevelField = arenaType.GetField("mpRegenLevel", PrivateInstance);
            maxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            maxMpProperty = arenaType.GetProperty("PlayerMaxMp", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            combatPowerProperty = arenaType.GetProperty("CombatPower", PrivateInstance);

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                critChanceLevelField = growthType.GetField("critChanceLevel", PrivateInstance);
                critDamageLevelField = growthType.GetField("critDamageLevel", PrivateInstance);
                goldGainLevelField = growthType.GetField("goldGainLevel", PrivateInstance);
                hpRegenLevelField = growthType.GetField("hpRegenLevel", PrivateInstance);
            }

            BuildStyles();
        }

        private void Update()
        {
            if (toastTimer > 0f) toastTimer -= Time.deltaTime;
        }

        private long Gold => goldField == null ? 0L : (long)goldField.GetValue(arena);
        private long Fragments => fragmentsField == null ? 0L : (long)fragmentsField.GetValue(arena);
        private int Diamonds => diamondsField == null ? 0 : (int)diamondsField.GetValue(arena);
        private float PlayerHp => playerHpField == null ? 0f : Convert.ToSingle(playerHpField.GetValue(arena));
        private float PlayerMp => playerMpField == null ? 0f : Convert.ToSingle(playerMpField.GetValue(arena));
        private float MaxHp => maxHpProperty == null ? 1f : Convert.ToSingle(maxHpProperty.GetValue(arena));
        private float MaxMp => maxMpProperty == null ? 1f : Convert.ToSingle(maxMpProperty.GetValue(arena));
        private float AttackDamage => attackDamageProperty == null ? 0f : Convert.ToSingle(attackDamageProperty.GetValue(arena));
        private int CombatPower => combatPowerProperty == null
            ? Mathf.RoundToInt(AttackDamage * 10f)
            : Convert.ToInt32(combatPowerProperty.GetValue(arena));

        private void BuildStyles()
        {
            fieldWashTexture = MakeTexture(new Color(0.82f, 0.94f, 0.71f, 0.16f));
            panelTexture = MakeTexture(new Color(0.96f, 0.91f, 0.70f, 0.96f));
            panelAltTexture = MakeTexture(new Color(0.82f, 0.93f, 0.77f, 0.96f));
            accentTexture = MakeTexture(new Color(0.22f, 0.61f, 0.31f, 1f));
            selectedTexture = MakeTexture(new Color(0.96f, 0.72f, 0.20f, 1f));
            barBackTexture = MakeTexture(new Color(0.20f, 0.24f, 0.17f, 0.78f));
            redTexture = MakeTexture(new Color(0.89f, 0.24f, 0.19f, 1f));
            blueTexture = MakeTexture(new Color(0.20f, 0.50f, 0.89f, 1f));
            goldTexture = MakeTexture(new Color(0.96f, 0.64f, 0.12f, 1f));

            Color darkBrown = new Color(0.18f, 0.11f, 0.05f);
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = darkBrown }
            };
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.13f, 0.39f, 0.16f) }
            };
            normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            darkTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = darkBrown }
            };
            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.22f, 0.25f, 0.14f) }
            };
            centeredStyle = new GUIStyle(normalStyle) { alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { background = panelAltTexture, textColor = darkBrown },
                hover = { background = selectedTexture, textColor = darkBrown },
                active = { background = accentTexture, textColor = Color.white }
            };
            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { background = selectedTexture, textColor = darkBrown }
            };
            bossButtonStyle = new GUIStyle(buttonStyle)
            {
                fontSize = 15,
                normal = { background = redTexture, textColor = Color.white },
                hover = { background = goldTexture, textColor = darkBrown }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (arena == null || stageFlow == null) return;
            GUI.depth = -1000;
            if (titleStyle == null) BuildStyles();

            if (mode == ScreenMode.Main)
            {
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), fieldWashTexture, ScaleMode.StretchToFill);
                DrawMainScreen();
            }
            else if (mode == ScreenMode.Growth)
            {
                DrawFullScreenBackground();
                DrawGrowthScreen();
            }
            else
            {
                DrawFullScreenBackground();
                DrawPlaceholderScreen();
            }

            if (toastTimer > 0f)
            {
                Rect toastRect = new Rect(Screen.width * 0.5f - 205f, Screen.height - 120f, 410f, 38f);
                GUI.DrawTexture(toastRect, accentTexture);
                GUI.Label(toastRect, toast, centeredStyle);
            }
        }

        private void DrawFullScreenBackground()
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), panelAltTexture, ScaleMode.StretchToFill);
        }

        private void DrawMainScreen()
        {
            float w = Screen.width;
            float h = Screen.height;
            DrawPlayerStatus(new Rect(16f, 16f, 320f, 104f));
            DrawCurrencyBar(new Rect(w * 0.36f, 16f, w * 0.34f, 54f));
            DrawStageBar(new Rect(w * 0.34f, 80f, w * 0.38f, 76f));
            DrawQuickMenu(new Rect(w - 258f, 82f, 242f, 275f));
            DrawSkillDock(new Rect(w - 342f, h - 226f, 326f, 132f));
            DrawBottomNavigation();

            GUI.DrawTexture(new Rect(16f, h - 73f, 350f, 42f), panelTexture);
            GUI.Label(new Rect(28f, h - 65f, 330f, 26f),
                $"전투력 {CombatPower:N0}  ·  땅콩 수호 처치 {stageFlow.MonsterKills}/100", darkTextStyle);
        }

        private void DrawPlayerStatus(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 7f, 190f, 30f), "땅콩전사", titleStyle);
            GUI.Label(new Rect(rect.x + 220f, rect.y + 11f, 82f, 24f),
                $"Lv.{Mathf.Max(1, CombatPower / 25)}", headerStyle);
            DrawBar(new Rect(rect.x + 14f, rect.y + 43f, rect.width - 28f, 20f), PlayerHp, MaxHp, redTexture,
                $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            DrawBar(new Rect(rect.x + 14f, rect.y + 70f, rect.width - 28f, 20f), PlayerMp, MaxMp, blueTexture,
                $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
        }

        private void DrawCurrencyBar(Rect rect)
        {
            float third = (rect.width - 12f) / 3f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, third, rect.height), panelTexture);
            GUI.DrawTexture(new Rect(rect.x + third + 6f, rect.y, third, rect.height), panelTexture);
            GUI.DrawTexture(new Rect(rect.x + (third + 6f) * 2f, rect.y, third, rect.height), panelTexture);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, third - 20f, 30f), $"골드 {Gold:N0}", headerStyle);
            GUI.Label(new Rect(rect.x + third + 18f, rect.y + 12f, third - 20f, 30f), $"다이아 {Diamonds:N0}", headerStyle);
            GUI.Label(new Rect(rect.x + (third + 6f) * 2f + 12f, rect.y + 12f, third - 20f, 30f), $"조각 {Fragments:N0}", headerStyle);
        }

        private void DrawStageBar(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 7f, rect.width - 32f, 24f),
                $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}", darkTextStyle);
            DrawBar(new Rect(rect.x + 16f, rect.y + 39f, rect.width - 122f, 22f),
                stageFlow.MonsterKills, StageFlowController.RequiredKills, goldTexture,
                $"보스 자격 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            if (GUI.Button(new Rect(rect.x + rect.width - 96f, rect.y + 31f, 80f, 36f), "균왕 도전", bossButtonStyle))
            {
                if (!stageFlow.TryStartBossBattle()) ShowToast("일반 몬스터 100마리를 먼저 처치해야 합니다");
            }
        }

        private void DrawQuickMenu(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 24f), "땅콩월드 바로가기", headerStyle);
            string[] labels =
            {
                "전직소", "검 보관함", "알 부화", "세계지도",
                "도전 기록", "방치 보상", "수호 임무", "우편함",
                "설정", "도움말", "쿠폰", "소식"
            };
            ScreenMode[] targets =
            {
                ScreenMode.Growth, ScreenMode.Swords, ScreenMode.Minis, ScreenMode.Adventure,
                ScreenMode.Missions, ScreenMode.Adventure, ScreenMode.Missions, ScreenMode.Main,
                ScreenMode.Main, ScreenMode.Main, ScreenMode.Shop, ScreenMode.Main
            };
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                Rect button = new Rect(rect.x + 9f + col * 57f, rect.y + 38f + row * 72f, 52f, 62f);
                if (GUI.Button(button, labels[i], buttonStyle)) mode = targets[i];
            }
        }

        private void DrawSkillDock(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 6f, rect.width - 20f, 22f),
                stageFlow.Phase == StageFlowPhase.BossBattle ? "균왕 전용 기술" : "땅콩 검술 자동 발동", headerStyle);
            string[] hunting = { "회전폭풍", "검기난사", "추적검무", "천지절단" };
            string[] boss = { "연속참격", "급소절개", "속성각인", "차원종결" };
            string[] names = stageFlow.Phase == StageFlowPhase.BossBattle ? boss : hunting;
            for (int i = 0; i < 4; i++)
            {
                Rect skill = new Rect(rect.x + 10f + i * 78f, rect.y + 35f, 70f, 78f);
                GUI.Button(skill, $"{i + 1}\n{names[i]}\nAUTO", buttonStyle);
            }
        }

        private void DrawBottomNavigation()
        {
            string[] labels = { "전사 성장", "검", "검술", "미니 땅콩", "모험", "수호 임무", "땅콩 상점" };
            ScreenMode[] modes =
            {
                ScreenMode.Growth, ScreenMode.Swords, ScreenMode.Skills,
                ScreenMode.Minis, ScreenMode.Adventure, ScreenMode.Missions, ScreenMode.Shop
            };
            float totalWidth = Mathf.Min(Screen.width - 32f, 950f);
            float buttonWidth = totalWidth / labels.Length;
            float start = (Screen.width - totalWidth) * 0.5f;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(start + i * buttonWidth, Screen.height - 78f, buttonWidth - 4f, 62f);
                if (GUI.Button(rect, labels[i], mode == modes[i] ? selectedButtonStyle : buttonStyle)) mode = modes[i];
            }
        }

        private void DrawGrowthScreen()
        {
            float w = Screen.width;
            float h = Screen.height;
            GUI.Label(new Rect(78f, 20f, 430f, 42f), "땅콩전사 성장", titleStyle);
            if (GUI.Button(new Rect(16f, 16f, 48f, 48f), "〈", selectedButtonStyle)) mode = ScreenMode.Main;
            GUI.DrawTexture(new Rect(w - 300f, 18f, 280f, 46f), panelTexture);
            GUI.Label(new Rect(w - 278f, 26f, 250f, 30f), $"보유 골드 {Gold:N0}", headerStyle);

            string[] tabs = { "기초 능력", "껍질 단련", "전직", "속성 연구", "방치 효율" };
            float tabWidth = Mathf.Min(180f, (w - 32f) / tabs.Length);
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tab = new Rect(16f + i * tabWidth, 82f, tabWidth - 6f, 48f);
                if (GUI.Button(tab, tabs[i], growthTab == i ? selectedButtonStyle : buttonStyle)) growthTab = i;
            }

            Rect summary = new Rect(16f, 148f, w * 0.37f, h - 168f);
            Rect list = new Rect(summary.xMax + 18f, 148f, w - summary.width - 50f, h - 168f);
            GUI.DrawTexture(summary, panelTexture);
            GUI.DrawTexture(list, panelAltTexture);

            if (growthTab == 0)
            {
                DrawGrowthSummary(summary);
                DrawGrowthList(list);
            }
            else
            {
                GUI.Label(new Rect(summary.x + 22f, summary.y + 24f, summary.width - 44f, 36f), tabs[growthTab], titleStyle);
                GUI.Label(new Rect(summary.x + 22f, summary.y + 76f, summary.width - 44f, 160f),
                    GrowthTabDescription(growthTab), darkTextStyle);
                GUI.Label(new Rect(list.x + 22f, list.y + 24f, list.width - 44f, 36f), "다음 연결 대상", titleStyle);
                GUI.Label(new Rect(list.x + 22f, list.y + 76f, list.width - 44f, 160f),
                    "현재 전투 데이터에 맞춰 순차적으로 연결합니다.\n헌터키우기의 메뉴를 복제하지 않고 땅콩월드 설정만 사용합니다.", darkTextStyle);
            }
        }

        private string GrowthTabDescription(int index)
        {
            return index switch
            {
                1 => "껍질의 방어력과 생명력, 회복 성능을 단련하는 땅콩 고유 성장입니다.",
                2 => "스테이지·전투력·골드·다이아 조건을 충족해 전투 껍질로 진화합니다.",
                3 => "화염·냉기·번개·무속성 검 효과와 상태이상을 연구합니다.",
                4 => "오프라인 골드·조각 획득량과 최대 방치 시간을 높입니다.",
                _ => "땅콩전사의 능력을 성장시킵니다."
            };
        }

        private void DrawGrowthSummary(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 34f), "현재 전투 능력", titleStyle);
            string[] names = { "공격력", "전투력", "최대 HP", "최대 MP", "초당 HP 회복", "초당 MP 회복", "치명타 확률", "치명타 피해" };
            string[] values =
            {
                AttackDamage.ToString("N1"), CombatPower.ToString("N0"), MaxHp.ToString("N0"), MaxMp.ToString("N0"),
                GetGrowthValue("hpRegenLevel", 1).ToString("N0"), GetLevel(mpRegenLevelField).ToString("N0"),
                $"{Mathf.Min(100f, 5f + (GetGrowthValue("critChanceLevel", 1) - 1) * 2f):0}%",
                $"{150f + (GetGrowthValue("critDamageLevel", 1) - 1) * 12f:0}%"
            };
            for (int i = 0; i < names.Length; i++)
            {
                float y = rect.y + 66f + i * 39f;
                GUI.Label(new Rect(rect.x + 22f, y, rect.width * 0.57f, 28f), names[i], darkTextStyle);
                GUI.Label(new Rect(rect.x + rect.width * 0.58f, y, rect.width * 0.36f, 28f), values[i], headerStyle);
            }
        }

        private void DrawGrowthList(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, 210f, 32f), "한 번에 강화", titleStyle);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                Rect amountRect = new Rect(rect.x + 240f + i * 95f, rect.y + 12f, 86f, 38f);
                if (GUI.Button(amountRect, $"×{amounts[i]}", purchaseAmount == amounts[i] ? selectedButtonStyle : buttonStyle))
                    purchaseAmount = amounts[i];
            }

            DrawUpgradeRow(rect, 0, "검 공격력", attackLevelField, 20L, "검");
            DrawUpgradeRow(rect, 1, "껍질 생명력", hpLevelField, 25L, "껍질");
            DrawUpgradeRow(rect, 2, "최대 마력", maxMpLevelField, 30L, "마력");
            DrawUpgradeRow(rect, 3, "껍질 재생", hpRegenLevelField, 40L, "재생");
            DrawUpgradeRow(rect, 4, "마력 회복", mpRegenLevelField, 35L, "회복");
            DrawUpgradeRow(rect, 5, "정밀 베기", critChanceLevelField, 45L, "치명");
            DrawUpgradeRow(rect, 6, "치명 일격", critDamageLevelField, 55L, "강타");
            DrawUpgradeRow(rect, 7, "땅콩 수확량", goldGainLevelField, 65L, "수확");
        }

        private void DrawUpgradeRow(Rect panel, int row, string label, FieldInfo field, long baseCost, string icon)
        {
            float y = panel.y + 64f + row * 62f;
            Rect rowRect = new Rect(panel.x + 18f, y, panel.width - 36f, 54f);
            GUI.DrawTexture(rowRect, row % 2 == 0 ? panelTexture : panelAltTexture);
            int level = GetLevel(field);
            long cost = baseCost * Mathf.Max(1, level) * purchaseAmount;
            GUI.Label(new Rect(rowRect.x + 12f, rowRect.y + 14f, 56f, 28f), icon, headerStyle);
            GUI.Label(new Rect(rowRect.x + 74f, rowRect.y + 4f, 220f, 25f), label, headerStyle);
            GUI.Label(new Rect(rowRect.x + 74f, rowRect.y + 28f, 220f, 20f),
                $"Lv.{level:N0} → Lv.{level + purchaseAmount:N0}", smallStyle);
            if (GUI.Button(new Rect(rowRect.xMax - 150f, rowRect.y + 7f, 136f, 40f),
                    $"강화\n{cost:N0}G", buttonStyle))
                UpgradeField(field, baseCost, label);
        }

        private void UpgradeField(FieldInfo field, long baseCost, string label)
        {
            if (field == null)
            {
                ShowToast($"{label} 연결 준비 중");
                return;
            }

            int level = GetLevel(field);
            long cost = baseCost * Mathf.Max(1, level) * purchaseAmount;
            if (Gold < cost)
            {
                ShowToast($"골드 부족 · {cost:N0}G 필요");
                return;
            }

            goldField.SetValue(arena, Gold - cost);
            object target = field.DeclaringType == typeof(GrowthExpansionPrototype) ? (object)growth : arena;
            field.SetValue(target, level + purchaseAmount);
            if (field == hpLevelField) playerHpField?.SetValue(arena, MaxHp);
            if (field == maxMpLevelField) playerMpField?.SetValue(arena, MaxMp);
            ShowToast($"{label} ×{purchaseAmount} 강화 완료");
        }

        private int GetLevel(FieldInfo field)
        {
            if (field == null) return 1;
            object target = field.DeclaringType == typeof(GrowthExpansionPrototype) ? (object)growth : arena;
            if (target == null) return 1;
            return Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        }

        private int GetGrowthValue(string name, int fallback)
        {
            if (growth == null) return fallback;
            FieldInfo field = typeof(GrowthExpansionPrototype).GetField(name, PrivateInstance);
            return field == null ? fallback : Mathf.Max(1, Convert.ToInt32(field.GetValue(growth)));
        }

        private void DrawPlaceholderScreen()
        {
            GUI.Label(new Rect(74f, 20f, 500f, 42f), ModeTitle(), titleStyle);
            if (GUI.Button(new Rect(16f, 16f, 48f, 48f), "〈", selectedButtonStyle)) mode = ScreenMode.Main;
            Rect panel = new Rect(24f, 92f, Screen.width - 48f, Screen.height - 122f);
            GUI.DrawTexture(panel, panelTexture);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 30f, panel.width - 60f, 44f), ModeTitle(), titleStyle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 94f, panel.width - 60f, 170f),
                ModeDescription(), darkTextStyle);
            DrawBottomNavigation();
        }

        private string ModeTitle()
        {
            return mode switch
            {
                ScreenMode.Swords => "검 보관함",
                ScreenMode.Skills => "땅콩 검술",
                ScreenMode.Minis => "미니 땅콩",
                ScreenMode.Adventure => "땅콩월드 모험",
                ScreenMode.Missions => "수호 임무",
                ScreenMode.Shop => "땅콩 상점",
                _ => "땅콩전사 키우기"
            };
        }

        private string ModeDescription()
        {
            return mode switch
            {
                ScreenMode.Swords => "사냥용 검과 균왕용 검을 따로 장착하고 속성·등급·중복 합성을 관리합니다.",
                ScreenMode.Skills => "사냥 검술 4개와 균왕 검술 4개를 강화하고 자동 사용 여부를 설정합니다.",
                ScreenMode.Minis => "알을 부화해 미니 땅콩 3마리를 편성하고 공격·치명타 능력을 성장시킵니다.",
                ScreenMode.Adventure => "클리어한 스테이지 이동, 오프라인 보상, 월드 진행 상황을 확인합니다.",
                ScreenMode.Missions => "몬스터 처치·스테이지 돌파·전직·성장 업적 보상을 수령합니다.",
                ScreenMode.Shop => "검 소환, 미니 알, 접속 보상 등 땅콩월드 전용 상품을 확인합니다.",
                _ => "땅콩월드를 침공한 곰팡이 군단을 막아내세요."
            };
        }

        private void ShowToast(string message)
        {
            toast = message;
            toastTimer = 2.2f;
        }

        private void DrawBar(Rect rect, float current, float maximum, Texture2D fill, string label)
        {
            GUI.DrawTexture(rect, barBackTexture);
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), fill);
            GUI.Label(rect, label, centeredStyle);
        }
    }
}
