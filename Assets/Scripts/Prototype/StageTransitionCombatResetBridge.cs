using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Ensures stage-map navigation resets the active hunting arena instead of carrying
    /// monsters and positions from the previous stage into the selected stage.
    /// </summary>
    [DefaultExecutionOrder(14000)]
    public sealed class StageTransitionCombatResetBridge : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private StageFlowController stageFlow;
        private CombatPrototypeArena arena;
        private MethodInfo resetForHuntingMethod;
        private int observedWorld;
        private int observedStage;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<StageTransitionCombatResetBridge>() != null) return;
            GameObject root = new GameObject("PeanutWarriorStageTransitionResetBridge");
            DontDestroyOnLoad(root);
            root.AddComponent<StageTransitionCombatResetBridge>();
        }

        private void Start()
        {
            stageFlow = FindFirstObjectByType<StageFlowController>();
            arena = FindFirstObjectByType<CombatPrototypeArena>();
            if (stageFlow == null || arena == null)
            {
                enabled = false;
                return;
            }

            resetForHuntingMethod = typeof(CombatPrototypeArena).GetMethod("ResetForHunting", PrivateInstance);
            observedWorld = stageFlow.World;
            observedStage = stageFlow.Stage;
            initialized = true;
            stageFlow.StateChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.StateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged()
        {
            if (!initialized || stageFlow == null || arena == null) return;
            bool stageChanged = observedWorld != stageFlow.World || observedStage != stageFlow.Stage;
            observedWorld = stageFlow.World;
            observedStage = stageFlow.Stage;
            if (!stageChanged || stageFlow.Phase == StageFlowPhase.BossBattle) return;
            resetForHuntingMethod?.Invoke(arena, null);
        }
    }
}
