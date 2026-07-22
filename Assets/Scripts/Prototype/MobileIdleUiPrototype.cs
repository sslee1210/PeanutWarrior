using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Consolidated mobile-style prototype UI inspired by the supplied idle-RPG references.
    /// It renders above the legacy debug panels and provides a clean main HUD plus a dedicated
    /// growth screen without disabling the underlying combat systems.
    /// </summary>
    public sealed class MobileIdleUiPrototype : MonoBehaviour
    {
        private enum ScreenMode { Main, Growth, Skills, Equipment, Mini, Missions, Shop }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private ScreenMode mode;
        private int growthTab;
        private int purchaseAmount = 1;
        private string toast = "모바일 통합 UI 적용";
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

        private Texture2D darkTexture;
        private Texture2D panelTexture;
        private Texture2D accentTexture;
        private Texture2D selectedTexture;
        private Texture2D redTexture;
        private Texture2D blueTexture;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle normalStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;

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
        private int CombatPower => combatPowerProperty == null ? Mathf.RoundToInt(AttackDamage * 10f) : Convert.ToInt32(combatPowerProperty.GetValue(arena));

        private void BuildStyles()
        {
            darkTexture = MakeTexture(new Color(0.015f, 0.055f, 0.09f, 0.94f));
            panelTexture = MakeTexture(new Color(0.025f, 0.16f, 0.29f, 0.98f));
            accentTexture = MakeTexture(new Color(0.02f, 0.55f, 0.53f, 1f));
            selectedTexture = MakeTexture(new Color(0.02f, 0.34f, 0.64f, 1f));
            redTexture = MakeTexture(new Color(0.78f, 0.08f, 0.08f, 1f));
            blueTexture = MakeTexture(new Color(0.02f, 0.34f, 0.78f, 1f));

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, normal = { textColor = new Color(1f, 0.83f, 0.16f) } };
            normalStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.82f, 0.9f, 1f) } };
            centeredStyle = new GUIStyle(normalStyle) { alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { background = panelTexture, textColor = Color.white },
                hover = { background = selectedTexture, textColor = Color.white },
                active = { background = accentTexture, textColor = Color.white }
            };
            selectedButtonStyle = new GUIStyle(buttonStyle) { normal = { background = accentTexture, textColor = Color.white } };
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

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture, ScaleMode.StretchToFill);
            if (mode == ScreenMode.Main) DrawMainScreen();
            else if (mode == ScreenMode.Growth) DrawGrowthScreen();
            else DrawPlaceholderScreen();

            if (toastTimer > 0f)
            {
                Rect toastRect = new Rect(Screen.width * 0.5f - 190f, Screen.height - 122f, 380f, 38f);
                GUI.DrawTexture(toastRect, accentTexture);
                GUI.Label(toastRect, toast, centeredStyle);
            }
        }

        private void DrawMainScreen()
        {
            float w = Screen.width;
            float h = Screen.height;
            DrawPlayerStatus(new Rect(18f, 18f, 310f, 102f));
            DrawCurrencyBar(new Rect(w * 0.37f, 18f, w * 0.34f, 54f));
            DrawStageBar(new Rect(w * 0.37f, 82f, w * 0.34f, 72f));
            DrawQuickMenu(new Rect(w - 280f, 82f, 260f, 340f));
            DrawSkillDock(new Rect(w - 370f, h - 230f, 350f, 138f));
            DrawBottomNavigation();

            GUI.Label(new Rect(20f, h - 56f, 320f, 28f), $"전투력 {CombatPower:N0} · 처치 {stageFlow.MonsterKills}/100", normalStyle);
        }

        private void DrawPlayerStatus(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, 120f, 28f), "땅콩전사", titleStyle);
            GUI.Label(new Rect(rect.x + 200f, rect.y + 10f, 95f, 24f), $"Lv.{Mathf.Max(1, CombatPower / 25)}", headerStyle);
            DrawBar(new Rect(rect.x + 14f, rect.y + 43f, rect.width - 28f, 20f), PlayerHp, MaxHp, redTexture,
                $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            DrawBar(new Rect(rect.x + 14f, rect.y + 69f, rect.width - 28f, 20f), PlayerMp, MaxMp, blueTexture,
                $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
        }

        private void DrawCurrencyBar(Rect rect)
        {
            float half = (rect.width - 8f) * 0.5f;
            GUI.DrawTexture(new Rect(rect.x, rect.y, half, rect.height), panelTexture);
            GUI.DrawTexture(new Rect(rect.x + half + 8f, rect.y, half, rect.height), panelTexture);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, half - 28f, 30f), $"● {Gold:N0}", titleStyle);
            GUI.Label(new Rect(rect.x + half + 24f, rect.y + 12f, half - 28f, 30f), $"◆ {Diamonds:N0}", titleStyle);
        }

        private void DrawStageBar(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 7f, rect.width - 32f, 24f),
                $"⚔ {stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage}", normalStyle);
            DrawBar(new Rect(rect.x + 16f, rect.y + 34f, rect.width - 110f, 22f),
                stageFlow.MonsterKills, StageFlowController.RequiredKills, redTexture,
                $"{stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            if (GUI.Button(new Rect(rect.x + rect.width - 86f, rect.y + 30f, 70f, 30f), "BOSS", buttonStyle))
                stageFlow.TryStartBossBattle();
        }

        private void DrawQuickMenu(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            string[] labels = { "상점", "이벤트", "패스", "우편", "메뉴", "랭킹", "전직", "제작", "도감", "의상", "미션", "설정" };
            ScreenMode[] targets = { ScreenMode.Shop, ScreenMode.Missions, ScreenMode.Shop, ScreenMode.Main, ScreenMode.Main, ScreenMode.Main, ScreenMode.Growth, ScreenMode.Equipment, ScreenMode.Equipment, ScreenMode.Equipment, ScreenMode.Missions, ScreenMode.Main };
            for (int i = 0; i < labels.Length; i++)
            {
                int col = i % 4;
                int row = i / 4;
                Rect button = new Rect(rect.x + 10f + col * 61f, rect.y + 12f + row * 82f, 56f, 68f);
                if (GUI.Button(button, labels[i], buttonStyle)) mode = targets[i];
            }
        }

        private void DrawSkillDock(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 22f), "자동 스킬", smallStyle);
            for (int i = 0; i < 8; i++)
            {
                int col = i % 4;
                int row = i / 4;
                Rect skill = new Rect(rect.x + 10f + col * 83f, rect.y + 34f + row * 49f, 73f, 42f);
                GUI.Button(skill, $"{i + 1}\nAUTO", buttonStyle);
            }
        }

        private void DrawBottomNavigation()
        {
            string[] labels = { "성장", "장비", "스킬", "미니", "소환", "미션", "상점" };
            ScreenMode[] modes = { ScreenMode.Growth, ScreenMode.Equipment, ScreenMode.Skills, ScreenMode.Mini, ScreenMode.Shop, ScreenMode.Missions, ScreenMode.Shop };
            float totalWidth = Mathf.Min(Screen.width - 40f, 760f);
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
            GUI.Label(new Rect(72f, 22f, 300f, 42f), "성장", titleStyle);
            if (GUI.Button(new Rect(16f, 18f, 46f, 46f), "〈", selectedButtonStyle)) mode = ScreenMode.Main;
            GUI.DrawTexture(new Rect(w - 270f, 18f, 250f, 46f), panelTexture);
            GUI.Label(new Rect(w - 250f, 26f, 220f, 30f), $"● {Gold:N0}", headerStyle);

            string[] tabs = { "능력", "레벨", "룬", "각인", "각성", "오라", "영역 전개" };
            float tabWidth = Mathf.Min(145f, (w - 32f) / tabs.Length);
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tab = new Rect(16f + i * tabWidth, 82f, tabWidth - 6f, 48f);
                if (GUI.Button(tab, tabs[i], growthTab == i ? selectedButtonStyle : buttonStyle)) growthTab = i;
            }

            Rect summary = new Rect(16f, 148f, w * 0.37f, h - 168f);
            Rect list = new Rect(summary.xMax + 18f, 148f, w - summary.width - 50f, h - 168f);
            GUI.DrawTexture(summary, panelTexture);
            GUI.DrawTexture(list, panelTexture);

            if (growthTab == 0)
            {
                DrawGrowthSummary(summary);
                DrawGrowthList(list);
            }
            else
            {
                GUI.Label(new Rect(summary.x + 22f, summary.y + 24f, summary.width - 44f, 36f), tabs[growthTab], titleStyle);
                GUI.Label(new Rect(summary.x + 22f, summary.y + 76f, summary.width - 44f, 120f),
                    "이 탭은 다음 구현 단계에서 실제 데이터와 연결됩니다.\n현재 능력 탭의 구조와 동일한 방식으로 확장됩니다.", normalStyle);
                GUI.Label(new Rect(list.x + 22f, list.y + 24f, list.width - 44f, 36f), "준비 중", titleStyle);
            }
        }

        private void DrawGrowthSummary(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 34f), "현재 능력치", titleStyle);
            string[] names = { "공격력", "전투력", "HP", "MP", "초당 HP 회복", "초당 MP 회복", "치명타 확률", "치명타 피해" };
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
                GUI.Label(new Rect(rect.x + 22f, y, rect.width * 0.55f, 28f), names[i], normalStyle);
                GUI.Label(new Rect(rect.x + rect.width * 0.55f, y, rect.width * 0.38f, 28f), values[i], headerStyle);
            }
        }

        private void DrawGrowthList(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, 210f, 32f), "강화 배수", titleStyle);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                Rect amountRect = new Rect(rect.x + 240f + i * 95f, rect.y + 12f, 86f, 38f);
                if (GUI.Button(amountRect, $"×{amounts[i]}", purchaseAmount == amounts[i] ? selectedButtonStyle : buttonStyle)) purchaseAmount = amounts[i];
            }

            DrawUpgradeRow(rect, 0, "공격력", attackLevelField, 20L, "⚔");
            DrawUpgradeRow(rect, 1, "HP", hpLevelField, 25L, "♥");
            DrawUpgradeRow(rect, 2, "MP", maxMpLevelField, 30L, "●");
            DrawUpgradeRow(rect, 3, "초당 HP 회복", hpRegenLevelField, 40L, "+");
            DrawUpgradeRow(rect, 4, "초당 MP 회복", mpRegenLevelField, 35L, "↻");
            DrawUpgradeRow(rect, 5, "치명타 확률", critChanceLevelField, 45L, "✦");
            DrawUpgradeRow(rect, 6, "치명타 피해", critDamageLevelField, 55L, "✹");
            DrawUpgradeRow(rect, 7, "골드 획득", goldGainLevelField, 65L, "●");
        }

        private void DrawUpgradeRow(Rect panel, int row, string label, FieldInfo field, long baseCost, string icon)
        {
            float y = panel.y + 64f + row * 62f;
            Rect rowRect = new Rect(panel.x + 18f, y, panel.width - 36f, 54f);
            GUI.DrawTexture(rowRect, row % 2 == 0 ? selectedTexture : panelTexture);
            int level = GetLevel(field);
            long cost = baseCost * Mathf.Max(1, level) * purchaseAmount;
            GUI.Label(new Rect(rowRect.x + 12f, rowRect.y + 7f, 48f, 38f), icon, titleStyle);
            GUI.Label(new Rect(rowRect.x + 64f, rowRect.y + 4f, 220f, 25f), label, headerStyle);
            GUI.Label(new Rect(rowRect.x + 64f, rowRect.y + 28f, 220f, 20f), $"Lv.{level:N0} → Lv.{level + purchaseAmount:N0}", smallStyle);
            if (GUI.Button(new Rect(rowRect.xMax - 150f, rowRect.y + 7f, 136f, 40f), $"강화\n{cost:N0}G", buttonStyle))
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
            GUI.Label(new Rect(72f, 22f, 500f, 42f), ModeTitle(), titleStyle);
            if (GUI.Button(new Rect(16f, 18f, 46f, 46f), "〈", selectedButtonStyle)) mode = ScreenMode.Main;
            Rect panel = new Rect(24f, 92f, Screen.width - 48f, Screen.height - 122f);
            GUI.DrawTexture(panel, panelTexture);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 30f, panel.width - 60f, 44f), ModeTitle(), titleStyle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 94f, panel.width - 60f, 140f),
                "메인 화면과 성장 화면의 모바일 UI 구조가 먼저 적용되었습니다.\n이 화면은 같은 디자인 시스템으로 장비·스킬·미니·미션·상점 기능을 옮기기 위한 자리입니다.", normalStyle);
            DrawBottomNavigation();
        }

        private string ModeTitle()
        {
            return mode switch
            {
                ScreenMode.Skills => "스킬",
                ScreenMode.Equipment => "장비",
                ScreenMode.Mini => "미니 땅콩",
                ScreenMode.Missions => "미션",
                ScreenMode.Shop => "상점",
                _ => "땅콩전사 키우기"
            };
        }

        private void ShowToast(string message)
        {
            toast = message;
            toastTimer = 2.2f;
        }

        private void DrawBar(Rect rect, float current, float maximum, Texture2D fill, string label)
        {
            GUI.DrawTexture(rect, darkTexture);
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), fill);
            GUI.Label(rect, label, centeredStyle);
        }
    }
}
