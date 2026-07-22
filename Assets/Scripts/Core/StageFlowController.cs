using System;
using UnityEngine;

namespace PeanutWarrior.Core
{
    public enum StageFlowPhase
    {
        Hunting,
        BossReady,
        BossBattle
    }

    /// <summary>
    /// Controls the core idle-RPG loop:
    /// hunt 100 monsters, keep farming while the boss is available,
    /// challenge the boss manually or automatically, and advance stages.
    /// </summary>
    public sealed class StageFlowController : MonoBehaviour
    {
        public const int RequiredKills = 100;
        public const int StagesPerWorld = 30;

        [Header("Progress")]
        [SerializeField, Min(1)] private int world = 1;
        [SerializeField, Range(1, StagesPerWorld)] private int stage = 1;
        [SerializeField, Range(0, RequiredKills)] private int monsterKills;
        [SerializeField] private bool autoChallenge;
        [SerializeField] private StageFlowPhase phase = StageFlowPhase.Hunting;

        public int World => world;
        public int Stage => stage;
        public int MonsterKills => monsterKills;
        public bool AutoChallenge => autoChallenge;
        public StageFlowPhase Phase => phase;
        public bool CanChallengeBoss => phase != StageFlowPhase.BossBattle && monsterKills >= RequiredKills;

        public event Action StateChanged;
        public event Action BossBattleStarted;
        public event Action BossBattleFailed;
        public event Action BossDefeated;
        public event Action HuntingDeath;

        public void SetAutoChallenge(bool enabled)
        {
            autoChallenge = enabled;
            NotifyChanged();

            if (enabled && CanChallengeBoss)
            {
                StartBossBattle();
            }
        }

        public void RegisterMonsterKill(int count = 1)
        {
            if (phase == StageFlowPhase.BossBattle || count <= 0)
            {
                return;
            }

            int previousKills = monsterKills;
            monsterKills = Mathf.Min(RequiredKills, monsterKills + count);

            // Reaching 100 unlocks the boss but does not stop idle farming.
            // The counter remains at 100 while additional monsters continue
            // providing gold, fragments, and other normal hunting rewards.
            if (previousKills < RequiredKills && monsterKills >= RequiredKills)
            {
                NotifyChanged();

                if (autoChallenge)
                {
                    StartBossBattle();
                }

                return;
            }

            NotifyChanged();
        }

        public void StartBossBattle()
        {
            if (!CanChallengeBoss)
            {
                return;
            }

            phase = StageFlowPhase.BossBattle;
            BossBattleStarted?.Invoke();
            NotifyChanged();
        }

        public void HandleBossBattleDeath()
        {
            if (phase != StageFlowPhase.BossBattle)
            {
                return;
            }

            autoChallenge = false;
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;

            BossBattleFailed?.Invoke();
            NotifyChanged();
        }

        public void HandleHuntingDeath()
        {
            if (phase == StageFlowPhase.BossBattle)
            {
                return;
            }

            autoChallenge = false;
            MoveToPreviousStage();
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;

            HuntingDeath?.Invoke();
            NotifyChanged();
        }

        public void HandleBossDefeated()
        {
            if (phase != StageFlowPhase.BossBattle)
            {
                return;
            }

            BossDefeated?.Invoke();
            MoveToNextStage();
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            NotifyChanged();
        }

        public void SelectStage(int targetWorld, int targetStage)
        {
            if (targetWorld < 1 || targetStage < 1 || targetStage > StagesPerWorld)
            {
                Debug.LogWarning($"Invalid stage selection: {targetWorld}-{targetStage}");
                return;
            }

            world = targetWorld;
            stage = targetStage;
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            NotifyChanged();
        }

        public string GetWorldDisplayName()
        {
            string[] baseWorldNames =
            {
                "땅콩밭 침공",
                "곰팡이 창고",
                "포식자의 숲",
                "얼어붙은 저장고",
                "불타는 이세계",
                "차원 균열 중심부"
            };

            int baseIndex = (world - 1) % baseWorldNames.Length;
            int enhancementTier = (world - 1) / baseWorldNames.Length;

            if (enhancementTier == 0)
            {
                return baseWorldNames[baseIndex];
            }

            return enhancementTier == 1
                ? $"강화된 {baseWorldNames[baseIndex]}"
                : $"강화된 {enhancementTier}단계 {baseWorldNames[baseIndex]}";
        }

        private void MoveToNextStage()
        {
            stage++;
            if (stage <= StagesPerWorld)
            {
                return;
            }

            stage = 1;
            world++;
        }

        private void MoveToPreviousStage()
        {
            if (world == 1 && stage == 1)
            {
                return;
            }

            stage--;
            if (stage >= 1)
            {
                return;
            }

            world--;
            stage = StagesPerWorld;
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
