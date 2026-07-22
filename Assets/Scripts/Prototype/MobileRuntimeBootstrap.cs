using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Applies mobile-oriented runtime defaults without requiring scene setup.
    /// Also exposes a lightweight safe-area overlay and performance toggle.
    /// </summary>
    public sealed class MobileRuntimeBootstrap : MonoBehaviour
    {
        private int targetFrameRate = 60;
        private bool showSafeArea;
        private bool batterySaver;
        private Rect lastSafeArea;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateBootstrap()
        {
            if (FindFirstObjectByType<MobileRuntimeBootstrap>() != null) return;
            GameObject root = new GameObject("PeanutWarriorMobileRuntimeBootstrap");
            DontDestroyOnLoad(root);
            root.AddComponent<MobileRuntimeBootstrap>();
        }

        private void Awake()
        {
            Input.multiTouchEnabled = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;
            ApplyPerformanceMode(false);
#if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
#endif
            lastSafeArea = Screen.safeArea;
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea)
                lastSafeArea = Screen.safeArea;
        }

        private void ApplyPerformanceMode(bool saver)
        {
            batterySaver = saver;
            targetFrameRate = saver ? 30 : 60;
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.antiAliasing = saver ? 0 : 2;
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        private void OnGUI()
        {
            float width = Mathf.Min(280f, Screen.width - 20f);
            Rect panel = new Rect(10f, Screen.height - 118f, width, 108f);
            GUI.Box(panel, "모바일 실행 설정");
            GUI.Label(new Rect(panel.x + 10f, panel.y + 26f, panel.width - 20f, 22f),
                $"{targetFrameRate} FPS · 절전 {(batterySaver ? "ON" : "OFF")} · 안전영역 {Mathf.RoundToInt(lastSafeArea.width)}×{Mathf.RoundToInt(lastSafeArea.height)}");

            if (GUI.Button(new Rect(panel.x + 10f, panel.y + 52f, 118f, 34f), batterySaver ? "60 FPS 전환" : "30 FPS 절전"))
                ApplyPerformanceMode(!batterySaver);
            if (GUI.Button(new Rect(panel.x + 138f, panel.y + 52f, 128f, 34f), showSafeArea ? "안전영역 숨김" : "안전영역 표시"))
                showSafeArea = !showSafeArea;

            if (showSafeArea)
            {
                Rect safe = Screen.safeArea;
                GUI.Box(new Rect(safe.x, Screen.height - safe.y - safe.height, safe.width, safe.height), "SAFE AREA");
            }
        }
    }
}
