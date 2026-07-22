using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Ensures runtime-created uGUI buttons receive mouse and touch input even when
    /// the scene already contains an EventSystem configured for the legacy input API.
    /// </summary>
    [DefaultExecutionOrder(24000)]
    public sealed class CanvasInputSystemGuard : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<CanvasInputSystemGuard>() != null) return;
            GameObject root = new GameObject("PeanutWarriorCanvasInputSystemGuard");
            DontDestroyOnLoad(root);
            root.AddComponent<CanvasInputSystemGuard>();
        }

        private IEnumerator Start()
        {
            yield return null;
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject root = new GameObject("PeanutWarriorEventSystem");
                DontDestroyOnLoad(root);
                eventSystem = root.AddComponent<EventSystem>();
            }

            InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
                inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

            MethodInfo assignDefaults = typeof(InputSystemUIInputModule).GetMethod(
                "AssignDefaultActions",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            assignDefaults?.Invoke(inputModule, null);

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null && modules[i] != inputModule)
                    modules[i].enabled = false;
            }

            inputModule.enabled = true;
        }
    }
}
