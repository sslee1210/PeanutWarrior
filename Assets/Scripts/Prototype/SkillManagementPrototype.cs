using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Stores the eight confirmed peanut sword arts and exposes their live combat values.
    /// Skill-level upgrades and the eight advancement tiers are separate, cumulative systems.
    /// Advancement changes damage, timing, hit structure, target counts and spectacle density.
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
            "껍질을 여러 개의 갑각검 날개로 해방합니다. 갑각검은 공격과 방어에 반응하고 마지막에 보스를 둘러싼 검진으로 붕괴합니다.",
            "보스 주위를 여러 방향으로 순간 이동하며 연속 참격을 남깁니다. 검을 거두는 순간 모든 검흔이 동시에 폭발합니다.",
            "보스 머리 위의 황금꽃과 발밑의 뿌리 검진이 피해를 저장합니다. 꽃과 뿌리가 동시에 닫히며 축적된 피해를 폭발시킵니다.",
            "알맹이에 담긴 황금 생명핵을 천상검으로 바꿉니다. 전장의 빛을 모아 내려쳐 하늘과 지면을 함께 가릅니다."
        };

        private static readonly float[] BaseMpCosts = { 20f, 25f, 30f, 42f, 22f, 30f, 38f, 55f };
        private static readonly float[] BaseCooldownSeconds = { 6f, 9f, 12f, 18f, 10f, 13f, 17f, 24f };
        private static readonly int[] BaseHitCounts = { 6, 12, 7, 16, 6, 9, 8, 1 };
        private static readonly int[] BaseTargetCounts = { 6, 8, 8, 12, 1, 1, 1, 1 };
        private static readonly int[] BaseWaveCounts = { 1, 4, 2, 5, 1, 1, 2, 1 };
        private static readonly int[] BaseVisualCounts = { 8, 20, 6, 24, 6, 8, 10, 12 };
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
        public bool UsesAdvancementSkillEvolution => true;
        public bool EvolvesHitCounts => true;
        public bool EvolvesTargetCounts => true;
        public bool EvolvesVisualDensity => true;
        public bool EvolvesSkillPatterns => true;
        public int CurrentAdvancementTier => advancementTierField == null || arena == null
            ? 0
            : Mathf.Clamp(Convert.ToInt32(advancementTierField.GetValue(arena)), 0, PeanutGameRules.AdvancementCount - 1);
        public int CurrentEvolutionRank => Mathf.Clamp(CurrentAdvancementTier / 2 + 1, 1, 4);
        public string CurrentAdvancementName => PeanutGameRules.GetAdvancement(CurrentAdvancementTier).Name;

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
            if (!IsValidSkill(index)) return 0f;
            float reduction = GetSkillAdvancementMpReduction(index);
            return Mathf.Max(1f, Mathf.Round(BaseMpCosts[index] * (1f - reduction)));
        }

        public float GetSkillBaseCooldown(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            float reduction = GetSkillAdvancementCooldownReduction(index);
            return Mathf.Max(1f, BaseCooldownSeconds[index] * (1f - reduction));
        }

        public int GetSkillHitCount(int index)
        {
            return GetSkillHitCountForTier(index, CurrentAdvancementTier);
        }

        public int GetSkillTargetCount(int index)
        {
            return GetSkillTargetCountForTier(index, CurrentAdvancementTier);
        }

        public int GetSkillWaveCount(int index)
        {
            return GetSkillWaveCountForTier(index, CurrentAdvancementTier);
        }

        public int GetSkillVisualObjectCount(int index)
        {
            return GetSkillVisualObjectCountForTier(index, CurrentAdvancementTier);
        }

        public float GetSkillRangeMultiplier(int index)
        {
            if (!IsValidSkill(index)) return 1f;
            return 1f + CurrentAdvancementTier * 0.12f;
        }

        public float GetSkillSpecialDuration(int index)
        {
            int tier = CurrentAdvancementTier;
            return index switch
            {
                4 => 6f + tier * 0.75f,
                6 => 3.2f + tier * 0.25f,
                _ => 0f
            };
        }

        public float GetSkillStoredDamageRatio(int index)
        {
            if (index != 6) return 0f;
            return Mathf.Clamp(0.32f + CurrentAdvancementTier * 0.04f, 0.32f, 0.60f);
        }

        public string GetEvolutionGradeName()
        {
            return CurrentEvolutionRank switch
            {
                1 => "초식",
                2 => "개화",
                3 => "왕실",
                4 => "차원",
                _ => "초식"
            };
        }

        public float GetSkillAdvancementDamageBonus(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            int tier = CurrentAdvancementTier;
            float perTier = 0.07f + (index % 4) * 0.01f;
            float bonus = tier * perTier;
            if (tier >= 3 && !IsBossSkill(index)) bonus += 0.08f;
            if (tier >= 4 && IsBossSkill(index)) bonus += 0.10f;
            if (tier >= 6 && (index == 3 || index == 7)) bonus += 0.20f;
            if (tier >= 7) bonus += 0.15f;
            return bonus;
        }

        public float GetSkillAdvancementCooldownReduction(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            int tier = CurrentAdvancementTier;
            float reduction = tier * 0.02f;
            if (tier >= 3) reduction += 0.02f;
            if (tier >= 6) reduction += 0.03f;
            return Mathf.Clamp(reduction, 0f, 0.22f);
        }

        public float GetSkillAdvancementMpReduction(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            int tier = CurrentAdvancementTier;
            float reduction = tier * 0.01f;
            if (tier >= 5) reduction += 0.02f;
            if (tier >= 7) reduction += 0.02f;
            return Mathf.Clamp(reduction, 0f, 0.12f);
        }

        public float GetSkillDamageMultiplier(int index)
        {
            if (!IsValidSkill(index)) return 0f;
            float advancementMultiplier = skillAdvancementMultiplierProperty == null || arena == null
                ? 1f
                : Convert.ToSingle(skillAdvancementMultiplierProperty.GetValue(arena));
            return BaseDamageMultipliers[index] *
                   (1f + (GetSkillLevel(index) - 1) * 0.15f) *
                   advancementMultiplier *
                   (1f + GetSkillAdvancementDamageBonus(index));
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

        public string GetSkillAdvancementSummary(int index)
        {
            if (!IsValidSkill(index)) return "전직 강화 정보 없음";
            return $"{GetEvolutionGradeName()} 진화 {CurrentEvolutionRank}단계 · 피해 +{GetSkillAdvancementDamageBonus(index) * 100f:0}% · " +
                   $"쿨타임 -{GetSkillAdvancementCooldownReduction(index) * 100f:0}% · MP -{GetSkillAdvancementMpReduction(index) * 100f:0}%";
        }

        public string GetSkillEvolutionSummary(int index)
        {
            if (!IsValidSkill(index)) return "진화 정보 없음";
            string targetText = IsBossSkill(index) ? "보스 1명" : $"최대 {GetSkillTargetCount(index)}명";
            return $"{GetEvolutionGradeName()} 진화 · {GetSkillHitCount(index)}타 · {targetText} · " +
                   $"{GetSkillWaveCount(index)}파동 · 연출 {GetSkillVisualObjectCount(index)}개 · 범위 ×{GetSkillRangeMultiplier(index):0.00}";
        }

        public string GetNextAdvancementEvolutionSummary(int index)
        {
            if (!IsValidSkill(index)) return "다음 진화 정보 없음";
            int current = CurrentAdvancementTier;
            if (current >= PeanutGameRules.AdvancementCount - 1) return "최종 전직 진화 완료";
            int next = current + 1;
            int hitGain = GetSkillHitCountForTier(index, next) - GetSkillHitCountForTier(index, current);
            int targetGain = GetSkillTargetCountForTier(index, next) - GetSkillTargetCountForTier(index, current);
            int waveGain = GetSkillWaveCountForTier(index, next) - GetSkillWaveCountForTier(index, current);
            int visualGain = GetSkillVisualObjectCountForTier(index, next) - GetSkillVisualObjectCountForTier(index, current);
            return $"다음 전직: 타격 +{hitGain} · 대상 +{targetGain} · 파동 +{waveGain} · 연출 +{visualGain}";
        }

        public string GetSkillCombatSummary(int index)
        {
            string targets = IsBossSkill(index) ? "보스 1명" : $"최대 {GetSkillTargetCount(index)}명";
            return $"총 피해 {GetSkillTotalDamage(index):N0} · {GetSkillHitCount(index)}타 · {targets}\n" +
                   $"쿨타임 {GetSkillBaseCooldown(index):0.0}초 · MP {GetSkillMpCost(index):N0} · 범위 ×{GetSkillRangeMultiplier(index):0.00}\n" +
                   GetSkillAdvancementSummary(index) + "\n" + GetNextAdvancementEvolutionSummary(index);
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
                5 => "보스 다방향 순간 연격",
                6 => "피해 저장 · 상하 압축 폭발",
                7 => "황금 생명핵 다단 초필살기",
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

        public void SetGlobalAuto(bool enabledValue)
        {
            globalAutoEnabled = enabledValue;
            for (int i = 0; i < autoEnabled.Length; i++) autoEnabled[i] = enabledValue;
            message = enabledValue ? "모든 스킬 AUTO ON" : "모든 스킬 AUTO OFF";
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

        private static int GetSkillHitCountForTier(int index, int tier)
        {
            if (!IsValidSkill(index)) return 1;
            tier = Mathf.Clamp(tier, 0, PeanutGameRules.AdvancementCount - 1);
            return index switch
            {
                0 => BaseHitCounts[index] + tier,
                1 => BaseHitCounts[index] + tier * 3,
                2 => BaseHitCounts[index] + tier,
                3 => BaseHitCounts[index] + tier * 4,
                4 => BaseHitCounts[index] + tier,
                5 => BaseHitCounts[index] + tier * 2,
                6 => BaseHitCounts[index] + tier * 2,
                7 => BaseHitCounts[index] + (tier >= 3 ? 1 : 0) + (tier >= 6 ? 1 : 0),
                _ => BaseHitCounts[index]
            };
        }

        private static int GetSkillTargetCountForTier(int index, int tier)
        {
            if (!IsValidSkill(index)) return 1;
            tier = Mathf.Clamp(tier, 0, PeanutGameRules.AdvancementCount - 1);
            if (index >= 4) return 1;
            return index switch
            {
                0 => BaseTargetCounts[index] + tier,
                1 => BaseTargetCounts[index] + tier * 2,
                2 => BaseTargetCounts[index] + tier * 2,
                3 => BaseTargetCounts[index] + tier * 3,
                _ => BaseTargetCounts[index]
            };
        }

        private static int GetSkillWaveCountForTier(int index, int tier)
        {
            if (!IsValidSkill(index)) return 1;
            tier = Mathf.Clamp(tier, 0, PeanutGameRules.AdvancementCount - 1);
            return index switch
            {
                0 => BaseWaveCounts[index] + tier / 3,
                1 => BaseWaveCounts[index] + tier,
                2 => BaseWaveCounts[index] + tier / 2,
                3 => BaseWaveCounts[index] + tier / 2,
                4 => BaseWaveCounts[index] + tier / 3,
                5 => BaseWaveCounts[index] + tier / 4,
                6 => BaseWaveCounts[index] + tier / 2,
                7 => BaseWaveCounts[index] + (tier >= 3 ? 1 : 0) + (tier >= 6 ? 1 : 0),
                _ => BaseWaveCounts[index]
            };
        }

        private static int GetSkillVisualObjectCountForTier(int index, int tier)
        {
            if (!IsValidSkill(index)) return 1;
            tier = Mathf.Clamp(tier, 0, PeanutGameRules.AdvancementCount - 1);
            int perTier = index switch
            {
                0 => 2,
                1 => 6,
                2 => 2,
                3 => 8,
                4 => 2,
                5 => 2,
                6 => 3,
                7 => 4,
                _ => 1
            };
            return BaseVisualCounts[index] + tier * perTier;
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
