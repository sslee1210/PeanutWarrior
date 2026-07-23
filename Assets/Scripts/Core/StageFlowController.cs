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
    /// Controls the endless idle loop: hunt 100 monsters, keep farming or enter the
    /// boss automatically, then advance through 30 stages per world.
    /// Automatic boss entry is deferred to the next frame so combat collections are
    /// never cleared while CombatPrototypeArena is iterating them.
    /// </summary>
    public sealed class StageFlowController : MonoBehaviour
    {
        public const int RequiredKills = PeanutGameRules.RequiredKillsPerStage;
        public const int StagesPerWorld = PeanutGameRules.StagesPerWorld;

        [Header("Progress")]
        [SerializeField, Min(1)] private int world = 1;
        [SerializeField, Range(1, StagesPerWorld)] private int stage = 1;
        [SerializeField, Range(0, RequiredKills)] private int monsterKills;
        [SerializeField] private bool autoChallenge = true;
        [SerializeField] private StageFlowPhase phase = StageFlowPhase.Hunting;

        private bool pendingAutoChallenge;

        public int World => world;
        public int Stage => stage;
        public int MonsterKills => monsterKills;
        public bool AutoChallenge => autoChallenge;
        public StageFlowPhase Phase => phase;
        public bool CanChallengeBoss => phase != StageFlowPhase.BossBattle && monsterKills >= RequiredKills;
        public int GlobalStage => PeanutGameRules.ToGlobalStage(world, stage);

        public event Action StateChanged;
        public event Action BossBattleStarted;
        public event Action BossBattleFailed;
        public event Action BossDefeated;
        public event Action HuntingDeath;

        private void Update()
        {
            if (!pendingAutoChallenge) return;

            pendingAutoChallenge = false;
            if (autoChallenge && CanChallengeBoss)
                StartBossBattle();
        }

        public void SetAutoChallenge(bool enabled)
        {
            autoChallenge = enabled;
            NotifyChanged();

            if (enabled && CanChallengeBoss)
                pendingAutoChallenge = true;
            else if (!enabled)
                pendingAutoChallenge = false;
        }

        public void RegisterMonsterKill(int count = 1)
        {
            if (phase == StageFlowPhase.BossBattle || count <= 0) return;

            int previousKills = monsterKills;
            monsterKills = Mathf.Min(RequiredKills, monsterKills + count);
            if (previousKills < RequiredKills && monsterKills >= RequiredKills)
            {
                phase = StageFlowPhase.BossReady;
                pendingAutoChallenge = autoChallenge;
                NotifyChanged();
                return;
            }

            NotifyChanged();
        }

        public bool TryStartBossBattle()
        {
            if (!CanChallengeBoss) return false;

            pendingAutoChallenge = false;
            StartBossBattle();
            return true;
        }

        public void StartBossBattle()
        {
            if (!CanChallengeBoss) return;

            pendingAutoChallenge = false;
            phase = StageFlowPhase.BossBattle;
            BossBattleStarted?.Invoke();
            NotifyChanged();
        }

        public void HandleBossBattleDeath()
        {
            if (phase != StageFlowPhase.BossBattle) return;

            pendingAutoChallenge = false;
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            BossBattleFailed?.Invoke();
            NotifyChanged();
        }

        public void HandleHuntingDeath()
        {
            if (phase == StageFlowPhase.BossBattle) return;

            pendingAutoChallenge = false;
            MoveToPreviousStage();
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            HuntingDeath?.Invoke();
            NotifyChanged();
        }

        public void HandleBossDefeated()
        {
            if (phase != StageFlowPhase.BossBattle) return;

            pendingAutoChallenge = false;
            BossDefeated?.Invoke();
            MoveToNextStage();
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            NotifyChanged();
        }

        public void SelectStage(int targetWorld, int targetStage)
        {
            if (phase == StageFlowPhase.BossBattle)
            {
                Debug.LogWarning("Cannot select another stage during an active boss battle.");
                return;
            }

            if (targetWorld < 1 || targetStage < 1 || targetStage > StagesPerWorld)
            {
                Debug.LogWarning($"Invalid stage selection: {targetWorld}-{targetStage}");
                return;
            }

            pendingAutoChallenge = false;
            world = targetWorld;
            stage = targetStage;
            monsterKills = 0;
            phase = StageFlowPhase.Hunting;
            NotifyChanged();
        }

        public string GetWorldDisplayName()
        {
            return PeanutGameRules.GetWorldName(world);
        }

        public string GetBossDisplayName()
        {
            return PeanutGameRules.GetBossName(world);
        }

        private void MoveToNextStage()
        {
            stage++;
            if (stage <= StagesPerWorld) return;

            stage = 1;
            world++;
        }

        private void MoveToPreviousStage()
        {
            if (world == 1 && stage == 1) return;

            stage--;
            if (stage >= 1) return;

            world--;
            stage = StagesPerWorld;
        }

        private void NotifyChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
