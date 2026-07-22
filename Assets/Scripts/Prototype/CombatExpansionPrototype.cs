using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Expands the zero-setup combat prototype with a basic charge passive,
    /// a complete boss-entry reset, and denser idle-hunting monster waves.
    /// </summary>
    public sealed class CombatExpansionPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const int HuntingMonsterCap = 18;
        private const float ReinforcementInterval = 0.18f;
        private const float ChargeCooldown = 1.65f;
        private const float ChargeMinimumDistance = 92f;
        private const float ChargeDetectionRange = 285f;
        private const float ChargeStopDistance = 54f;
        private const float ChargeDamageRatio = 0.55f;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private FieldInfo playerAttackDamagePropertyBackingField;
        private PropertyInfo playerAttackDamageProperty;
        private FieldInfo skillCooldownsField;
        private MethodInfo spawnNormalEnemyMethod;
        private MethodInfo dealDamageMethod;
        private MethodInfo fullRestoreMethod;

        private float reinforcementTimer;
        private float chargeTimer;
        private float chargeFlashTimer;
        private Vector2 chargeFrom;
        private Vector2 chargeTo;
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
            spawnNormalEnemyMethod = arenaType.GetMethod("SpawnNormalEnemy", PrivateInstance);
            dealDamageMethod = arenaType.GetMethod("DealDamage", PrivateInstance);
            fullRestoreMethod = arenaType.GetMethod("FullRestore", PrivateInstance);

            stageFlow.BossBattleStarted += HandleBossBattleStarted;
            chargeTimer = 0.4f;
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
            chargeFlashTimer -= Time.deltaTime;

            if (stageFlow.Phase != StageFlowPhase.BossBattle)
                UpdateDenseMonsterWave();

            UpdateChargePassive();
        }

        private void UpdateDenseMonsterWave()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count >= HuntingMonsterCap) return;

            reinforcementTimer -= Time.deltaTime;
            if (reinforcementTimer > 0f) return;

            int missing = HuntingMonsterCap - enemies.Count;
            int spawnBatch = Mathf.Min(3, missing);
            for (int i = 0; i < spawnBatch; i++)
                spawnNormalEnemyMethod?.Invoke(arena, null);

            reinforcementTimer = ReinforcementInterval;
        }

        private void UpdateChargePassive()
        {
            if (chargeTimer > 0f) return;

            IList enemies = enemiesField?.GetValue(arena) as IList;
            if (enemies == null || enemies.Count == 0 || playerPositionField == null) return;

            Vector2 playerPosition = (Vector2)playerPositionField.GetValue(arena);
            object target = FindClosestEnemy(enemies, playerPosition, out Vector2 targetPosition, out float distance);
            if (target == null || distance < ChargeMinimumDistance || distance > ChargeDetectionRange) return;

            Vector2 direction = (targetPosition - playerPosition).normalized;
            Vector2 destination = targetPosition - direction * ChargeStopDistance;
            destination.x = Mathf.Clamp(destination.x, 55f, 705f);
            destination.y = Mathf.Clamp(destination.y, 155f, 425f);

            chargeFrom = playerPosition;
            chargeTo = destination;
            playerPositionField.SetValue(arena, destination);

            float attackDamage = playerAttackDamageProperty != null
                ? Convert.ToSingle(playerAttackDamageProperty.GetValue(arena))
                : 18f;
            dealDamageMethod?.Invoke(arena, new[] { target, (object)(attackDamage * ChargeDamageRatio), true });

            chargeTimer = ChargeCooldown;
            chargeFlashTimer = 0.16f;
            passiveMessage = $"기본 패시브: 돌진 베기 · 재사용 {ChargeCooldown:0.0}초";
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
                if (enemy == null) continue;
                FieldInfo positionField = enemy.GetType().GetField("Position", PublicInstance);
                if (positionField == null) continue;

                Vector2 position = (Vector2)positionField.GetValue(enemy);
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
            // Boss entry must always begin from a fully clean combat state.
            fullRestoreMethod?.Invoke(arena, null);

            float[] cooldowns = skillCooldownsField?.GetValue(arena) as float[];
            if (cooldowns != null)
            {
                for (int i = 0; i < cooldowns.Length; i++)
                    cooldowns[i] = 0f;
            }

            chargeTimer = 0f;
            passiveMessage = "보스 입장 · HP/MP 완전 회복 · 모든 스킬 쿨타임 초기화";
        }

        private void OnGUI()
        {
            if (arena == null || stageFlow == null) return;

            Rect info = new Rect(15f, Screen.height - 78f, 330f, 63f);
            GUI.Box(info, GUIContent.none);
            GUI.Label(new Rect(info.x + 9f, info.y + 7f, 310f, 22f), passiveMessage);
            GUI.Label(new Rect(info.x + 9f, info.y + 31f, 310f, 22f),
                $"사냥 몬스터 목표 {HuntingMonsterCap}마리 · 현재 {CurrentEnemyCount()}마리");

            if (chargeFlashTimer > 0f)
            {
                Vector2 delta = chargeTo - chargeFrom;
                float length = delta.magnitude;
                if (length > 1f)
                {
                    float left = Mathf.Min(chargeFrom.x, chargeTo.x);
                    float top = Mathf.Min(chargeFrom.y, chargeTo.y);
                    GUI.Box(new Rect(left, top, Mathf.Max(12f, Mathf.Abs(delta.x)), Mathf.Max(8f, Mathf.Abs(delta.y))),
                        "돌진!");
                }
            }
        }

        private int CurrentEnemyCount()
        {
            IList enemies = enemiesField?.GetValue(arena) as IList;
            return enemies?.Count ?? 0;
        }
    }
}
