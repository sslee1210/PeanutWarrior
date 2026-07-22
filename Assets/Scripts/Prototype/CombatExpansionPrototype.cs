using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Advanced zero-setup combat layer for the prototype:
    /// smooth charge movement, trail/AOE damage, crowd separation,
    /// dense idle waves, and complete boss-entry combat reset.
    /// </summary>
    public sealed class CombatExpansionPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private const int HuntingMonsterCap = 24;
        private const float ReinforcementInterval = 0.14f;
        private const int ReinforcementBatch = 4;

        private const float ChargeCooldown = 1.55f;
        private const float ChargeMinimumDistance = 88f;
        private const float ChargeDetectionRange = 320f;
        private const float ChargeStopDistance = 48f;
        private const float ChargeDuration = 0.18f;
        private const float ChargeDirectDamageRatio = 0.65f;
        private const float ChargePathDamageRatio = 0.28f;
        private const float ChargePathRadius = 42f;

        private const float EnemySeparationRadius = 47f;
        private const float BossSeparationRadius = 76f;
        private const float SeparationStrength = 52f;
        private const float PlayerPersonalSpace = 42f;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private PropertyInfo playerAttackDamageProperty;
        private FieldInfo skillCooldownsField;
        private FieldInfo playerAttackCooldownField;
        private MethodInfo spawnNormalEnemyMethod;
        private MethodInfo dealDamageMethod;
        private MethodInfo fullRestoreMethod;

        private float reinforcementTimer;
        private float chargeTimer;
        private bool charging;
        private float chargeElapsed;
        private Vector2 chargeFrom;
        private Vector2 chargeTo;
        private object chargeTarget;
        private float chargeAttackDamage;
        private readonly HashSet<object> chargeHitTargets = new HashSet<object>();
        private readonly List<Vector2> trailPoints = new List<Vector2>();
        private float trailVisibleTimer;
        private string passiveMessage = "돌진 패시브 준비";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateExpansion()
        {
            if (FindFirstObjectByType<CombatExpansionPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCombatExpansionPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<CombatExpansionPrototype>();
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
            playerAttackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            skillCooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            playerAttackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            spawnNormalEnemyMethod = arenaType.GetMethod("SpawnNormalEnemy", PrivateInstance);
            dealDamageMethod = arenaType.GetMethod("DealDamage", PrivateInstance);
            fullRestoreMethod = arenaType.GetMethod("FullRestore", PrivateInstance);

            stageFlow.BossBattleStarted += HandleBossBattleStarted;
            chargeTimer = 0.35f;
        }

        private void OnDestroy()
        {
            if (stageFlow != null)
                stageFlow.BossBattleStarted -= HandleBossBattleStarted;
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;

            chargeTimer -= Time.deltaTime;
            trailVisibleTimer -= Time.deltaTime;

            if (stageFlow.Phase != StageFlowPhase.BossBattle)
                UpdateDenseMonsterWave();

            ApplyCrowdSeparation();

            if (charging)
                UpdateChargeMotion();
            else
                TryStartCharge();
        }

        private void UpdateDenseMonsterWave()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count >= HuntingMonsterCap) return;

            reinforcementTimer -= Time.deltaTime;
            if (reinforcementTimer > 0f) return;

            int missing = HuntingMonsterCap - enemies.Count;
            int spawnBatch = Mathf.Min(ReinforcementBatch, missing);
            for (int i = 0; i < spawnBatch; i++)
                spawnNormalEnemyMethod?.Invoke(arena, null);

            reinforcementTimer = ReinforcementInterval;
        }

        private void TryStartCharge()
        {
            if (chargeTimer > 0f || stageFlow.Phase == StageFlowPhase.BossReady) return;

            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count == 0 || playerPositionField == null) return;

            Vector2 playerPosition = (Vector2)playerPositionField.GetValue(arena);
            object target = FindClosestEnemy(enemies, playerPosition, out Vector2 targetPosition, out float distance);
            if (target == null || distance < ChargeMinimumDistance || distance > ChargeDetectionRange) return;

            Vector2 direction = (targetPosition - playerPosition).normalized;
            Vector2 destination = targetPosition - direction * ChargeStopDistance;
            destination = ClampToArena(destination);

            chargeFrom = playerPosition;
            chargeTo = destination;
            chargeTarget = target;
            chargeAttackDamage = playerAttackDamageProperty != null
                ? Convert.ToSingle(playerAttackDamageProperty.GetValue(arena))
                : 18f;
            chargeElapsed = 0f;
            charging = true;
            chargeHitTargets.Clear();
            trailPoints.Clear();
            trailPoints.Add(chargeFrom);
            trailVisibleTimer = 0.35f;
            passiveMessage = "기본 패시브: 돌진 베기 발동";
        }

        private void UpdateChargeMotion()
        {
            chargeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(chargeElapsed / ChargeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Vector2 current = Vector2.Lerp(chargeFrom, chargeTo, eased);
            playerPositionField.SetValue(arena, current);

            if (trailPoints.Count == 0 || Vector2.Distance(trailPoints[trailPoints.Count - 1], current) >= 16f)
                trailPoints.Add(current);

            DamageEnemiesAlongCharge(current);

            if (t < 1f) return;

            charging = false;
            playerPositionField.SetValue(arena, chargeTo);

            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (chargeTarget != null && enemies != null && ContainsReference(enemies, chargeTarget))
            {
                dealDamageMethod?.Invoke(arena,
                    new[] { chargeTarget, (object)(chargeAttackDamage * ChargeDirectDamageRatio), true });
            }

            chargeTimer = ChargeCooldown;
            trailVisibleTimer = 0.28f;
            passiveMessage = $"돌진 완료 · 경로 타격 {chargeHitTargets.Count}개 · 쿨타임 {ChargeCooldown:0.00}초";
        }

        private void DamageEnemiesAlongCharge(Vector2 current)
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null) return;

            var snapshot = new List<object>();
            for (int i = 0; i < enemies.Count; i++) snapshot.Add(enemies[i]);

            for (int i = 0; i < snapshot.Count; i++)
            {
                object enemy = snapshot[i];
                if (enemy == null || chargeHitTargets.Contains(enemy)) continue;
                if (!TryGetEnemyPosition(enemy, out Vector2 enemyPosition)) continue;
                if (Vector2.Distance(current, enemyPosition) > ChargePathRadius) continue;

                chargeHitTargets.Add(enemy);
                dealDamageMethod?.Invoke(arena,
                    new[] { enemy, (object)(chargeAttackDamage * ChargePathDamageRatio), true });
            }
        }

        private void ApplyCrowdSeparation()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count <= 1) return;

            Vector2 playerPosition = playerPositionField != null
                ? (Vector2)playerPositionField.GetValue(arena)
                : Vector2.zero;

            int count = enemies.Count;
            var objects = new object[count];
            var positions = new Vector2[count];
            var isBoss = new bool[count];

            for (int i = 0; i < count; i++)
            {
                objects[i] = enemies[i];
                TryGetEnemyPosition(objects[i], out positions[i]);
                isBoss[i] = TryGetEnemyBoss(objects[i]);
            }

            for (int i = 0; i < count; i++)
            {
                if (objects[i] == null) continue;
                Vector2 push = Vector2.zero;
                float radiusI = isBoss[i] ? BossSeparationRadius : EnemySeparationRadius;

                for (int j = 0; j < count; j++)
                {
                    if (i == j || objects[j] == null) continue;
                    float radiusJ = isBoss[j] ? BossSeparationRadius : EnemySeparationRadius;
                    float desired = (radiusI + radiusJ) * 0.5f;
                    Vector2 delta = positions[i] - positions[j];
                    float distance = delta.magnitude;
                    if (distance >= desired) continue;

                    Vector2 direction = distance > 0.01f
                        ? delta / distance
                        : UnityEngine.Random.insideUnitCircle.normalized;
                    push += direction * (desired - distance) / desired;
                }

                Vector2 fromPlayer = positions[i] - playerPosition;
                float playerDistance = fromPlayer.magnitude;
                if (playerDistance < PlayerPersonalSpace && playerDistance > 0.01f)
                    push += fromPlayer.normalized * (PlayerPersonalSpace - playerDistance) / PlayerPersonalSpace;

                if (push.sqrMagnitude <= 0.0001f) continue;
                Vector2 next = positions[i] + Vector2.ClampMagnitude(push, 1.5f) * SeparationStrength * Time.deltaTime;
                SetEnemyPosition(objects[i], ClampToArena(next));
            }
        }

        private static object FindClosestEnemy(
            IList enemies,
            Vector2 origin,
            out Vector2 closestPosition,
            out float closestDistance)
        {
            object closest = null;
            closestPosition = Vector2.zero;
            closestDistance = float.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null || !TryGetEnemyPosition(enemy, out Vector2 position)) continue;
                float distance = Vector2.Distance(origin, position);
                if (distance >= closestDistance) continue;

                closest = enemy;
                closestPosition = position;
                closestDistance = distance;
            }
            return closest;
        }

        private void HandleBossBattleStarted()
        {
            fullRestoreMethod?.Invoke(arena, null);

            float[] cooldowns = skillCooldownsField?.GetValue(arena) as float[];
            if (cooldowns != null)
                Array.Clear(cooldowns, 0, cooldowns.Length);

            playerAttackCooldownField?.SetValue(arena, 0f);
            charging = false;
            chargeTarget = null;
            chargeHitTargets.Clear();
            trailPoints.Clear();
            chargeTimer = 0f;
            passiveMessage = "보스 입장 · HP/MP/기본공격/8스킬/돌진 쿨타임 전체 초기화";
        }

        private void OnGUI()
        {
            if (arena == null || stageFlow == null) return;

            Rect info = new Rect(15f, Screen.height - 86f, 390f, 71f);
            GUI.Box(info, GUIContent.none);
            GUI.Label(new Rect(info.x + 9f, info.y + 7f, 370f, 22f), passiveMessage);
            GUI.Label(new Rect(info.x + 9f, info.y + 30f, 370f, 22f),
                $"사냥 몬스터 {CurrentEnemyCount()}/{HuntingMonsterCap} · 돌진 {(charging ? "이동 중" : Mathf.Max(0f, chargeTimer).ToString("0.00") + "초")}");
            GUI.Label(new Rect(info.x + 9f, info.y + 49f, 370f, 18f),
                "군중 간격 유지 · 돌진 경로 범위 타격 · 보스 입장 전체 회복");

            if (trailVisibleTimer > 0f && trailPoints.Count > 1)
            {
                for (int i = 1; i < trailPoints.Count; i++)
                {
                    Vector2 a = trailPoints[i - 1];
                    Vector2 b = trailPoints[i];
                    DrawTrailSegment(a, b, i);
                }
            }
        }

        private static void DrawTrailSegment(Vector2 a, Vector2 b, int index)
        {
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 1f) return;

            Vector2 midpoint = (a + b) * 0.5f;
            float width = Mathf.Max(12f, Mathf.Abs(delta.x) + 10f);
            float height = Mathf.Max(10f, Mathf.Abs(delta.y) + 8f);
            Rect rect = new Rect(midpoint.x - width * 0.5f, midpoint.y - height * 0.5f, width, height);
            GUIContent content = index == 1 ? new GUIContent("돌진") : GUIContent.none;
            GUI.Box(rect, content);
        }

        private int CurrentEnemyCount()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            return enemies?.Count ?? 0;
        }

        private static bool ContainsReference(IList list, object target)
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], target)) return true;
            return false;
        }

        private static bool TryGetEnemyPosition(object enemy, out Vector2 position)
        {
            position = Vector2.zero;
            if (enemy == null) return false;
            FieldInfo field = enemy.GetType().GetField("Position", PublicInstance);
            if (field == null) return false;
            position = (Vector2)field.GetValue(enemy);
            return true;
        }

        private static void SetEnemyPosition(object enemy, Vector2 position)
        {
            if (enemy == null) return;
            FieldInfo field = enemy.GetType().GetField("Position", PublicInstance);
            field?.SetValue(enemy, position);
        }

        private static bool TryGetEnemyBoss(object enemy)
        {
            if (enemy == null) return false;
            FieldInfo field = enemy.GetType().GetField("IsBoss", PublicInstance);
            return field != null && (bool)field.GetValue(enemy);
        }

        private static Vector2 ClampToArena(Vector2 point)
        {
            point.x = Mathf.Clamp(point.x, 55f, 705f);
            point.y = Mathf.Clamp(point.y, 155f, 425f);
            return point;
        }
    }
}
