using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Adds a timed boss encounter with readable warning zones, five attack patterns,
    /// world-based pattern offsets, and an enrage phase without requiring scene setup.
    /// </summary>
    public sealed class BossPatternPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const float BossTimeLimit = 45f;
        private const int PatternCount = 5;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private FieldInfo playerHpField;
        private PropertyInfo playerMaxHpProperty;
        private MethodInfo bossDeathMethod;

        private float remainingTime;
        private float nextPatternTimer;
        private float warningTimer;
        private float warningDuration;
        private int patternIndex;
        private int patternSequenceOffset;
        private bool encounterActive;
        private bool enraged;
        private Vector2 warningCenter;
        private float warningRadius;
        private string patternName = "균왕 패턴 대기";

        public float RemainingTime => remainingTime;
        public string PatternName => patternName;
        public bool IsEnraged => enraged;

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
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            playerHpField = arenaType.GetField("playerHp", PrivateInstance);
            playerMaxHpProperty = arenaType.GetProperty("PlayerMaxHp", PrivateInstance);
            bossDeathMethod = typeof(StageFlowController).GetMethod("HandleBossBattleDeath", BindingFlags.Instance | BindingFlags.Public);

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
            nextPatternTimer = Mathf.Max(1.8f, 3f - WorldDifficulty * 0.08f);
            warningTimer = 0f;
            encounterActive = true;
            enraged = false;
            patternSequenceOffset = Mathf.Abs(stageFlow.World - 1) % PatternCount;
            patternIndex = patternSequenceOffset - 1;
            patternName = $"{stageFlow.GetWorldDisplayName()} 균왕전 시작";
        }

        private void EndEncounter()
        {
            encounterActive = false;
            warningTimer = 0f;
        }

        private void Update()
        {
            if (!encounterActive || stageFlow.Phase != StageFlowPhase.BossBattle) return;

            remainingTime -= Time.deltaTime;
            if (!enraged && remainingTime <= BossTimeLimit * 0.35f)
            {
                enraged = true;
                patternName = "균왕 광폭화 · 패턴 속도 증가";
            }

            if (remainingTime <= 0f)
            {
                FailByTimeout();
                return;
            }

            if (warningTimer > 0f)
            {
                warningTimer -= Time.deltaTime;
                if (warningTimer <= 0f) ResolvePattern();
                return;
            }

            nextPatternTimer -= Time.deltaTime;
            if (nextPatternTimer <= 0f) StartNextPattern();
        }

        private void StartNextPattern()
        {
            Vector2 playerPosition = GetPlayerPosition();
            patternIndex = (patternIndex + 1) % PatternCount;
            float worldWarningReduction = Mathf.Min(0.18f, WorldDifficulty * 0.008f);
            warningDuration = Mathf.Max(0.52f, (enraged ? 0.72f : 1.10f) - worldWarningReduction);
            warningTimer = warningDuration;

            switch (patternIndex)
            {
                case 0:
                    patternName = "내려찍기 예고";
                    warningCenter = playerPosition;
                    warningRadius = 92f;
                    break;
                case 1:
                    patternName = "추적 폭발 예고";
                    warningCenter = playerPosition + UnityEngine.Random.insideUnitCircle * 38f;
                    warningRadius = 68f;
                    break;
                case 2:
                    patternName = "광역 충격파 예고";
                    warningCenter = GetBossPosition();
                    warningRadius = enraged ? 205f : 165f;
                    break;
                case 3:
                    patternName = "독성 포자 지대 예고";
                    warningCenter = playerPosition + UnityEngine.Random.insideUnitCircle * 92f;
                    warningRadius = enraged ? 132f : 112f;
                    break;
                default:
                    patternName = "균사 압축장 예고";
                    warningCenter = Vector2.Lerp(GetBossPosition(), playerPosition, 0.55f);
                    warningRadius = enraged ? 152f : 128f;
                    break;
            }
        }

        private void ResolvePattern()
        {
            Vector2 playerPosition = GetPlayerPosition();
            float distance = Vector2.Distance(playerPosition, warningCenter);
            float maxHp = playerMaxHpProperty != null
                ? Convert.ToSingle(playerMaxHpProperty.GetValue(arena))
                : 100f;

            float ratio;
            string hitName;
            string dodgeName;
            switch (patternIndex)
            {
                case 0:
                    ratio = enraged ? 0.24f : 0.18f;
                    hitName = "내려찍기 적중";
                    dodgeName = "내려찍기 회피";
                    break;
                case 1:
                    ratio = enraged ? 0.20f : 0.14f;
                    hitName = "추적 폭발 적중";
                    dodgeName = "추적 폭발 회피";
                    break;
                case 2:
                    ratio = enraged ? 0.28f : 0.20f;
                    hitName = "광역 충격파 적중";
                    dodgeName = "광역 충격파 회피";
                    break;
                case 3:
                    ratio = enraged ? 0.18f : 0.12f;
                    hitName = "독성 포자 지대 적중";
                    dodgeName = "독성 포자 지대 회피";
                    break;
                default:
                    ratio = enraged ? 0.26f : 0.19f;
                    hitName = "균사 압축장 적중";
                    dodgeName = "균사 압축장 회피";
                    break;
            }

            patternName = distance <= warningRadius ? hitName : dodgeName;
            if (distance <= warningRadius)
            {
                float worldDamageScale = 1f + Mathf.Min(0.35f, WorldDifficulty * 0.012f);
                DamagePlayer(maxHp * ratio * worldDamageScale);
            }

            float worldIntervalReduction = Mathf.Min(0.45f, WorldDifficulty * 0.018f);
            nextPatternTimer = Mathf.Max(0.95f, (enraged ? 1.45f : 2.45f) - worldIntervalReduction);
        }

        private void DamagePlayer(float damage)
        {
            if (playerHpField == null) return;
            float hp = Convert.ToSingle(playerHpField.GetValue(arena));
            hp = Mathf.Max(0f, hp - damage);
            playerHpField.SetValue(arena, hp);
            if (hp > 0f) return;

            encounterActive = false;
            bossDeathMethod?.Invoke(stageFlow, null);
        }

        private void FailByTimeout()
        {
            patternName = "제한시간 초과 · 균왕전 실패";
            encounterActive = false;
            bossDeathMethod?.Invoke(stageFlow, null);
        }

        private Vector2 GetPlayerPosition()
        {
            return playerPositionField != null
                ? (Vector2)playerPositionField.GetValue(arena)
                : new Vector2(380f, 280f);
        }

        private Vector2 GetBossPosition()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null) return new Vector2(520f, 280f);

            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                Type type = enemy.GetType();
                FieldInfo bossField = type.GetField("IsBoss", PublicInstance);
                FieldInfo positionField = type.GetField("Position", PublicInstance);
                if (bossField == null || positionField == null) continue;
                if ((bool)bossField.GetValue(enemy)) return (Vector2)positionField.GetValue(enemy);
            }
            return new Vector2(520f, 280f);
        }

        private int WorldDifficulty => Mathf.Max(0,
            (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage - 1);

        private void OnGUI()
        {
            if (FindFirstObjectByType<PeanutMobileCanvasPrototype>() != null) return;
            if (!encounterActive || stageFlow == null || stageFlow.Phase != StageFlowPhase.BossBattle) return;

            Rect panel = new Rect((Screen.width - 360f) * 0.5f, 14f, 360f, 62f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, 340f, 22f),
                $"균왕 제한시간 {Mathf.CeilToInt(remainingTime)}초 {(enraged ? "· 광폭화" : string.Empty)}");
            GUI.Label(new Rect(panel.x + 10f, panel.y + 31f, 340f, 22f), patternName);

            if (warningTimer <= 0f) return;
            float pulse = 1f + Mathf.Sin(Time.time * 16f) * 0.08f;
            float size = warningRadius * 2f * pulse;
            Rect warning = new Rect(warningCenter.x - warningRadius * pulse,
                warningCenter.y - warningRadius * pulse, size, size);
            GUI.Box(warning, $"위험\n{warningTimer:0.0}");
        }
    }
}
