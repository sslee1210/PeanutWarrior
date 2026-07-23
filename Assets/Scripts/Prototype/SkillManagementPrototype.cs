using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Stores the eight confirmed peanut sword arts and exposes their live combat values.
    /// Hunting and boss skills use separate names, timings, hit structures, and roles.
    /// </summary>
    public sealed class SkillManagementPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string GlobalAutoKey = "PeanutWarrior.SkillAuto.Global";

        private static readonly string[] SkillNames =
        {
            "껍질 회전참", "낙화검우", "지맥꼬투리진", "왕실 꼬투리 천개",
            "갑각해방", "땅콩 연환검", "낙화귀근", "황금핵 천단"
        };

        private static readonly string[] SkillDescriptions =
        {
            "황금 갑각검을 검륜처럼 펼치고 적 무리 사이를 질주합니다. 이동 경로에 남은 원형 검흔이 시간차로 터집니다.",
            "하늘에 피어난 황금 땅콩꽃에서 추적 검비가 쏟아집니다. 땅에 박힌 검들이 연결된 뒤 한꺼번에 폭발합니다.",
            "황금 지맥이 적을 연결하고 거대한 꼬투리 칼날이 지면에서 연속 분출합니다. 마지막 대검이 적들을 띄워 마무리합니다.",
            "왕관 모양의 초대형 꼬투리 무기고를 열어 수십 자루의 왕실 검을 전개하고, 마지막에 하나의 거대검으로 합쳐 내려칩니다.",
            "껍질을 여섯 개의 갑각검 날개로 해방합니다. 갑각검은 공격과 방어에 반응하고 마지막에 보스를 둘러싼 검진으로 붕괴합니다.",
            "보스 주위를 여덟 방향으로 순간 이동하며 연속 참격을 남깁니다. 검을 거두는 순간 모든 검흔이 동시에 폭발합니다.",
            "보스 머리 위의 황금꽃과 발밑의 뿌리 검진이 피해를 저장합니다. 꽃과 뿌리가 동시에 닫히며 축적된 피해를 폭발시킵니다.",
            "알맹이에 담긴 황금 생명핵을 천상검으로 바꿉니다. 전장의 빛을 모아 단 한 번 내려쳐 하늘과 지면을 함께 가릅니다."
        };

        private static readonly float[] MpCosts = { 18f, 24f, 30f, 42f, 22f, 30f, 38f, 55f };
        private static readonly float[] BaseCooldownSeconds = { 6f, 9f, 12f, 18f, 10f, 13f, 17f, 24f };
        private static readonly int[] BaseHitCounts = { 6, 12, 7, 16, 6, 9, 8, 1 };
        private static readonly float[] BaseDamageMultipliers = { 2.8f, 4.6f, 5.4f, 8.8f, 3.6f, 7.2f, 6.8f, 14.5f };

        private CombatPrototypeArena arena;
        private FieldInfo skillLevelsField;
        private FieldInfo cooldownsField;
        private FieldInfo fragmentsField;
        private FieldInfo playerMpField;
        private FieldInfo advancementTierField;
        private PropertyInfo maxMpProperty;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo skillAdvancementMultiplierProperty;

        private readonly bool[] autoEnabled = { true, true, true, true, true, true, true, true };
        private bool globalAutoEnabled = true;
        private string message = "전체 스킬 AUTO 준비";

        public bool GlobalAutoEnabled => globalAutoEnabled;
        public string LastMessage => message;
        public int ConfirmedSkillCount => SkillNames.Length;
        public bool UsesDistinctSkillTimings => true;
        public bool UsesSpectacularPeanutSwordArts => true;

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
            return IsValidSkill(index) ? MpCosts[index] : 0f;
        }

        public float GetSkillBaseCooldown(int index)
        {
            return IsValidSkill(index) ? BaseCooldownSeconds[index] : 0f;
        }

        public int GetSkillHitCount(int index)
        {
            if (!IsValidSkill(index)) return 1;
            int tier = advancementTierField == null || arena == null
                ? 0
                : Mathf.Max(0, Convert.ToInt32(advancementTierField.GetValue(arena)));
            return BaseHitCounts[index] + (index == 0 || index == 5 ? tier : 0);
        }

        public float GetSkillDamageMultiplier(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            float advancementMultiplier = skillAdvancementMultiplierProperty == null || arena == null
                ? 1f
                : Convert.ToSingle(skillAdvancementMultiplierProperty.GetValue(arena));
            return BaseDamageMultipliers[index] *
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

        public string GetSkillRole(int index)
        {
            return index switch
            {
                0 => "이동형 갑각 검륜 · 다중 추적",
                1 => "전장 추적 검비 · 연결 폭발",
                2 => "지맥 설치 · 공중 제어",
                3 => "화면 전체 왕실 무기고 필살기",
                4 => "갑각검 해방 · 공격과 방어 동시 강화",
                5 => "보스 8방향 순간 연격",
                6 => "피해 저장 · 상하 압축 폭발",
                7 => "황금 생명핵 단일 초필살기",
                _ => "검술"
            };
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
