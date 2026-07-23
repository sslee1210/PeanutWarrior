using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Keeps the three immortal pets visually and tactically separated. During normal
    /// hunting each pet receives a different enemy whenever possible. During boss
    /// combat they surround the boss from three different directions.
    /// </summary>
    [DefaultExecutionOrder(17500)]
    public sealed class PetCombatSpreadPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const float MinPetSpacing = 86f;
        private const float RepositionSpeed = 92f;
        private const float MapLeft = 55f;
        private const float MapRight = 705f;
        private const float MapTop = 155f;
        private const float MapBottom = 425f;

        private static readonly Vector2[] BossOffsets =
        {
            new Vector2(-92f, 46f),
            new Vector2(0f, -82f),
            new Vector2(92f, 46f)
        };

        private static readonly Vector2[] IdleOffsets =
        {
            new Vector2(-118f, 40f),
            new Vector2(0f, -104f),
            new Vector2(118f, 40f)
        };

        private IdleSystemsPrototype idle;
        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private FieldInfo minisField;
        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private readonly List<object> claimedEnemies = new List<object>(3);
        private bool initialized;

        public float MinimumSpacing => MinPetSpacing;
        public bool UsesSeparateTargets => true;
        public bool UsesBossSurroundFormation => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PetCombatSpreadPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorPetCombatSpread");
            DontDestroyOnLoad(root);
            root.AddComponent<PetCombatSpreadPrototype>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                idle = FindFirstObjectByType<IdleSystemsPrototype>();
                arena = FindFirstObjectByType<CombatPrototypeArena>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                if (idle != null && arena != null && stageFlow != null) break;
            }

            if (idle == null || arena == null || stageFlow == null)
            {
                enabled = false;
                yield break;
            }

            minisField = typeof(IdleSystemsPrototype).GetField("minis", PrivateInstance);
            Type arenaType = typeof(CombatPrototypeArena);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            initialized = minisField != null && enemiesField != null;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            IList pets = minisField.GetValue(idle) as IList;
            if (pets == null || pets.Count == 0) return;

            IList enemies = enemiesField.GetValue(arena) as IList;
            Vector2 player = playerPositionField == null
                ? new Vector2(380f, 280f)
                : (Vector2)playerPositionField.GetValue(arena);

            claimedEnemies.Clear();
            for (int i = 0; i < pets.Count; i++)
            {
                object pet = pets[i];
                if (pet == null) continue;
                Type petType = pet.GetType();
                FieldInfo positionField = petType.GetField("Position", PublicInstance);
                FieldInfo targetField = petType.GetField("TargetPosition", PublicInstance);
                if (positionField == null || targetField == null) continue;

                Vector2 current = (Vector2)positionField.GetValue(pet);
                Vector2 desired;
                if (stageFlow.Phase == StageFlowPhase.BossBattle)
                {
                    Vector2 bossPosition = FindBossPosition(enemies, current);
                    desired = bossPosition + BossOffsets[i % BossOffsets.Length];
                }
                else
                {
                    object assigned = FindNearestUnclaimedEnemy(enemies, current, out Vector2 enemyPosition);
                    if (assigned != null)
                    {
                        claimedEnemies.Add(assigned);
                        Vector2 approach = (current - enemyPosition).normalized;
                        if (approach.sqrMagnitude < 0.01f)
                            approach = Quaternion.Euler(0f, 0f, i * 120f) * Vector2.right;
                        desired = enemyPosition + approach * 70f;
                    }
                    else
                    {
                        desired = player + IdleOffsets[i % IdleOffsets.Length];
                    }
                }

                desired = ClampToMap(desired);
                current = Vector2.MoveTowards(current, desired, RepositionSpeed * Time.deltaTime);
                positionField.SetValue(pet, current);
                targetField.SetValue(pet, desired);
            }

            ApplyPairwiseSeparation(pets);
        }

        private object FindNearestUnclaimedEnemy(IList enemies, Vector2 origin, out Vector2 position)
        {
            position = Vector2.zero;
            if (enemies == null || enemies.Count == 0) return null;

            object bestEnemy = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null || claimedEnemies.Contains(enemy)) continue;
                FieldInfo field = enemy.GetType().GetField("Position", PublicInstance);
                if (field == null) continue;
                Vector2 enemyPosition = (Vector2)field.GetValue(enemy);
                float distance = (enemyPosition - origin).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestEnemy = enemy;
                position = enemyPosition;
            }

            if (bestEnemy != null) return bestEnemy;

            // Fewer enemies than pets: reuse an enemy, but the later separation step
            // keeps the pets at different attack angles.
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo field = enemy.GetType().GetField("Position", PublicInstance);
                if (field == null) continue;
                Vector2 enemyPosition = (Vector2)field.GetValue(enemy);
                float distance = (enemyPosition - origin).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestEnemy = enemy;
                position = enemyPosition;
            }
            return bestEnemy;
        }

        private static Vector2 FindBossPosition(IList enemies, Vector2 fallback)
        {
            if (enemies == null || enemies.Count == 0) return fallback;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                Type type = enemy.GetType();
                FieldInfo bossField = type.GetField("IsBoss", PublicInstance);
                FieldInfo positionField = type.GetField("Position", PublicInstance);
                if (positionField == null) continue;
                if (bossField == null || Convert.ToBoolean(bossField.GetValue(enemy)))
                    return (Vector2)positionField.GetValue(enemy);
            }
            return fallback;
        }

        private static void ApplyPairwiseSeparation(IList pets)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < pets.Count; i++)
                {
                    object first = pets[i];
                    if (first == null) continue;
                    FieldInfo firstField = first.GetType().GetField("Position", PublicInstance);
                    if (firstField == null) continue;
                    Vector2 firstPosition = (Vector2)firstField.GetValue(first);

                    for (int j = i + 1; j < pets.Count; j++)
                    {
                        object second = pets[j];
                        if (second == null) continue;
                        FieldInfo secondField = second.GetType().GetField("Position", PublicInstance);
                        if (secondField == null) continue;
                        Vector2 secondPosition = (Vector2)secondField.GetValue(second);
                        Vector2 delta = secondPosition - firstPosition;
                        float distance = delta.magnitude;
                        if (distance >= MinPetSpacing) continue;

                        Vector2 direction = distance > 0.01f
                            ? delta / distance
                            : Quaternion.Euler(0f, 0f, (i + 1) * 120f) * Vector2.right;
                        float correction = (MinPetSpacing - distance) * 0.5f;
                        firstPosition = ClampToMap(firstPosition - direction * correction);
                        secondPosition = ClampToMap(secondPosition + direction * correction);
                        firstField.SetValue(first, firstPosition);
                        secondField.SetValue(second, secondPosition);
                    }
                }
            }
        }

        private static Vector2 ClampToMap(Vector2 point)
        {
            point.x = Mathf.Clamp(point.x, MapLeft, MapRight);
            point.y = Mathf.Clamp(point.y, MapTop, MapBottom);
            return point;
        }
    }
}
