using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Disables legacy prototype IMGUI panels after the uGUI Canvas is ready.
    /// Gameplay lifecycle methods are forwarded manually so combat, saving,
    /// missions and progression remain active without drawing debug controls.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class LegacyGuiSuppressor : MonoBehaviour
    {
        private const BindingFlags LifecycleFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private sealed class SuppressedBehaviour
        {
            public MonoBehaviour Behaviour;
            public MethodInfo UpdateMethod;
            public MethodInfo FixedUpdateMethod;
            public MethodInfo LateUpdateMethod;
            public MethodInfo PauseMethod;
            public MethodInfo FocusMethod;
            public MethodInfo QuitMethod;
        }

        private readonly List<SuppressedBehaviour> suppressed = new List<SuppressedBehaviour>();
        private float rescanTimer;
        private bool suppressionActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<LegacyGuiSuppressor>() != null) return;
            GameObject root = new GameObject("PeanutWarriorLegacyGuiSuppressor");
            DontDestroyOnLoad(root);
            root.AddComponent<LegacyGuiSuppressor>();
        }

        private void Start()
        {
            TryActivateSuppression();
        }

        private void Update()
        {
            if (!suppressionActive)
            {
                TryActivateSuppression();
                return;
            }

            InvokeLifecycle("Update", entry => entry.UpdateMethod);
            rescanTimer -= Time.unscaledDeltaTime;
            if (rescanTimer <= 0f)
            {
                rescanTimer = 1f;
                CaptureLegacyGuiComponents();
            }
        }

        private void FixedUpdate()
        {
            if (suppressionActive) InvokeLifecycle("FixedUpdate", entry => entry.FixedUpdateMethod);
        }

        private void LateUpdate()
        {
            if (suppressionActive) InvokeLifecycle("LateUpdate", entry => entry.LateUpdateMethod);
        }

        private void TryActivateSuppression()
        {
            PeanutMobileCanvasPrototype canvasUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            if (canvasUi == null || !canvasUi.enabled) return;
            suppressionActive = true;
            CaptureLegacyGuiComponents();
        }

        private void CaptureLegacyGuiComponents()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || ShouldRemainEnabled(behaviour)) continue;
                if (!behaviour.enabled || IsAlreadySuppressed(behaviour)) continue;

                Type type = behaviour.GetType();
                if (type.Namespace != typeof(LegacyGuiSuppressor).Namespace) continue;
                MethodInfo onGui = type.GetMethod("OnGUI", LifecycleFlags, null, Type.EmptyTypes, null);
                if (onGui == null) continue;

                suppressed.Add(new SuppressedBehaviour
                {
                    Behaviour = behaviour,
                    UpdateMethod = FindNoArgMethod(type, "Update"),
                    FixedUpdateMethod = FindNoArgMethod(type, "FixedUpdate"),
                    LateUpdateMethod = FindNoArgMethod(type, "LateUpdate"),
                    PauseMethod = type.GetMethod("OnApplicationPause", LifecycleFlags, null, new[] { typeof(bool) }, null),
                    FocusMethod = type.GetMethod("OnApplicationFocus", LifecycleFlags, null, new[] { typeof(bool) }, null),
                    QuitMethod = FindNoArgMethod(type, "OnApplicationQuit")
                });
                behaviour.enabled = false;
            }
        }

        private bool ShouldRemainEnabled(MonoBehaviour behaviour)
        {
            return behaviour == this ||
                   behaviour is PeanutMobileCanvasPrototype ||
                   behaviour is RuntimeWorldViewPrototype;
        }

        private bool IsAlreadySuppressed(MonoBehaviour behaviour)
        {
            for (int i = 0; i < suppressed.Count; i++)
                if (suppressed[i].Behaviour == behaviour) return true;
            return false;
        }

        private static MethodInfo FindNoArgMethod(Type type, string name)
        {
            return type.GetMethod(name, LifecycleFlags, null, Type.EmptyTypes, null);
        }

        private void InvokeLifecycle(string lifecycle, Func<SuppressedBehaviour, MethodInfo> selector)
        {
            for (int i = suppressed.Count - 1; i >= 0; i--)
            {
                SuppressedBehaviour entry = suppressed[i];
                if (entry.Behaviour == null)
                {
                    suppressed.RemoveAt(i);
                    continue;
                }
                MethodInfo method = selector(entry);
                if (method != null) InvokeSafely(entry.Behaviour, method, null, lifecycle);
            }
        }

        private static void InvokeSafely(MonoBehaviour target, MethodInfo method, object[] arguments, string lifecycle)
        {
            try
            {
                method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                Debug.LogException(exception.InnerException ?? exception, target);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PeanutWarrior] Legacy {lifecycle} forwarding failed: {exception.Message}", target);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            ForwardBoolean(paused, entry => entry.PauseMethod, "OnApplicationPause");
        }

        private void OnApplicationFocus(bool focused)
        {
            ForwardBoolean(focused, entry => entry.FocusMethod, "OnApplicationFocus");
        }

        private void ForwardBoolean(bool value, Func<SuppressedBehaviour, MethodInfo> selector, string lifecycle)
        {
            object[] arguments = { value };
            for (int i = 0; i < suppressed.Count; i++)
            {
                SuppressedBehaviour entry = suppressed[i];
                if (entry.Behaviour == null) continue;
                MethodInfo method = selector(entry);
                if (method != null) InvokeSafely(entry.Behaviour, method, arguments, lifecycle);
            }
        }

        private void OnApplicationQuit()
        {
            for (int i = 0; i < suppressed.Count; i++)
            {
                SuppressedBehaviour entry = suppressed[i];
                if (entry.Behaviour == null || entry.QuitMethod == null) continue;
                InvokeSafely(entry.Behaviour, entry.QuitMethod, null, "OnApplicationQuit");
            }
        }
    }
}
