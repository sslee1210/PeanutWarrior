using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PeanutWarrior.Core;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Sole authority for menu rendering. V2-V6 are used only as page builder
    /// libraries: their Update/LateUpdate loops stay disabled permanently so they can
    /// never delete and recreate the same Content hierarchy on alternating frames.
    /// </summary>
    [DefaultExecutionOrder(24000)]
    public sealed class MenuLayoutCoordinatorV6 : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private PeanutMobileCanvasPrototype sourceUi;
        private PeanutMenuLayoutV2 layoutV2;
        private PeanutCoreMenuCompletionV3 layoutV3;
        private PeanutMenuLayoutV4 layoutV4;
        private PeanutEquipmentAndShopMenuV5 layoutV5;
        private PeanutSkillMenuV6 skillV6;

        private FieldInfo currentPageField;
        private FieldInfo contentHostField;
        private GameObject contentHost;
        private string renderedPage = string.Empty;
        private string expectedRootName = string.Empty;
        private object activeRenderer;
        private FieldInfo activeRefreshersField;
        private float refreshTimer;
        private bool ready;

        private readonly HashSet<MonoBehaviour> initialized = new HashSet<MonoBehaviour>();

        public bool UsesSingleOwnerPerPage => true;
        public bool KeepsLegacyLayoutLoopsDisabled => true;
        public string CurrentOwner { get; private set; } = "None";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<MenuLayoutCoordinatorV6>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMenuLayoutCoordinatorV6");
            DontDestroyOnLoad(root);
            root.AddComponent<MenuLayoutCoordinatorV6>();
        }

        private IEnumerator Start()
        {
            BindObjects();
            DisableAllLayoutLoops();

            for (int frame = 0; frame < 30; frame++)
            {
                BindObjects();
                DisableAllLayoutLoops();
                if (TryBindSource() && InitializeAllBuilders())
                {
                    ready = true;
                    RenderIfRequired(true);
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[PeanutWarrior] 메뉴 단일 렌더러 초기화 실패");
            enabled = false;
        }

        private void Update()
        {
            if (!ready)
            {
                BindObjects();
                DisableAllLayoutLoops();
                ready = TryBindSource() && InitializeAllBuilders();
                if (!ready) return;
            }

            DisableAllLayoutLoops();
            RenderIfRequired(false);
            RefreshActivePage();
        }

        private void LateUpdate()
        {
            if (!ready) return;
            DisableAllLayoutLoops();

            // A source button can rebuild its legacy page during the same Update.
            // Checking again in LateUpdate guarantees the final hierarchy rendered by
            // Unity belongs to exactly one builder.
            RenderIfRequired(false);
        }

        private void BindObjects()
        {
            sourceUi = sourceUi != null ? sourceUi : FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            layoutV2 = layoutV2 != null ? layoutV2 : FindFirstObjectByType<PeanutMenuLayoutV2>();
            layoutV3 = layoutV3 != null ? layoutV3 : FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            layoutV4 = layoutV4 != null ? layoutV4 : FindFirstObjectByType<PeanutMenuLayoutV4>();
            layoutV5 = layoutV5 != null ? layoutV5 : FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>();
            skillV6 = skillV6 != null ? skillV6 : FindFirstObjectByType<PeanutSkillMenuV6>();
        }

        private bool TryBindSource()
        {
            if (sourceUi == null) return false;
            Type sourceType = typeof(PeanutMobileCanvasPrototype);
            currentPageField ??= sourceType.GetField("currentPage", PrivateInstance);
            contentHostField ??= sourceType.GetField("contentHost", PrivateInstance);
            contentHost = contentHostField?.GetValue(sourceUi) as GameObject;
            return currentPageField != null && contentHost != null;
        }

        private bool InitializeAllBuilders()
        {
            bool v2 = InitializeBuilder(layoutV2, "TryBind", "CreateAssets");
            bool v3 = InitializeBuilder(layoutV3, "Bind", "CreateAssets");
            bool v4 = InitializeBuilder(layoutV4, "Bind", "CreateAssets");
            bool v5 = InitializeBuilder(layoutV5, "Bind", "CreateAssets");
            bool v6 = InitializeBuilder(skillV6, "Bind", "CreateAssets");

            if (v2)
            {
                StageFlowController flow = FindFirstObjectByType<StageFlowController>();
                FieldInfo stageMapWorld = typeof(PeanutMenuLayoutV2).GetField("stageMapWorld", PrivateInstance);
                if (flow != null && stageMapWorld != null) stageMapWorld.SetValue(layoutV2, flow.World);
            }
            return v2 && v3 && v4 && v5 && v6;
        }

        private bool InitializeBuilder(MonoBehaviour builder, string bindMethodName, string assetMethodName)
        {
            if (builder == null) return false;
            builder.StopAllCoroutines();
            builder.enabled = false;
            if (initialized.Contains(builder)) return true;

            Type type = builder.GetType();
            MethodInfo bind = type.GetMethod(bindMethodName, PrivateInstance);
            MethodInfo assets = type.GetMethod(assetMethodName, PrivateInstance);
            if (bind == null || assets == null) return false;

            object result = bind.Invoke(builder, null);
            if (result is bool success && !success) return false;
            assets.Invoke(builder, null);
            initialized.Add(builder);
            return true;
        }

        private void DisableAllLayoutLoops()
        {
            Disable(layoutV2);
            Disable(layoutV3);
            Disable(layoutV4);
            Disable(layoutV5);
            Disable(skillV6);
        }

        private static void Disable(MonoBehaviour layout)
        {
            if (layout == null) return;
            if (layout.enabled) layout.enabled = false;
        }

        private void RenderIfRequired(bool force)
        {
            if (sourceUi == null || contentHost == null) return;
            string page = CurrentPage;
            ResolveBuilder(page, out object renderer, out string rootName, out string buildMethod, out object[] arguments, out string owner);

            if (renderer == null)
            {
                renderedPage = page;
                expectedRootName = string.Empty;
                activeRenderer = null;
                activeRefreshersField = null;
                CurrentOwner = "None";
                return;
            }

            bool correctRoot = HasOnlyExpectedRoot(rootName);
            if (!force && page == renderedPage && correctRoot && ReferenceEquals(activeRenderer, renderer)) return;

            InvokeBuilder(renderer, buildMethod, arguments);
            SetActivePage(renderer, page);
            renderedPage = page;
            expectedRootName = rootName;
            activeRenderer = renderer;
            activeRefreshersField = renderer.GetType().GetField("refreshers", PrivateInstance);
            CurrentOwner = owner;
            refreshTimer = 0f;
        }

        private string CurrentPage => currentPageField?.GetValue(sourceUi)?.ToString() ?? "Main";

        private bool HasOnlyExpectedRoot(string rootName)
        {
            if (contentHost == null || string.IsNullOrEmpty(rootName)) return false;
            int activeChildren = 0;
            bool found = false;
            for (int i = 0; i < contentHost.transform.childCount; i++)
            {
                GameObject child = contentHost.transform.GetChild(i).gameObject;
                if (!child.activeSelf) continue;
                activeChildren++;
                if (child.name == rootName) found = true;
            }
            return found && activeChildren == 1;
        }

        private void ResolveBuilder(string page, out object renderer, out string rootName,
            out string buildMethod, out object[] arguments, out string owner)
        {
            renderer = null;
            rootName = string.Empty;
            buildMethod = string.Empty;
            arguments = null;
            owner = "None";

            switch (page)
            {
                case "StageSelect":
                    renderer = layoutV2;
                    rootName = "Peanut Menu Layout V2";
                    buildMethod = "RebuildPage";
                    arguments = new object[] { page };
                    owner = "MenuV2";
                    break;
                case "Pets":
                case "Settings":
                    renderer = layoutV3;
                    rootName = "Peanut Core Menu V3";
                    buildMethod = "Rebuild";
                    arguments = new object[] { page };
                    owner = "CoreV3";
                    break;
                case "Growth":
                case "Advancement":
                    renderer = layoutV4;
                    rootName = "Peanut Menu Layout V4";
                    buildMethod = "BuildPage";
                    arguments = new object[] { page };
                    owner = "MenuV4";
                    break;
                case "Equipment":
                case "Shop":
                    renderer = layoutV5;
                    rootName = "Peanut Equipment Shop V5";
                    buildMethod = "BuildPage";
                    arguments = new object[] { page };
                    owner = "EquipmentShopV5";
                    break;
                case "Skills":
                    renderer = skillV6;
                    rootName = "Peanut Skill Menu V6";
                    buildMethod = "BuildPage";
                    arguments = Array.Empty<object>();
                    owner = "SkillV6";
                    break;
            }
        }

        private static void InvokeBuilder(object renderer, string methodName, object[] arguments)
        {
            MethodInfo method = renderer.GetType().GetMethod(methodName, PrivateInstance);
            if (method == null)
                throw new MissingMethodException(renderer.GetType().Name, methodName);
            method.Invoke(renderer, arguments);
        }

        private static void SetActivePage(object renderer, string page)
        {
            FieldInfo activePage = renderer.GetType().GetField("activePage", PrivateInstance);
            activePage?.SetValue(renderer, page);
        }

        private void RefreshActivePage()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f || activeRenderer == null || activeRefreshersField == null) return;
            refreshTimer = 0.12f;

            IList refreshers = activeRefreshersField.GetValue(activeRenderer) as IList;
            if (refreshers == null) return;
            for (int i = 0; i < refreshers.Count; i++)
            {
                if (refreshers[i] is not Action action) continue;
                try { action.Invoke(); }
                catch (MissingReferenceException) { }
                catch (TargetInvocationException exception)
                {
                    Debug.LogException(exception.InnerException ?? exception, this);
                }
            }
        }
    }
}
