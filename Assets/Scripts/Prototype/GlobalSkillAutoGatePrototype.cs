using System;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Runs before CombatPrototypeArena and prevents every automatic skill from firing
    /// when the single global AUTO switch is disabled.
    /// </summary>
    [DefaultExecutionOrder(-1200)]
    public sealed class GlobalSkillAutoGatePrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private CombatPrototypeArena arena;
        private SkillManagementPrototype skillManager;
        private FieldInfo cooldownsField;

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
            skillManager = FindFirstObjectByType<SkillManagementPrototype>();
            cooldownsField = typeof(CombatPrototypeArena).GetField("skillCooldowns", PrivateInstance);
            if (arena == null || skillManager == null || cooldownsField == null) enabled = false;
        }

        private void Update()
        {
            if (skillManager.GlobalAutoEnabled) return;
            float[] cooldowns = cooldownsField.GetValue(arena) as float[];
            if (cooldowns == null) return;

            for (int i = 0; i < cooldowns.Length; i++)
                cooldowns[i] = Mathf.Max(cooldowns[i], 0.2f);
        }
    }
}
