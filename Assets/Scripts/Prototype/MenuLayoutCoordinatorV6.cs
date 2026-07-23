using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Assigns exactly one menu renderer to each page before the layered menu scripts
    /// get a chance to rebuild the same content. This is the single authority for
    /// enabling V2/V3/V4/V5/V6 menu components.
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
        private bool coordinated;
        private string lastPage = string.Empty;

        public bool UsesSingleOwnerPerPage => true;
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
            // RuntimeInitializeOnLoadMethod has already created the layout objects.
            // Bind before their later execution-order Start methods can build pages.
            for (int i = 0; i < 12; i++)
            {
                BindObjects();
                if (sourceUi != null && currentPageField != null) break;
                yield return null;
            }

            if (sourceUi == null || currentPageField == null)
            {
                enabled = false;
                yield break;
            }

            coordinated = true;
            ApplyOwnership(true);
        }

        private void BindObjects()
        {
            sourceUi = sourceUi != null ? sourceUi : FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            layoutV2 = layoutV2 != null ? layoutV2 : FindFirstObjectByType<PeanutMenuLayoutV2>();
            layoutV3 = layoutV3 != null ? layoutV3 : FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            layoutV4 = layoutV4 != null ? layoutV4 : FindFirstObjectByType<PeanutMenuLayoutV4>();
            layoutV5 = layoutV5 != null ? layoutV5 : FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>();
            skillV6 = skillV6 != null ? skillV6 : FindFirstObjectByType<PeanutSkillMenuV6>();
            if (sourceUi != null && currentPageField == null)
                currentPageField = typeof(PeanutMobileCanvasPrototype).GetField("currentPage", PrivateInstance);
        }

        private void Update()
        {
            if (!coordinated)
            {
                BindObjects();
                if (sourceUi == null || currentPageField == null) return;
                coordinated = true;
            }
            ApplyOwnership(false);
        }

        private void LateUpdate()
        {
            if (!coordinated) return;
            // Reassert after all menu LateUpdate methods. No older layout may re-enable
            // another renderer during the same frame.
            ApplyOwnership(false);
        }

        private void ApplyOwnership(bool force)
        {
            string page = currentPageField.GetValue(sourceUi)?.ToString() ?? "Main";
            if (force || page != lastPage) lastPage = page;
            SetOwnersForPage(page);
        }

        private void SetOwnersForPage(string page)
        {
            bool useV2 = page == "StageSelect";
            bool useV3 = page == "Pets" || page == "Settings";
            bool useV4 = page == "Growth" || page == "Advancement";
            bool useV5 = page == "Equipment" || page == "Shop";
            bool useSkillV6 = page == "Skills";

            SetEnabled(layoutV2, useV2);
            SetEnabled(layoutV3, useV3);
            SetEnabled(layoutV4, useV4);
            SetEnabled(layoutV5, useV5);
            SetEnabled(skillV6, useSkillV6);

            CurrentOwner = useSkillV6 ? "SkillV6" :
                useV5 ? "EquipmentShopV5" :
                useV4 ? "MenuV4" :
                useV3 ? "CoreV3" :
                useV2 ? "MenuV2" : "None";
        }

        private static void SetEnabled(Behaviour behaviour, bool value)
        {
            if (behaviour != null && behaviour.enabled != value) behaviour.enabled = value;
        }
    }
}
