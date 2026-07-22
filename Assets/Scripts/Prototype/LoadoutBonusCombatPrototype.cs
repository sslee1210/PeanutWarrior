using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Applies the bonus portion of the equipped sword's rarity, level and collection
    /// progress to basic attacks and automatic skills without replacing the authoritative
    /// combat loop in CombatPrototypeArena.
    /// </summary>
    [DefaultExecutionOrder(16000)]
    public sealed class LoadoutBonusCombatPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private SwordProgressionPrototype swords;

        private FieldInfo enemiesField;
        private FieldInfo playerPositionField;
        private FieldInfo attackCooldownField;
        private FieldInfo skillCooldownsField;
        private FieldInfo huntingElementField;
        private FieldInfo bossElementField;
        private PropertyInfo attackDamageProperty;
        private MethodInfo dealDamageMethod;

        private float previousAttackCooldown;
        private readonly float[] previousSkillCooldowns = new float[8];
        private string lastMessage = "검 장비 전투 연결 준비";

        public string LastMessage => lastMessage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<LoadoutBonusCombatPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorLoadoutBonusCombatPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<LoadoutBonusCombatPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            swords = FindFirstObjectByType<SwordProgressionPrototype>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type type = typeof(CombatPrototypeArena);
            enemiesField = type.GetField("enemies", PrivateInstance);
            playerPositionField = type.GetField("playerPosition", PrivateInstance);
            attackCooldownField = type.GetField("playerAttackCooldown", PrivateInstance);
            skillCooldownsField = type.GetField("skillCooldowns", PrivateInstance);
            huntingElementField = type.GetField("huntingElement", PrivateInstance);
            bossElementField = type.GetField("bossElement", PrivateInstance);
            attackDamageProperty = type.GetProperty("PlayerAttackDamage", PrivateInstance);
            dealDamageMethod = type.GetMethod("DealDamage", PrivateInstance);

            previousAttackCooldown = ReadAttackCooldown();
            float[] cooldowns = ReadSkillCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousSkillCooldowns, Mathf.Min(cooldowns.Length, previousSkillCooldowns.Length));
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;
            DetectBasicAttack();
            DetectAutomaticSkills();
        }

        private void DetectBasicAttack()
        {
            float current = ReadAttackCooldown();
            bool attackStarted = current > previousAttackCooldown + 0.18f;
            previousAttackCooldown = current;
            if (!attackStarted) return;

            float bonusRatio = CurrentBonusRatio();
            if (bonusRatio <= 0.0001f) return;

            object target = FindClosestEnemy(GetEnemies(), GetPlayerPosition());
            if (target == null) return;

            float bonusDamage = ReadAttackDamage() * bonusRatio;
            DealBonusDamage(target, bonusDamage);
            lastMessage = $"검 기본 공격 보너스 +{Mathf.CeilToInt(bonusDamage)}";
        }

        private void DetectAutomaticSkills()
        {
            float[] cooldowns = ReadSkillCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;

            int activeStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int i = 0; i < cooldowns.Length && i < previousSkillCooldowns.Length; i++)
            {
                bool castStarted = i >= activeStart && i < activeStart + 4 &&
                                   cooldowns[i] > previousSkillCooldowns[i] + 1f;
                previousSkillCooldowns[i] = cooldowns[i];
                if (castStarted) ApplySkillBonus(i);
            }
        }

        private void ApplySkillBonus(int skillIndex)
        {
            float bonusRatio = CurrentBonusRatio();
            if (bonusRatio <= 0.0001f) return;

            IList enemies = GetEnemies();
            if (enemies == null || enemies.Count == 0) return;

            int localIndex = skillIndex % 4;
            int maximumTargets = localIndex switch
            {
                0 => 5,
                1 => 3,
                2 => 4,
                _ => 12
            };
            float radius = localIndex switch
            {
                0 => 150f,
                1 => 260f,
                2 => 220f,
                _ => float.MaxValue
            };

            Vector2 origin = GetPlayerPosition();
            List<object> targets = GetNearestEnemies(enemies, origin, maximumTargets, radius);
            if (targets.Count == 0) return;

            float skillFactor = 1.15f + localIndex * 0.30f;
            float totalBonusDamage = ReadAttackDamage() * skillFactor * bonusRatio;
            float damagePerTarget = totalBonusDamage / Mathf.Max(1, targets.Count);
            int hitCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!ContainsReference(GetEnemies(), targets[i])) continue;
                DealBonusDamage(targets[i], damagePerTarget);
                hitCount++;
            }

            lastMessage = $"검술 장비 보너스 · {hitCount}대상 +{Mathf.CeilToInt(totalBonusDamage)}";
        }

        private float CurrentBonusRatio()
        {
            int element = GetActiveElementIndex();
            float swordMultiplier = swords != null ? swords.GetDamageMultiplier(element) : 1f;
            return Mathf.Max(0f, swordMultiplier - 1f);
        }

        private int GetActiveElementIndex()
        {
            FieldInfo field = stageFlow.Phase == StageFlowPhase.BossBattle ? bossElementField : huntingElementField;
            if (field == null) return 0;
            return Mathf.Clamp(Convert.ToInt32(field.GetValue(arena)), 0, 3);
        }

        private float ReadAttackCooldown()
        {
            return attackCooldownField == null ? 0f : Convert.ToSingle(attackCooldownField.GetValue(arena));
        }

        private float[] ReadSkillCooldowns()
        {
            return skillCooldownsField?.GetValue(arena) as float[];
        }

        private float ReadAttackDamage()
        {
            return attackDamageProperty == null ? 18f : Convert.ToSingle(attackDamageProperty.GetValue(arena));
        }

        private Vector2 GetPlayerPosition()
        {
            return playerPositionField == null ? Vector2.zero : (Vector2)playerPositionField.GetValue(arena);
        }

        private IList GetEnemies()
        {
            return enemiesField?.GetValue(arena) as IList;
        }

        private void DealBonusDamage(object target, float damage)
        {
            if (target == null || damage <= 0f || dealDamageMethod == null) return;
            dealDamageMethod.Invoke(arena, new[] { target, (object)damage, false });
        }

        private static object FindClosestEnemy(IList enemies, Vector2 origin)
        {
            if (enemies == null || enemies.Count == 0) return null;
            object closest = null;
            float best = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (!TryGetEnemyPosition(enemy, out Vector2 position)) continue;
                float distance = Vector2.SqrMagnitude(position - origin);
                if (distance >= best) continue;
                best = distance;
                closest = enemy;
            }
            return closest;
        }

        private static List<object> GetNearestEnemies(IList enemies, Vector2 origin, int maximum, float radius)
        {
            var candidates = new List<(object enemy, float distance)>();
            float radiusSquared = radius == float.MaxValue ? float.MaxValue : radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (!TryGetEnemyPosition(enemy, out Vector2 position)) continue;
                float distance = Vector2.SqrMagnitude(position - origin);
                if (distance > radiusSquared) continue;
                candidates.Add((enemy, distance));
            }

            candidates.Sort((left, right) => left.distance.CompareTo(right.distance));
            var result = new List<object>(Mathf.Min(maximum, candidates.Count));
            for (int i = 0; i < candidates.Count && i < maximum; i++) result.Add(candidates[i].enemy);
            return result;
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

        private static bool ContainsReference(IList list, object target)
        {
            if (list == null || target == null) return false;
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], target)) return true;
            return false;
        }
    }
}
