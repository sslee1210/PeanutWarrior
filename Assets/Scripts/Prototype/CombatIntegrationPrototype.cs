using System;
using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Connects prototype systems that were previously UI-only:
    /// per-skill auto-use switches, player critical hits, and cleared-stage navigation.
    /// </summary>
    public sealed class CombatIntegrationPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PublicInstance = BindingFlags.Instance | BindingFlags.Public;
        private const string HighestStageKey = "PeanutWarrior.Progress.HighestGlobalStage";

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private GrowthExpansionPrototype growth;

        private FieldInfo enemiesField;
        private FieldInfo cooldownsField;
        private FieldInfo attackCooldownField;
        private PropertyInfo attackDamageProperty;
        private MethodInfo dealDamageMethod;
        private PropertyInfo critChanceProperty;
        private PropertyInfo critDamageProperty;

        private float previousAttackCooldown;
        private int highestGlobalStage = 1;
        private string message = "전투 시스템 연결 준비";
        private float criticalFlashTimer;
        private bool panelOpen;

        private int CurrentGlobalStage =>
            (stageFlow.World - 1) * StageFlowController.StagesPerWorld + stageFlow.Stage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CombatIntegrationPrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCombatIntegrationPrototype");
            DontDestroyOnLoad(root);
            root.AddComponent<CombatIntegrationPrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            growth = FindFirstObjectByType<GrowthExpansionPrototype>();
            if (arena == null || stageFlow == null)
            {
                enabled = false;
                return;
            }

            Type arenaType = typeof(CombatPrototypeArena);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            attackCooldownField = arenaType.GetField("playerAttackCooldown", PrivateInstance);
            attackDamageProperty = arenaType.GetProperty("PlayerAttackDamage", PrivateInstance);
            dealDamageMethod = arenaType.GetMethod("DealDamage", PrivateInstance);

            if (growth != null)
            {
                Type growthType = typeof(GrowthExpansionPrototype);
                critChanceProperty = growthType.GetProperty("CritChance", PrivateInstance);
                critDamageProperty = growthType.GetProperty("CritDamage", PrivateInstance);
            }

            highestGlobalStage = Mathf.Max(CurrentGlobalStage,
                PlayerPrefs.GetInt(HighestStageKey, CurrentGlobalStage));
            previousAttackCooldown = ReadAttackCooldown();
            stageFlow.BossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.BossDefeated -= HandleBossDefeated;
        }

        private void Update()
        {
            if (arena == null || stageFlow == null) return;
            criticalFlashTimer -= Time.deltaTime;
            DetectBasicAttackCritical();
        }

        private void LateUpdate()
        {
            if (arena == null || stageFlow == null) return;
            ApplySkillAutoSwitches();
        }

        private void ApplySkillAutoSwitches()
        {
            float[] cooldowns = cooldownsField?.GetValue(arena) as float[];
            if (cooldowns == null || cooldowns.Length < 8) return;

            int activeStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int i = activeStart; i < activeStart + 4; i++)
            {
                bool enabledForAuto = PlayerPrefs.GetInt("PeanutWarrior.SkillAuto." + i, 1) == 1;
                if (!enabledForAuto)
                {
                    // Keep the disabled skill just above zero so the arena cannot auto-cast it.
                    cooldowns[i] = Mathf.Max(cooldowns[i], 0.2f);
                }
            }
        }

        private void DetectBasicAttackCritical()
        {
            float current = ReadAttackCooldown();
            bool attackStarted = current > previousAttackCooldown + 0.18f;
            previousAttackCooldown = current;
            if (!attackStarted || growth == null) return;

            float chance = critChanceProperty != null
                ? Convert.ToSingle(critChanceProperty.GetValue(growth))
                : 0.05f;
            if (UnityEngine.Random.value > chance) return;

            IList enemies = enemiesField?.GetValue(arena) as IList;
            object target = FindClosestEnemy(enemies);
            if (target == null) return;

            float multiplier = critDamageProperty != null
                ? Convert.ToSingle(critDamageProperty.GetValue(growth))
                : 1.5f;
            float attackDamage = attackDamageProperty != null
                ? Convert.ToSingle(attackDamageProperty.GetValue(arena))
                : 18f;

            // Base damage was already dealt by the arena. Apply only the critical bonus portion.
            float bonusDamage = attackDamage * Mathf.Max(0f, multiplier - 1f);
            dealDamageMethod?.Invoke(arena, new[] { target, (object)bonusDamage, true });
            criticalFlashTimer = 0.35f;
            message = $"치명타 발동! 추가 피해 {Mathf.RoundToInt(bonusDamage)}";
        }

        private object FindClosestEnemy(IList enemies)
        {
            if (enemies == null || enemies.Count == 0) return null;
            FieldInfo playerPositionField = typeof(CombatPrototypeArena)
                .GetField("playerPosition", PrivateInstance);
            Vector2 playerPosition = playerPositionField != null
                ? (Vector2)playerPositionField.GetValue(arena)
                : Vector2.zero;

            object closest = null;
            float best = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo positionField = enemy.GetType().GetField("Position", PublicInstance);
                if (positionField == null) continue;
                Vector2 position = (Vector2)positionField.GetValue(enemy);
                float distance = Vector2.SqrMagnitude(position - playerPosition);
                if (distance >= best) continue;
                best = distance;
                closest = enemy;
            }
            return closest;
        }

        private float ReadAttackCooldown()
        {
            return attackCooldownField == null
                ? 0f
                : Convert.ToSingle(attackCooldownField.GetValue(arena));
        }

        private void HandleBossDefeated()
        {
            int unlocked = CurrentGlobalStage + 1;
            highestGlobalStage = Mathf.Max(highestGlobalStage, unlocked);
            PlayerPrefs.SetInt(HighestStageKey, highestGlobalStage);
            PlayerPrefs.Save();
            message = $"새 스테이지 해금: {FormatGlobalStage(highestGlobalStage)}";
        }

        private void MoveStage(int direction)
        {
            int target = CurrentGlobalStage + direction;
            if (target < 1)
            {
                message = "1-1보다 이전으로 이동할 수 없음";
                return;
            }
            if (target > highestGlobalStage)
            {
                message = $"미해금 스테이지 · 최고 {FormatGlobalStage(highestGlobalStage)}";
                return;
            }

            int world = (target - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (target - 1) % StageFlowController.StagesPerWorld + 1;
            stageFlow.SelectStage(world, stage);
            message = $"클리어 스테이지 이동: {world}-{stage}";
        }

        private static string FormatGlobalStage(int globalStage)
        {
            int world = (globalStage - 1) / StageFlowController.StagesPerWorld + 1;
            int stage = (globalStage - 1) % StageFlowController.StagesPerWorld + 1;
            return $"{world}-{stage}";
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(285f, 88f, 140f, 38f),
                    panelOpen ? "연결 시스템 닫기" : "전투 연결 상태"))
                panelOpen = !panelOpen;

            if (criticalFlashTimer > 0f)
                GUI.Box(new Rect(Screen.width * 0.5f - 90f, 50f, 180f, 38f), "치명타!");

            if (!panelOpen || arena == null || stageFlow == null) return;

            Rect panel = new Rect(825f, 132f, 340f, 220f);
            GUI.Box(panel, "실전 연결 시스템");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 30f, 315f, 44f), message);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 76f, 315f, 42f),
                $"현재 {stageFlow.World}-{stageFlow.Stage}\n최고 해금 {FormatGlobalStage(highestGlobalStage)}");
            GUI.Label(new Rect(panel.x + 12f, panel.y + 120f, 315f, 38f),
                "스킬 자동 ON/OFF 실제 전투 적용\n치명타 확률·피해 실제 기본 공격 적용");

            if (GUI.Button(new Rect(panel.x + 12f, panel.y + 168f, 145f, 36f), "이전 클리어 스테이지"))
                MoveStage(-1);
            if (GUI.Button(new Rect(panel.x + 181f, panel.y + 168f, 145f, 36f), "다음 해금 스테이지"))
                MoveStage(1);
        }
    }
}
