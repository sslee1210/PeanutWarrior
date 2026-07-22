using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Peanut Warrior-specific mobile HUD. Reference screenshots are used only for
    /// information hierarchy; menu names, progression and presentation are original.
    /// </summary>
    public sealed class MobileIdleUiPrototype : MonoBehaviour
    {
        private enum ScreenMode
        {
            Main,
            Growth,
            Swords,
            Skills,
            Minis,
            Adventure,
            Missions,
            Shop
        }

        private const float VirtualWidth = 1388f;
        private const float VirtualHeight = 830f;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly string[] skillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단",
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

        private readonly string[] elementNames = { "무속성", "화염", "냉기", "번개" };
        private readonly string[] elementDescriptions =
        {
            "공격 누적 후 추가 참격",
            "적에게 화상 지속 피해",
            "적의 이동속도 감소",
            "주변 적에게 연쇄 피해"
        };

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;
        private SkillManagementPrototype skillManager;
        private IdleSystemsPrototype idleSystems;
        private PrototypeShopAndDaily shopSystem;

        private ScreenMode mode;
        private int growthTab;
        private int purchaseAmount = 1;
        private bool quickMenuOpen;
        private string toast = "땅콩월드 전투 준비 완료";
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
        private FieldInfo skillLevelsField;
        private FieldInfo skillCooldownsField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private FieldInfo miniSlotsUnlockedField;
        private FieldInfo advancementTierField;
        private PropertyInfo maxHpProperty;
        private PropertyInfo maxMpProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo combatPowerProperty;

        private FieldInfo critChanceLevelField;
        private FieldInfo critDamageLevelField;
        private FieldInfo goldGainLevelField;
        private FieldInfo hpRegenLevelField;
        private FieldInfo skillAutoField;

        private FieldInfo miniAttackLevelField;
        private FieldInfo miniCritLevelField;
        private FieldInfo miniCritDamageLevelField;
        private FieldInfo eggsField;
        private FieldInfo hatchedMinisField;
        private FieldInfo incubatingField;
        private FieldInfo incubationRemainingField;

        private Texture2D screenTexture;
        private Texture2D panelTexture;
        private Texture2D panelAltTexture;
        private Texture2D panelStrongTexture;
        private Texture2D accentTexture;
        private Texture2D selectedTexture;
        private Texture2D barBackTexture;
        private Texture2D redTexture;
        private Texture2D blueTexture;
        private Texture2D goldTexture;
        private Texture2D transparentTexture;

        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle bossButtonStyle;
        private GUIStyle resourceStyle;
        private GUIStyle whiteStyle;

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
            skillManager = FindFirstObjectByType<SkillManagementPrototype>();
            idleSystems = FindFirstObjectByType<IdleSystemsPrototype>();
            shopSystem = FindFirstObjectByType<PrototypeShopAndDaily>();

            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            BindFields();
            BuildStyles();
        }

        private void BindFields()
        {
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
            skillLevelsField = arenaType.GetField("skillLevels", PrivateInstance);
            skillCooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            huntingElementField = arenaType.GetField("huntingElement", PrivateInstance);
            bossElementField = arenaType.GetField("bossElement", PrivateInstance);
            miniSlotsUnlockedField = arenaType.GetField("miniSlotsUnlocked", PrivateInstance);
            advancementTierField = arenaType.GetField("advancementTier", PrivateInstance);
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

            if (skillManager != null)
            {
                skillAutoField = typeof(SkillManagementPrototype).GetField("autoEnabled", PrivateInstance);
            }

            if (idleSystems != null)
            {
                Type idleType = typeof(IdleSystemsPrototype);
                miniAttackLevelField = idleType.GetField("miniAttackLevel", PrivateInstance);
                miniCritLevelField = idleType.GetField("miniCritLevel", PrivateInstance);
                miniCritDamageLevelField = idleType.GetField("miniCritDamageLevel", PrivateInstance);
                eggsField = idleType.GetField("eggs", PrivateInstance);
                hatchedMinisField = idleType.GetField("hatchedMinis", PrivateInstance);
                incubatingField = idleType.GetField("incubating", PrivateInstance);
                incubationRemainingField = idleType.GetField("incubationRemaining", PrivateInstance);
            }
        }

        private void Update()
        {
            if (toastTimer > 0f) toastTimer -= Time.deltaTime;
        }

        private long Gold => ReadLong(goldField, arena);
        private long Fragments => ReadLong(fragmentsField, arena);
        private int Diamonds => ReadInt(diamondsField, arena);
        private float PlayerHp => ReadFloat(playerHpField, arena);
        private float PlayerMp => ReadFloat(playerMpField, arena);
        private float MaxHp => ReadPropertyFloat(maxHpProperty, arena, 1f);
        private float MaxMp => ReadPropertyFloat(maxMpProperty, arena, 1f);
        private float AttackDamage => ReadPropertyFloat(attackDamageProperty, arena, 0f);
        private int CombatPower => combatPowerProperty == null
            ? Mathf.RoundToInt(AttackDamage * 10f)
            : Convert.ToInt32(combatPowerProperty.GetValue(arena));

        private int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        private float[] SkillCooldowns => skillCooldownsField?.GetValue(arena) as float[];
        private bool[] SkillAuto => skillAutoField?.GetValue(skillManager) as bool[];

        private void BuildStyles()
        {
            screenTexture = MakeTexture(new Color(0.90f, 0.95f, 0.81f, 1f));
            panelTexture = MakeTexture(new Color(0.98f, 0.93f, 0.72f, 0.96f));
            panelAltTexture = MakeTexture(new Color(0.82f, 0.93f, 0.76f, 0.96f));
            panelStrongTexture = MakeTexture(new Color(0.23f, 0.48f, 0.25f, 0.96f));
            accentTexture = MakeTexture(new Color(0.22f, 0.61f, 0.31f, 1f));
            selectedTexture = MakeTexture(new Color(0.96f, 0.72f, 0.20f, 1f));
            barBackTexture = MakeTexture(new Color(0.20f, 0.24f, 0.17f, 0.82f));
            redTexture = MakeTexture(new Color(0.89f, 0.24f, 0.19f, 1f));
            blueTexture = MakeTexture(new Color(0.20f, 0.50f, 0.89f, 1f));
            goldTexture = MakeTexture(new Color(0.96f, 0.64f, 0.12f, 1f));
            transparentTexture = MakeTexture(new Color(1f, 1f, 1f, 0f));

            Color brown = new Color(0.18f, 0.11f, 0.05f);
            Color green = new Color(0.11f, 0.38f, 0.14f);

            titleStyle = NewLabelStyle(28, brown, FontStyle.Bold, TextAnchor.MiddleLeft);
            headerStyle = NewLabelStyle(19, green, FontStyle.Bold, TextAnchor.MiddleLeft);
            bodyStyle = NewLabelStyle(16, brown, FontStyle.Bold, TextAnchor.MiddleLeft);
            smallStyle = NewLabelStyle(13, new Color(0.24f, 0.28f, 0.17f), FontStyle.Normal, TextAnchor.MiddleLeft);
            centeredStyle = NewLabelStyle(16, brown, FontStyle.Bold, TextAnchor.MiddleCenter);
            resourceStyle = NewLabelStyle(20, green, FontStyle.Bold, TextAnchor.MiddleCenter);
            whiteStyle = NewLabelStyle(15, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(5, 5, 4, 4),
                normal = { background = panelAltTexture, textColor = brown },
                hover = { background = selectedTexture, textColor = brown },
                active = { background = accentTexture, textColor = Color.white }
            };
            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { background = selectedTexture, textColor = brown }
            };
            bossButtonStyle = new GUIStyle(buttonStyle)
            {
                fontSize = 15,
                normal = { background = redTexture, textColor = Color.white },
                hover = { background = goldTexture, textColor = brown }
            };
        }

        private static GUIStyle NewLabelStyle(int size, Color color, FontStyle fontStyle, TextAnchor anchor)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = anchor,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = color }
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (arena == null || stageFlow == null) return;
            if (titleStyle == null) BuildStyles();

            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
            float offsetX = (Screen.width - VirtualWidth * scale) * 0.5f;
            float offsetY = (Screen.height - VirtualHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, Vector3.one * scale);
            GUI.depth = -1000;

            if (mode == ScreenMode.Main) DrawMainScreen();
            else DrawMenuScreen();

            if (toastTimer > 0f)
            {
                Rect toastRect = new Rect(469f, 690f, 450f, 42f);
                GUI.DrawTexture(toastRect, accentTexture);
                GUI.Label(toastRect, toast, whiteStyle);
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawMainScreen()
        {
            DrawPlayerStatus(new Rect(16f, 16f, 320f, 108f));
            DrawResourceBar(new Rect(492f, 16f, 480f, 54f));
            DrawStageBar(new Rect(445f, 80f, 570f, 78f));
            DrawStatusChip(new Rect(16f, 748f, 215f, 58f));
            DrawBottomNavigation();
            DrawSkillDock(new Rect(950f, 610f, 422f, 128f));

            if (GUI.Button(new Rect(1264f, 18f, 108f, 48f), quickMenuOpen ? "메뉴 닫기" : "땅콩 메뉴", buttonStyle))
                quickMenuOpen = !quickMenuOpen;

            if (quickMenuOpen) DrawQuickMenu(new Rect(1108f, 78f, 264f, 336f));
        }

        private void DrawPlayerStatus(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 6f, 184f, 34f), "땅콩전사", titleStyle);
            GUI.Label(new Rect(rect.x + 218f, rect.y + 9f, 88f, 28f), $"Lv.{Mathf.Max(1, CombatPower / 25)}", headerStyle);
            DrawBar(new Rect(rect.x + 14f, rect.y + 44f, rect.width - 28f, 21f), PlayerHp, MaxHp, redTexture,
                $"HP {Mathf.CeilToInt(PlayerHp):N0} / {Mathf.CeilToInt(MaxHp):N0}");
            DrawBar(new Rect(rect.x + 14f, rect.y + 72f, rect.width - 28f, 21f), PlayerMp, MaxMp, blueTexture,
                $"MP {Mathf.CeilToInt(PlayerMp):N0} / {Mathf.CeilToInt(MaxMp):N0}");
        }

        private void DrawResourceBar(Rect rect)
        {
            float third = (rect.width - 12f) / 3f;
            DrawResourceCell(new Rect(rect.x, rect.y, third, rect.height), $"골드\n{Gold:N0}");
            DrawResourceCell(new Rect(rect.x + third + 6f, rect.y, third, rect.height), $"다이아\n{Diamonds:N0}");
            DrawResourceCell(new Rect(rect.x + (third + 6f) * 2f, rect.y, third, rect.height), $"조각\n{Fragments:N0}");
        }

        private void DrawResourceCell(Rect rect, string text)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(rect, text, resourceStyle);
        }

        private void DrawStageBar(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 5f, 360f, 26f),
                $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}", bodyStyle);

            DrawBar(new Rect(rect.x + 16f, rect.y + 39f, 350f, 23f),
                stageFlow.MonsterKills, StageFlowController.RequiredKills, goldTexture,
                $"균왕 자격 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");

            string autoLabel = stageFlow.AutoChallenge ? "자동 도전 ON" : "자동 도전 OFF";
            if (GUI.Button(new Rect(rect.x + 378f, rect.y + 34f, 94f, 34f), autoLabel,
                    stageFlow.AutoChallenge ? selectedButtonStyle : buttonStyle))
                stageFlow.SetAutoChallenge(!stageFlow.AutoChallenge);

            GUI.enabled = stageFlow.CanChallengeBoss;
            if (GUI.Button(new Rect(rect.x + 480f, rect.y + 31f, 76f, 40f), "균왕 도전", bossButtonStyle))
            {
                if (!stageFlow.TryStartBossBattle()) ShowToast("일반 몬스터 100마리를 먼저 처치해야 합니다");
            }
            GUI.enabled = true;
        }

        private void DrawStatusChip(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 22f), $"전투력 {CombatPower:N0}", headerStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 30f, rect.width - 24f, 20f),
                $"현재 처치 {stageFlow.MonsterKills}/100", smallStyle);
        }

        private void DrawQuickMenu(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 28f), "땅콩월드 바로가기", headerStyle);

            string[] labels =
            {
                "전직", "검 보관함", "알 부화",
                "스테이지", "방치 보상", "수호 임무",
                "검술 연구", "업적", "소환 상점",
                "설정", "도움말", "소식"
            };
            ScreenMode[] targets =
            {
                ScreenMode.Growth, ScreenMode.Swords, ScreenMode.Minis,
                ScreenMode.Adventure, ScreenMode.Adventure, ScreenMode.Missions,
                ScreenMode.Skills, ScreenMode.Missions, ScreenMode.Shop,
                ScreenMode.Main, ScreenMode.Main, ScreenMode.Main
            };

            for (int i = 0; i < labels.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                Rect button = new Rect(rect.x + 12f + column * 81f, rect.y + 44f + row * 70f, 74f, 62f);
                if (!GUI.Button(button, labels[i], buttonStyle)) continue;
                mode = targets[i];
                quickMenuOpen = false;
                if (targets[i] == ScreenMode.Main) ShowToast($"{labels[i]} 기능은 제작 단계에서 연결합니다");
            }
        }

        private void DrawSkillDock(Rect rect)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 5f, rect.width - 24f, 26f),
                stageFlow.Phase == StageFlowPhase.BossBattle ? "균왕 전용 검술" : "자동 검술", headerStyle);

            int offset = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            int[] levels = SkillLevels;
            float[] cooldowns = SkillCooldowns;
            for (int i = 0; i < 4; i++)
            {
                int index = offset + i;
                int level = levels != null && index < levels.Length ? levels[index] : 1;
                float cooldown = cooldowns != null && index < cooldowns.Length ? Mathf.Max(0f, cooldowns[index]) : 0f;
                string state = cooldown > 0.05f ? $"{cooldown:0.0}초" : "AUTO";
                Rect skill = new Rect(rect.x + 10f + i * 102f, rect.y + 35f, 94f, 78f);
                if (GUI.Button(skill, $"{skillNames[index]}\nLv.{level}\n{state}", buttonStyle)) mode = ScreenMode.Skills;
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

            const float startX = 239f;
            const float width = 131f;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect rect = new Rect(startX + i * width, 748f, width - 5f, 58f);
                if (GUI.Button(rect, labels[i], mode == modes[i] ? selectedButtonStyle : buttonStyle)) mode = modes[i];
            }
        }

        private void DrawMenuScreen()
        {
            GUI.DrawTexture(new Rect(0f, 0f, VirtualWidth, VirtualHeight), screenTexture);
            GUI.DrawTexture(new Rect(0f, 0f, VirtualWidth, 76f), panelStrongTexture);
            if (GUI.Button(new Rect(16f, 14f, 52f, 48f), "뒤로", buttonStyle)) mode = ScreenMode.Main;
            GUI.Label(new Rect(86f, 12f, 460f, 52f), ModeTitle(), whiteStyle);
            DrawCompactResources(new Rect(980f, 14f, 392f, 48f));

            switch (mode)
            {
                case ScreenMode.Growth:
                    DrawGrowthScreen();
                    break;
                case ScreenMode.Swords:
                    DrawSwordScreen();
                    break;
                case ScreenMode.Skills:
                    DrawSkillScreen();
                    break;
                case ScreenMode.Minis:
                    DrawMiniScreen();
                    break;
                case ScreenMode.Adventure:
                    DrawAdventureScreen();
                    break;
                case ScreenMode.Missions:
                    DrawMissionScreen();
                    break;
                case ScreenMode.Shop:
                    DrawShopScreen();
                    break;
            }
        }

        private void DrawCompactResources(Rect rect)
        {
            float third = rect.width / 3f;
            GUI.Label(new Rect(rect.x, rect.y, third, rect.height), $"골드 {Gold:N0}", whiteStyle);
            GUI.Label(new Rect(rect.x + third, rect.y, third, rect.height), $"다이아 {Diamonds:N0}", whiteStyle);
            GUI.Label(new Rect(rect.x + third * 2f, rect.y, third, rect.height), $"조각 {Fragments:N0}", whiteStyle);
        }

        private void DrawGrowthScreen()
        {
            string[] tabs = { "기초 능력", "껍질 단련", "전직", "속성 연구", "방치 효율" };
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect tab = new Rect(20f + i * 180f, 88f, 170f, 48f);
                if (GUI.Button(tab, tabs[i], growthTab == i ? selectedButtonStyle : buttonStyle)) growthTab = i;
            }

            Rect summary = new Rect(20f, 154f, 410f, 650f);
            Rect list = new Rect(448f, 154f, 920f, 650f);
            GUI.DrawTexture(summary, panelTexture);
            GUI.DrawTexture(list, panelAltTexture);

            if (growthTab != 0)
            {
                GUI.Label(new Rect(summary.x + 22f, summary.y + 22f, summary.width - 44f, 40f), tabs[growthTab], titleStyle);
                GUI.Label(new Rect(summary.x + 22f, summary.y + 80f, summary.width - 44f, 190f), GrowthTabDescription(growthTab), bodyStyle);
                GUI.Label(new Rect(list.x + 28f, list.y + 28f, list.width - 56f, 40f), "전용 성장 화면 준비 중", titleStyle);
                GUI.Label(new Rect(list.x + 28f, list.y + 90f, list.width - 56f, 170f),
                    "현재 전투·전직·속성 데이터를 기반으로 순차 연결합니다.\n게임 고유 규칙만 사용하며 다른 게임의 메뉴를 복제하지 않습니다.", bodyStyle);
                return;
            }

            DrawGrowthSummary(summary);
            DrawGrowthList(list);
        }

        private string GrowthTabDescription(int index)
        {
            return index switch
            {
                1 => "껍질 생명력과 회복력, 피해 감소를 단련하는 땅콩 고유 성장입니다.",
                2 => "스테이지·전투력·골드·다이아 조건을 충족해 새로운 껍질로 전직합니다.",
                3 => "무속성·화염·냉기·번개 검의 상태이상과 공격 구조를 연구합니다.",
                4 => "오프라인 골드·조각 획득량과 최대 방치 보상 시간을 높입니다.",
                _ => "땅콩전사의 능력을 성장시킵니다."
            };
        }

        private void DrawGrowthSummary(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 20f, rect.y + 16f, rect.width - 40f, 38f), "현재 전투 능력", titleStyle);
            string[] names = { "검 공격력", "전투력", "최대 HP", "최대 MP", "껍질 재생", "마력 회복", "치명타 확률", "치명타 피해" };
            string[] values =
            {
                AttackDamage.ToString("N1"), CombatPower.ToString("N0"), MaxHp.ToString("N0"), MaxMp.ToString("N0"),
                ReadLevel(hpRegenLevelField, growth).ToString("N0"), ReadLevel(mpRegenLevelField, arena).ToString("N0"),
                $"{Mathf.Min(100f, 5f + (ReadLevel(critChanceLevelField, growth) - 1) * 2f):0}%",
                $"{150f + (ReadLevel(critDamageLevelField, growth) - 1) * 12f:0}%"
            };

            for (int i = 0; i < names.Length; i++)
            {
                float y = rect.y + 72f + i * 52f;
                GUI.Label(new Rect(rect.x + 24f, y, 215f, 30f), names[i], bodyStyle);
                GUI.Label(new Rect(rect.x + 246f, y, 140f, 30f), values[i], headerStyle);
            }
        }

        private void DrawGrowthList(Rect rect)
        {
            GUI.Label(new Rect(rect.x + 22f, rect.y + 14f, 210f, 36f), "한 번에 강화", titleStyle);
            int[] amounts = { 1, 10, 100 };
            for (int i = 0; i < amounts.Length; i++)
            {
                Rect amountRect = new Rect(rect.x + 560f + i * 100f, rect.y + 12f, 90f, 40f);
                if (GUI.Button(amountRect, $"×{amounts[i]}", purchaseAmount == amounts[i] ? selectedButtonStyle : buttonStyle))
                    purchaseAmount = amounts[i];
            }

            DrawUpgradeRow(rect, 0, "검 공격력", attackLevelField, arena, 20L);
            DrawUpgradeRow(rect, 1, "껍질 생명력", hpLevelField, arena, 25L);
            DrawUpgradeRow(rect, 2, "최대 마력", maxMpLevelField, arena, 30L);
            DrawUpgradeRow(rect, 3, "껍질 재생", hpRegenLevelField, growth, 40L);
            DrawUpgradeRow(rect, 4, "마력 회복", mpRegenLevelField, arena, 35L);
            DrawUpgradeRow(rect, 5, "정밀 베기", critChanceLevelField, growth, 45L);
            DrawUpgradeRow(rect, 6, "치명 일격", critDamageLevelField, growth, 55L);
            DrawUpgradeRow(rect, 7, "땅콩 수확량", goldGainLevelField, growth, 65L);
        }

        private void DrawUpgradeRow(Rect panel, int row, string label, FieldInfo field, object target, long baseCost)
        {
            float y = panel.y + 66f + row * 68f;
            Rect rowRect = new Rect(panel.x + 18f, y, panel.width - 36f, 58f);
            GUI.DrawTexture(rowRect, row % 2 == 0 ? panelTexture : transparentTexture);
            int level = ReadLevel(field, target);
            long cost = baseCost * Mathf.Max(1, level) * purchaseAmount;
            GUI.Label(new Rect(rowRect.x + 18f, rowRect.y + 4f, 260f, 28f), label, headerStyle);
            GUI.Label(new Rect(rowRect.x + 18f, rowRect.y + 31f, 260f, 22f), $"Lv.{level:N0} → Lv.{level + purchaseAmount:N0}", smallStyle);
            if (GUI.Button(new Rect(rowRect.xMax - 170f, rowRect.y + 8f, 154f, 42f), $"강화  {cost:N0}G", buttonStyle))
                UpgradeField(field, target, baseCost, label);
        }

        private void UpgradeField(FieldInfo field, object target, long baseCost, string label)
        {
            if (field == null || target == null)
            {
                ShowToast($"{label} 데이터 연결 준비 중");
                return;
            }

            int level = ReadLevel(field, target);
            long cost = baseCost * Mathf.Max(1, level) * purchaseAmount;
            if (!SpendGold(cost))
            {
                ShowToast($"골드 부족 · {cost:N0}G 필요");
                return;
            }

            field.SetValue(target, level + purchaseAmount);
            if (field == hpLevelField) playerHpField?.SetValue(arena, MaxHp);
            if (field == maxMpLevelField) playerMpField?.SetValue(arena, MaxMp);
            ShowToast($"{label} ×{purchaseAmount} 강화 완료");
        }

        private void DrawSwordScreen()
        {
            GUI.Label(new Rect(24f, 94f, 700f, 42f), "사냥 검과 균왕 검을 따로 장착합니다", titleStyle);
            GUI.Label(new Rect(24f, 136f, 900f, 30f),
                $"현재 사냥 검: {ElementName(ReadEnumIndex(huntingElementField))}   ·   현재 균왕 검: {ElementName(ReadEnumIndex(bossElementField))}", headerStyle);

            for (int i = 0; i < 4; i++)
            {
                Rect card = new Rect(28f + i * 335f, 190f, 310f, 390f);
                GUI.DrawTexture(card, i % 2 == 0 ? panelTexture : panelAltTexture);
                GUI.Label(new Rect(card.x + 20f, card.y + 22f, card.width - 40f, 42f), elementNames[i], titleStyle);
                GUI.Label(new Rect(card.x + 20f, card.y + 82f, card.width - 40f, 100f), elementDescriptions[i], bodyStyle);
                GUI.Label(new Rect(card.x + 20f, card.y + 196f, card.width - 40f, 70f),
                    i == ReadEnumIndex(huntingElementField) ? "사냥 검 장착 중" : "사냥 검 미장착", headerStyle);
                GUI.Label(new Rect(card.x + 20f, card.y + 252f, card.width - 40f, 70f),
                    i == ReadEnumIndex(bossElementField) ? "균왕 검 장착 중" : "균왕 검 미장착", headerStyle);
                if (GUI.Button(new Rect(card.x + 20f, card.y + 310f, 128f, 50f), "사냥 검 장착", buttonStyle)) SetElement(huntingElementField, i, "사냥 검");
                if (GUI.Button(new Rect(card.x + 162f, card.y + 310f, 128f, 50f), "균왕 검 장착", buttonStyle)) SetElement(bossElementField, i, "균왕 검");
            }

            if (GUI.Button(new Rect(410f, 620f, 270f, 62f), "사냥 검 소환 · 다이아 5", selectedButtonStyle))
                InvokePrivate(shopSystem, "SummonSword", new object[] { false }, "사냥 검 소환");
            if (GUI.Button(new Rect(708f, 620f, 270f, 62f), "균왕 검 소환 · 다이아 5", selectedButtonStyle))
                InvokePrivate(shopSystem, "SummonSword", new object[] { true }, "균왕 검 소환");
        }

        private void DrawSkillScreen()
        {
            GUI.Label(new Rect(24f, 94f, 600f, 42f), "땅콩 검술 강화", titleStyle);
            GUI.Label(new Rect(940f, 96f, 410f, 38f), $"보유 강화 조각 {Fragments:N0}", headerStyle);

            int[] levels = SkillLevels;
            float[] cooldowns = SkillCooldowns;
            bool[] auto = SkillAuto;
            for (int i = 0; i < 8; i++)
            {
                int column = i % 2;
                int row = i / 2;
                Rect card = new Rect(28f + column * 675f, 160f + row * 145f, 650f, 126f);
                GUI.DrawTexture(card, row % 2 == 0 ? panelTexture : panelAltTexture);
                int level = levels != null && i < levels.Length ? levels[i] : 1;
                float cooldown = cooldowns != null && i < cooldowns.Length ? Mathf.Max(0f, cooldowns[i]) : 0f;
                long cost = SkillUpgradeCost(i, level);
                GUI.Label(new Rect(card.x + 18f, card.y + 12f, 300f, 32f), skillNames[i], headerStyle);
                GUI.Label(new Rect(card.x + 18f, card.y + 48f, 250f, 26f), $"Lv.{level} · 쿨타임 {cooldown:0.0}초", bodyStyle);
                GUI.Label(new Rect(card.x + 18f, card.y + 80f, 260f, 24f), i < 4 ? "사냥용 검술" : "균왕용 검술", smallStyle);
                if (GUI.Button(new Rect(card.x + 350f, card.y + 20f, 130f, 42f), $"강화 {cost}조각", buttonStyle)) UpgradeSkill(i);
                bool isAuto = auto == null || i >= auto.Length || auto[i];
                if (GUI.Button(new Rect(card.x + 495f, card.y + 20f, 135f, 42f), isAuto ? "자동 사용 ON" : "자동 사용 OFF",
                        isAuto ? selectedButtonStyle : buttonStyle)) ToggleSkillAuto(i);
            }
        }

        private void UpgradeSkill(int index)
        {
            int[] levels = SkillLevels;
            if (levels == null || index < 0 || index >= levels.Length) return;
            long cost = SkillUpgradeCost(index, levels[index]);
            if (Fragments < cost)
            {
                ShowToast($"조각 부족 · {cost}개 필요");
                return;
            }
            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            ShowToast($"{skillNames[index]} Lv.{levels[index]} 강화 완료");
        }

        private static long SkillUpgradeCost(int index, int level)
        {
            return 2L + Mathf.Max(1, level) * 2L + index / 4;
        }

        private void ToggleSkillAuto(int index)
        {
            bool[] auto = SkillAuto;
            if (auto == null || index < 0 || index >= auto.Length)
            {
                ShowToast("자동 검술 설정 연결 준비 중");
                return;
            }
            auto[index] = !auto[index];
            PlayerPrefs.SetInt("PeanutWarrior.SkillAuto." + index, auto[index] ? 1 : 0);
            PlayerPrefs.Save();
            ShowToast($"{skillNames[index]} 자동 사용 {(auto[index] ? "활성" : "비활성")}");
        }

        private void DrawMiniScreen()
        {
            bool unlocked = miniSlotsUnlockedField != null && (bool)miniSlotsUnlockedField.GetValue(arena);
            int advancement = ReadInt(advancementTierField, arena);
            int eggs = ReadInt(eggsField, idleSystems);
            int hatched = ReadInt(hatchedMinisField, idleSystems);
            bool incubating = incubatingField != null && idleSystems != null && (bool)incubatingField.GetValue(idleSystems);
            float remaining = ReadFloat(incubationRemainingField, idleSystems);

            GUI.Label(new Rect(24f, 94f, 700f, 42f), "미니 땅콩 원정대", titleStyle);
            Rect status = new Rect(24f, 154f, 410f, 610f);
            Rect growthPanel = new Rect(452f, 154f, 916f, 610f);
            GUI.DrawTexture(status, panelTexture);
            GUI.DrawTexture(growthPanel, panelAltTexture);

            GUI.Label(new Rect(status.x + 22f, status.y + 20f, status.width - 44f, 40f), unlocked ? "미니 슬롯 3/3 해금" : "미니 슬롯 잠김", titleStyle);
            GUI.Label(new Rect(status.x + 22f, status.y + 76f, status.width - 44f, 120f),
                unlocked
                    ? $"주인공보다 한 단계 낮은 전직 외형으로\n3마리가 독립 이동·자동 공격합니다.\n현재 주인공 전직 {advancement}단계"
                    : "2차 전직을 달성하면 미니 땅콩 3슬롯이 한 번에 열립니다.", bodyStyle);
            GUI.Label(new Rect(status.x + 22f, status.y + 222f, status.width - 44f, 36f), $"보유 알 {eggs} · 부화 도감 {hatched}", headerStyle);
            GUI.Label(new Rect(status.x + 22f, status.y + 272f, status.width - 44f, 36f),
                incubating ? $"부화 진행 중 · {Mathf.CeilToInt(remaining)}초" : "현재 부화 대기", bodyStyle);
            if (GUI.Button(new Rect(status.x + 22f, status.y + 334f, 170f, 52f), "미니 알 구매\n다이아 3", buttonStyle))
                InvokePrivate(idleSystems, "BuyEgg", null, "미니 알 구매");
            if (GUI.Button(new Rect(status.x + 206f, status.y + 334f, 170f, 52f), "부화 시작", selectedButtonStyle))
                InvokePrivate(idleSystems, "StartIncubation", null, "알 부화 시작");

            GUI.Label(new Rect(growthPanel.x + 24f, growthPanel.y + 20f, 420f, 40f), "미니 성장", titleStyle);
            DrawMiniUpgrade(growthPanel, 0, "미니 공격력", miniAttackLevelField, 80L);
            DrawMiniUpgrade(growthPanel, 1, "미니 치명타 확률", miniCritLevelField, 100L);
            DrawMiniUpgrade(growthPanel, 2, "미니 치명타 피해", miniCritDamageLevelField, 120L);

            GUI.Label(new Rect(growthPanel.x + 24f, growthPanel.y + 330f, 600f, 34f), "편성 원칙", headerStyle);
            GUI.Label(new Rect(growthPanel.x + 24f, growthPanel.y + 378f, growthPanel.width - 48f, 150f),
                "사냥 편성과 균왕 편성을 별도로 저장합니다.\n미니는 검을 장착하지 않으며 화염·냉기·번개 고유 속성으로 공격합니다.\nHP와 MP가 없고 적의 공격 대상이 되지 않습니다.", bodyStyle);
        }

        private void DrawMiniUpgrade(Rect panel, int row, string label, FieldInfo field, long baseCost)
        {
            int level = ReadLevel(field, idleSystems);
            long cost = baseCost * level;
            Rect rowRect = new Rect(panel.x + 24f, panel.y + 82f + row * 78f, panel.width - 48f, 66f);
            GUI.DrawTexture(rowRect, panelTexture);
            GUI.Label(new Rect(rowRect.x + 18f, rowRect.y + 9f, 330f, 28f), label, headerStyle);
            GUI.Label(new Rect(rowRect.x + 18f, rowRect.y + 36f, 260f, 22f), $"현재 Lv.{level}", smallStyle);
            if (GUI.Button(new Rect(rowRect.xMax - 180f, rowRect.y + 12f, 160f, 42f), $"강화 {cost:N0}G", buttonStyle))
            {
                if (field == null || idleSystems == null) return;
                if (!SpendGold(cost))
                {
                    ShowToast("골드가 부족합니다");
                    return;
                }
                field.SetValue(idleSystems, level + 1);
                ShowToast($"{label} Lv.{level + 1} 강화 완료");
            }
        }

        private void DrawAdventureScreen()
        {
            GUI.Label(new Rect(24f, 94f, 700f, 42f), "땅콩월드 모험", titleStyle);
            Rect stageCard = new Rect(24f, 154f, 650f, 500f);
            Rect ruleCard = new Rect(694f, 154f, 674f, 500f);
            GUI.DrawTexture(stageCard, panelTexture);
            GUI.DrawTexture(ruleCard, panelAltTexture);

            GUI.Label(new Rect(stageCard.x + 24f, stageCard.y + 22f, stageCard.width - 48f, 40f),
                $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage}", titleStyle);
            GUI.Label(new Rect(stageCard.x + 24f, stageCard.y + 84f, stageCard.width - 48f, 35f),
                $"일반 몬스터 처치 {stageFlow.MonsterKills}/100", headerStyle);
            DrawBar(new Rect(stageCard.x + 24f, stageCard.y + 132f, stageCard.width - 48f, 30f),
                stageFlow.MonsterKills, 100f, goldTexture, $"균왕 도전 준비 {stageFlow.MonsterKills}%");

            if (GUI.Button(new Rect(stageCard.x + 24f, stageCard.y + 210f, 270f, 58f),
                    stageFlow.AutoChallenge ? "자동 균왕 도전 ON" : "자동 균왕 도전 OFF",
                    stageFlow.AutoChallenge ? selectedButtonStyle : buttonStyle))
                stageFlow.SetAutoChallenge(!stageFlow.AutoChallenge);

            GUI.enabled = stageFlow.CanChallengeBoss;
            if (GUI.Button(new Rect(stageCard.x + 318f, stageCard.y + 210f, 270f, 58f), "균왕에게 도전", bossButtonStyle))
                stageFlow.TryStartBossBattle();
            GUI.enabled = true;

            GUI.Label(new Rect(ruleCard.x + 24f, ruleCard.y + 22f, ruleCard.width - 48f, 40f), "확정 모험 규칙", titleStyle);
            GUI.Label(new Rect(ruleCard.x + 24f, ruleCard.y + 84f, ruleCard.width - 48f, 300f),
                "· 100마리를 처치하면 균왕 도전 자격 획득\n\n· 도전하지 않으면 일반 몬스터가 계속 등장하고 방치 재화 획득\n\n· 균왕 입장 시 HP·MP와 모든 스킬 쿨타임 초기화\n\n· 사냥 중 사망하면 이전 스테이지로 이동\n\n· 균왕전 사망 시 현재 스테이지 0/100부터 재시작", bodyStyle);
        }

        private void DrawMissionScreen()
        {
            GUI.Label(new Rect(24f, 94f, 700f, 42f), "땅콩월드 수호 임무", titleStyle);
            DrawMissionCard(new Rect(28f, 170f, 420f, 430f), "몬스터 정리", "누적 몬스터 처치량에 따라\n다이아 보상을 받습니다.", "ClaimKillMission", "처치 임무 보상");
            DrawMissionCard(new Rect(484f, 170f, 420f, 430f), "지역 개척", "새로운 스테이지를 돌파할수록\n다이아 보상이 증가합니다.", "ClaimStageMission", "스테이지 임무 보상");
            DrawMissionCard(new Rect(940f, 170f, 420f, 430f), "전사 성장", "전직과 미니 성장을 진행해\n조각과 다이아를 획득합니다.", "ClaimGrowthAchievement", "성장 업적 보상");
        }

        private void DrawMissionCard(Rect rect, string title, string description, string method, string successMessage)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 24f, rect.width - 48f, 44f), title, titleStyle);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 98f, rect.width - 48f, 130f), description, bodyStyle);
            if (GUI.Button(new Rect(rect.x + 52f, rect.y + 310f, rect.width - 104f, 64f), "보상 확인 및 수령", selectedButtonStyle))
                InvokePrivate(idleSystems, method, null, successMessage);
        }

        private void DrawShopScreen()
        {
            GUI.Label(new Rect(24f, 94f, 700f, 42f), "땅콩 상점", titleStyle);
            DrawShopCard(new Rect(28f, 170f, 310f, 460f), "오늘의 보급", "하루 한 번\n골드·다이아·조각 지급", "접속 보상 받기", () => InvokePrivate(shopSystem, "ClaimDailyReward", null, "오늘의 보급 확인"));
            DrawShopCard(new Rect(368f, 170f, 310f, 460f), "사냥 검 소환", "사냥 편성에 사용할\n속성 검을 획득", "다이아 5", () => InvokePrivate(shopSystem, "SummonSword", new object[] { false }, "사냥 검 소환"));
            DrawShopCard(new Rect(708f, 170f, 310f, 460f), "균왕 검 소환", "균왕전 편성에 사용할\n속성 검을 획득", "다이아 5", () => InvokePrivate(shopSystem, "SummonSword", new object[] { true }, "균왕 검 소환"));
            DrawShopCard(new Rect(1048f, 170f, 310f, 460f), "미니 알", "부화시켜 미니 도감을\n채우는 전용 알", "다이아 3", () => InvokePrivate(shopSystem, "BuyEgg", null, "미니 알 구매"));
        }

        private void DrawShopCard(Rect rect, string title, string description, string price, Action action)
        {
            GUI.DrawTexture(rect, panelTexture);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 24f, rect.width - 40f, 42f), title, titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 100f, rect.width - 40f, 120f), description, bodyStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 260f, rect.width - 40f, 38f), price, headerStyle);
            if (GUI.Button(new Rect(rect.x + 28f, rect.y + 342f, rect.width - 56f, 66f), "구매·수령", selectedButtonStyle)) action?.Invoke();
        }

        private string ModeTitle()
        {
            return mode switch
            {
                ScreenMode.Growth => "땅콩전사 성장",
                ScreenMode.Swords => "검 보관함",
                ScreenMode.Skills => "땅콩 검술",
                ScreenMode.Minis => "미니 땅콩",
                ScreenMode.Adventure => "모험",
                ScreenMode.Missions => "수호 임무",
                ScreenMode.Shop => "땅콩 상점",
                _ => "땅콩전사 키우기"
            };
        }

        private bool SpendGold(long amount)
        {
            if (Gold < amount || goldField == null) return false;
            goldField.SetValue(arena, Gold - amount);
            return true;
        }

        private int ReadEnumIndex(FieldInfo field)
        {
            if (field == null) return 0;
            return Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, elementNames.Length - 1);
        }

        private string ElementName(int index)
        {
            return elementNames[Mathf.Clamp(index, 0, elementNames.Length - 1)];
        }

        private void SetElement(FieldInfo field, int index, string slotName)
        {
            if (field == null) return;
            field.SetValue(arena, Enum.ToObject(field.FieldType, index));
            ShowToast($"{slotName}에 {ElementName(index)} 검 장착");
        }

        private void InvokePrivate(object target, string methodName, object[] arguments, string successMessage)
        {
            if (target == null)
            {
                ShowToast("해당 시스템 초기화 대기 중");
                return;
            }

            Type[] argumentTypes;
            if (arguments == null || arguments.Length == 0)
            {
                argumentTypes = Type.EmptyTypes;
                arguments = null;
            }
            else
            {
                argumentTypes = new Type[arguments.Length];
                for (int i = 0; i < arguments.Length; i++) argumentTypes[i] = arguments[i].GetType();
            }

            MethodInfo method = target.GetType().GetMethod(methodName, PrivateInstance, null, argumentTypes, null);
            if (method == null)
            {
                ShowToast("기능 연결 정보를 찾지 못했습니다");
                return;
            }

            try
            {
                method.Invoke(target, arguments);
                ShowToast(successMessage);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception, target as UnityEngine.Object);
                ShowToast("기능 실행 중 오류가 발생했습니다");
            }
        }

        private void ShowToast(string message)
        {
            toast = message;
            toastTimer = 2.3f;
        }

        private void DrawBar(Rect rect, float current, float maximum, Texture2D fill, string label)
        {
            GUI.DrawTexture(rect, barBackTexture);
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), fill);
            GUI.Label(rect, label, whiteStyle);
        }

        private static int ReadLevel(FieldInfo field, object target)
        {
            if (field == null || target == null) return 1;
            return Mathf.Max(1, Convert.ToInt32(field.GetValue(target)));
        }

        private static int ReadInt(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0;
            return Convert.ToInt32(field.GetValue(target));
        }

        private static long ReadLong(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0L;
            return Convert.ToInt64(field.GetValue(target));
        }

        private static float ReadFloat(FieldInfo field, object target)
        {
            if (field == null || target == null) return 0f;
            return Convert.ToSingle(field.GetValue(target));
        }

        private static float ReadPropertyFloat(PropertyInfo property, object target, float fallback)
        {
            if (property == null || target == null) return fallback;
            return Convert.ToSingle(property.GetValue(target));
        }
    }
}
