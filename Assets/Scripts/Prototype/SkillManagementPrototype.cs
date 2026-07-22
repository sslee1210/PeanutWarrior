using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Stores the current eight prototype skill levels and one global AUTO state.
    /// Individual auto switches were intentionally removed: AUTO now controls every
    /// equipped hunting and boss skill together.
    /// </summary>
    public sealed class SkillManagementPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string GlobalAutoKey = "PeanutWarrior.SkillAuto.Global";

        private static readonly string[] SkillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단",
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

        private CombatPrototypeArena arena;
        private FieldInfo skillLevelsField;
        private FieldInfo cooldownsField;
        private FieldInfo fragmentsField;
        private FieldInfo playerMpField;
        private PropertyInfo maxMpProperty;

        // Retained for compatibility with older saves and reflection audits. Every
        // entry always mirrors globalAutoEnabled.
        private readonly bool[] autoEnabled = { true, true, true, true, true, true, true, true };
        private bool globalAutoEnabled = true;
        private string message = "전체 스킬 AUTO 준비";

        public bool GlobalAutoEnabled => globalAutoEnabled;
        public string LastMessage => message;

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
            if (arena == null)
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

        public int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        public float[] Cooldowns => cooldownsField?.GetValue(arena) as float[];
        public long Fragments => fragmentsField == null || arena == null
            ? 0L
            : Convert.ToInt64(fragmentsField.GetValue(arena));

        public string GetSkillName(int index)
        {
            return index >= 0 && index < SkillNames.Length ? SkillNames[index] : "SKILL";
        }

        public long GetUpgradeCost(int index)
        {
            int[] levels = SkillLevels;
            int level = levels != null && index >= 0 && index < levels.Length ? levels[index] : 1;
            return 2L + Math.Max(1, level) * 2L + Math.Max(0, index) / 4;
        }

        public bool UpgradeSkill(int index)
        {
            int[] levels = SkillLevels;
            if (levels == null || index < 0 || index >= levels.Length)
            {
                message = "스킬 연결 상태를 확인하십시오";
                return false;
            }

            long cost = GetUpgradeCost(index);
            if (Fragments < cost || fragmentsField == null)
            {
                message = $"조각 부족 · {cost:N0}개 필요";
                return false;
            }

            fragmentsField.SetValue(arena, Fragments - cost);
            levels[index]++;
            message = $"{GetSkillName(index)} Lv.{levels[index]} 강화 완료";
            Save();
            return true;
        }

        public void ToggleGlobalAuto()
        {
            SetGlobalAuto(!globalAutoEnabled);
        }

        public void SetGlobalAuto(bool enabled)
        {
            globalAutoEnabled = enabled;
            for (int i = 0; i < autoEnabled.Length; i++) autoEnabled[i] = enabled;
            message = enabled ? "모든 스킬 AUTO ON" : "모든 스킬 AUTO OFF";
            Save();
        }

        public void ResetCooldownsForTesting()
        {
            float[] cooldowns = Cooldowns;
            if (cooldowns != null) Array.Clear(cooldowns, 0, cooldowns.Length);
            if (maxMpProperty != null && playerMpField != null)
                playerMpField.SetValue(arena, Convert.ToSingle(maxMpProperty.GetValue(arena)));
            message = "MP와 전체 스킬 쿨타임 초기화";
        }

        private void Save()
        {
            PlayerPrefs.SetInt(GlobalAutoKey, globalAutoEnabled ? 1 : 0);
            for (int i = 0; i < autoEnabled.Length; i++)
                PlayerPrefs.SetInt("PeanutWarrior.SkillAuto." + i, globalAutoEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            int legacyDefault = PlayerPrefs.GetInt("PeanutWarrior.SkillAuto.0", 1);
            globalAutoEnabled = PlayerPrefs.GetInt(GlobalAutoKey, legacyDefault) == 1;
            for (int i = 0; i < autoEnabled.Length; i++) autoEnabled[i] = globalAutoEnabled;
            Save();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
