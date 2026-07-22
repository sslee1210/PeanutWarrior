using UnityEngine;

namespace PeanutWarrior.Prototype
{
    /// <summary>
    /// Hides legacy prototype IMGUI controls while keeping their combat, save,
    /// progression and update logic running. The consolidated mobile UI uses
    /// explicit GUIStyles, so making the shared default skin transparent only
    /// suppresses the older debug panels.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class LegacyGuiSuppressor : MonoBehaviour
    {
        private GUISkin transparentSkin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<LegacyGuiSuppressor>() != null) return;
            GameObject root = new GameObject("PeanutWarriorLegacyGuiSuppressor");
            DontDestroyOnLoad(root);
            root.AddComponent<LegacyGuiSuppressor>();
        }

        private void Awake()
        {
            transparentSkin = Instantiate(GUI.skin);
            MakeTransparent(transparentSkin.box);
            MakeTransparent(transparentSkin.button);
            MakeTransparent(transparentSkin.label);
            MakeTransparent(transparentSkin.toggle);
            MakeTransparent(transparentSkin.textField);
            MakeTransparent(transparentSkin.textArea);
            MakeTransparent(transparentSkin.window);
            MakeTransparent(transparentSkin.horizontalSlider);
            MakeTransparent(transparentSkin.horizontalSliderThumb);
            MakeTransparent(transparentSkin.verticalSlider);
            MakeTransparent(transparentSkin.verticalSliderThumb);
            MakeTransparent(transparentSkin.horizontalScrollbar);
            MakeTransparent(transparentSkin.horizontalScrollbarThumb);
            MakeTransparent(transparentSkin.horizontalScrollbarLeftButton);
            MakeTransparent(transparentSkin.horizontalScrollbarRightButton);
            MakeTransparent(transparentSkin.verticalScrollbar);
            MakeTransparent(transparentSkin.verticalScrollbarThumb);
            MakeTransparent(transparentSkin.verticalScrollbarUpButton);
            MakeTransparent(transparentSkin.verticalScrollbarDownButton);
            MakeTransparent(transparentSkin.scrollView);
        }

        private static void MakeTransparent(GUIStyle style)
        {
            if (style == null) return;
            style.normal.textColor = Color.clear;
            style.hover.textColor = Color.clear;
            style.active.textColor = Color.clear;
            style.focused.textColor = Color.clear;
            style.onNormal.textColor = Color.clear;
            style.onHover.textColor = Color.clear;
            style.onActive.textColor = Color.clear;
            style.onFocused.textColor = Color.clear;
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.onNormal.background = null;
            style.onHover.background = null;
            style.onActive.background = null;
            style.onFocused.background = null;
        }

        private void OnGUI()
        {
            if (FindFirstObjectByType<MobileIdleUiPrototype>() == null) return;
            GUI.skin = transparentSkin;
        }
    }
}
