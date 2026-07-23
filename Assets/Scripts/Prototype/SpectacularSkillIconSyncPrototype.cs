using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace PeanutWarrior.Prototype
{
    [DefaultExecutionOrder(36500)]
    public sealed class SpectacularSkillIconSyncPrototype : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

        public bool SynchronizesMenuAndBattleIcons => true;
        public bool SynchronizesSkillColors => true;
        public bool WaitsForBuilderAssetInitialization => true;
        public int SynchronizedIconCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (FindFirstObjectByType<SpectacularSkillIconSyncPrototype>() != null) return;
            GameObject go = new GameObject("PeanutWarriorSpectacularSkillIconSync");
            DontDestroyOnLoad(go);
            go.AddComponent<SpectacularSkillIconSyncPrototype>();
        }

        private IEnumerator Start()
        {
            FieldInfo menuFontField = typeof(PeanutSkillMenuV6).GetField("font", PrivateInstance);
            FieldInfo dockRoundedSpriteField = typeof(BattleSkillDockV6).GetField("roundedSprite", PrivateInstance);

            for (int frame = 0; frame < 75; frame++)
            {
                PeanutSkillMenuV6 menu = FindFirstObjectByType<PeanutSkillMenuV6>();
                BattleSkillDockV6 dock = FindFirstObjectByType<BattleSkillDockV6>();
                bool menuReady = menu != null && menuFontField?.GetValue(menu) != null;
                bool dockReady = dock != null && dockRoundedSpriteField?.GetValue(dock) != null;
                if (menuReady && dockReady)
                {
                    Apply(menu, dock);
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("[PeanutWarrior] 새 스킬 문양 동기화 초기화 실패");
            enabled = false;
        }

        private void Apply(PeanutSkillMenuV6 menu, BattleSkillDockV6 dock)
        {
            Sprite[] shared = new Sprite[8];
            Color[] sharedColors = new Color[8];
            for (int i = 0; i < shared.Length; i++)
            {
                shared[i] = SkillIconFactoryV6.Create(i);
                sharedColors[i] = SkillIconFactoryV6.ColorFor(i);
            }

            FieldInfo menuSpritesField = typeof(PeanutSkillMenuV6).GetField("skillSprites", PrivateInstance);
            Sprite[] menuSprites = menuSpritesField?.GetValue(menu) as Sprite[];
            if (menuSprites != null)
            {
                for (int i = 0; i < menuSprites.Length && i < shared.Length; i++) menuSprites[i] = shared[i];
            }

            FieldInfo menuColorsField = typeof(PeanutSkillMenuV6).GetField("SkillColors", PrivateStatic);
            Color[] menuColors = menuColorsField?.GetValue(null) as Color[];
            if (menuColors != null)
            {
                for (int i = 0; i < menuColors.Length && i < sharedColors.Length; i++) menuColors[i] = sharedColors[i];
            }

            FieldInfo dockIconsField = typeof(BattleSkillDockV6).GetField("icons", PrivateInstance);
            Sprite[] dockIcons = dockIconsField?.GetValue(dock) as Sprite[];
            if (dockIcons != null)
            {
                for (int i = 0; i < dockIcons.Length && i < shared.Length; i++) dockIcons[i] = shared[i];
            }

            FieldInfo menuRootField = typeof(PeanutSkillMenuV6).GetField("root", PrivateInstance);
            GameObject menuRoot = menuRootField?.GetValue(menu) as GameObject;
            if (menuRoot != null)
            {
                Image[] images = menuRoot.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < images.Length; i++)
                {
                    Image image = images[i];
                    if (image == null || !image.gameObject.name.StartsWith("Skill Icon ")) continue;
                    string suffix = image.gameObject.name.Substring("Skill Icon ".Length);
                    if (!int.TryParse(suffix, out int index) || index < 0 || index >= shared.Length) continue;
                    image.sprite = shared[index];
                    image.color = sharedColors[index];
                }
            }

            SynchronizedIconCount = shared.Length;
        }
    }
}
