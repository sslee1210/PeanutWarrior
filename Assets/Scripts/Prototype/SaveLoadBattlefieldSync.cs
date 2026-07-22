using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(22500)]
    public sealed class SaveLoadBattlefieldSync : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SaveLoadBattlefieldSync>() != null) return;
            GameObject root = new GameObject("PeanutWarriorSaveLoadBattlefieldSync");
            DontDestroyOnLoad(root);
            root.AddComponent<SaveLoadBattlefieldSync>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 8; i++) yield return null;

            CombatPrototypeArena arena = FindFirstObjectByType<CombatPrototypeArena>();
            StageFlowController flow = FindFirstObjectByType<StageFlowController>();
            PeanutSaveGameService save = FindFirstObjectByType<PeanutSaveGameService>();
            if (arena == null || flow == null || save == null) yield break;

            MethodInfo reset = typeof(CombatPrototypeArena).GetMethod("ResetForHunting", PrivateInstance);
            reset?.Invoke(arena, null);
            if (flow.AutoChallenge && flow.CanChallengeBoss) flow.StartBossBattle();
        }
    }
}
