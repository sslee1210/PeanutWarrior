using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Keeps only the automatic boss time limit. Manual dodge zones and pattern
    /// damage were removed because Peanut Warrior is designed to run unattended.
    /// Boss difficulty now comes from the boss's normal damage, HP and the timer.
    /// </summary>
    public sealed class BossPatternPrototype : MonoBehaviour
    {
        private const float BossTimeLimit = 45f;

        private StageFlowController stageFlow;
        private MethodInfo bossDeathMethod;
        private float remainingTime;
        private bool encounterActive;
        private bool enraged;
        private string patternName = "보스 자동 전투 대기";

        public float RemainingTime => remainingTime;
        public string PatternName => patternName;
        public bool IsEnraged => enraged;
        public bool EncounterActive => encounterActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<BossPatternPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorBossPatternPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<BossPatternPrototype>();
        }

        private void Start()
        {
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (stageFlow == null)
            {
                enabled = false;
                return;
            }

            bossDeathMethod = typeof(StageFlowController).GetMethod(
                "HandleBossBattleDeath", BindingFlags.Instance | BindingFlags.Public);
            stageFlow.BossBattleStarted += BeginEncounter;
            stageFlow.BossDefeated += EndEncounter;
            stageFlow.BossBattleFailed += EndEncounter;
        }

        private void OnDestroy()
        {
            if (stageFlow == null) return;
            stageFlow.BossBattleStarted -= BeginEncounter;
            stageFlow.BossDefeated -= EndEncounter;
            stageFlow.BossBattleFailed -= EndEncounter;
        }

        private void BeginEncounter()
        {
            remainingTime = BossTimeLimit;
            encounterActive = true;
            enraged = false;
            patternName = "AUTO · 보스와 전투 중";
        }

        private void EndEncounter()
        {
            encounterActive = false;
            enraged = false;
            patternName = "보스 자동 전투 대기";
        }

        private void Update()
        {
            if (!encounterActive || stageFlow.Phase != StageFlowPhase.BossBattle) return;

            remainingTime -= Time.deltaTime;
            if (!enraged && remainingTime <= BossTimeLimit * 0.35f)
            {
                enraged = true;
                patternName = "AUTO · 제한시간 임박";
            }

            if (remainingTime > 0f) return;
            remainingTime = 0f;
            encounterActive = false;
            patternName = "제한시간 초과 · 보스전 실패";
            bossDeathMethod?.Invoke(stageFlow, null);
        }
    }
}
