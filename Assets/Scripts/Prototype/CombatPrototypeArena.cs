using System.Collections.Generic;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class CombatPrototypeArena : MonoBehaviour
    {
        private sealed class EnemyUnit
        {
            public Vector2 Position;
            public float Hp;
            public float MaxHp;
            public float AttackCooldown;
            public bool IsBoss;
        }

        private const float ArenaWidth = 760f;
        private const float ArenaHeight = 420f;
        private const float PlayerMaxHp = 100f;
        private const float PlayerAttackDamage = 18f;
        private const float PlayerAttackInterval = 0.42f;
        private const float PlayerMoveSpeed = 72f;
        private const float NormalEnemyHp = 34f;
        private const float BossHp = 650f;

        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();

        private StageFlowController stageFlow;
        private Vector2 playerPosition;
        private Vector2 playerMoveTarget;
        private float playerMoveTargetTimer;
        private float playerHp = PlayerMaxHp;
        private float playerAttackCooldown;
        private float spawnCooldown;
        private string combatMessage = "전투 준비";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateArena()
        {
            if (FindFirstObjectByType<CombatPrototypeArena>() != null) return;

            GameObject arena = new GameObject("PeanutWarriorCombatPrototype");
            DontDestroyOnLoad(arena);
            arena.AddComponent<CombatPrototypeArena>();
        }

        private void Start()
        {
            stageFlow = FindFirstObjectByType<StageFlowController>();
            if (stageFlow == null)
            {
                GameObject flowRoot = new GameObject("PeanutWarriorStageFlow");
                DontDestroyOnLoad(flowRoot);
                stageFlow = flowRoot.AddComponent<StageFlowController>();
            }

            stageFlow.StateChanged += HandleStageStateChanged;
            stageFlow.BossBattleStarted += BeginBossBattle;
            stageFlow.BossBattleFailed += ResetForHunting;
            stageFlow.BossDefeated += ResetForHunting;
            stageFlow.HuntingDeath += ResetForHunting;

            ResetForHunting();
        }

        private void OnDestroy()
        {
            if (stageFlow == null) return;

            stageFlow.StateChanged -= HandleStageStateChanged;
            stageFlow.BossBattleStarted -= BeginBossBattle;
            stageFlow.BossBattleFailed -= ResetForHunting;
            stageFlow.BossDefeated -= ResetForHunting;
            stageFlow.HuntingDeath -= ResetForHunting;
        }

        private void Update()
        {
            if (stageFlow == null) return;

            UpdatePlayerMovement();

            if (stageFlow.Phase == StageFlowPhase.BossReady)
            {
                enemies.Clear();
                combatMessage = "100마리 처치 완료 · 보스 도전 대기";
                return;
            }

            if (stageFlow.Phase == StageFlowPhase.Hunting) UpdateHunting();
            else if (stageFlow.Phase == StageFlowPhase.BossBattle) UpdateBossBattle();

            UpdateAutomaticAttack();
        }

        private void UpdatePlayerMovement()
        {
            playerMoveTargetTimer -= Time.deltaTime;
            bool reachedTarget = Vector2.Distance(playerPosition, playerMoveTarget) < 8f;

            if (playerMoveTargetTimer <= 0f || reachedTarget)
            {
                ChooseRandomPlayerTarget();
            }

            playerPosition = Vector2.MoveTowards(
                playerPosition,
                playerMoveTarget,
                PlayerMoveSpeed * Time.deltaTime);
        }

        private void ChooseRandomPlayerTarget()
        {
            playerMoveTarget = new Vector2(
                Random.Range(95f, ArenaWidth * 0.58f),
                Random.Range(115f, ArenaHeight - 55f));
            playerMoveTargetTimer = Random.Range(1.2f, 3.2f);
        }

        private void UpdateHunting()
        {
            spawnCooldown -= Time.deltaTime;
            if (spawnCooldown <= 0f && enemies.Count < 4)
            {
                SpawnNormalEnemy();
                spawnCooldown = 0.55f;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                enemy.Position = Vector2.MoveTowards(
                    enemy.Position,
                    playerPosition,
                    45f * Time.deltaTime);

                if (Vector2.Distance(enemy.Position, playerPosition) <= 58f)
                {
                    DamagePlayerFrom(enemy, 4.5f, 1.1f);
                }
            }
        }

        private void UpdateBossBattle()
        {
            if (enemies.Count == 0) SpawnBoss();

            EnemyUnit boss = enemies[0];
            boss.Position = Vector2.MoveTowards(
                boss.Position,
                playerPosition,
                25f * Time.deltaTime);

            if (Vector2.Distance(boss.Position, playerPosition) <= 100f)
            {
                DamagePlayerFrom(boss, 11f, 0.9f);
            }
        }

        private void UpdateAutomaticAttack()
        {
            if (enemies.Count == 0) return;

            playerAttackCooldown -= Time.deltaTime;
            if (playerAttackCooldown > 0f) return;

            playerAttackCooldown = PlayerAttackInterval;
            EnemyUnit target = FindClosestEnemy();
            target.Hp -= PlayerAttackDamage;
            combatMessage = target.IsBoss ? "보스에게 연속 참격!" : "기본 공격!";

            if (target.Hp > 0f) return;

            bool wasBoss = target.IsBoss;
            enemies.Remove(target);

            if (wasBoss)
            {
                combatMessage = "보스 처치! 다음 스테이지로 이동";
                stageFlow.HandleBossDefeated();
            }
            else
            {
                stageFlow.RegisterMonsterKill();
                combatMessage = $"몬스터 처치 · {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}";
            }
        }

        private void DamagePlayerFrom(EnemyUnit enemy, float damage, float interval)
        {
            enemy.AttackCooldown -= Time.deltaTime;
            if (enemy.AttackCooldown > 0f) return;

            enemy.AttackCooldown = interval;
            playerHp = Mathf.Max(0f, playerHp - damage);
            if (playerHp > 0f) return;

            combatMessage = enemy.IsBoss
                ? "보스전 패배 · 현재 스테이지 0/100 재시작"
                : "사냥 중 사망 · 이전 스테이지 이동";

            enemies.Clear();
            playerHp = PlayerMaxHp;

            if (stageFlow.Phase == StageFlowPhase.BossBattle) stageFlow.HandleBossBattleDeath();
            else stageFlow.HandleHuntingDeath();
        }

        private void SpawnNormalEnemy()
        {
            float stageScale = 1f + ((stageFlow.World - 1) * 30 + stageFlow.Stage - 1) * 0.025f;
            float hp = NormalEnemyHp * stageScale;

            enemies.Add(new EnemyUnit
            {
                Position = new Vector2(ArenaWidth - 90f, Random.Range(110f, ArenaHeight - 65f)),
                Hp = hp,
                MaxHp = hp,
                AttackCooldown = 0.8f,
                IsBoss = false
            });
        }

        private void SpawnBoss()
        {
            float stageScale = 1f + ((stageFlow.World - 1) * 30 + stageFlow.Stage - 1) * 0.05f;
            float hp = BossHp * stageScale;

            enemies.Add(new EnemyUnit
            {
                Position = new Vector2(ArenaWidth - 120f, ArenaHeight * 0.54f),
                Hp = hp,
                MaxHp = hp,
                AttackCooldown = 1f,
                IsBoss = true
            });

            combatMessage = $"{stageFlow.World}-{stageFlow.Stage} 보스 출현";
        }

        private EnemyUnit FindClosestEnemy()
        {
            EnemyUnit closest = enemies[0];
            float closestDistance = Vector2.SqrMagnitude(closest.Position - playerPosition);

            for (int i = 1; i < enemies.Count; i++)
            {
                float distance = Vector2.SqrMagnitude(enemies[i].Position - playerPosition);
                if (distance >= closestDistance) continue;

                closest = enemies[i];
                closestDistance = distance;
            }

            return closest;
        }

        private void BeginBossBattle()
        {
            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerAttackCooldown = 0f;
            playerPosition = new Vector2(180f, ArenaHeight * 0.54f);
            ChooseRandomPlayerTarget();
            SpawnBoss();
        }

        private void ResetForHunting()
        {
            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerAttackCooldown = 0f;
            spawnCooldown = 0f;
            playerPosition = new Vector2(180f, ArenaHeight * 0.54f);
            ChooseRandomPlayerTarget();
            combatMessage = "자동 사냥 시작";
        }

        private void HandleStageStateChanged()
        {
            if (stageFlow.Phase == StageFlowPhase.Hunting && enemies.Count == 0)
            {
                spawnCooldown = 0f;
            }
        }

        private void OnGUI()
        {
            if (stageFlow == null) return;

            float left = Mathf.Max(20f, (Screen.width - ArenaWidth) * 0.5f);
            float top = Mathf.Max(20f, Screen.height - ArenaHeight - 35f);
            Rect arena = new Rect(left, top, ArenaWidth, ArenaHeight);

            GUI.Box(arena, GUIContent.none);
            GUI.Label(new Rect(left + 16f, top + 12f, 520f, 28f),
                $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}   " +
                $"처치 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            GUI.Label(new Rect(left + 16f, top + 38f, 520f, 24f), combatMessage);

            DrawHealthBar(new Rect(left + 18f, top + 70f, 220f, 20f), playerHp, PlayerMaxHp, "땅콩전사 HP");

            Rect playerRect = new Rect(
                left + playerPosition.x - 28f,
                top + playerPosition.y - 28f,
                56f,
                56f);
            GUI.Box(playerRect, "🥜\n전사");

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float size = enemy.IsBoss ? 88f : 48f;
                Rect enemyRect = new Rect(
                    left + enemy.Position.x - size * 0.5f,
                    top + enemy.Position.y - size * 0.5f,
                    size,
                    size);

                GUI.Box(enemyRect, enemy.IsBoss ? "👹\n보스" : "균");
                DrawHealthBar(
                    new Rect(enemyRect.x, enemyRect.y - 15f, enemyRect.width, 11f),
                    enemy.Hp,
                    enemy.MaxHp,
                    string.Empty);
            }

            if (stageFlow.Phase == StageFlowPhase.BossReady)
            {
                GUI.Label(new Rect(left + 275f, top + 165f, 300f, 34f),
                    "보스 도전 버튼을 누르세요");
            }
        }

        private static void DrawHealthBar(Rect rect, float current, float maximum, string label)
        {
            GUI.Box(rect, GUIContent.none);
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            GUI.Box(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), GUIContent.none);

            if (!string.IsNullOrEmpty(label))
            {
                GUI.Label(rect, $"{label} {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(maximum)}");
            }
        }
    }
}
