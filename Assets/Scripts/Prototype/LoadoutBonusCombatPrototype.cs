using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(16000)]
    public sealed class LoadoutBonusCombatPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private SwordProgressionPrototype swords;
        private ElementEquipmentCatalogPrototype equipmentCatalog;

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
        private string lastMessage = "장비 전투 모드 연결 준비";

        public string LastMessage => lastMessage;
        public bool UsesHuntingMultiTargetPatterns => true;
        public bool UsesBossSingleTargetPatterns => true;
        public bool ExecutionKillsBossOnExtremeChance => true;

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
            equipmentCatalog = FindFirstObjectByType<ElementEquipmentCatalogPrototype>();
            if (arena == null || stageFlow == null || equipmentCatalog == null)
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
            if (arena == null || stageFlow == null || equipmentCatalog == null) return;
            DetectBasicAttack();
            DetectAutomaticSkills();
        }

        private void DetectBasicAttack()
        {
            float current = ReadAttackCooldown();
            bool attackStarted = current > previousAttackCooldown + 0.18f;
            previousAttackCooldown = current;
            if (!attackStarted) return;

            if (stageFlow.Phase == StageFlowPhase.BossBattle)
                ApplyBossPattern(1f, "기본 공격");
            else
                ApplyHuntingPattern(1f, "기본 공격");
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
                if (!castStarted) continue;

                int local = i % 4;
                float skillScale = 1.10f + local * 0.22f;
                if (stageFlow.Phase == StageFlowPhase.BossBattle)
                    ApplyBossPattern(skillScale, $"보스 스킬 {local + 1}");
                else
                    ApplyHuntingPattern(skillScale, $"사냥 스킬 {local + 1}");
            }
        }

        private void ApplyHuntingPattern(float triggerScale, string source)
        {
            IList enemies = GetEnemies();
            if (enemies == null || enemies.Count == 0) return;

            int itemId = equipmentCatalog.GetEquippedItem(false);
            ElementEquipmentCatalogPrototype.HuntingModeProfile profile =
                equipmentCatalog.GetHuntingModeProfile(itemId);
            Vector2 playerPosition = GetPlayerPosition();
            object primary = FindClosestEnemy(enemies, playerPosition);
            if (primary == null || !TryGetEnemyPosition(primary, out Vector2 primaryPosition)) return;

            Vector2 center = profile.Style == ElementEquipmentCatalogPrototype.HuntingAttackStyle.Cleave
                ? playerPosition
                : primaryPosition;
            List<object> targets = GetNearestEnemies(
                enemies, center, profile.MaxTargets, profile.Radius, bossOnly: false);
            if (targets.Count == 0) return;

            float legacyRatio = GetLegacyBonusRatio(false);
            float baseDamage = ReadAttackDamage() * (profile.DamageRatio + legacyRatio) * triggerScale;
            int hitTargets = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                object target = targets[i];
                if (!ContainsReference(GetEnemies(), target)) continue;
                float damage = baseDamage;
                if (profile.Style == ElementEquipmentCatalogPrototype.HuntingAttackStyle.Chain)
                    damage *= Mathf.Pow(profile.ChainFalloff, i);
                DealBonusDamage(target, damage);
                hitTargets++;
            }

            lastMessage = $"{source} · {profile.StyleName} · {hitTargets}마리 범위 공격";
        }

        private void ApplyBossPattern(float triggerScale, string source)
        {
            IList enemies = GetEnemies();
            if (enemies == null || enemies.Count == 0) return;

            object boss = FindBoss(enemies) ?? FindClosestEnemy(enemies, GetPlayerPosition());
            if (boss == null) return;

            int itemId = equipmentCatalog.GetEquippedItem(true);
            ElementEquipmentCatalogPrototype.BossModeProfile profile =
                equipmentCatalog.GetBossModeProfile(itemId);

            bool executionTriggered = profile.Style == ElementEquipmentCatalogPrototype.BossAttackStyle.Execution &&
                                      profile.ExecuteChance > 0f &&
                                      UnityEngine.Random.value < profile.ExecuteChance;
            if (executionTriggered)
            {
                float remainingHp = GetEnemyCurrentHp(boss);
                DealBonusDamage(boss, Mathf.Max(1f, remainingHp + 1f));
                lastMessage = $"{source} · 극저확률 처형 발동 · 보스 즉사";
                return;
            }

            float legacyRatio = GetLegacyBonusRatio(true);
            float totalDamage = ReadAttackDamage() * (profile.TotalDamageRatio + legacyRatio) * triggerScale;
            int requestedHits = Mathf.Max(1, profile.HitCount);
            float damagePerHit = totalDamage / requestedHits;
            int actualHits = 0;
            for (int hit = 0; hit < requestedHits; hit++)
            {
                if (!ContainsReference(GetEnemies(), boss)) break;
                DealBonusDamage(boss, damagePerHit);
                actualHits++;
            }

            string chanceText = profile.Style == ElementEquipmentCatalogPrototype.BossAttackStyle.Execution
                ? $" · 처형 미발동 ({profile.ExecuteChance * 100f:0.####}%)"
                : string.Empty;
            lastMessage = $"{source} · {profile.StyleName} · 보스 1명 {actualHits}타 집중{chanceText}";
        }

        private float GetLegacyBonusRatio(bool boss)
        {
            if (swords == null) return 0f;
            int element = GetActiveElementIndex(boss);
            return Mathf.Max(0f, swords.GetDamageMultiplier(element) - 1f) * 0.35f;
        }

        private int GetActiveElementIndex(bool boss)
        {
            FieldInfo field = boss ? bossElementField : huntingElementField;
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

        private static object FindBoss(IList enemies)
        {
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo bossField = enemy.GetType().GetField("IsBoss", PublicInstance);
                if (bossField != null && Convert.ToBoolean(bossField.GetValue(enemy))) return enemy;
            }
            return null;
        }

        private static List<object> GetNearestEnemies(
            IList enemies, Vector2 origin, int maximum, float radius, bool bossOnly)
        {
            var candidates = new List<(object enemy, float distance)>();
            float radiusSquared = radius >= float.MaxValue ? float.MaxValue : radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null || !TryGetEnemyPosition(enemy, out Vector2 position)) continue;
                FieldInfo bossField = enemy.GetType().GetField("IsBoss", PublicInstance);
                bool isBoss = bossField != null && Convert.ToBoolean(bossField.GetValue(enemy));
                if (bossOnly != isBoss) continue;
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

        private static float GetEnemyCurrentHp(object enemy)
        {
            if (enemy == null) return 1f;
            FieldInfo hpField = enemy.GetType().GetField("Hp", PublicInstance);
            return hpField == null ? 1f : Mathf.Max(0f, Convert.ToSingle(hpField.GetValue(enemy)));
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
