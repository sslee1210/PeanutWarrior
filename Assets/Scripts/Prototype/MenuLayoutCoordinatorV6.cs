using System.Collections;
using System.Reflection;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Assigns exactly one menu layout component to each page. Older layout layers are
    /// kept for the pages they still own, but they never run against the same page in
    /// the same frame, which removes the repeated destroy/rebuild flicker.
    /// </summary>
    [DefaultExecutionOrder(27000)]
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
            // Let every layout finish its own Start binding and asset creation first.
            for (int i = 0; i < 24; i++) yield return null;

            sourceUi = FindFirstObjectByType<PeanutMobileCanvasPrototype>();
            layoutV2 = FindFirstObjectByType<PeanutMenuLayoutV2>();
            layoutV3 = FindFirstObjectByType<PeanutCoreMenuCompletionV3>();
            layoutV4 = FindFirstObjectByType<PeanutMenuLayoutV4>();
            layoutV5 = FindFirstObjectByType<PeanutEquipmentAndShopMenuV5>();
            skillV6 = FindFirstObjectByType<PeanutSkillMenuV6>();
            if (sourceUi == null)
            {
                enabled = false;
                yield break;
            }

            currentPageField = typeof(PeanutMobileCanvasPrototype).GetField("currentPage", PrivateInstance);
            coordinated = currentPageField != null;
            ApplyOwnership(true);
        }

        private void LateUpdate()
        {
            if (!coordinated) return;
            ApplyOwnership(false);
        }

        private void ApplyOwnership(bool force)
        {
            string page = currentPageField.GetValue(sourceUi)?.ToString() ?? "Main";
            if (!force && page == lastPage)
            {
                // Reassert the exact owner in case an older component changed another
                // component's enabled state during its previous page cleanup.
                SetOwnersForPage(page);
                return;
            }

            lastPage = page;
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
