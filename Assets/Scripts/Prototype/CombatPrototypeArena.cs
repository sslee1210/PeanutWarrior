using System.Collections.Generic;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class CombatPrototypeArena : MonoBehaviour
    {
        private enum SwordElement { Neutral, Fire, Ice, Lightning }

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
        private const float ArenaHeight = 470f;
        private const float MapLeft = 55f;
        private const float MapRight = ArenaWidth - 55f;
        private const float MapTop = 155f;
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
        private int diamonds;
        private int lifetimeKills;
        private int lastDiamondMilestone;
        private int attackLevel = 1;
        private int hpLevel = 1;
        private int maxMpLevel = 1;
        private int mpRegenLevel = 1;
        private int basicAttackLevel = 1;
        private int advancementTier;
        private bool miniSlotsUnlocked;
        private SwordElement huntingElement = SwordElement.Neutral;
        private SwordElement bossElement = SwordElement.Fire;

        private float AdvancementStatMultiplier => 1f + advancementTier * 0.35f;
        private float PlayerMaxHp => (100f + (hpLevel - 1) * 25f) * AdvancementStatMultiplier;
        private float PlayerMaxMp => 100f + (maxMpLevel - 1) * 20f + advancementTier * 15f;
        private float PlayerMpRegen => 8f + (mpRegenLevel - 1) * 2.5f + advancementTier;
        private float PlayerAttackDamage => (18f + (attackLevel - 1) * 6f) *
            (1f + (basicAttackLevel - 1) * 0.12f) * AdvancementStatMultiplier;
        private int BasicAttackHits => 1 + advancementTier;
        private float SkillAdvancementMultiplier => 1f + advancementTier * 0.25f;
        private long AttackUpgradeCost => 20L * attackLevel;
        private long HpUpgradeCost => 25L * hpLevel;
        private long MaxMpUpgradeCost => 30L * maxMpLevel;
        private long MpRegenUpgradeCost => 35L * mpRegenLevel;
        private int GlobalStage => (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;
        private int CombatPower => Mathf.RoundToInt(PlayerAttackDamage * 8f + PlayerMaxHp * 0.7f + PlayerMaxMp * 0.25f);
        private SwordElement ActiveElement => stageFlow.Phase == StageFlowPhase.BossBattle ? bossElement : huntingElement;

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

            if (stageFlow.Phase == StageFlowPhase.BossBattle) UpdateBossBattle();
            else UpdateHunting();
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
            for (int hit = 0; hit < BasicAttackHits; hit++)
            {
                if (!enemies.Contains(target)) break;
                DealDamage(target, PlayerAttackDamage / BasicAttackHits, true);
            }
            combatMessage = $"{AdvancementName()} 기본 공격 · {BasicAttackHits}타";
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
                    break;
                case SwordElement.Ice:
                    target.FrostTimer = 3f;
                    break;
                case SwordElement.Lightning:
                    if (target.ShockCooldown <= 0f)
                    {
                        target.ShockCooldown = 1.2f;
                        ChainLightning(target, baseDamage * 0.45f);
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
                if (other == source || Vector2.Distance(source.Position, other.Position) > 150f) continue;
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
                float multiplier = (1.4f + offset * 0.35f) *
                    (1f + (skillLevels[index] - 1) * 0.15f) * SkillAdvancementMultiplier;
                int skillHits = 1 + advancementTier + (offset >= 2 ? 1 : 0);
                for (int hit = 0; hit < skillHits; hit++)
                {
                    if (!enemies.Contains(target)) break;
                    DealDamage(target, PlayerAttackDamage * multiplier / skillHits, true);
                }
                combatMessage = $"{(stageFlow.Phase == StageFlowPhase.BossBattle ? "보스" : "사냥")} 스킬 {offset + 1} · {skillHits}타";
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
                diamonds += 2;
                combatMessage = $"보스 처치 +{reward}G +4조각 +2다이아";
                stageFlow.HandleBossDefeated();
            }
            else
            {
                lifetimeKills++;
                long reward = 2L + stageFlow.Stage;
                gold += reward;
                if (Random.value < 0.2f) fragments++;
                AwardKillMilestoneDiamonds();
                stageFlow.RegisterMonsterKill();
                combatMessage = $"몬스터 처치 +{reward}G · {stageFlow.MonsterKills}/100";
            }
        }

        private void AwardKillMilestoneDiamonds()
        {
            int milestone = lifetimeKills / 25;
            if (milestone <= lastDiamondMilestone) return;
            int gained = milestone - lastDiamondMilestone;
            lastDiamondMilestone = milestone;
            diamonds += gained;
            combatMessage = $"업적: 누적 {lifetimeKills}마리 · 다이아 +{gained}";
        }

        private void DamagePlayerFrom(EnemyUnit enemy, float damage, float interval)
        {
            enemy.AttackCooldown -= Time.deltaTime;
            if (enemy.AttackCooldown > 0f) return;
            enemy.AttackCooldown = interval;
            playerHp = Mathf.Max(0f, playerHp - damage);
            if (playerHp > 0f) return;

            enemies.Clear();
            FullRestore();
            if (stageFlow.Phase == StageFlowPhase.BossBattle) stageFlow.HandleBossBattleDeath();
            else stageFlow.HandleHuntingDeath();
        }

        private void SpawnNormalEnemy()
        {
            float scale = 1f + (GlobalStage - 1) * 0.025f;
            Vector2 spawn = RandomMapPoint();
            if (Vector2.Distance(spawn, playerPosition) < 160f)
                spawn = new Vector2(MapRight - 20f, Random.Range(MapTop, MapBottom));

            float hp = NormalEnemyBaseHp * scale;
            enemies.Add(new EnemyUnit
            {
                Position = spawn,
                WanderTarget = RandomMapPoint(),
                WanderTimer = Random.Range(1.5f, 4.5f),
                Hp = hp,
                MaxHp = hp,
                AttackCooldown = 0.8f
            });
        }

        private void SpawnBoss()
        {
            float scale = 1f + (GlobalStage - 1) * 0.05f;
            float hp = BossBaseHp * scale;
            enemies.Add(new EnemyUnit
            {
                Position = new Vector2(MapRight - 30f, (MapTop + MapBottom) * 0.5f),
                WanderTarget = RandomMapPoint(),
                Hp = hp,
                MaxHp = hp,
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

        private bool CanAdvance(out string reason)
        {
            if (advancementTier >= 2)
            {
                reason = "현재 프로토타입 최고 전직 단계";
                return false;
            }

            int requiredStage = advancementTier == 0 ? 2 : 4;
            int requiredPower = advancementTier == 0 ? 180 : 420;
            long requiredGold = advancementTier == 0 ? 150L : 500L;
            int requiredDiamonds = advancementTier == 0 ? 5 : 15;

            if (GlobalStage < requiredStage) { reason = $"스테이지 {requiredStage} 필요"; return false; }
            if (CombatPower < requiredPower) { reason = $"전투력 {requiredPower} 필요"; return false; }
            if (gold < requiredGold) { reason = $"골드 {requiredGold} 필요"; return false; }
            if (diamonds < requiredDiamonds) { reason = $"다이아 {requiredDiamonds} 필요"; return false; }
            reason = "전직 가능";
            return true;
        }

        private void TryAdvance()
        {
            if (!CanAdvance(out string reason))
            {
                combatMessage = reason;
                return;
            }

            long goldCost = advancementTier == 0 ? 150L : 500L;
            int diamondCost = advancementTier == 0 ? 5 : 15;
            gold -= goldCost;
            diamonds -= diamondCost;
            advancementTier++;
            if (advancementTier >= 2) miniSlotsUnlocked = true;
            FullRestore();
            combatMessage = $"전직 성공: {AdvancementName()} · 기본 공격 {BasicAttackHits}타";
        }

        private string AdvancementName()
        {
            return advancementTier switch
            {
                0 => "새싹 껍질",
                1 => "전투 껍질",
                2 => "황금 수호 껍질",
                _ => $"전직 {advancementTier}단계"
            };
        }

        private string AdvancementRequirementText()
        {
            if (advancementTier >= 2) return "최고 전직 달성";
            int stage = advancementTier == 0 ? 2 : 4;
            int power = advancementTier == 0 ? 180 : 420;
            long requiredGold = advancementTier == 0 ? 150L : 500L;
            int requiredDiamonds = advancementTier == 0 ? 5 : 15;
            return $"조건: {stage}스테이지 / 전투력 {power} / {requiredGold}G / {requiredDiamonds}다이아";
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

            GUI.Label(new Rect(left + 16f, top + 10f, 490f, 24f),
                $"{stageFlow.GetWorldDisplayName()} {stageFlow.World}-{stageFlow.Stage} · 처치 {stageFlow.MonsterKills}/100");
            GUI.Label(new Rect(left + 16f, top + 34f, 500f, 24f), combatMessage);
            GUI.Label(new Rect(left + 510f, top + 10f, 235f, 42f),
                $"골드 {gold} · 조각 {fragments}\n다이아 {diamonds} · 전투력 {CombatPower}");

            DrawBar(new Rect(left + 16f, top + 62f, 220f, 18f), playerHp, PlayerMaxHp,
                $"HP {Mathf.CeilToInt(playerHp)}/{Mathf.CeilToInt(PlayerMaxHp)}");
            DrawBar(new Rect(left + 16f, top + 84f, 220f, 18f), playerMp, PlayerMaxMp,
                $"MP {Mathf.CeilToInt(playerMp)}/{Mathf.CeilToInt(PlayerMaxMp)}");

            if (GUI.Button(new Rect(left + 250f, top + 58f, 110f, 40f), $"사냥검\n{ElementName(huntingElement)}")) CycleElement(false);
            if (GUI.Button(new Rect(left + 365f, top + 58f, 110f, 40f), $"보스검\n{ElementName(bossElement)}")) CycleElement(true);

            GUI.Box(new Rect(left + 485f, top + 55f, 260f, 88f), GUIContent.none);
            GUI.Label(new Rect(left + 495f, top + 60f, 245f, 22f), $"전직 {advancementTier}단계 · {AdvancementName()}");
            GUI.Label(new Rect(left + 495f, top + 81f, 245f, 36f), AdvancementRequirementText());
            if (GUI.Button(new Rect(left + 495f, top + 112f, 115f, 27f), "전직 시도")) TryAdvance();
            GUI.Label(new Rect(left + 615f, top + 115f, 125f, 22f), miniSlotsUnlocked ? "미니 슬롯 3/3 해금" : "미니 슬롯 잠김");

            Rect playerRect = new Rect(left + playerPosition.x - 30f, top + playerPosition.y - 30f, 60f, 60f);
            GUI.Box(playerRect, $"{ShellMark()}\n전사");

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                float size = enemy.IsBoss ? 88f : 48f;
                Rect enemyRect = new Rect(left + enemy.Position.x - size * 0.5f, top + enemy.Position.y - size * 0.5f, size, size);
                string status = enemy.BurnTimer > 0f ? "화상" : enemy.FrostTimer > 0f ? "빙결" : enemy.RuptureStacks > 0 ? $"파열{enemy.RuptureStacks}" : string.Empty;
                GUI.Box(enemyRect, enemy.IsBoss ? $"보스\n{status}" : $"균\n{status}");
                DrawBar(new Rect(enemyRect.x, enemyRect.y - 13f, enemyRect.width, 10f), enemy.Hp, enemy.MaxHp, string.Empty);
            }

            if (GUI.Button(new Rect(left + 500f, top + 150f, 110f, 34f), $"공격 Lv.{attackLevel}\n{AttackUpgradeCost}G") && gold >= AttackUpgradeCost)
            {
                gold -= AttackUpgradeCost;
                attackLevel++;
            }
            if (GUI.Button(new Rect(left + 620f, top + 150f, 110f, 34f), $"체력 Lv.{hpLevel}\n{HpUpgradeCost}G") && gold >= HpUpgradeCost)
            {
                gold -= HpUpgradeCost;
                hpLevel++;
                playerHp = PlayerMaxHp;
            }
            if (GUI.Button(new Rect(left + 500f, top + 190f, 110f, 34f), $"최대MP Lv.{maxMpLevel}\n{MaxMpUpgradeCost}G") && gold >= MaxMpUpgradeCost)
            {
                gold -= MaxMpUpgradeCost;
                maxMpLevel++;
                playerMp = PlayerMaxMp;
            }
            if (GUI.Button(new Rect(left + 620f, top + 190f, 110f, 34f), $"MP회복 Lv.{mpRegenLevel}\n{MpRegenUpgradeCost}G") && gold >= MpRegenUpgradeCost)
            {
                gold -= MpRegenUpgradeCost;
                mpRegenLevel++;
            }
        }

        private string ShellMark()
        {
            return advancementTier switch
            {
                0 => "새싹",
                1 => "전투",
                2 => "황금",
                _ => "강화"
            };
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
