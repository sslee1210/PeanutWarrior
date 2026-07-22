using System.Collections;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(21000)]
    public sealed class IdleFirstRunDefaults : MonoBehaviour
    {
        private const string AutoChallengeKey = "PeanutWarrior.CoreSave.autoChallenge";
        private const string InitializedKey = "PeanutWarrior.Defaults.Initialized";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<IdleFirstRunDefaults>() != null) return;
            GameObject root = new GameObject("PeanutWarriorIdleFirstRunDefaults");
            DontDestroyOnLoad(root);
            root.AddComponent<IdleFirstRunDefaults>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            yield return null;

            if (PlayerPrefs.GetInt(InitializedKey, 0) == 1) yield break;
            StageFlowController flow = FindFirstObjectByType<StageFlowController>();
            if (flow != null && !PlayerPrefs.HasKey(AutoChallengeKey))
            {
                flow.SetAutoChallenge(true);
                PlayerPrefs.SetInt(AutoChallengeKey, 1);
            }
            PlayerPrefs.SetInt(InitializedKey, 1);
            PlayerPrefs.Save();
        }
    }
}
