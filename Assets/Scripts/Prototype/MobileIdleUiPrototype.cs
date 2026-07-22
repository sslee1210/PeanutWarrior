using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Compatibility marker retained only because the runtime world view checks for this
    /// type before showing its old developer toggle. All legacy IMGUI menus were removed;
    /// PeanutMobileCanvasPrototype is the only active game UI.
    /// </summary>
    public sealed class MobileIdleUiPrototype : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateMarker()
        {
            if (FindFirstObjectByType<MobileIdleUiPrototype>() != null) return;
            GameObject marker = new GameObject("PeanutWarriorLegacyUiCompatibilityMarker");
            DontDestroyOnLoad(marker);
            marker.AddComponent<MobileIdleUiPrototype>();
        }
    }
}
