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
            public Vector2 WanderTarget;
            public float WanderTimer;
            public float Hp;
            public float MaxHp;
            public float AttackCooldown;
            public bool IsBoss;
        }

        private const float ArenaWidth = 760f;
        private const float ArenaHeight = 420f;
        private const float MapLeft = 55f;
        private const float MapRight = ArenaWidth - 55f;
        private const float MapTop = 105f;
        private const float MapBottom = ArenaHeight - 45f;

        private const float PlayerMaxHp = 100f;
        private const float PlayerAttackDamage = 18f;
        private const float PlayerAttackInterval = 0.42f;
        private const float PlayerMoveSpeed = 92f;
        private const float PlayerAttackRange = 92f;
        private const float PlayerSearchRange = 250f;

        private const float NormalEnemyHp = 34f;
        private const float NormalEnemyMoveSpeed = 48f;
        private const float NormalEnemyAggroRange = 170f;
        private const float BossHp = 650f;
        private const float BossMoveSpeed = 42f;

        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();

        private StageFlowController stageFlow;
        private Vector2 playerPosition;
        private Vector2 playerWanderTarget;
        private float playerWanderTimer;
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

            if (stageFlow.Phase == StageFlowPhase.BossReady)
            {
                enemies.Clear();
                UpdatePlayerFreeRoam(null);
                combatMessage = "100마리 처치 완료 · 보스 도전 대기";
                return;
            }

            if (stageFlow.Phase == StageFlowPhase.Hunting) UpdateHunting();
            else if (stageFlow.Phase == StageFlowPhase.BossBattle) UpdateBossBattle();
        }

        private void UpdateHunting()
        {
            spawnCooldown -= Time.deltaTime;
            if (spawnCooldown <= 0f && enemies.Count < 7)
            {
                SpawnNormalEnemy();
                spawnCooldown = 0.65f;
            }

            EnemyUnit nearest = FindClosestEnemy();
            UpdatePlayerFreeRoam(nearest);
            UpdateEnemies();
            UpdateAutomaticAttack(nearest);
        }

        private void UpdateBossBattle()
        {
            if (enemies.Count == 0) SpawnBoss();

            EnemyUnit boss = enemies[0];
            UpdatePlayerFreeRoam(boss);
            UpdateEnemies();
            UpdateAutomaticAttack(boss);
        }

        private void UpdatePlayerFreeRoam(EnemyUnit nearest)
        {
            Vector2 destination;
            bool hasNearbyEnemy = nearest != null &&
                Vector2.Distance(playerPosition, nearest.Position) <= PlayerSearchRange;

            if (hasNearbyEnemy)
            {
                destination = nearest.Position;
            }
            else
            {
                playerWanderTimer -= Time.deltaTime;
                if (playerWanderTimer <= 0f || Vector2.Distance(playerPosition, playerWanderTarget) < 10f)
                {
                    ChoosePlayerWanderTarget();
                }

                destination = playerWanderTarget;
            }

            if (nearest != null && Vector2.Distance(playerPosition, nearest.Position) <= PlayerAttackRange * 0.82f)
            {
                return;
            }

            playerPosition = Vector2.MoveTowards(
                playerPosition,
                destination,
                PlayerMoveSpeed * Time.deltaTime);
            playerPosition = ClampToMap(playerPosition);
        }

        private void UpdateEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float distanceToPlayer = Vector2.Distance(enemy.Position, playerPosition);
                float aggroRange = enemy.IsBoss ? 9999f : NormalEnemyAggroRange;
                float moveSpeed = enemy.IsBoss ? BossMoveSpeed : NormalEnemyMoveSpeed;

                Vector2 destination;
                if (distanceToPlayer <= aggroRange)
                {
                    destination = playerPosition;
                }
                else
                {
                    enemy.WanderTimer -= Time.deltaTime;
                    if (enemy.WanderTimer <= 0f || Vector2.Distance(enemy.Position, enemy.WanderTarget) < 8f)
                    {
                        ChooseEnemyWanderTarget(enemy);
                    }
                    destination = enemy.WanderTarget;
                }

                float stopDistance = enemy.IsBoss ? 82f : 46f;
                if (distanceToPlayer > stopDistance)
                {
                    enemy.Position = Vector2.MoveTowards(
                        enemy.Position,
                        destination,
                        moveSpeed * Time.deltaTime);
                    enemy.Position = ClampToMap(enemy.Position);
                }

                if (distanceToPlayer <= stopDistance + 12f)
                {
                    DamagePlayerFrom(
                        enemy,
                        enemy.IsBoss ? 11f : 4.5f,
                        enemy.IsBoss ? 0.9f : 1.1f);
                }
            }
        }

        private void UpdateAutomaticAttack(EnemyUnit preferredTarget)
        {
            if (preferredTarget == null) return;
            if (Vector2.Distance(playerPosition, preferredTarget.Position) > PlayerAttackRange) return;

            playerAttackCooldown -= Time.deltaTime;
            if (playerAttackCooldown > 0f) return;

            playerAttackCooldown = PlayerAttackInterval;
            preferredTarget.Hp -= PlayerAttackDamage;
            combatMessage = preferredTarget.IsBoss ? "보스에게 연속 참격!" : "가까운 몬스터 공격!";

            if (preferredTarget.Hp > 0f) return;

            bool wasBoss = preferredTarget.IsBoss;
            enemies.Remove(preferredTarget);

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
            Vector2 spawn = RandomMapPoint();

            if (Vector2.Distance(spawn, playerPosition) < 180f)
            {
                spawn = new Vector2(MapRight - 20f, Random.Range(MapTop, MapBottom));
            }

            EnemyUnit enemy = new EnemyUnit
            {
                Position = spawn,
                Hp = hp,
                MaxHp = hp,
                AttackCooldown = 0.8f,
                IsBoss = false
            };
            ChooseEnemyWanderTarget(enemy);
            enemies.Add(enemy);
        }

        private void SpawnBoss()
        {
            float stageScale = 1f + ((stageFlow.World - 1) * 30 + stageFlow.Stage - 1) * 0.05f;
            float hp = BossHp * stageScale;

            EnemyUnit boss = new EnemyUnit
            {
                Position = new Vector2(MapRight - 30f, (MapTop + MapBottom) * 0.5f),
                Hp = hp,
                MaxHp = hp,
                AttackCooldown = 1f,
                IsBoss = true
            };
            ChooseEnemyWanderTarget(boss);
            enemies.Add(boss);
            combatMessage = $"{stageFlow.World}-{stageFlow.Stage} 보스 출현";
        }

        private EnemyUnit FindClosestEnemy()
        {
            if (enemies.Count == 0) return null;

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

        private void ChoosePlayerWanderTarget()
        {
            playerWanderTarget = RandomMapPoint();
            playerWanderTimer = Random.Range(1.4f, 3.8f);
        }

        private void ChooseEnemyWanderTarget(EnemyUnit enemy)
        {
            enemy.WanderTarget = RandomMapPoint();
            enemy.WanderTimer = Random.Range(1.5f, 4.5f);
        }

        private static Vector2 RandomMapPoint()
        {
            return new Vector2(
                Random.Range(MapLeft, MapRight),
                Random.Range(MapTop, MapBottom));
        }

        private static Vector2 ClampToMap(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, MapLeft, MapRight);
            position.y = Mathf.Clamp(position.y, MapTop, MapBottom);
            return position;
        }

        private void BeginBossBattle()
        {
            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerAttackCooldown = 0f;
            playerPosition = new Vector2(MapLeft + 70f, (MapTop + MapBottom) * 0.5f);
            ChoosePlayerWanderTarget();
            SpawnBoss();
        }

        private void ResetForHunting()
        {
            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerAttackCooldown = 0f;
            spawnCooldown = 0f;
            playerPosition = new Vector2(ArenaWidth * 0.5f, (MapTop + MapBottom) * 0.5f);
            ChoosePlayerWanderTarget();
            combatMessage = "맵 자유 사냥 시작";
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
            GUI.Label(new Rect(left + 16f, top + 12f, 600f, 28f),
                $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}   " +
                $"처치 {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}");
            GUI.Label(new Rect(left + 16f, top + 38f, 600f, 24f), combatMessage);
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
