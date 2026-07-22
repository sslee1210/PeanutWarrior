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

        private const float PlayerMoveSpeed = 92f;
        private const float PlayerSearchRange = 245f;
        private const float PlayerAttackRange = 72f;
        private const float PlayerStopDistance = 58f;
        private const float PlayerAttackInterval = 0.45f;

        private const float EnemyAggroRange = 150f;
        private const float EnemyAttackRange = 50f;
        private const float EnemyMoveSpeed = 48f;
        private const float BossAggroRange = 320f;
        private const float BossAttackRange = 82f;
        private const float BossMoveSpeed = 42f;

        private const float NormalEnemyBaseHp = 34f;
        private const float BossBaseHp = 650f;

        private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();

        private StageFlowController stageFlow;
        private Vector2 playerPosition;
        private Vector2 playerWanderTarget;
        private float playerWanderTimer;
        private float playerHp;
        private float playerAttackCooldown;
        private float spawnCooldown;
        private string combatMessage = "전투 준비";

        private long gold;
        private int attackLevel = 1;
        private int hpLevel = 1;

        private float PlayerMaxHp => 100f + (hpLevel - 1) * 25f;
        private float PlayerAttackDamage => 18f + (attackLevel - 1) * 6f;
        private long AttackUpgradeCost => 20L * attackLevel;
        private long HpUpgradeCost => 25L * hpLevel;

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
                UpdatePlayerMovement(null);
                combatMessage = "100마리 처치 완료 · 보스 도전 대기";
                return;
            }

            if (stageFlow.Phase == StageFlowPhase.Hunting)
            {
                UpdateHunting();
            }
            else if (stageFlow.Phase == StageFlowPhase.BossBattle)
            {
                UpdateBossBattle();
            }
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
            UpdatePlayerMovement(nearest);
            UpdateEnemies();
            UpdateAutomaticAttack(nearest);
        }

        private void UpdateBossBattle()
        {
            if (enemies.Count == 0) SpawnBoss();

            EnemyUnit boss = enemies[0];
            UpdatePlayerMovement(boss);
            UpdateEnemies();
            UpdateAutomaticAttack(boss);
        }

        private void UpdatePlayerMovement(EnemyUnit nearest)
        {
            if (nearest != null)
            {
                float distance = Vector2.Distance(playerPosition, nearest.Position);

                if (distance <= PlayerSearchRange && distance > PlayerStopDistance)
                {
                    playerPosition = Vector2.MoveTowards(
                        playerPosition,
                        nearest.Position,
                        PlayerMoveSpeed * Time.deltaTime);
                    playerPosition = ClampToMap(playerPosition);
                    return;
                }

                if (distance <= PlayerStopDistance)
                {
                    return;
                }
            }

            playerWanderTimer -= Time.deltaTime;
            if (playerWanderTimer <= 0f || Vector2.Distance(playerPosition, playerWanderTarget) < 10f)
            {
                ChoosePlayerWanderTarget();
            }

            playerPosition = Vector2.MoveTowards(
                playerPosition,
                playerWanderTarget,
                PlayerMoveSpeed * 0.75f * Time.deltaTime);
            playerPosition = ClampToMap(playerPosition);
        }

        private void UpdateEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float distance = Vector2.Distance(enemy.Position, playerPosition);
                float aggroRange = enemy.IsBoss ? BossAggroRange : EnemyAggroRange;
                float attackRange = enemy.IsBoss ? BossAttackRange : EnemyAttackRange;
                float moveSpeed = enemy.IsBoss ? BossMoveSpeed : EnemyMoveSpeed;

                if (distance <= aggroRange)
                {
                    if (distance > attackRange)
                    {
                        enemy.Position = Vector2.MoveTowards(
                            enemy.Position,
                            playerPosition,
                            moveSpeed * Time.deltaTime);
                        enemy.Position = ClampToMap(enemy.Position);
                    }
                    else
                    {
                        DamagePlayerFrom(
                            enemy,
                            enemy.IsBoss ? 11f : 4.5f,
                            enemy.IsBoss ? 0.9f : 1.1f);
                    }

                    continue;
                }

                enemy.WanderTimer -= Time.deltaTime;
                if (enemy.WanderTimer <= 0f || Vector2.Distance(enemy.Position, enemy.WanderTarget) < 8f)
                {
                    ChooseEnemyWanderTarget(enemy);
                }

                enemy.Position = Vector2.MoveTowards(
                    enemy.Position,
                    enemy.WanderTarget,
                    moveSpeed * 0.65f * Time.deltaTime);
                enemy.Position = ClampToMap(enemy.Position);
            }
        }

        private void UpdateAutomaticAttack(EnemyUnit target)
        {
            if (target == null) return;
            if (Vector2.Distance(playerPosition, target.Position) > PlayerAttackRange) return;

            playerAttackCooldown -= Time.deltaTime;
            if (playerAttackCooldown > 0f) return;

            playerAttackCooldown = PlayerAttackInterval;
            target.Hp -= PlayerAttackDamage;
            combatMessage = target.IsBoss ? "보스에게 연속 참격!" : "몬스터에게 접근해 기본 공격!";

            if (target.Hp > 0f) return;

            bool wasBoss = target.IsBoss;
            enemies.Remove(target);

            if (wasBoss)
            {
                long reward = 40L + stageFlow.Stage * 10L;
                gold += reward;
                combatMessage = $"보스 처치! +{reward} 골드 · 다음 스테이지 이동";
                stageFlow.HandleBossDefeated();
            }
            else
            {
                long reward = 2L + stageFlow.Stage;
                gold += reward;
                stageFlow.RegisterMonsterKill();
                combatMessage = $"몬스터 처치 +{reward} 골드 · {stageFlow.MonsterKills}/{StageFlowController.RequiredKills}";
            }
        }

        private void DamagePlayerFrom(EnemyUnit enemy, float damage, float interval)
        {
            enemy.AttackCooldown -= Time.deltaTime;
            if (enemy.AttackCooldown > 0f) return;

            enemy.AttackCooldown = interval;
            playerHp = Mathf.Max(0f, playerHp - damage);
            combatMessage = enemy.IsBoss ? "보스에게 피격!" : "몬스터에게 피격!";

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
            float hp = NormalEnemyBaseHp * stageScale;
            Vector2 spawn = RandomMapPoint();

            if (Vector2.Distance(spawn, playerPosition) < 160f)
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
            float hp = BossBaseHp * stageScale;

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
            playerWanderTimer = Random.Range(1.2f, 3.2f);
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

        private void UpgradeAttack()
        {
            if (gold < AttackUpgradeCost) return;
            gold -= AttackUpgradeCost;
            attackLevel++;
            combatMessage = $"공격력 강화 완료 · Lv.{attackLevel}";
        }

        private void UpgradeHp()
        {
            if (gold < HpUpgradeCost) return;
            gold -= HpUpgradeCost;
            hpLevel++;
            playerHp = PlayerMaxHp;
            combatMessage = $"체력 강화 완료 · Lv.{hpLevel}";
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
            GUI.Label(new Rect(left + 540f, top + 12f, 190f, 24f), $"골드 {gold}");

            DrawHealthBar(new Rect(left + 18f, top + 70f, 220f, 20f), playerHp, PlayerMaxHp, "땅콩전사 HP");

            if (GUI.Button(new Rect(left + 500f, top + 50f, 110f, 34f),
                    $"공격 Lv.{attackLevel}\n{AttackUpgradeCost}G"))
            {
                UpgradeAttack();
            }

            if (GUI.Button(new Rect(left + 620f, top + 50f, 110f, 34f),
                    $"체력 Lv.{hpLevel}\n{HpUpgradeCost}G"))
            {
                UpgradeHp();
            }

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
