using System;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Runs before CombatPrototypeArena. It applies the global AUTO switch, prevents casts
    /// below each skill's confirmed MP cost, and selects one tactical skill priority per frame.
    /// </summary>
    [DefaultExecutionOrder(-1200)]
    public sealed class GlobalSkillAutoGatePrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly int[] HuntingPriority = { 2, 3, 1, 0 };
        private static readonly int[] BossPriority = { 4, 6, 5, 7 };

        private CombatPrototypeArena arena;
        private StageFlowController stageFlow;
        private SkillManagementPrototype skillManager;
        private FieldInfo cooldownsField;
        private FieldInfo playerMpField;

        public bool UsesConfirmedMpCosts => true;
        public bool UsesTacticalAutoPriority => true;
        public string HuntingAutoPriority => "지맥꼬투리진 → 왕실 꼬투리 천개 → 낙화검우 → 껍질 회전참";
        public string BossAutoPriority => "갑각해방 → 낙화귀근 → 땅콩 연환검 → 황금핵 천단";

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
            Type arenaType = typeof(CombatPrototypeArena);
            cooldownsField = arenaType.GetField("skillCooldowns", PrivateInstance);
            playerMpField = arenaType.GetField("playerMp", PrivateInstance);
            if (arena == null || stageFlow == null || skillManager == null || cooldownsField == null || playerMpField == null)
                enabled = false;
        }

        private void Update()
        {
            float[] cooldowns = cooldownsField.GetValue(arena) as float[];
            if (cooldowns == null || cooldowns.Length < 8) return;

            if (!skillManager.GlobalAutoEnabled)
            {
                for (int i = 0; i < cooldowns.Length; i++) cooldowns[i] = Mathf.Max(cooldowns[i], 0.2f);
                return;
            }

            float currentMp = Convert.ToSingle(playerMpField.GetValue(arena));
            int[] priority = stageFlow.Phase == StageFlowPhase.BossBattle ? BossPriority : HuntingPriority;
            int selected = -1;

            for (int i = 0; i < priority.Length; i++)
            {
                int index = priority[i];
                bool ready = cooldowns[index] <= 0.05f;
                bool affordable = currentMp >= skillManager.GetSkillMpCost(index);
                if (ready && affordable)
                {
                    selected = index;
                    break;
                }
            }

            int start = stageFlow.Phase == StageFlowPhase.BossBattle ? 4 : 0;
            for (int index = start; index < start + 4; index++)
            {
                bool affordable = currentMp >= skillManager.GetSkillMpCost(index);
                if (!affordable || selected >= 0 && index < selected)
                    cooldowns[index] = Mathf.Max(cooldowns[index], 0.2f);
            }
        }
    }
}
