using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Gives the eight prototype skills explicit names, upgrade controls,
    /// per-loadout auto-use switches, cooldown readouts, and a fragment economy.
    /// </summary>
    public sealed class SkillManagementPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly string[] HuntingSkillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단"
        };

        private static readonly string[] BossSkillNames =
        {
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo skillLevelsField;
        private FieldInfo cooldownsField;
        private FieldInfo fragmentsField;
        private FieldInfo playerMpField;
        private PropertyInfo maxMpProperty;
        private readonly bool[] autoEnabled = { true, true, true, true, true, true, true, true };
        private string message = "스킬 관리 준비";
        private bool panelOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SkillManagementPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorSkillManagementPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<SkillManagementPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type type = typeof(CombatPrototypeArena);
            skillLevelsField = type.GetField("skillLevels", PrivateInstance);
            cooldownsField = type.GetField("skillCooldowns", PrivateInstance);
            fragmentsField = type.GetField("fragments", PrivateInstance);
            playerMpField = type.GetField("playerMp", PrivateInstance);
            maxMpProperty = type.GetProperty("PlayerMaxMp", PrivateInstance);
            Load();
        }

        private int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        private float[] Cooldowns => cooldownsField?.GetValue(arena) as float[];
        private long Fragments => fragmentsField == null ? 0L : (long)fragmentsField.GetValue(arena);

        private long UpgradeCost(int index)
        {
            int[] levels = SkillLevels;
            int level = levels != null && index < levels.Length ? levels[index] : 1;
            return 2L + level * 2L + index / 4;
        }

        private void UpgradeSkill(int index)
        {
            int[] levels = SkillLevels;
            if (levels == null || index < 0 || index >= levels.Length) return;

            long cost = UpgradeCost(index);
            if (Fragments < cost)
            {
                message = $"조각 부족 · {cost}개 필요";
                return;
            }

            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            message = $"{SkillName(index)} Lv.{levels[index]} 강화 완료";
            Save();
        }

        private void ResetCooldownsForTesting()
        {
            float[] cooldowns = Cooldowns;
            if (cooldowns != null) Array.Clear(cooldowns, 0, cooldowns.Length);
            if (maxMpProperty != null && playerMpField != null)
                playerMpField.SetValue(arena, Convert.ToSingle(maxMpProperty.GetValue(arena)));
            message = "테스트용 MP·8스킬 쿨타임 초기화";
        }

        private void Save()
        {
            for (int i = 0; i < autoEnabled.Length; i++)
                PlayerPrefs.SetInt("PeanutWarrior.SkillAuto." + i, autoEnabled[i] ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            for (int i = 0; i < autoEnabled.Length; i++)
                autoEnabled[i] = PlayerPrefs.GetInt("PeanutWarrior.SkillAuto." + i, 1) == 1;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnGUI()
        {
            Rect button = new Rect(15f, 88f, 126f, 38f);
            if (GUI.Button(button, panelOpen ? "스킬 닫기" : "스킬 관리")) panelOpen = !panelOpen;
            if (!panelOpen || arena == null || stageFlow == null) return;

            Rect panel = new Rect(15f, 132f, 430f, 390f);
            GUI.Box(panel, "스킬 관리");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 28f, 400f, 22f),
                $"조각 {Fragments} · 현재 {(stageFlow.Phase == StageFlowPhase.BossBattle ? "보스" : "사냥")} 스킬 사용 중");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 50f, 400f, 22f), message);

            int[] levels = SkillLevels;
            float[] cooldowns = Cooldowns;
            for (int i = 0; i < 8; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float x = panel.x + 12f + column * 202f;
                float y = panel.y + 80f + row * 62f;
                int level = levels != null && i < levels.Length ? levels[i] : 1;
                float cooldown = cooldowns != null && i < cooldowns.Length ? Mathf.Max(0f, cooldowns[i]) : 0f;

                GUI.Box(new Rect(x, y, 194f, 56f), GUIContent.none);
                GUI.Label(new Rect(x + 6f, y + 4f, 116f, 20f), $"{SkillName(i)} Lv.{level}");
                GUI.Label(new Rect(x + 6f, y + 25f, 96f, 20f), $"쿨 {cooldown:0.0}초");
                if (GUI.Button(new Rect(x + 105f, y + 4f, 82f, 23f), $"강화 {UpgradeCost(i)}")) UpgradeSkill(i);
                if (GUI.Button(new Rect(x + 105f, y + 29f, 82f, 22f), autoEnabled[i] ? "자동 ON" : "자동 OFF"))
                {
                    autoEnabled[i] = !autoEnabled[i];
                    message = $"{SkillName(i)} 자동 사용 {(autoEnabled[i] ? "활성" : "비활성")}";
                    Save();
                }
            }

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 338f, 194f, 34f), "MP·쿨타임 테스트 초기화")) ResetCooldownsForTesting();
            GUI.Label(new Rect(panel.x + 216f, panel.y + 340f, 200f, 32f),
                "사냥 4개 / 보스 4개\n각 스킬 개별 강화·설정 저장");
        }

        private static string SkillName(int index)
        {
            return index < 4 ? HuntingSkillNames[index] : BossSkillNames[index - 4];
        }
    }
}
