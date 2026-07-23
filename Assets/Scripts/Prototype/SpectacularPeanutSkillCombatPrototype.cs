using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(15800)]
    public sealed class SpectacularPeanutSkillCombatPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private SkillManagementPrototype skills;
        private FieldInfo enemiesField;
        private FieldInfo cooldownsField;
        private FieldInfo playerMpField;
        private FieldInfo playerPositionField;
        private FieldInfo playerAttackCooldownField;
        private FieldInfo combatMessageField;
        private FieldInfo advancementTierField;
        private PropertyInfo attackDamageProperty;
        private PropertyInfo advancementMultiplierProperty;
        private MethodInfo dealDamageMethod;
        private readonly float[] previousCooldowns = new float[8];
        private float armorReleaseTimer;

        public bool UsesEightDistinctSkillExecutions => true;
        public bool CorrectsLegacySkillCostsAndCooldowns => true;
        public bool UsesHuntingAreaAndBossFocusRoles => true;
        public string LastTriggeredSkill { get; private set; } = "스킬 전투 대기";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SpectacularPeanutSkillCombatPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorSpectacularSkillCombat");
            DontDestroyOnLoad(go);
            go.AddComponent<SpectacularPeanutSkillCombatPrototype>();
        }

        private IEnumerator Start()
        {
            for (int frame = 0; frame < 24; frame++)
            {
                arena = FindFirstObjectByType<CombatPrototypeArena>();
                stageFlow = FindFirstObjectByType<StageFlowController>();
                skills = FindFirstObjectByType<SkillManagementPrototype>();
                if (arena != null && stageFlow != null && skills != null) break;
                yield return null;
            }

            if (arena == null || stageFlow == null || skills == null)
            {
                enabled = false;
                yield break;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            playerPositionField = arenaType.GetField("playerPosition", PrivateInstance);
            playerAttackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            combatMessageField = arenaType.GetField("combatMessage", PrivateInstance);
            advancementTierField = arenaType.GetField("advancementTier", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            advancementMultiplierProperty = arenaType.GetProperty("SkillAdvancementMultiplier", PrivateInstance);
            dealDamageMethod = arenaType.GetMethod("DealDamage", PrivateInstance);

            float[] cooldowns = ReadCooldowns();
            if (cooldowns != null)
                Array.Copy(cooldowns, previousCooldowns, Mathf.Min(cooldowns.Length, previousCooldowns.Length));
        }

        private void Update()
        {
            DetectSkillCasts();
            UpdateArmorRelease();
        }

        private void DetectSkillCasts()
        {
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;
            int activeStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;

            for (int i = 0; i < cooldowns.Length && i < previousCooldowns.Length; i++)
            {
                bool cast = i >= activeStart && i < activeStart + 4 &&
                            cooldowns[i] > previousCooldowns[i] + 1f;
                previousCooldowns[i] = cooldowns[i];
                if (!cast) continue;

                CorrectResourceAndCooldown(i, cooldowns);
                ExecuteSkill(i);
                previousCooldowns[i] = cooldowns[i];
            }
        }

        private void CorrectResourceAndCooldown(int index, float[] cooldowns)
        {
            int local = index % 4;
            float legacyCost = 20f + local * 5f;
            float desiredCost = skills.GetSkillMpCost(index);
            if (playerMpField != null)
            {
                float current = Convert.ToSingle(playerMpField.GetValue(arena));
                playerMpField.SetValue(arena, Mathf.Max(0f, current - (desiredCost - legacyCost)));
            }
            cooldowns[index] = skills.GetSkillBaseCooldown(index);
        }

        private void ExecuteSkill(int index)
        {
            IList enemies = GetEnemies();
            if (enemies == null || enemies.Count == 0) return;

            float supplementalDamage = Mathf.Max(0f, skills.GetSkillTotalDamage(index) - LegacyDirectDamage(index));
            LastTriggeredSkill = skills.GetSkillName(index);
            SetCombatMessage($"{LastTriggeredSkill} · {skills.GetSkillRole(index)}");

            switch (index)
            {
                case 0:
                    ExecuteShellCyclone(enemies, supplementalDamage);
                    break;
                case 1:
                    StartCoroutine(ExecuteFallingFlowerSwordRain(supplementalDamage));
                    break;
                case 2:
                    ExecuteLeylinePodFormation(enemies, supplementalDamage);
                    break;
                case 3:
                    StartCoroutine(ExecuteRoyalPodArmory(supplementalDamage));
                    break;
                case 4:
                    ExecuteCarapaceRelease(enemies, supplementalDamage);
                    break;
                case 5:
                    ExecutePeanutChainSword(enemies, supplementalDamage);
                    break;
                case 6:
                    StartCoroutine(ExecuteFallenFlowerRoot(enemies, supplementalDamage));
                    break;
                case 7:
                    ExecuteGoldenCoreHeavenSever(enemies, supplementalDamage);
                    break;
            }
        }

        private void ExecuteShellCyclone(IList enemies, float damage)
        {
            List<object> targets = GetNormalEnemiesSortedByDistance(enemies, ReadPlayerPosition(), 6);
            if (targets.Count == 0) return;
            float perTarget = damage / Mathf.Max(1, targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                object target = targets[i];
                if (TryReadPosition(target, out Vector2 position) && playerPositionField != null)
                    playerPositionField.SetValue(arena, ClampArenaPosition(position + UnityEngine.Random.insideUnitCircle * 34f));
                Deal(target, perTarget, true);
            }
        }

        private IEnumerator ExecuteFallingFlowerSwordRain(float damage)
        {
            const int waves = 4;
            for (int wave = 0; wave < waves; wave++)
            {
                IList enemies = GetEnemies();
                if (enemies == null || enemies.Count == 0) yield break;
                List<object> targets = CopyTargets(enemies, bossOnly: false);
                float perHit = damage / Mathf.Max(1, waves * targets.Count);
                for (int i = 0; i < targets.Count; i++) Deal(targets[i], perHit, true);
                yield return new WaitForSeconds(0.12f);
            }
        }

        private void ExecuteLeylinePodFormation(IList enemies, float damage)
        {
            List<object> targets = CopyTargets(enemies, bossOnly: false);
            if (targets.Count == 0) return;
            Vector2 center = AveragePosition(targets);
            float perTarget = damage / Mathf.Max(1, targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                object target = targets[i];
                WritePosition(target, Vector2.Lerp(ReadPosition(target), center, 0.72f));
                WriteFloat(target, "FrostTimer", 2.4f);
                Deal(target, perTarget, true);
            }
        }

        private IEnumerator ExecuteRoyalPodArmory(float damage)
        {
            const int waves = 5;
            for (int wave = 0; wave < waves; wave++)
            {
                IList enemies = GetEnemies();
                if (enemies == null || enemies.Count == 0) yield break;
                List<object> targets = CopyTargets(enemies, bossOnly: false);
                float waveScale = wave == waves - 1 ? 0.36f : 0.16f;
                float perTarget = damage * waveScale / Mathf.Max(1, targets.Count);
                for (int i = 0; i < targets.Count; i++) Deal(targets[i], perTarget, true);
                yield return new WaitForSeconds(wave == waves - 1 ? 0.04f : 0.13f);
            }
        }

        private void ExecuteCarapaceRelease(IList enemies, float damage)
        {
            object boss = FindBoss(enemies);
            if (boss == null) return;
            armorReleaseTimer = 6f;
            float perBlade = damage / 6f;
            for (int i = 0; i < 6; i++) Deal(boss, perBlade, true);
        }

        private void ExecutePeanutChainSword(IList enemies, float damage)
        {
            object boss = FindBoss(enemies);
            if (boss == null) return;
            int hits = Mathf.Max(1, skills.GetSkillHitCount(5));
            float perHit = damage / hits;
            for (int i = 0; i < hits; i++)
            {
                if (!ContainsReference(GetEnemies(), boss)) break;
                Deal(boss, perHit, true);
            }
        }

        private IEnumerator ExecuteFallenFlowerRoot(IList enemies, float damage)
        {
            object boss = FindBoss(enemies);
            if (boss == null) yield break;
            float startHp = ReadFloat(boss, "Hp", 0f);
            Deal(boss, damage * 0.45f, true);
            yield return new WaitForSeconds(3.2f);
            if (!ContainsReference(GetEnemies(), boss)) yield break;
            float currentHp = ReadFloat(boss, "Hp", 0f);
            float storedDamage = Mathf.Max(0f, startHp - currentHp) * 0.32f;
            Deal(boss, damage * 0.55f + storedDamage, true);
        }

        private void ExecuteGoldenCoreHeavenSever(IList enemies, float damage)
        {
            object boss = FindBoss(enemies);
            if (boss == null) return;
            Deal(boss, damage, true);
        }

        private void UpdateArmorRelease()
        {
            if (armorReleaseTimer <= 0f) return;
            armorReleaseTimer -= Time.deltaTime;
            if (playerAttackCooldownField == null) return;
            float cooldown = Convert.ToSingle(playerAttackCooldownField.GetValue(arena));
            if (cooldown > 0f)
                playerAttackCooldownField.SetValue(arena, cooldown - Time.deltaTime * 0.55f);
        }

        private float LegacyDirectDamage(int index)
        {
            int local = index % 4;
            int level = skills.GetSkillLevel(index);
            float advancement = advancementMultiplierProperty == null
                ? 1f
                : Convert.ToSingle(advancementMultiplierProperty.GetValue(arena));
            float multiplier = (1.4f + local * 0.35f) * (1f + (level - 1) * 0.15f) * advancement;
            return ReadAttackDamage() * multiplier;
        }

        private float ReadAttackDamage()
        {
            return attackDamageProperty == null ? 18f : Convert.ToSingle(attackDamageProperty.GetValue(arena));
        }

        private IList GetEnemies()
        {
            return enemiesField?.GetValue(arena) as IList;
        }

        private float[] ReadCooldowns()
        {
            return cooldownsField?.GetValue(arena) as float[];
        }

        private Vector2 ReadPlayerPosition()
        {
            return playerPositionField == null ? Vector2.zero : (Vector2)playerPositionField.GetValue(arena);
        }

        private void Deal(object target, float damage, bool applyElement)
        {
            if (target == null || damage <= 0f || dealDamageMethod == null) return;
            dealDamageMethod.Invoke(arena, new[] { target, (object)damage, applyElement });
        }

        private void SetCombatMessage(string message)
        {
            combatMessageField?.SetValue(arena, message);
        }

        private static object FindBoss(IList enemies)
        {
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo field = enemy.GetType().GetField("IsBoss", PublicInstance);
                if (field != null && Convert.ToBoolean(field.GetValue(enemy))) return enemy;
            }
            return null;
        }

        private static List<object> CopyTargets(IList enemies, bool bossOnly)
        {
            var result = new List<object>();
            if (enemies == null) return result;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                bool isBoss = Convert.ToBoolean(enemy.GetType().GetField("IsBoss", PublicInstance)?.GetValue(enemy) ?? false);
                if (isBoss == bossOnly) result.Add(enemy);
            }
            return result;
        }

        private static List<object> GetNormalEnemiesSortedByDistance(IList enemies, Vector2 origin, int maximum)
        {
            List<object> targets = CopyTargets(enemies, bossOnly: false);
            targets.Sort((left, right) =>
                Vector2.SqrMagnitude(ReadPosition(left) - origin).CompareTo(Vector2.SqrMagnitude(ReadPosition(right) - origin)));
            if (targets.Count > maximum) targets.RemoveRange(maximum, targets.Count - maximum);
            return targets;
        }

        private static Vector2 AveragePosition(List<object> targets)
        {
            if (targets == null || targets.Count == 0) return Vector2.zero;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < targets.Count; i++) sum += ReadPosition(targets[i]);
            return sum / targets.Count;
        }

        private static bool TryReadPosition(object enemy, out Vector2 position)
        {
            position = ReadPosition(enemy);
            return enemy != null;
        }

        private static Vector2 ReadPosition(object enemy)
        {
            if (enemy == null) return Vector2.zero;
            FieldInfo field = enemy.GetType().GetField("Position", PublicInstance);
            return field == null ? Vector2.zero : (Vector2)field.GetValue(enemy);
        }

        private static void WritePosition(object enemy, Vector2 position)
        {
            enemy?.GetType().GetField("Position", PublicInstance)?.SetValue(enemy, ClampArenaPosition(position));
        }

        private static void WriteFloat(object enemy, string fieldName, float value)
        {
            enemy?.GetType().GetField(fieldName, PublicInstance)?.SetValue(enemy, value);
        }

        private static float ReadFloat(object enemy, string fieldName, float fallback)
        {
            FieldInfo field = enemy?.GetType().GetField(fieldName, PublicInstance);
            return field == null ? fallback : Convert.ToSingle(field.GetValue(enemy));
        }

        private static bool ContainsReference(IList list, object target)
        {
            if (list == null || target == null) return false;
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], target)) return true;
            return false;
        }

        private static Vector2 ClampArenaPosition(Vector2 position)
        {
            position.x = Mathf.Clamp(position.x, 55f, 705f);
            position.y = Mathf.Clamp(position.y, 155f, 425f);
            return position;
        }
    }
}
