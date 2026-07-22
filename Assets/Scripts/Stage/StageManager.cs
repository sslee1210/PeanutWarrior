using PeanutWarrior.Combat;
using PeanutWarrior.Core;
using PeanutWarrior.Data;
using UnityEngine;
using UnityEngine.Events;

namespace PeanutWarrior.Stage
{
    public class StageManager : MonoBehaviour
    {
        public static StageManager Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private UnityEvent onStageChanged;
        [SerializeField] private UnityEvent onBossReady;
        [SerializeField] private UnityEvent onBossStarted;
        [SerializeField] private UnityEvent onBossDefeated;

        public BattlePhase Phase { get; private set; } = BattlePhase.Hunting;
        public StageDefinition CurrentStage { get; private set; }
        public int CurrentKills { get; private set; }
        public bool BossReady => CurrentKills >= CurrentStage.requiredKills;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            LoadStage(GameManager.Instance.State.currentStageIndex);
        }

        public void LoadStage(int globalStageIndex)
        {
            int maxUnlocked = GameManager.Instance.State.highestUnlockedStageIndex;
            globalStageIndex = Mathf.Clamp(globalStageIndex, 1, maxUnlocked);
            GameManager.Instance.State.currentStageIndex = globalStageIndex;
            CurrentStage = InfiniteStageGenerator.Generate(globalStageIndex);
            CurrentKills = 0;
            Phase = BattlePhase.Hunting;
            player.FullRestore();
            onStageChanged?.Invoke();
        }

        public void RegisterMonsterKill(long baseGold = 1, long fragments = 0)
        {
            if (Phase != BattlePhase.Hunting) return;

            CurrentKills = Mathf.Min(CurrentStage.requiredKills, CurrentKills + 1);
            long gold = (long)(baseGold * GameManager.Instance.State.playerStats.GoldMultiplier);
            GameManager.Instance.AddGold(gold);
            GameManager.Instance.AddSkillFragments(fragments);

            if (!BossReady) return;
            onBossReady?.Invoke();
            if (GameManager.Instance.State.autoChallenge) StartBossBattle();
        }

        public void StartBossBattle()
        {
            if (Phase != BattlePhase.Hunting || !BossReady) return;
            Phase = BattlePhase.Boss;
            player.FullRestore();
            onBossStarted?.Invoke();
        }

        public void DefeatBoss(long goldReward, long fragmentReward, long diamondReward = 0)
        {
            if (Phase != BattlePhase.Boss) return;

            GameManager.Instance.AddGold(goldReward);
            GameManager.Instance.AddSkillFragments(fragmentReward);
            GameManager.Instance.AddDiamonds(diamondReward);

            int nextStage = CurrentStage.globalIndex + 1;
            GameManager.Instance.State.highestUnlockedStageIndex = Mathf.Max(
                GameManager.Instance.State.highestUnlockedStageIndex, nextStage);

            onBossDefeated?.Invoke();

            if (GameManager.Instance.State.autoChallenge) LoadStage(nextStage);
            else
            {
                Phase = BattlePhase.Hunting;
                CurrentKills = CurrentStage.requiredKills;
                player.FullRestore();
            }
        }

        public void HandlePlayerDeath()
        {
            GameManager.Instance.State.autoChallenge = false;

            if (Phase == BattlePhase.Boss)
            {
                // 보스전 사망: 현재 스테이지에 남되 보스 도전 자격을 초기화한다.
                Phase = BattlePhase.Hunting;
                CurrentKills = 0;
                player.FullRestore();
                onStageChanged?.Invoke();
                return;
            }

            // 일반 사냥 중 사망: 이전 스테이지로 이동.
            int previousStage = Mathf.Max(1, CurrentStage.globalIndex - 1);
            LoadStage(previousStage);
        }

        public void SetAutoChallenge(bool enabled)
        {
            GameManager.Instance.State.autoChallenge = enabled;
            if (enabled && Phase == BattlePhase.Hunting && BossReady) StartBossBattle();
        }
    }
}
