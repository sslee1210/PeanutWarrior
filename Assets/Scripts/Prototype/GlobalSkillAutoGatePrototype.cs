using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Owns automatic skill scheduling. The arena's legacy one-skill priority loop is blocked,
    /// ready skills are queued by the exact moment their cooldown finishes, and every skill that
    /// starts ready overlaps in the opening volley.
    /// </summary>
    [DefaultExecutionOrder(-1200)]
    public sealed class GlobalSkillAutoGatePrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly bool[] waitingForCast = new bool[8];
        private readonly long[] readySequence = new long[8];
        private readonly bool[] readyThisFrame = new bool[8];

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private SkillManagementPrototype skillManager;
        private SpectacularPeanutSkillCombatPrototype spectacularCombat;
        private FieldInfo cooldownsField;
        private FieldInfo playerMpField;
        private FieldInfo enemiesField;
        private MethodInfo legacyAutoMethod;
        private MethodInfo correctResourceMethod;
        private MethodInfo executeSkillMethod;
        private FieldInfo spectacularPreviousCooldownsField;
        private FieldInfo spectacularArenaField;

        private long nextReadySequence;
        private int activeStart = -1;
        private bool openingVolleyUsed;
        private bool openingVolleyPending;

        public bool UsesConfirmedMpCosts => true;
        public bool UsesCooldownCompletionOrder => true;
        public bool AllowsOpeningSkillOverlap => true;
        public bool UsesTacticalAutoPriority => false;
        public bool OpeningVolleyPending => openingVolleyPending;
        public string HuntingAutoPriority => "쿨타임 완료 순서 · 동시 완료 시 동시 발동";
        public string BossAutoPriority => "쿨타임 완료 순서 · 동시 완료 시 동시 발동";
        public int WaitingSkillCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < waitingForCast.Length; i++)
                    if (waitingForCast[i]) count++;
                return count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<GlobalSkillAutoGatePrototype>() != null) return;
            GameObject root = new GameObject("PeanutWarriorGlobalSkillAutoGate");
            DontDestroyOnLoad(root);
            root.AddComponent<GlobalSkillAutoGatePrototype>();
        }

        private void Start()
        {
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            stageFlow = FindFirstObjectByType<StageFlowController>();
            skillManager = FindFirstObjectByType<SkillManagementPrototype>();
            spectacularCombat = FindFirstObjectByType<SpectacularPeanutSkillCombatPrototype>();

            Type arenaType = typeof(CombatPrototypeArena);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            enemiesField = arenaType.GetField("enemies", PrivateInstance);
            legacyAutoMethod = arenaType.GetMethod("TryUseAutomaticSkill", PrivateInstance);

            Type spectacularType = typeof(SpectacularPeanutSkillCombatPrototype);
            correctResourceMethod = spectacularType.GetMethod("CorrectResourceAndCooldown", PrivateInstance);
            executeSkillMethod = spectacularType.GetMethod("ExecuteSkill", PrivateInstance);
            spectacularPreviousCooldownsField = spectacularType.GetField("previousCooldowns", PrivateInstance);
            spectacularArenaField = spectacularType.GetField("arena", PrivateInstance);

            if (arena == null || stageFlow == null || skillManager == null || spectacularCombat == null ||
                cooldownsField == null || playerMpField == null || enemiesField == null || legacyAutoMethod == null ||
                correctResourceMethod == null || executeSkillMethod == null)
                enabled = false;
        }

        private void Update()
        {
            if (!enabled) return;
            float[] cooldowns = ReadCooldowns();
            if (cooldowns == null || cooldowns.Length < 8) return;

            int nextStart = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            if (nextStart != activeStart)
            {
                activeStart = nextStart;
                openingVolleyUsed = false;
                openingVolleyPending = false;
                for (int i = 0; i < waitingForCast.Length; i++)
                {
                    waitingForCast[i] = false;
                    readySequence[i] = 0L;
                    readyThisFrame[i] = false;
                }
            }

            Array.Clear(readyThisFrame, 0, readyThisFrame.Length);

            if (!skillManager.GlobalAutoEnabled)
            {
                openingVolleyPending = false;
                for (int index = activeStart; index < activeStart + 4; index++)
                {
                    waitingForCast[index] = false;
                    readySequence[index] = 0L;
                    cooldowns[index] = Mathf.Max(cooldowns[index], 0.2f);
                }
                return;
            }

            float readyThreshold = Mathf.Max(0.05f, Time.deltaTime + 0.01f);
            for (int index = activeStart; index < activeStart + 4; index++)
            {
                if (!waitingForCast[index] && cooldowns[index] <= readyThreshold)
                {
                    waitingForCast[index] = true;
                    readyThisFrame[index] = true;
                    readySequence[index] = ++nextReadySequence;
                }

                if (waitingForCast[index])
                    cooldowns[index] = Mathf.Max(cooldowns[index], 0.2f);
            }

            if (!openingVolleyUsed && !openingVolleyPending)
            {
                bool allWaiting = true;
                bool allFresh = true;
                for (int index = activeStart; index < activeStart + 4; index++)
                {
                    if (!waitingForCast[index]) allWaiting = false;
                    if (!readyThisFrame[index]) allFresh = false;
                }
                openingVolleyPending = allWaiting && allFresh;
            }
        }

        private void LateUpdate()
        {
            if (!enabled || !skillManager.GlobalAutoEnabled || activeStart < 0) return;
            if (spectacularArenaField?.GetValue(spectacularCombat) == null) return;

            float[] cooldowns = ReadCooldowns();
            IList enemies = enemiesField.GetValue(arena) as IList;
            object target = FindTarget(enemies, stageFlow.Phase == StageFlowPhase.BossBattle);
            if (cooldowns == null || target == null) return;

            var queue = new List<int>(4);
            bool allFourWaiting = true;
            for (int index = activeStart; index < activeStart + 4; index++)
            {
                if (waitingForCast[index]) queue.Add(index);
                else allFourWaiting = false;
            }

            queue.Sort((left, right) => readySequence[left].CompareTo(readySequence[right]));
            bool openingVolley = !openingVolleyUsed && openingVolleyPending && allFourWaiting;
            float openingMp = ReadPlayerMp();
            float openingCost = 0f;

            for (int i = 0; i < queue.Count; i++)
            {
                int index = queue[i];
                float cost = skillManager.GetSkillMpCost(index);
                if (!openingVolley && ReadPlayerMp() + 0.001f < cost) continue;

                if (openingVolley && ReadPlayerMp() < cost)
                    playerMpField.SetValue(arena, cost);

                if (!CastQueuedSkill(index, target, cooldowns)) continue;
                waitingForCast[index] = false;
                readySequence[index] = 0L;
                openingCost += cost;
            }

            if (openingVolley)
            {
                openingVolleyUsed = true;
                openingVolleyPending = false;
                playerMpField.SetValue(arena, Mathf.Max(0f, openingMp - openingCost));
            }
        }

        private bool CastQueuedSkill(int index, object target, float[] cooldowns)
        {
            float desiredCost = skillManager.GetSkillMpCost(index);
            float legacyCost = 20f + (index % 4) * 5f;
            float mpBefore = ReadPlayerMp();
            if (mpBefore < legacyCost)
                playerMpField.SetValue(arena, legacyCost);

            float[] saved = new float[4];
            for (int offset = 0; offset < 4; offset++)
            {
                int slot = activeStart + offset;
                saved[offset] = cooldowns[slot];
                cooldowns[slot] = slot == index ? 0f : 999f;
            }

            legacyAutoMethod.Invoke(arena, new[] { target, (object)activeStart });
            bool cast = cooldowns[index] > 1f;

            for (int offset = 0; offset < 4; offset++)
            {
                int slot = activeStart + offset;
                if (slot != index) cooldowns[slot] = saved[offset];
            }

            if (!cast)
            {
                cooldowns[index] = saved[index - activeStart];
                playerMpField.SetValue(arena, mpBefore);
                return false;
            }

            correctResourceMethod.Invoke(spectacularCombat, new object[] { index, cooldowns });
            executeSkillMethod.Invoke(spectacularCombat, new object[] { index });
            playerMpField.SetValue(arena, Mathf.Max(0f, mpBefore - desiredCost));

            float[] previous = spectacularPreviousCooldownsField?.GetValue(spectacularCombat) as float[];
            if (previous != null && index < previous.Length) previous[index] = cooldowns[index];
            return true;
        }

        private float[] ReadCooldowns()
        {
            return cooldownsField?.GetValue(arena) as float[];
        }

        private float ReadPlayerMp()
        {
            return playerMpField == null || arena == null ? 0f : Convert.ToSingle(playerMpField.GetValue(arena));
        }

        private static object FindTarget(IList enemies, bool bossPhase)
        {
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                object enemy = enemies[i];
                if (enemy == null) continue;
                FieldInfo bossField = enemy.GetType().GetField("IsBoss", BindingFlags.Instance | BindingFlags.Public);
                bool isBoss = bossField != null && Convert.ToBoolean(bossField.GetValue(enemy));
                if (isBoss == bossPhase) return enemy;
            }
            return enemies.Count > 0 ? enemies[0] : null;
        }
    }
}
