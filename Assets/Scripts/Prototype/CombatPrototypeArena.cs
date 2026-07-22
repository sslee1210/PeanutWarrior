using System.Collections.Generic;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class CombatPrototypeArena : MonoBehaviour
    {
        private enum SwordElement
        {
            Neutral,
            Fire,
            Ice,
            Lightning
        }

        private sealed class EnemyUnit
        {
            public Vector2 Position;
            public Vector2 WanderTarget;
            public float WanderTimer;
            public float Hp;
            public float MaxHp;
            public float AttackCooldown;
            public float BurnTimer;
            public float BurnTickTimer;
            public float BurnDamage;
            public float FrostTimer;
            public float ShockCooldown;
            public int RuptureStacks;
            public bool IsBoss;
        }

        private const float ArenaWidth = 760f;
        private const float ArenaHeight = 420f;
        private const float MapLeft = 55f;
        private const float MapRight = ArenaWidth - 55f;
        private const float MapTop = 115f;
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
        private readonly int[] skillLevels = { 1, 1, 1, 1, 1, 1, 1, 1 };
        private readonly float[] skillCooldowns = new float[8];

        private StageFlowController stageFlow;
        private Vector2 playerPosition;
        private Vector2 playerWanderTarget;
        private float playerWanderTimer;
        private float playerHp;
        private float playerMp;
        private float playerAttackCooldown;
        private float spawnCooldown;
        private string combatMessage = "전투 준비";

        private long gold;
        private long fragments;
        private int attackLevel = 1;
        private int hpLevel = 1;
        private int maxMpLevel = 1;
        private int mpRegenLevel = 1;
        private int basicAttackLevel = 1;
        private SwordElement huntingElement = SwordElement.Neutral;
        private SwordElement bossElement = SwordElement.Fire;

        private float PlayerMaxHp => 100f + (hpLevel - 1) * 25f;
        private float PlayerMaxMp => 100f + (maxMpLevel - 1) * 20f;
        private float PlayerMpRegen => 8f + (mpRegenLevel - 1) * 2.5f;
        private float PlayerAttackDamage => (18f + (attackLevel - 1) * 6f) * (1f + (basicAttackLevel - 1) * 0.12f);
        private long AttackUpgradeCost => 20L * attackLevel;
        private long HpUpgradeCost => 25L * hpLevel;
        private long MaxMpUpgradeCost => 30L * maxMpLevel;
        private long MpRegenUpgradeCost => 35L * mpRegenLevel;
        private SwordElement ActiveElement => stageFlow != null && stageFlow.Phase == StageFlowPhase.BossBattle ? bossElement : huntingElement;

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
                GameObject root = new GameObject("PeanutWarriorStageFlow");
                DontDestroyOnLoad(root);
                stageFlow = root.AddComponent<StageFlowController>();
            }

            stageFlow.BossBattleStarted += BeginBossBattle;
            stageFlow.BossBattleFailed += ResetForHunting;
            stageFlow.BossDefeated += ResetForHunting;
            stageFlow.HuntingDeath += ResetForHunting;
            ResetForHunting();
        }

        private void OnDestroy()
        {
            if (stageFlow == null) return;
            stageFlow.BossBattleStarted -= BeginBossBattle;
            stageFlow.BossBattleFailed -= ResetForHunting;
            stageFlow.BossDefeated -= ResetForHunting;
            stageFlow.HuntingDeath -= ResetForHunting;
        }

        private void Update()
        {
            if (stageFlow == null) return;

            playerMp = Mathf.Min(PlayerMaxMp, playerMp + PlayerMpRegen * Time.deltaTime);
            playerAttackCooldown -= Time.deltaTime;
            for (int i = 0; i < skillCooldowns.Length; i++) skillCooldowns[i] -= Time.deltaTime;

            UpdateStatuses();

            if (stageFlow.Phase == StageFlowPhase.BossReady)
            {
                enemies.Clear();
                UpdatePlayerMovement(null);
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
            UpdatePlayerMovement(nearest);
            UpdateEnemies();
            UpdateAutomaticAttack(nearest);
            TryUseAutomaticSkill(nearest, 0);
        }

        private void UpdateBossBattle()
        {
            if (enemies.Count == 0) SpawnBoss();
            EnemyUnit boss = enemies[0];
            UpdatePlayerMovement(boss);
            UpdateEnemies();
            UpdateAutomaticAttack(boss);
            TryUseAutomaticSkill(boss, 4);
        }

        private void UpdatePlayerMovement(EnemyUnit nearest)
        {
            if (nearest != null)
            {
                float distance = Vector2.Distance(playerPosition, nearest.Position);
                if (distance <= PlayerSearchRange && distance > PlayerStopDistance)
                {
                    playerPosition = Vector2.MoveTowards(playerPosition, nearest.Position, PlayerMoveSpeed * Time.deltaTime);
                    playerPosition = ClampToMap(playerPosition);
                    return;
                }
                if (distance <= PlayerStopDistance) return;
            }

            playerWanderTimer -= Time.deltaTime;
            if (playerWanderTimer <= 0f || Vector2.Distance(playerPosition, playerWanderTarget) < 10f)
            {
                playerWanderTarget = RandomMapPoint();
                playerWanderTimer = Random.Range(1.2f, 3.2f);
            }

            playerPosition = Vector2.MoveTowards(playerPosition, playerWanderTarget, PlayerMoveSpeed * 0.75f * Time.deltaTime);
            playerPosition = ClampToMap(playerPosition);
        }

        private void UpdateEnemies()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float distance = Vector2.Distance(enemy.Position, playerPosition);
                float aggro = enemy.IsBoss ? BossAggroRange : EnemyAggroRange;
                float range = enemy.IsBoss ? BossAttackRange : EnemyAttackRange;
                float speed = enemy.IsBoss ? BossMoveSpeed : EnemyMoveSpeed;
                if (enemy.FrostTimer > 0f) speed *= 0.55f;

                if (distance <= aggro)
                {
                    if (distance > range)
                    {
                        enemy.Position = Vector2.MoveTowards(enemy.Position, playerPosition, speed * Time.deltaTime);
                        enemy.Position = ClampToMap(enemy.Position);
                    }
                    else
                    {
                        DamagePlayerFrom(enemy, enemy.IsBoss ? 11f : 4.5f, enemy.IsBoss ? 0.9f : 1.1f);
                    }
                    continue;
                }

                enemy.WanderTimer -= Time.deltaTime;
                if (enemy.WanderTimer <= 0f || Vector2.Distance(enemy.Position, enemy.WanderTarget) < 8f)
                {
                    enemy.WanderTarget = RandomMapPoint();
                    enemy.WanderTimer = Random.Range(1.5f, 4.5f);
                }

                enemy.Position = Vector2.MoveTowards(enemy.Position, enemy.WanderTarget, speed * 0.65f * Time.deltaTime);
                enemy.Position = ClampToMap(enemy.Position);
            }
        }

        private void UpdateAutomaticAttack(EnemyUnit target)
        {
            if (target == null || playerAttackCooldown > 0f) return;
            if (Vector2.Distance(playerPosition, target.Position) > PlayerAttackRange) return;

            playerAttackCooldown = PlayerAttackInterval;
            DealDamage(target, PlayerAttackDamage, true);
        }

        private void DealDamage(EnemyUnit target, float damage, bool applyElement)
        {
            if (target == null || !enemies.Contains(target)) return;
            target.Hp -= damage;

            if (applyElement) ApplyElement(target, damage);
            if (target.Hp <= 0f) KillEnemy(target);
        }

        private void ApplyElement(EnemyUnit target, float baseDamage)
        {
            switch (ActiveElement)
            {
                case SwordElement.Neutral:
                    target.RuptureStacks++;
                    if (target.RuptureStacks >= 4)
                    {
                        target.RuptureStacks = 0;
                        target.Hp -= baseDamage * 0.8f;
                        combatMessage = "무속성 파열 · 추가 참격!";
                    }
                    break;
                case SwordElement.Fire:
                    target.BurnTimer = 4f;
                    target.BurnTickTimer = 0.5f;
                    target.BurnDamage = Mathf.Max(target.BurnDamage, baseDamage * 0.18f);
                    combatMessage = "화염 검 · 화상 부여";
                    break;
                case SwordElement.Ice:
                    target.FrostTimer = 3f;
                    combatMessage = "냉기 검 · 이동속도 감소";
                    break;
                case SwordElement.Lightning:
                    if (target.ShockCooldown <= 0f)
                    {
                        target.ShockCooldown = 1.2f;
                        ChainLightning(target, baseDamage * 0.45f);
                        combatMessage = "번개 검 · 연쇄 감전";
                    }
                    break;
            }
        }

        private void ChainLightning(EnemyUnit source, float damage)
        {
            int hits = 0;
            for (int i = 0; i < enemies.Count && hits < 2; i++)
            {
                EnemyUnit other = enemies[i];
                if (other == source) continue;
                if (Vector2.Distance(source.Position, other.Position) > 150f) continue;
                other.Hp -= damage;
                hits++;
            }
        }

        private void UpdateStatuses()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                EnemyUnit enemy = enemies[i];
                enemy.FrostTimer -= Time.deltaTime;
                enemy.ShockCooldown -= Time.deltaTime;
                if (enemy.BurnTimer > 0f)
                {
                    enemy.BurnTimer -= Time.deltaTime;
                    enemy.BurnTickTimer -= Time.deltaTime;
                    if (enemy.BurnTickTimer <= 0f)
                    {
                        enemy.BurnTickTimer = 0.5f;
                        enemy.Hp -= enemy.BurnDamage;
                    }
                }

                if (enemy.Hp <= 0f && enemies.Contains(enemy)) KillEnemy(enemy);
            }
        }

        private void TryUseAutomaticSkill(EnemyUnit target, int startIndex)
        {
            if (target == null) return;
            for (int offset = 0; offset < 4; offset++)
            {
                int index = startIndex + offset;
                float mpCost = 20f + offset * 5f;
                if (skillCooldowns[index] > 0f || playerMp < mpCost) continue;

                playerMp -= mpCost;
                skillCooldowns[index] = 5f + offset * 1.5f;
                float multiplier = (1.4f + offset * 0.35f) * (1f + (skillLevels[index] - 1) * 0.15f);
                DealDamage(target, PlayerAttackDamage * multiplier, true);
                combatMessage = stageFlow.Phase == StageFlowPhase.BossBattle
                    ? $"보스 스킬 {offset + 1} 발동"
                    : $"사냥 스킬 {offset + 1} 발동";
                break;
            }
        }

        private void KillEnemy(EnemyUnit enemy)
        {
            if (!enemies.Remove(enemy)) return;

            if (enemy.IsBoss)
            {
                long reward = 40L + stageFlow.Stage * 10L;
                gold += reward;
                fragments += 4;
                combatMessage = $"보스 처치 +{reward}G +4조각";
                stageFlow.HandleBossDefeated();
            }
            else
            {
                long reward = 2L + stageFlow.Stage;
                gold += reward;
                if (Random.value < 0.2f) fragments++;
                stageFlow.RegisterMonsterKill();
                combatMessage = $"몬스터 처치 +{reward}G · {stageFlow.MonsterKills}/100";
            }
        }

        private void DamagePlayerFrom(EnemyUnit enemy, float damage, float interval)
        {
            enemy.AttackCooldown -= Time.deltaTime;
            if (enemy.AttackCooldown > 0f) return;
            enemy.AttackCooldown = interval;
            playerHp = Mathf.Max(0f, playerHp - damage);
            if (playerHp > 0f) return;

            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerMp = PlayerMaxMp;
            if (stageFlow.Phase == StageFlowPhase.BossBattle) stageFlow.HandleBossBattleDeath();
            else stageFlow.HandleHuntingDeath();
        }

        private void SpawnNormalEnemy()
        {
            float scale = 1f + ((stageFlow.World - 1) * 30 + stageFlow.Stage - 1) * 0.025f;
            Vector2 spawn = RandomMapPoint();
            if (Vector2.Distance(spawn, playerPosition) < 160f) spawn = new Vector2(MapRight - 20f, Random.Range(MapTop, MapBottom));
            EnemyUnit enemy = new EnemyUnit
            {
                Position = spawn,
                WanderTarget = RandomMapPoint(),
                WanderTimer = Random.Range(1.5f, 4.5f),
                Hp = NormalEnemyBaseHp * scale,
                MaxHp = NormalEnemyBaseHp * scale,
                AttackCooldown = 0.8f
            };
            enemies.Add(enemy);
        }

        private void SpawnBoss()
        {
            float scale = 1f + ((stageFlow.World - 1) * 30 + stageFlow.Stage - 1) * 0.05f;
            enemies.Add(new EnemyUnit
            {
                Position = new Vector2(MapRight - 30f, (MapTop + MapBottom) * 0.5f),
                WanderTarget = RandomMapPoint(),
                Hp = BossBaseHp * scale,
                MaxHp = BossBaseHp * scale,
                AttackCooldown = 1f,
                IsBoss = true
            });
            combatMessage = $"{stageFlow.World}-{stageFlow.Stage} 보스 출현";
        }

        private EnemyUnit FindClosestEnemy()
        {
            if (enemies.Count == 0) return null;
            EnemyUnit closest = enemies[0];
            float best = Vector2.SqrMagnitude(closest.Position - playerPosition);
            for (int i = 1; i < enemies.Count; i++)
            {
                float distance = Vector2.SqrMagnitude(enemies[i].Position - playerPosition);
                if (distance >= best) continue;
                closest = enemies[i];
                best = distance;
            }
            return closest;
        }

        private void BeginBossBattle()
        {
            enemies.Clear();
            FullRestore();
            playerPosition = new Vector2(MapLeft + 70f, (MapTop + MapBottom) * 0.5f);
            SpawnBoss();
        }

        private void ResetForHunting()
        {
            enemies.Clear();
            FullRestore();
            spawnCooldown = 0f;
            playerPosition = new Vector2(ArenaWidth * 0.5f, (MapTop + MapBottom) * 0.5f);
            playerWanderTarget = RandomMapPoint();
            combatMessage = "맵 자유 사냥 시작";
        }

        private void FullRestore()
        {
            playerHp = PlayerMaxHp;
            playerMp = PlayerMaxMp;
            playerAttackCooldown = 0f;
        }

        private static Vector2 RandomMapPoint()
        {
            return new Vector2(Random.Range(MapLeft, MapRight), Random.Range(MapTop, MapBottom));
        }

        private static Vector2 ClampToMap(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, MapLeft, MapRight);
            position.y = Mathf.Clamp(position.y, MapTop, MapBottom);
            return position;
        }

        private void CycleElement(bool boss)
        {
            if (boss) bossElement = (SwordElement)(((int)bossElement + 1) % 4);
            else huntingElement = (SwordElement)(((int)huntingElement + 1) % 4);
        }

        private void OnGUI()
        {
            if (stageFlow == null) return;
            float left = Mathf.Max(20f, (Screen.width - ArenaWidth) * 0.5f);
            float top = Mathf.Max(20f, Screen.height - ArenaHeight - 35f);
            Rect arena = new Rect(left, top, ArenaWidth, ArenaHeight);
            GUI.Box(arena, GUIContent.none);

            GUI.Label(new Rect(left + 16f, top + 10f, 500f, 24f),
                $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage} · 처치 {stageFlow.MonsterKills}/100");
            GUI.Label(new Rect(left + 16f, top + 34f, 520f, 24f), combatMessage);
            GUI.Label(new Rect(left + 535f, top + 10f, 210f, 24f), $"골드 {gold} · 조각 {fragments}");

            DrawBar(new Rect(left + 16f, top + 62f, 220f, 18f), playerHp, PlayerMaxHp, $"HP {Mathf.CeilToInt(playerHp)}/{Mathf.CeilToInt(PlayerMaxHp)}");
            DrawBar(new Rect(left + 16f, top + 84f, 220f, 18f), playerMp, PlayerMaxMp, $"MP {Mathf.CeilToInt(playerMp)}/{Mathf.CeilToInt(PlayerMaxMp)}");

            if (GUI.Button(new Rect(left + 255f, top + 58f, 112f, 40f), $"사냥검: {ElementName(huntingElement)}")) CycleElement(false);
            if (GUI.Button(new Rect(left + 375f, top + 58f, 112f, 40f), $"보스검: {ElementName(bossElement)}")) CycleElement(true);
            GUI.Label(new Rect(left + 500f, top + 70f, 220f, 22f), $"현재 속성: {ElementName(ActiveElement)}");

            Rect playerRect = new Rect(left + playerPosition.x - 28f, top + playerPosition.y - 28f, 56f, 56f);
            GUI.Box(playerRect, "땅콩\n전사");

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float size = enemy.IsBoss ? 88f : 48f;
                Rect enemyRect = new Rect(left + enemy.Position.x - size * 0.5f, top + enemy.Position.y - size * 0.5f, size, size);
                string status = enemy.BurnTimer > 0f ? "화상" : enemy.FrostTimer > 0f ? "빙결" : enemy.RuptureStacks > 0 ? $"파열{enemy.RuptureStacks}" : string.Empty;
                GUI.Box(enemyRect, enemy.IsBoss ? $"보스\n{status}" : $"균\n{status}");
                DrawBar(new Rect(enemyRect.x, enemyRect.y - 13f, enemyRect.width, 10f), enemy.Hp, enemy.MaxHp, string.Empty);
            }

            if (GUI.Button(new Rect(left + 500f, top + 100f, 110f, 34f), $"공격 Lv.{attackLevel}\n{AttackUpgradeCost}G") && gold >= AttackUpgradeCost)
            {
                gold -= AttackUpgradeCost;
                attackLevel++;
            }
            if (GUI.Button(new Rect(left + 620f, top + 100f, 110f, 34f), $"체력 Lv.{hpLevel}\n{HpUpgradeCost}G") && gold >= HpUpgradeCost)
            {
                gold -= HpUpgradeCost;
                hpLevel++;
                playerHp = PlayerMaxHp;
            }
            if (GUI.Button(new Rect(left + 500f, top + 140f, 110f, 34f), $"최대MP Lv.{maxMpLevel}\n{MaxMpUpgradeCost}G") && gold >= MaxMpUpgradeCost)
            {
                gold -= MaxMpUpgradeCost;
                maxMpLevel++;
                playerMp = PlayerMaxMp;
            }
            if (GUI.Button(new Rect(left + 620f, top + 140f, 110f, 34f), $"MP회복 Lv.{mpRegenLevel}\n{MpRegenUpgradeCost}G") && gold >= MpRegenUpgradeCost)
            {
                gold -= MpRegenUpgradeCost;
                mpRegenLevel++;
            }

            GUI.Label(new Rect(left + 500f, top + 182f, 220f, 70f),
                "무: 4타 후 추가참격\n화: 화상 지속피해\n빙: 이동속도 감소\n뇌: 주변 연쇄피해");
        }

        private static string ElementName(SwordElement element)
        {
            return element switch
            {
                SwordElement.Neutral => "무속성",
                SwordElement.Fire => "화염",
                SwordElement.Ice => "냉기",
                SwordElement.Lightning => "번개",
                _ => element.ToString()
            };
        }

        private static void DrawBar(Rect rect, float current, float maximum, string label)
        {
            GUI.Box(rect, GUIContent.none);
            float ratio = maximum <= 0f ? 0f : Mathf.Clamp01(current / maximum);
            GUI.Box(new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * ratio, rect.height - 4f), GUIContent.none);
            if (!string.IsNullOrEmpty(label)) GUI.Label(rect, label);
        }
    }
}
