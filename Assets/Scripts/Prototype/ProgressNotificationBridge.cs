using System.Collections;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    public sealed class ProgressNotificationBridge : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private StageFlowController stageFlow;
        private FirstClearRewardPrototype firstClear;
        private PeanutMobileCanvasPrototype mobileUi;
        private MethodInfo toastMethod;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<ProgressNotificationBridge>() != null) return;
            GameObject root = new GameObject("PeanutWarriorProgressNotificationBridge");
            DontDestroyOnLoad(root);
            root.AddComponent<ProgressNotificationBridge>();
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            stageFlow = FindFirstObjectByType<StageFlowController>();
            firstClear = FindFirstObjectByType<FirstClearRewardPrototype>();
            mobileUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            toastMethod = typeof(PeanutMobileCanvasPrototype).GetMethod("Toast", PrivateInstance);
            if (stageFlow != null) stageFlow.BossDefeated += HandleBossDefeated;
        }

        private void OnDestroy()
        {
            if (stageFlow != null) stageFlow.BossDefeated -= HandleBossDefeated;
        }

        private void HandleBossDefeated()
        {
            StartCoroutine(ShowRewardNextFrame());
        }

        private IEnumerator ShowRewardNextFrame()
        {
            yield return null;
            if (firstClear == null) firstClear = FindFirstObjectByType<FirstClearRewardPrototype>();
            if (mobileUi == null) mobileUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (firstClear == null || mobileUi == null || toastMethod == null) yield break;
            toastMethod.Invoke(mobileUi, new object[] { firstClear.LastMessage });
        }
    }
}
