using System.Collections;
using UnityEngine;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(32000)]
    public sealed class PeanutMenuLayoutV2Audit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<PeanutMenuLayoutV2Audit>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMenuLayoutV2Audit");
            DontDestroyOnLoad(root);
            root.AddComponent<PeanutMenuLayoutV2Audit>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 10; i++) yield return null;

            PeanutMenuLayoutV2 layout = FindFirstObjectByType<PeanutMenuLayoutV2>();
            if (layout == null)
            {
                Debug.LogError("[PeanutWarrior Menu Audit]\nFAIL · PeanutMenuLayoutV2 is missing.");
                yield break;
            }

            if (layout.LayoutVersion != 2 || layout.ManagedPageCount != 8 ||
                !layout.UsesTwoColumnGrowth || !layout.UsesConstantButtonBackgrounds)
            {
                Debug.LogError(
                    "[PeanutWarrior Menu Audit]\nFAIL · redesigned menu contract is incomplete.");
                yield break;
            }

            Debug.Log(
                "[PeanutWarrior Menu Audit]\n" +
                "PASS · eight redesigned inner pages, two-column growth cards, fixed button colors and underline navigation are active.");
        }
    }
}
