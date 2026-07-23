using System;
using UnityEngine;

namespace PeanutWarrior.Core
{
    public static class PeanutGameRules
    {
        public const int RequiredKillsPerStage = 100;
        public const int StagesPerWorld = 30;
        public const int BossTimeLimitSeconds = 45;
        public const int MaxOfflineHours = 8;
        public const int AutoSaveIntervalSeconds = 10;
        public const int BackupSaveIntervalSeconds = 30;

        [Serializable]
        public sealed class AdvancementDefinition
        {
            public string Name;
            public int RequiredGlobalStage;
            public int RequiredCombatPower;
            public long RequiredGold;
            public int RequiredDiamonds;
            public float StatMultiplier;
            public int BasicAttackHits;
            public bool UnlocksPets;

            public AdvancementDefinition(
                string name,
                int requiredGlobalStage,
                int requiredCombatPower,
                long requiredGold,
                int requiredDiamonds,
                float statMultiplier,
                int basicAttackHits,
                bool unlocksPets)
            {
                Name = name;
                RequiredGlobalStage = requiredGlobalStage;
                RequiredCombatPower = requiredCombatPower;
                RequiredGold = requiredGold;
                RequiredDiamonds = requiredDiamonds;
                StatMultiplier = statMultiplier;
                BasicAttackHits = basicAttackHits;
                UnlocksPets = unlocksPets;
            }
        }

        private static readonly AdvancementDefinition[] AdvancementDefinitions =
        {
            new AdvancementDefinition("새싹 땅콩", 1, 0, 0L, 0, 1.00f, 1, false),
            new AdvancementDefinition("전투 땅콩", 2, 180, 150L, 5, 1.35f, 2, false),
            new AdvancementDefinition("황금 수호 땅콩", 4, 420, 500L, 15, 1.70f, 3, true),
            new AdvancementDefinition("화염 갑각 땅콩", 15, 1200, 5000L, 25, 2.05f, 4, true),
            new AdvancementDefinition("빙결 갑각 땅콩", 30, 3000, 20000L, 40, 2.40f, 5, true),
            new AdvancementDefinition("뇌광 갑각 땅콩", 60, 8000, 100000L, 60, 2.75f, 6, true),
            new AdvancementDefinition("왕실 갑주 땅콩", 120, 20000, 500000L, 90, 3.10f, 7, true),
            new AdvancementDefinition("차원 수호 땅콩", 240, 50000, 2500000L, 130, 3.45f, 8, true)
        };

        private static readonly string[] WorldNames =
        {
            "땅콩밭 침공",
            "곰팡이 창고",
            "포식자의 숲",
            "얼어붙은 저장고",
            "불타는 이세계",
            "폭풍의 공중정원",
            "황금 껍질 왕국",
            "차원 균열 중심부"
        };

        private static readonly string[] BossNames =
        {
            "거대 땅강아지",
            "포자 군주",
            "송곳니 사냥꾼",
            "빙결 저장고지기",
            "화염 껍질 파괴자",
            "폭풍 날개왕",
            "황금 껍질 폭군",
            "차원 포식자"
        };

        public static int AdvancementCount => AdvancementDefinitions.Length;

        public static AdvancementDefinition GetAdvancement(int tier)
        {
            return AdvancementDefinitions[Mathf.Clamp(tier, 0, AdvancementDefinitions.Length - 1)];
        }

        public static bool HasNextAdvancement(int currentTier)
        {
            return currentTier >= 0 && currentTier < AdvancementDefinitions.Length - 1;
        }

        public static AdvancementDefinition GetNextAdvancement(int currentTier)
        {
            return HasNextAdvancement(currentTier) ? GetAdvancement(currentTier + 1) : null;
        }

        public static string GetWorldName(int world)
        {
            world = Mathf.Max(1, world);
            int index = (world - 1) % WorldNames.Length;
            int cycle = (world - 1) / WorldNames.Length;
            if (cycle == 0) return WorldNames[index];
            return cycle == 1
                ? "강화된 " + WorldNames[index]
                : $"초월 {cycle}단계 {WorldNames[index]}";
        }

        public static string GetBossName(int world)
        {
            world = Mathf.Max(1, world);
            int index = (world - 1) % BossNames.Length;
            int cycle = (world - 1) / BossNames.Length;
            if (cycle == 0) return BossNames[index];
            return cycle == 1
                ? "강화된 " + BossNames[index]
                : $"초월 {cycle}단계 {BossNames[index]}";
        }

        public static int ToGlobalStage(int world, int stage)
        {
            world = Mathf.Max(1, world);
            stage = Mathf.Clamp(stage, 1, StagesPerWorld);
            return (world - 1) * StagesPerWorld + stage;
        }

        public static void FromGlobalStage(int globalStage, out int world, out int stage)
        {
            globalStage = Mathf.Max(1, globalStage);
            world = (globalStage - 1) / StagesPerWorld + 1;
            stage = (globalStage - 1) % StagesPerWorld + 1;
        }
    }
}
