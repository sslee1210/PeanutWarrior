using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Stores the current eight skill levels and one global AUTO state. It also exposes
    /// the exact combat values used by CombatPrototypeArena so the skill detail window
    /// never displays a different damage, cooldown or MP cost from the actual battle.
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

        private static readonly string[] SkillDescriptions =
        {
            "회전하는 검기를 일으켜 가까운 적을 빠르게 베어냅니다.",
            "여러 개의 검기를 연속으로 날려 한 대상을 집중 공격합니다.",
            "적을 끝까지 추적하는 검무를 펼쳐 추가 타격을 가합니다.",
            "하늘과 땅을 가르는 강한 검격으로 대상을 크게 베어냅니다.",
            "보스에게 빈틈 없는 연속 참격을 가해 체력을 빠르게 깎습니다.",
            "보스의 급소를 정확히 절개해 높은 피해를 집중시킵니다.",
            "현재 장비 속성을 검에 각인해 보스에게 강한 일격을 가합니다.",
            "차원의 균열을 열어 보스에게 가장 강한 종결 공격을 가합니다."
        };

        private CombatPrototypeArena arena;
        private FieldInfo skillLevelsField;
        private FieldInfo cooldownsField;
        private FieldInfo fragmentsField;
        private FieldInfo playerMpField;
        private FieldInfo advancementTierField;
        private PropertyInfo maxMpProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo skillAdvancementMultiplierProperty;

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
            advancementTierField = type.GetField("advancementTier", PrivateInstance);
            maxMpProperty = type.GetProperty("PlayerMaxMp", PrivateInstance);
            attackDamageProperty = type.GetProperty("PlayerAttackDamage", PrivateInstance);
            skillAdvancementMultiplierProperty = type.GetProperty("SkillAdvancementMultiplier", PrivateInstance);
            Load();
        }

        public int[] SkillLevels => skillLevelsField?.GetValue(arena) as int[];
        public float[] Cooldowns => cooldownsField?.GetValue(arena) as float[];
        public long Fragments => fragmentsField == null || arena == null
            ? 0L
            : Convert.ToInt64(fragmentsField.GetValue(arena));

        public string GetSkillName(int index)
        {
            return IsValidSkill(index) ? SkillNames[index] : "스킬";
        }

        public string GetSkillDescription(int index)
        {
            return IsValidSkill(index) ? SkillDescriptions[index] : "스킬 정보를 불러올 수 없습니다.";
        }

        public bool IsBossSkill(int index)
        {
            return index >= 4 && index < 8;
        }

        public int GetSkillLevel(int index)
        {
            int[] levels = SkillLevels;
            return levels != null && index >= 0 && index < levels.Length ? Mathf.Max(1, levels[index]) : 1;
        }

        public float GetSkillMpCost(int index)
        {
            int local = Mathf.Clamp(index % 4, 0, 3);
            return 20f + local * 5f;
        }

        public float GetSkillBaseCooldown(int index)
        {
            int local = Mathf.Clamp(index % 4, 0, 3);
            return 5f + local * 1.5f;
        }

        public int GetSkillHitCount(int index)
        {
            int local = Mathf.Clamp(index % 4, 0, 3);
            int tier = advancementTierField == null || arena == null
                ? 0
                : Mathf.Max(0, Convert.ToInt32(advancementTierField.GetValue(arena)));
            return 1 + tier + (local >= 2 ? 1 : 0);
        }

        public float GetSkillDamageMultiplier(int index)
        {
            int local = Mathf.Clamp(index % 4, 0, 3);
            float advancementMultiplier = skillAdvancementMultiplierProperty == null || arena == null
                ? 1f
                : Convert.ToSingle(skillAdvancementMultiplierProperty.GetValue(arena));
            return (1.4f + local * 0.35f) *
                   (1f + (GetSkillLevel(index) - 1) * 0.15f) *
                   advancementMultiplier;
        }

        public float GetSkillTotalDamage(int index)
        {
            float attackDamage = attackDamageProperty == null || arena == null
                ? 18f
                : Convert.ToSingle(attackDamageProperty.GetValue(arena));
            return Mathf.Max(0f, attackDamage * GetSkillDamageMultiplier(index));
        }

        public float GetSkillDamagePerHit(int index)
        {
            return GetSkillTotalDamage(index) / Mathf.Max(1, GetSkillHitCount(index));
        }

        public string GetSkillCombatSummary(int index)
        {
            return $"총 피해 {GetSkillTotalDamage(index):N0} · {GetSkillHitCount(index)}타\n" +
                   $"타격당 {GetSkillDamagePerHit(index):N0} · 쿨타임 {GetSkillBaseCooldown(index):0.0}초 · MP {GetSkillMpCost(index):N0}";
        }

        public long GetUpgradeCost(int index)
        {
            int level = GetSkillLevel(index);
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

        private static bool IsValidSkill(int index)
        {
            return index >= 0 && index < SkillNames.Length;
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
