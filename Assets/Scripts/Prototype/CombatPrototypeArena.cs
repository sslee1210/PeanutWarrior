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
        private const float MapTop = 125f;
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
        private readonly int[] huntingSkillLevels = { 1, 1, 1, 1 };
        private readonly int[] bossSkillLevels = { 1, 1, 1, 1 };
        private readonly float[] huntingSkillCooldowns = new float[4];
        private readonly float[] bossSkillCooldowns = new float[4];

        private readonly string[] huntingSkillNames =
        {
            "회전 폭풍", "검기 난사", "추적 검무", "천지 절단"
        };

        private readonly string[] bossSkillNames =
        {
            "연속 참격", "급소 절개", "속성 각인", "차원 종결"
        };

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
        private long skillFragments;
        private int attackLevel = 1;
        private int hpLevel = 1;
        private int maxMpLevel = 1;
        private int mpRegenLevel = 1;
        private int basicAttackLevel = 1;

        private float PlayerMaxHp => 100f + (hpLevel - 1) * 25f;
        private float PlayerMaxMp => 100f + (maxMpLevel - 1) * 15f;
        private float PlayerMpRegen => 9f + (mpRegenLevel - 1) * 1.5f;
        private float PlayerAttackDamage => (18f + (attackLevel - 1) * 6f) * (1f + (basicAttackLevel - 1) * 0.12f);
        private long AttackUpgradeCost => 20L * attackLevel;
        private long HpUpgradeCost => 25L * hpLevel;
        private long MaxMpUpgradeCost => 24L * maxMpLevel;
        private long MpRegenUpgradeCost => 28L * mpRegenLevel;
        private long BasicAttackUpgradeCost => 8L * basicAttackLevel;

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

            playerMp = Mathf.Min(PlayerMaxMp, playerMp + PlayerMpRegen * Time.deltaTime);
            TickSkillCooldowns();

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
            UpdateHuntingSkills(nearest);
            UpdateAutomaticAttack(nearest);
        }

        private void UpdateBossBattle()
        {
            if (enemies.Count == 0) SpawnBoss();

            EnemyUnit boss = enemies[0];
            UpdatePlayerMovement(boss);
            UpdateEnemies();
            UpdateBossSkills(boss);
            UpdateAutomaticAttack(boss);
        }

        private void TickSkillCooldowns()
        {
            for (int i = 0; i < 4; i++)
            {
                huntingSkillCooldowns[i] = Mathf.Max(0f, huntingSkillCooldowns[i] - Time.deltaTime);
                bossSkillCooldowns[i] = Mathf.Max(0f, bossSkillCooldowns[i] - Time.deltaTime);
            }
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
                ChoosePlayerWanderTarget();
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
                float aggroRange = enemy.IsBoss ? BossAggroRange : EnemyAggroRange;
                float attackRange = enemy.IsBoss ? BossAttackRange : EnemyAttackRange;
                float moveSpeed = enemy.IsBoss ? BossMoveSpeed : EnemyMoveSpeed;

                if (distance <= aggroRange)
                {
                    if (distance > attackRange)
                    {
                        enemy.Position = Vector2.MoveTowards(enemy.Position, playerPosition, moveSpeed * Time.deltaTime);
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
                    ChooseEnemyWanderTarget(enemy);
                }

                enemy.Position = Vector2.MoveTowards(enemy.Position, enemy.WanderTarget, moveSpeed * 0.65f * Time.deltaTime);
                enemy.Position = ClampToMap(enemy.Position);
            }
        }

        private void UpdateAutomaticAttack(EnemyUnit target)
        {
            if (target == null || Vector2.Distance(playerPosition, target.Position) > PlayerAttackRange) return;

            playerAttackCooldown -= Time.deltaTime;
            if (playerAttackCooldown > 0f) return;

            playerAttackCooldown = PlayerAttackInterval;
            ApplyDamage(target, PlayerAttackDamage, "기본 공격");
        }

        private void UpdateHuntingSkills(EnemyUnit nearest)
        {
            if (nearest == null) return;

            if (TryUseSkill(huntingSkillCooldowns, 0, 24f, 4.5f))
            {
                float damage = PlayerAttackDamage * (1.15f + huntingSkillLevels[0] * 0.12f);
                DamageEnemiesInRange(playerPosition, 115f, damage, "회전 폭풍");
                return;
            }

            if (TryUseSkill(huntingSkillCooldowns, 1, 30f, 6f))
            {
                float damage = PlayerAttackDamage * (0.65f + huntingSkillLevels[1] * 0.08f);
                for (int i = 0; i < 3; i++)
                {
                    EnemyUnit target = FindClosestEnemy();
                    if (target == null) break;
                    ApplyDamage(target, damage, "검기 난사");
                }
                return;
            }

            if (TryUseSkill(huntingSkillCooldowns, 2, 36f, 7.5f))
            {
                float damage = PlayerAttackDamage * (1.7f + huntingSkillLevels[2] * 0.15f);
                EnemyUnit target = FindLowestHpEnemy();
                if (target != null) ApplyDamage(target, damage, "추적 검무");
                return;
            }

            if (TryUseSkill(huntingSkillCooldowns, 3, 48f, 11f))
            {
                float damage = PlayerAttackDamage * (2.0f + huntingSkillLevels[3] * 0.2f);
                DamageAllEnemies(damage, "천지 절단");
            }
        }

        private void UpdateBossSkills(EnemyUnit boss)
        {
            if (boss == null) return;

            if (TryUseSkill(bossSkillCooldowns, 0, 22f, 3.8f))
            {
                float damage = PlayerAttackDamage * (1.35f + bossSkillLevels[0] * 0.14f);
                ApplyDamage(boss, damage, "연속 참격");
                return;
            }

            if (TryUseSkill(bossSkillCooldowns, 1, 30f, 5.5f))
            {
                float missingRatio = 1f - Mathf.Clamp01(boss.Hp / boss.MaxHp);
                float damage = PlayerAttackDamage * (1.7f + missingRatio + bossSkillLevels[1] * 0.16f);
                ApplyDamage(boss, damage, "급소 절개");
                return;
            }

            if (TryUseSkill(bossSkillCooldowns, 2, 38f, 7.5f))
            {
                float damage = PlayerAttackDamage * (2.15f + bossSkillLevels[2] * 0.18f);
                ApplyDamage(boss, damage, "속성 각인");
                return;
            }

            if (boss.Hp / boss.MaxHp <= 0.35f && TryUseSkill(bossSkillCooldowns, 3, 55f, 12f))
            {
                float damage = PlayerAttackDamage * (3.2f + bossSkillLevels[3] * 0.25f);
                ApplyDamage(boss, damage, "차원 종결");
            }
        }

        private bool TryUseSkill(float[] cooldowns, int index, float mpCost, float cooldown)
        {
            if (cooldowns[index] > 0f || playerMp < mpCost) return false;
            playerMp -= mpCost;
            cooldowns[index] = cooldown;
            return true;
        }

        private void DamageEnemiesInRange(Vector2 center, float radius, float damage, string skillName)
        {
            List<EnemyUnit> targets = new List<EnemyUnit>();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (Vector2.Distance(center, enemies[i].Position) <= radius) targets.Add(enemies[i]);
            }

            for (int i = 0; i < targets.Count; i++) ApplyDamage(targets[i], damage, skillName);
        }

        private void DamageAllEnemies(float damage, string skillName)
        {
            List<EnemyUnit> targets = new List<EnemyUnit>(enemies);
            for (int i = 0; i < targets.Count; i++) ApplyDamage(targets[i], damage, skillName);
        }

        private void ApplyDamage(EnemyUnit target, float damage, string attackName)
        {
            if (target == null || !enemies.Contains(target)) return;

            target.Hp -= damage;
            combatMessage = $"{attackName}! {Mathf.CeilToInt(damage)} 피해";
            if (target.Hp > 0f) return;

            bool wasBoss = target.IsBoss;
            enemies.Remove(target);

            if (wasBoss)
            {
                long reward = 40L + stageFlow.Stage * 10L;
                long fragmentReward = 4L + stageFlow.Stage / 5L;
                gold += reward;
                skillFragments += fragmentReward;
                combatMessage = $"보스 처치! +{reward}G +{fragmentReward}조각";
                stageFlow.HandleBossDefeated();
            }
            else
            {
                long reward = 2L + stageFlow.Stage;
                long fragmentReward = Random.value < 0.35f ? 1L : 0L;
                gold += reward;
                skillFragments += fragmentReward;
                stageFlow.RegisterMonsterKill();
                combatMessage = fragmentReward > 0
                    ? $"몬스터 처치 +{reward}G +1조각 · {stageFlow.MonsterKills}/100"
                    : $"몬스터 처치 +{reward}G · {stageFlow.MonsterKills}/100";
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

            enemies.Clear();
            playerHp = PlayerMaxHp;
            playerMp = PlayerMaxMp;
            combatMessage = enemy.IsBoss
                ? "보스전 패배 · 현재 스테이지 0/100 재시작"
                : "사냥 중 사망 · 이전 스테이지 이동";

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

        private EnemyUnit FindLowestHpEnemy()
        {
            if (enemies.Count == 0) return null;
            EnemyUnit result = enemies[0];
            for (int i = 1; i < enemies.Count; i++)
            {
                if (enemies[i].Hp < result.Hp) result = enemies[i];
            }
            return result;
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
            return new Vector2(Random.Range(MapLeft, MapRight), Random.Range(MapTop, MapBottom));
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
            FullRestore();
            ResetCooldowns();
            playerPosition = new Vector2(MapLeft + 70f, (MapTop + MapBottom) * 0.5f);
            ChoosePlayerWanderTarget();
            SpawnBoss();
        }

        private void ResetForHunting()
        {
            enemies.Clear();
            FullRestore();
            ResetCooldowns();
            spawnCooldown = 0f;
            playerPosition = new Vector2(ArenaWidth * 0.5f, (MapTop + MapBottom) * 0.5f);
            ChoosePlayerWanderTarget();
            combatMessage = "맵 자유 사냥 시작";
        }

        private void FullRestore()
        {
            playerHp = PlayerMaxHp;
            playerMp = PlayerMaxMp;
            playerAttackCooldown = 0f;
        }

        private void ResetCooldowns()
        {
            for (int i = 0; i < 4; i++)
            {
                huntingSkillCooldowns[i] = 0f;
                bossSkillCooldowns[i] = 0f;
            }
        }

        private void HandleStageStateChanged()
        {
            if (stageFlow.Phase == StageFlowPhase.Hunting && enemies.Count == 0) spawnCooldown = 0f;
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

        private void UpgradeMaxMp()
        {
            if (gold < MaxMpUpgradeCost) return;
            gold -= MaxMpUpgradeCost;
            maxMpLevel++;
            playerMp = PlayerMaxMp;
            combatMessage = $"최대 MP 강화 완료 · Lv.{maxMpLevel}";
        }

        private void UpgradeMpRegen()
        {
            if (gold < MpRegenUpgradeCost) return;
            gold -= MpRegenUpgradeCost;
            mpRegenLevel++;
            combatMessage = $"MP 회복 강화 완료 · Lv.{mpRegenLevel}";
        }

        private void UpgradeBasicAttack()
        {
            if (skillFragments < BasicAttackUpgradeCost) return;
            skillFragments -= BasicAttackUpgradeCost;
            basicAttackLevel++;
            combatMessage = $"기본 공격 강화 완료 · Lv.{basicAttackLevel}";
        }

        private void UpgradeSkill(bool boss, int index)
        {
            int[] levels = boss ? bossSkillLevels : huntingSkillLevels;
            long cost = 10L * levels[index];
            if (skillFragments < cost) return;
            skillFragments -= cost;
            levels[index]++;
            string name = boss ? bossSkillNames[index] : huntingSkillNames[index];
            combatMessage = $"{name} 강화 완료 · Lv.{levels[index]}";
        }

        private void OnGUI()
        {
            if (stageFlow == null) return;

            float left = Mathf.Max(20f, (Screen.width - ArenaWidth) * 0.5f);
            float top = Mathf.Max(20f, Screen.height - ArenaHeight - 35f);
            Rect arena = new Rect(left, top, ArenaWidth, ArenaHeight);

            GUI.Box(arena, GUIContent.none);
            GUI.Label(new Rect(left + 16f, top + 10f, 500f, 24f),
                $"{stageFlow.GetWorldDisplayName()}  {stageFlow.World}-{stageFlow.Stage}   처치 {stageFlow.MonsterKills}/100");
            GUI.Label(new Rect(left + 16f, top + 34f, 500f, 22f), combatMessage);
            GUI.Label(new Rect(left + 525f, top + 10f, 215f, 22f), $"골드 {gold} · 조각 {skillFragments}");

            DrawBar(new Rect(left + 16f, top + 62f, 220f, 18f), playerHp, PlayerMaxHp,
                $"HP {Mathf.CeilToInt(playerHp)}/{Mathf.CeilToInt(PlayerMaxHp)}");
            DrawBar(new Rect(left + 16f, top + 84f, 220f, 18f), playerMp, PlayerMaxMp,
                $"MP {Mathf.CeilToInt(playerMp)}/{Mathf.CeilToInt(PlayerMaxMp)}");

            if (GUI.Button(new Rect(left + 250f, top + 58f, 92f, 42f), $"공격 {attackLevel}\n{AttackUpgradeCost}G")) UpgradeAttack();
            if (GUI.Button(new Rect(left + 346f, top + 58f, 92f, 42f), $"체력 {hpLevel}\n{HpUpgradeCost}G")) UpgradeHp();
            if (GUI.Button(new Rect(left + 442f, top + 58f, 92f, 42f), $"MP {maxMpLevel}\n{MaxMpUpgradeCost}G")) UpgradeMaxMp();
            if (GUI.Button(new Rect(left + 538f, top + 58f, 92f, 42f), $"MP회복 {mpRegenLevel}\n{MpRegenUpgradeCost}G")) UpgradeMpRegen();
            if (GUI.Button(new Rect(left + 634f, top + 58f, 106f, 42f), $"기본공격 {basicAttackLevel}\n{BasicAttackUpgradeCost}조각")) UpgradeBasicAttack();

            DrawSkillButtons(left, top);

            Rect playerRect = new Rect(left + playerPosition.x - 28f, top + playerPosition.y - 28f, 56f, 56f);
            GUI.Box(playerRect, "🥜\n전사");

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float size = enemy.IsBoss ? 88f : 48f;
                Rect enemyRect = new Rect(left + enemy.Position.x - size * 0.5f, top + enemy.Position.y - size * 0.5f, size, size);
                GUI.Box(enemyRect, enemy.IsBoss ? "👹\n보스" : "균");
                DrawBar(new Rect(enemyRect.x, enemyRect.y - 15f, enemyRect.width, 11f), enemy.Hp, enemy.MaxHp, string.Empty);
            }

            if (stageFlow.Phase == StageFlowPhase.BossReady)
            {
                GUI.Label(new Rect(left + 275f, top + 205f, 300f, 34f), "보스 도전 버튼을 누르세요");
            }
        }

        private void DrawSkillButtons(float left, float top)
        {
            bool bossMode = stageFlow.Phase == StageFlowPhase.BossBattle;
            string[] names = bossMode ? bossSkillNames : huntingSkillNames;
            int[] levels = bossMode ? bossSkillLevels : huntingSkillLevels;
            float[] cooldowns = bossMode ? bossSkillCooldowns : huntingSkillCooldowns;

            for (int i = 0; i < 4; i++)
            {
                float x = left + 16f + i * 180f;
                long cost = 10L * levels[i];
                string cooldown = cooldowns[i] > 0f ? $" {cooldowns[i]:0.0}s" : " 준비";
                if (GUI.Button(new Rect(x, top + 106f, 172f, 38f),
                    $"{names[i]} Lv.{levels[i]}{cooldown}\n강화 {cost}조각"))
                {
                    UpgradeSkill(bossMode, i);
                }
            }
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
